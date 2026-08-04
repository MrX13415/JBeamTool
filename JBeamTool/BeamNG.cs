
public abstract class BeamNG
{
    public static string Root
    {
        get
        {
            var app = Steam.Find(Steam.AppId.BeamNG);
            if (app?.InstallDir != null)
                return app.InstallDir;
            throw new Exception("BeamNG.drive installation folder not found.");
        }
    }

    public static string UserFolder 
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeamNG", "BeamNG.drive", "current");
    public static string ModsFolder 
        => Path.Combine(UserFolder, "mods");
    public static string UnpackedModsFolder 
        => Path.Combine(ModsFolder, "unpacked");
    
    public static string TranslationsFolder
        => Path.Combine(Root, TranslationsRelativePath);
    public static string TranslationsRelativePath
        => Path.Combine("locales", "translations");
}