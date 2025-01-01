using Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.Worker.Api.IntegrationTests;

public interface IApiWorkerSpec
{
    IMessageBusStore MessageBusStore { get; }

    IQueueStore QueueStore { get; }

    public TService GetRequiredService<TService>()
        where TService : notnull;

    void OverrideTestingDependencies(Action<IServiceCollection> overrideDependencies);

    void StartHost();

    void WaitForQueueProcessingToComplete();
}

public abstract class ApiWorkerSpec<TSetup> : IClassFixture<TSetup>, IDisposable
    where TSetup : class, IApiWorkerSpec
{
    protected readonly IApiWorkerSpec Setup;

    protected ApiWorkerSpec(TSetup setup, Action<IServiceCollection>? overrideDependencies = null)
    {
        if (overrideDependencies.Exists())
        {
            setup.OverrideTestingDependencies(overrideDependencies);
        }

        setup.StartHost();
        Setup = setup;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (Setup is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}