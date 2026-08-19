
partial class JBeam
{
    public string FilePath { get; set; } = string.Empty;
    public ObjectNode Root { get; set; } = new();
    public List<Part> Parts { get; set; } = new();

    public JBeam(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public delegate void LoadFileEvent(JBeam jBeam);
    public static event LoadFileEvent? OnLoadFile;

    public bool Load()
    {
        if (!File.Exists(FilePath))
        {
            Warn($"File not found: {FilePath}");
            return false;
        }

        try
        {
            Root = JBeam.Parse(FileContent);
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
    public string FileContent => File.ReadAllText(FilePath);
    public bool Sanity => Root.PropertiesNamed.Count == Parts.Count;

    public static List<JBeam> LoadPath(string path)
    {
       List<JBeam> jbeams = new();

        path = Path.GetFullPath(path);
        if (File.Exists(path))
        {
            var jbeam = new JBeam(path);
            if (jbeam.Load()) jbeams.Add(jbeam);
        }
        else if (Directory.Exists(path))
        {
            foreach (var file in EnumerateFiles(path))
            {
                var jbeam = new JBeam(file);
                if (jbeam.Load()) jbeams.Add(jbeam);
            }
        }
        else
        {
            throw new Exception($"Path not found: {path}");
        }
        return jbeams;
    }
    public static IEnumerable<string> EnumerateFiles(string directory, SearchOption searchOption = SearchOption.AllDirectories)
    {
        return Directory.EnumerateFiles(directory, "*.jbeam", searchOption);
    }

    private readonly List<string> _warnings = new();
    public IReadOnlyList<string> GetWarnings() => _warnings;
    public void ClearWarnings() => _warnings.Clear();
    protected void Warn(string message) => _warnings.Add(message);

}
