using System.Reflection;

namespace JBeamTool;

public static class AppInfo
{
    public static string Name => "JBeamTool";
    public static string Version => GetAssemblyVersion();
    public static string Description => "BeamNG JBeam Validator and Localization Manager";
    public static string FullTitle => $"{Name} v{Version} - {Description}";

    private static string GetAssemblyVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "0.0.0";
        return version;
    }
}
