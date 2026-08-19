
using System.Dynamic;

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
            Parts = Part.BuildParts(FilePath, Root).ToList();

            // Sanity check: ensure all top-level keys are objects (or comments/empty lines)
            foreach (var kv in Root.Properties)
            {
                if (kv.Value is ObjectNode && kv.Key is not KeyNamed)
                    Warn($"Top-level key '{kv.Key}' is {kv.Key.GetType().Name} and will be ignored.");
                if (kv.Value is not CommentNode && kv.Value is not EmptyLineNode && kv.Value is not ObjectNode)
                    Warn($"Top-level key '{kv.Key}' is not an object and will be ignored.");
            }

            // Sanity check: ensure the number of top-level keys matches the number of detected parts
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
    public static IEnumerable<string> EnumerateFiles(string directory, SearchOption searchOption = SearchOption.AllDirectories)
    {
        return Directory.EnumerateFiles(directory, "*.jbeam", searchOption);
    }


    public ObjectNode Parse() => ParseText(FileContent);
    public static ObjectNode ParseText(string jbeamText)
    {
        var lexer = new JBeamLexer(jbeamText);
        var parser = new JBeamParser(lexer.LexAll());
        ObjectNode root = parser.ParseFile();
        return root;
    }

}

public partial class JBeamFile
{
    public class ContentLines : List<string>
    {
        public ContentLines(IEnumerable<string> lines) : base(lines) { }

        public int? GetLine(TextLocation location)
            => (location.Line < 1 || location.Line > Count) ? null : location.Line - 1; // Convert to 0-based index
        public (int Index, int Count)? GetLines(TextRegion region)
        {
            int? startIndex = GetLine(region.Start);
            int? endIndex = GetLine(region.End);
            if (startIndex == null || endIndex == null || startIndex > endIndex)
                return null;
            return (startIndex.Value, endIndex.Value - startIndex.Value + 1);
        }

        public int GetIndent(int index, int size = 2)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range for lines count {Count}.");

            var line = this[index];
            int indent = 0;
            foreach (var ch in line)
            {
                if (ch == ' ' || ch == '\t')
                    indent++;
                else
                    break;
            }

            if (size > 0)
                indent = indent / size;

            return indent;
        }
        public int GetIndent(TextLocation location, int size = 2)
        {
            var lineIndex = GetLine(location);
            if (lineIndex == null)
                throw new ArgumentException($"Invalid location: {location}");
            return GetIndent(lineIndex.Value, size);
        }

        public void Set(ContentLines newLines, TextRegion region)
        {
            var lineRange = GetLines(region);
            if (lineRange == null)
                throw new ArgumentException($"Invalid region: {region}");

            this.RemoveRange(lineRange.Value.Index, lineRange.Value.Count);
            this.InsertRange(lineRange.Value.Index, newLines);
        }

        public static ContentLines FromString(string? content)
        {
            if (content == null) return new ContentLines(Array.Empty<string>());
            var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            return new ContentLines(lines);
        }
        public override string ToString() => ToString("\r\n");
        public string ToString(string lineEndCharacter) => string.Join(lineEndCharacter, this.Where(l => l != null));
    }

    public string ReadFileContent() => File.ReadAllText(FilePath);
    public ContentLines GetLines() => ContentLines.FromString(FileContent);
    public string SetLines(ContentLines lines) => FileContent = lines.ToString(GetLineEndCharacter());
    public string GetLineEndCharacter() => FileContent.Contains("\r\n") ? "\r\n" : FileContent.Contains("\r") ? "\r" : "\n";
}
