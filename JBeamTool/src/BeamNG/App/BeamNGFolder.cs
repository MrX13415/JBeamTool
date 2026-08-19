
namespace BeamNG.App;

public abstract class BeamNGFolder
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

    public static string User 
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeamNG", "BeamNG.drive", "current");
    public static string Mods 
        => Path.Combine(User, "mods");
    public static string UnpackedMods 
        => Path.Combine(Mods, "unpacked");
    
    public static string Translations
        => Path.Combine(Root, TranslationsFolderName);
    public static string TranslationsFolderName
        => Path.Combine("locales", "translations");
}