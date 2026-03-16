namespace Application.Resources.Shared;

public class ApiHostHealth
{
    public required string Name { get; set; }

    public string Status { get; set; } = "OK";
}

public class ApiStatistics
{
    public required string ApiVersion { get; set; }

    public required string BaseUrl { get; set; }

    public MethodGroupStatistics Methods { get; set; } = new();

    public required string Name { get; set; }

    public int Total { get; set; }
}

public class MethodGroupStatistics
{
    public MethodGroupEndpointStatistics Deletes { get; set; } = new();

    public MethodGroupEndpointStatistics Gets { get; set; } = new();

    public MethodGroupEndpointStatistics Patches { get; set; } = new();

    public MethodGroupEndpointStatistics Posts { get; set; } = new();

    public MethodGroupEndpointStatistics Puts { get; set; } = new();

    public int Total { get; set; }
}

public class MethodGroupEndpointStatistics
{
    public List<EndpointStatistic>? Endpoints { get; set; }

    public int Total { get; set; }
}

public class EndpointStatistic
{
    public string Description { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = [];
}