using Microsoft.Win32;
using System.Text.RegularExpressions;

// TODO: Windows only, add Linux support.
public static class Steam
{
    public static string? GetRootFolder()
    {
        // 1. Registry (best)
        using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
        {
            var reg = key?.GetValue("SteamPath") as string;
            if (!string.IsNullOrEmpty(reg) && Directory.Exists(reg))
                return reg;
        }

        // 2. Default Program Files
        string pf = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam"
        );
        if (Directory.Exists(pf))
            return pf;

        // 3. Common fallback locations
        string[] fallback =
        {
            @"C:\Steam",
            @"D:\Steam",
            @"E:\Steam"
        };

        return fallback.FirstOrDefault(Directory.Exists);
    }

    public static IEnumerable<string> GetSteamLibraries()
    {
        string? root = GetRootFolder();
        if (string.IsNullOrEmpty(root))
            yield break;

        string steam = Path.Combine(root, "steamapps", "libraryfolders.vdf");

        if (!File.Exists(steam))
            yield break;

        foreach (Match m in Regex.Matches(File.ReadAllText(steam), "\"path\"\\s+\"([^\"]+)\""))
            yield return m.Groups[1].Value.Replace("\\\\", "\\");
    }

    public static App? Find(string appId)
    {
        foreach (var lib in GetSteamLibraries())
        {
            string manifest = Path.Combine(lib, "steamapps", App.ManifestFile(appId));
            string content = File.Exists(manifest) ? File.ReadAllText(manifest) : "";
            if (string.IsNullOrEmpty(content))
                continue;

            return new App(appId, lib, content);
        }

        return null;
    }

    public class App
    {
        public string Id { get; }
        public string LibraryPath { get; }
        public string ManifestContent { get; }

        public App(string id, string libraryPath, string manifestContent)
        {
            Id = id;
            LibraryPath = libraryPath;
            ManifestContent = manifestContent;
        } 

        public string Name => Regex.Match(ManifestContent, "\"name\"\\s+\"([^\"]+)\"").Groups[1].Value;
        public string? InstallDir 
        {
            get
            {
                var match = Regex.Match(ManifestContent, "\"installdir\"\\s+\"([^\"]+)\"");
                if (match.Success)
                {
                    string installDir = match.Groups[1].Value;
                    string path = Path.Combine(LibraryPath, "steamapps", "common", installDir);
                    if (Directory.Exists(path))
                        return path;
                }
                return null;
            }
        }

        public static string ManifestFile(string id) => $"appmanifest_{id}.acf";
    }

    public abstract class AppId
    {
        public const string BeamNG = "284160";
    }
}
