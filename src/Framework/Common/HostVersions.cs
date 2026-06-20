namespace Common;

/// <summary>
///     Defines the runtime version details for a host
/// </summary>
public class HostVersions
{
    public required HostDeploymentVersion ProductVersion { get; set; }

    public required string RuntimeVersion { get; set; }
}

/// <summary>
///     Defines the deployment version detailed for a host
/// </summary>
public class HostDeploymentVersion
{
    public DateTime? Deployed { get; set; }

    public required string Hash { get; set; }

    public required string Version { get; set; }
}