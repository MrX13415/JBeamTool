
partial class JBeam
{
    ObjectNode Root { get; set; } = new();

    public List<Part> Parts { get; set; } = new();

    public void LoadFile(string fileContent)
    {
        Root = JBeam.Parse(fileContent);
        Parts.Clear();

        foreach (var kv in Root.Properties)
        {
            if (kv.Value is ObjectNode obj)
            {
                Parts.Add(new Part(new KeyedNode(kv.Key, obj)));
            }
        }

        // Sanity check: print some info about the parsed structure
        // root.Properties.Count == parts.Count -> perfect
        // root.Properties.Count > parts.Count -> some top-level keys weren’t objects
        // root.Properties.Count < parts.Count -> impossible (means parser bug)
        bool sanity = Root.Properties.Count == Parts.Count;
        if (!sanity)
        {
            Console.WriteLine($"Warning: Top-level keys count ({Root.Properties.Count}) does not match detected parts count ({Parts.Count}).");
            if (Root.Properties.Count > Parts.Count)
                Console.WriteLine($"Some top-level keys were not objects and thus not counted as parts.");
            if (Root.Properties.Count < Parts.Count)
                Console.WriteLine($"Error: More parts detected than top-level keys. This should not happen. (Parser bug?)");
        }
    }

    public IEnumerable<TranslationKey> GetTranslationKeys()
    {
        foreach (var part in Parts)
            foreach (var key in part.GetTranslationKeys())
                yield return key;
    }
}
