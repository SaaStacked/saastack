using Microsoft.CodeAnalysis;

namespace Tools.Analyzers.NonFramework.Extensions;

public static class ResourceExtensions
{
    public static LocalizableResourceString GetLocalizableString(this string name)
    {
        return new LocalizableResourceString(name, Resources.ResourceManager, typeof(Resources));
    }
}