using System.Reflection;

namespace Common.Extensions;

public static class RuntimeExtensions
{
    private const string
        DeployedDateMsBuildPropertyName = "DeployedDate"; //from the AssemblyMetadataAttribute of the assembly

    /// <summary>
    ///     Returns the versions found in the current running assembly
    ///     Notes:
    ///     - The <see cref="AssemblyInformationalVersionAttribute" /> and <see cref="AssemblyMetadataAttribute" />
    ///     are assumed to be populated at runtime
    /// </summary>
    public static HostVersions GetHostVersions(this Assembly assembly)
    {
        var assemblyInformational = assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion;

        var productVersion = "unknown";
        var productHash = "unknown";

        if (assemblyInformational.HasValue())
        {
            productVersion = assemblyInformational;

            // Windows build pipelines add a hash to the version, Linux build pipelines do not
            if (productVersion.Contains('+'))
            {
                var split = productVersion.Split('+');
                productVersion = split[0];
                productHash = split[1];
            }
        }

        var buildDate = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(amd => amd.Key == DeployedDateMsBuildPropertyName)?.Value;
        var productCreated = buildDate.HasValue()
            ? buildDate.FromIso8601()
            : (DateTime?)null;

        var runtimeVersion = Environment.Version.ToString();

        return new HostVersions
        {
            RuntimeVersion = runtimeVersion,
            ProductVersion = new HostDeploymentVersion
            {
                Version = productVersion,
                Hash = productHash,
                Deployed = productCreated
            }
        };
    }
}