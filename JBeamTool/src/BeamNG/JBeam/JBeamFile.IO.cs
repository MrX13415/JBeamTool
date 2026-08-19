
namespace BeamNG.JBeam;

public partial class JBeamFile
{
    public delegate void LoadFileEvent(JBeamFile jBeamFile);
    public static event LoadFileEvent? OnLoadFile;

    public bool LoadFile()
    {
        if (!File.Exists(FilePath))
        {
            Warn($"File not found: {FilePath}");
            return false;
        }

        try
        {
            FileContent = ReadFileContent();
            Root = Parse();
            Parts.Clear();

            foreach (var kv in Root.Properties)
            {
                // Ignore comments
                if (kv.Value is CommentNode) continue;

                if (kv.Value is ObjectNode obj && kv.Key is KeyNamed key)
                    Parts.Add(new Part(new NodeEntry(key, obj)));
                else if (kv.Value is ObjectNode)
                    Warn($"Top-level key '{kv.Key}' is {kv.Key.GetType().Name} and will be ignored.");
                else if (kv.Value is not EmptyLineNode)
                    Warn($"Top-level key '{kv.Key}' is not an object and will be ignored.");
            }

            // Sanity check: print some info about the parsed structure
            // root.Properties.Count == parts.Count -> perfect
            // root.Properties.Count > parts.Count -> some top-level keys weren’t objects
            // root.Properties.Count < parts.Count -> impossible (means parser bug)
            if (!Sanity)
            {                
                Warn($"Top-level keys count ({Root.PropertiesNamed.Count}) does not match detected parts count ({Parts.Count}).");
                if (Root.PropertiesNamed.Count > Parts.Count)
                    Warn($"Some top-level keys were not objects and thus not counted as parts.");
                if (Root.PropertiesNamed.Count < Parts.Count)
                    Warn($"More parts detected than top-level keys. This should not happen. (Parser bug?)");
            }

            OnLoadFile?.Invoke(this);
            return true;
        }
        catch (Exception ex)
        {
            Warn($"Unable to load JBeam file: {ex.Message} {ex.StackTrace}");
            OnLoadFile?.Invoke(this);
            return false;
        }
    }
    public static List<JBeamFile> LoadFiles(string path)
    {
       List<JBeamFile> jbeams = new();

        path = Path.GetFullPath(path);
        if (File.Exists(path))
        {
            var jbeam = new JBeamFile(path);
            if (jbeam.LoadFile()) jbeams.Add(jbeam);
        }
        else if (Directory.Exists(path))
        {
            foreach (var file in EnumerateFiles(path))
            {
                var jbeam = new JBeamFile(file);
                if (jbeam.LoadFile()) jbeams.Add(jbeam);
            }
        }
        else
        {
            throw new Exception($"Path not found: {path}");
        }
        return jbeams;
    }
    public string ReadFileContent() => File.ReadAllText(FilePath);


    public ObjectNode Parse() => ParseText(FileContent);
    public static ObjectNode ParseText(string jbeamText)
    {
        var lexer = new JBeamLexer(jbeamText);
        var parser = new JBeamParser(lexer.LexAll());
        ObjectNode root = parser.ParseFile();
        return root;
    }


    public static IEnumerable<string> EnumerateFiles(string directory, SearchOption searchOption = SearchOption.AllDirectories)
    {
        return Directory.EnumerateFiles(directory, "*.jbeam", searchOption);
    }
}
