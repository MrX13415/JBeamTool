using BeamNG.JBeam;

namespace JBeamTool;

public static class JBeamLoader
{
    public static void InitializeLoader(string sourceDir)
    {
        JBeamFile.OnLoadFile += (jbeam) =>
        {
            var relativePath = Path.GetRelativePath(sourceDir, jbeam.FilePath);
            Console.WriteLine($"   {relativePath} [{jbeam.Parts.Count} parts, {jbeam.GetTranslationKeys().Count()} translation keys]");
            foreach (var warning in jbeam.GetWarnings())
                Console.WriteLine($"      Warning: {warning}");
        };
    }
}
