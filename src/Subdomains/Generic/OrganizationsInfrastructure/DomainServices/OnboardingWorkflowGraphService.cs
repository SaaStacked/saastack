using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using OrganizationsApplication.ApplicationServices;
using OrganizationsDomain;
using OrganizationsDomain.DomainServices;

namespace OrganizationsInfrastructure.DomainServices;

/// <summary>
///     Provides a service for validating, manipulating, navigating a Directed Acyclic Graph (DAG) of steps that define an
///     onboarding workflow. <see href="https://www.geeksforgeeks.org/dsa/introduction-to-directed-acyclic-graph/" />
/// </summary>
public class OnboardingWorkflowGraphService : IOnboardingWorkflowService
{
    private readonly ICustomOnboardingWorkflowService _customWorkflowService;

    public OnboardingWorkflowGraphService(ICustomOnboardingWorkflowService customWorkflowService)
    {
        _customWorkflowService = customWorkflowService;
    }

    public Journey CalculateShortestPath(IReadOnlyDictionary<string, StepSchema> steps, string startStepId,
        string endStepId)
    {
        return CalculateShortestPathToEnd(steps, startStepId, endStepId);
    }

    public Journey CalculateShortestPathToEnd(IReadOnlyDictionary<string, StepSchema> steps,
        string fromStepId,
        string endStepId)
    {
        if (steps.Count == 0)
        {
            return Journey.Empty;
        }

        // Build adjacency list: stepId -> list of (nextStepId, weight)
        var graph = BuildGraph(steps);

        // Use BFS with distance tracking to find the shortest path
        var (_, predecessors) = FindShortestPaths(graph, fromStepId);

        // Reconstruct the path from fromStepId to endStepId
        var pathStepIds = ReconstructPath(predecessors, fromStepId, endStepId);

        // Convert step IDs to Step objects
        var path = new List<Step>();
        foreach (var stepId in pathStepIds)
        {
            if (steps.TryGetValue(stepId, out var stepSchema))
            {
                var step = stepSchema.ToStep().Value;
                path.Add(step);
            }
        }

        return Journey.Create(path).Value;
    }

    public Result<WorkflowSchema, Error> FindWorkflow(Identifier organizationId)
    {
        var retrieved = _customWorkflowService.FindWorkflowAsync(organizationId, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        return retrieved.Value;
    }

    public Result<Error> ValidateWorkflow(IReadOnlyDictionary<string, StepSchema> steps, string startStepId,
        string endStepId)
    {
        // Validate single start and end nodes
        var startNodes = steps.Values.Where(s => s.Type == OnboardingStepType.Start).ToList();
        if (startNodes.Count > 1)
        {
            return Error.Validation(
                Resources.OnboardingWorkflowGraphService_Validate_MultipleStartNodes.Format(string.Join(", ",
                    startNodes.Select(s => s.Id))));
        }

        var endNodes = steps.Values.Where(s => s.Type == OnboardingStepType.End).ToList();
        if (endNodes.Count > 1)
        {
            return Error.Validation(
                Resources.OnboardingWorkflowGraphService_Validate_MultipleEndNodes.Format(string.Join(", ",
                    endNodes.Select(s => s.Id))));
        }

        // Validate end step has no outgoing edges
        var endStep = steps[endStepId];
        if (endStep.NextStepId.HasValue || endStep.Branches.Items.Count > 0)
        {
            return Error.Validation(
                Resources.OnboardingWorkflowGraphService_Validate_EndStepHasOutgoingEdges.Format(endStepId));
        }

        // Validate all step references
        foreach (var step in steps.Values)
        {
            // Each step that references a next step, must reference a defined step
            if (step.NextStepId.HasValue && !steps.ContainsKey(step.NextStepId.Value))
            {
                return Error.Validation(
                    Resources.OnboardingWorkflowGraphService_Validate_AnyStepNextStepUndefined.Format(step.Id,
                        step.NextStepId.Value));
            }

            // Each step in each branch, that references a next step, must reference a defined step
            foreach (var branch in step.Branches.Items)
            {
                if (!steps.ContainsKey(branch.NextStepId))
                {
                    return Error.Validation(
                        Resources.OnboardingWorkflowGraphService_Validate_AnyBranchNextStepUndefined.Format(step.Id,
                            branch.Id,
                            branch.NextStepId));
                }
            }
        }

        // Validate start step has no incoming edges
        var incomingEdges = GetIncomingEdges(steps);
        if (incomingEdges.ContainsKey(startStepId) && incomingEdges[startStepId].Count > 0)
        {
            return Error.Validation(
                Resources.OnboardingWorkflowGraphService_Validate_StartStepHasIncomingEdges.Format(startStepId));
        }

        // Validate no cycles (DAG property)
        var cycleDetection = DetectCycle(steps, startStepId);
        if (cycleDetection.stepId != null)
        {
            return Error.Validation(
                Resources.OnboardingWorkflowGraphService_Validate_CycleDetected.Format(cycleDetection.stepId));
        }

        // Validate all steps are reachable from start
        var reachableFromStart = GetReachableSteps(steps, startStepId);
        foreach (var step in steps.Values)
        {
            if (!reachableFromStart.Contains(step.Id))
            {
                return Error.Validation(Resources.OnboardingWorkflowGraphService_Validate_StepNotReachableFromStart
                    .Format(step.Id));
            }
        }

        // Validate all steps (except end) can reach end
        var canReachEnd = GetStepsThatCanReachEnd(steps, endStepId);
        foreach (var step in steps.Values)
        {
            if (step.Id != endStepId && !canReachEnd.Contains(step.Id))
            {
                return Error.Validation(
                    Resources.OnboardingWorkflowGraphService_Validate_StepCannotReachEnd.Format(step.Id));
            }
        }

        // Validate end step has weight of 0
        if (endStep.Weight != 0)
        {
            return Error.Validation(
                Resources.OnboardingWorkflowGraphService_Validate_EndStepMustHaveZeroWeight.Format(endStepId,
                    endStep.Weight));
        }

        // Validate linear workflows (no branches) have total weight of 100
        var hasBranches = steps.Values.Any(s => s.Type == OnboardingStepType.Branch);
        if (!hasBranches)
        {
            var totalWeight = steps.Values.Sum(s => s.Weight);
            if (totalWeight != 100)
            {
                return Error.Validation(Resources.OnboardingWorkflowGraphService_Validate_LinearWorkflowWeightMustBe100
                    .Format(totalWeight));
            }
        }

        return Result.Ok;
    }

    /// <summary>
    ///     Builds an adjacency list graph from the workflow steps
    /// </summary>
    private static Dictionary<string, List<(string nextStepId, int weight)>> BuildGraph(
        IReadOnlyDictionary<string, StepSchema> steps)
    {
        var graph = new Dictionary<string, List<(string nextStepId, int weight)>>();
        foreach (var kvp in steps)
        {
            var stepId = kvp.Key;
            var stepSchema = kvp.Value;
            graph[stepId] = [];

            if (stepSchema.Type == OnboardingStepType.Branch)
            {
                // For branch steps, add all branch options as edges
                foreach (var branch in stepSchema.Branches.Items)
                {
                    graph[stepId].Add((branch.NextStepId, stepSchema.Weight));
                }
            }
            else if (stepSchema.NextStepId.HasValue)
            {
                // For normal/start steps, add the single next step
                graph[stepId].Add((stepSchema.NextStepId.Value, stepSchema.Weight));
            }
            // End steps have no outgoing edges
        }

        return graph;
    }

    /// <summary>
    ///     Uses BFS with distance tracking to find the shortest paths from a start step
    /// </summary>
    // ReSharper disable once UnusedTupleComponentInReturnValue
    private static (Dictionary<string, int> distances, Dictionary<string, string> predecessors) FindShortestPaths(
        Dictionary<string, List<(string nextStepId, int weight)>> graph, string startStepId)
    {
        var distances = new Dictionary<string, int>();
        var predecessors = new Dictionary<string, string>();
        var queue = new Queue<string>();
        var visited = new HashSet<string>();

        distances[startStepId] = 0;
        queue.Enqueue(startStepId);

        while (queue.Count > 0)
        {
            var currentStepId = queue.Dequeue();

            // Skip if we've already processed this node
            // ReSharper disable once CanSimplifySetAddingWithSingleCall
            if (visited.Contains(currentStepId))
            {
                continue;
            }

            visited.Add(currentStepId);
            var currentDistance = distances[currentStepId];

            if (!graph.TryGetValue(currentStepId, out var neighbors))
            {
                continue;
            }

            foreach (var (nextStepId, weight) in neighbors)
            {
                var newDistance = currentDistance + weight;

                // If we haven't visited this step yet, or we found a shorter path
                if (!distances.ContainsKey(nextStepId) || newDistance < distances[nextStepId])
                {
                    distances[nextStepId] = newDistance;
                    predecessors[nextStepId] = currentStepId;
                    queue.Enqueue(nextStepId);
                }
            }
        }

        return (distances, predecessors);
    }

    /// <summary>
    ///     Reconstructs the path from start to end using predecessors
    /// </summary>
    private static List<string> ReconstructPath(Dictionary<string, string> predecessors, string startStepId,
        string endStepId)
    {
        var pathStepIds = new List<string>();
        var currentId = endStepId;

        while (currentId.Exists())
        {
            pathStepIds.Add(currentId);
            if (currentId.EqualsIgnoreCase(startStepId))
            {
                break;
            }

            if (!predecessors.TryGetValue(currentId, out var predecessor))
            {
                break;
            }

            currentId = predecessor;
        }

        pathStepIds.Reverse();

        // Remove the fromStepId from the beginning if this is a "path ahead" calculation
        // (when called from JourneySchema.GetBestPathAhead)
        // This is handled by the caller, so we return the full path here

        return pathStepIds;
    }

    /// <summary>
    ///     Gets all outgoing edges (next steps) from a given step
    /// </summary>
    private static List<string> GetOutgoingEdges(StepSchema step)
    {
        var edges = new List<string>();

        if (step.NextStepId.HasValue)
        {
            edges.Add(step.NextStepId.Value);
        }

        foreach (var branch in step.Branches.Items)
        {
            edges.Add(branch.NextStepId);
        }

        return edges;
    }

    /// <summary>
    ///     Builds a dictionary of incoming edges for all steps
    /// </summary>
    private static Dictionary<string, List<string>> GetIncomingEdges(IReadOnlyDictionary<string, StepSchema> steps)
    {
        var incomingEdges = new Dictionary<string, List<string>>();

        foreach (var step in steps.Values)
        {
            foreach (var nextStepId in GetOutgoingEdges(step))
            {
                if (!incomingEdges.ContainsKey(nextStepId))
                {
                    incomingEdges[nextStepId] = [];
                }

                incomingEdges[nextStepId].Add(step.Id);
            }
        }

        return incomingEdges;
    }

    /// <summary>
    ///     Performs BFS to find all steps reachable from the start step
    /// </summary>
    private static HashSet<string> GetReachableSteps(IReadOnlyDictionary<string, StepSchema> steps, string startStepId)
    {
        var reachable = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(startStepId);
        reachable.Add(startStepId);

        while (queue.Count > 0)
        {
            var currentStepId = queue.Dequeue();
            if (!steps.TryGetValue(currentStepId, out var currentStep))
            {
                continue;
            }

            foreach (var nextStepId in GetOutgoingEdges(currentStep))
            {
                if (!reachable.Contains(nextStepId))
                {
                    reachable.Add(nextStepId);
                    queue.Enqueue(nextStepId);
                }
            }
        }

        return reachable;
    }

    /// <summary>
    ///     Performs reverse BFS to find all steps that can reach the end step
    /// </summary>
    private static HashSet<string> GetStepsThatCanReachEnd(IReadOnlyDictionary<string, StepSchema> steps,
        string endStepId)
    {
        var canReachEnd = new HashSet<string>();
        var queue = new Queue<string>();
        var incomingEdges = GetIncomingEdges(steps);

        queue.Enqueue(endStepId);
        canReachEnd.Add(endStepId);

        while (queue.Count > 0)
        {
            var currentStepId = queue.Dequeue();

            if (incomingEdges.TryGetValue(currentStepId, out var predecessors))
            {
                foreach (var predecessorId in predecessors)
                {
                    if (!canReachEnd.Contains(predecessorId))
                    {
                        canReachEnd.Add(predecessorId);
                        queue.Enqueue(predecessorId);
                    }
                }
            }
        }

        return canReachEnd;
    }

    /// <summary>
    ///     Detects cycles using DFS with recursion stack tracking
    ///     Returns the step ID where a cycle was detected, or null if no cycle exists
    /// </summary>
    private static (string? stepId, bool hasCycle) DetectCycle(IReadOnlyDictionary<string, StepSchema> steps,
        string startStepId)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        bool DfsVisit(string stepId)
        {
            if (recursionStack.Contains(stepId))
            {
                return true; // Cycle detected
            }

            if (visited.Contains(stepId))
            {
                return false; // Already processed
            }

            visited.Add(stepId);
            recursionStack.Add(stepId);

            if (steps.TryGetValue(stepId, out var step))
            {
                foreach (var nextStepId in GetOutgoingEdges(step))
                {
                    if (DfsVisit(nextStepId))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(stepId);
            return false;
        }

        // Start DFS from the start step
        if (DfsVisit(startStepId))
        {
            // Find which step is in the cycle
            foreach (var stepId in recursionStack)
            {
                return (stepId, true);
            }

            return (startStepId, true);
        }

        return (null, false);
    }
}