
using System.Runtime.CompilerServices;

partial class JBeam
{
    public class Part
    {
        public KeyedNode Node { get; set; }
        public List<Section> Sections { get; set; }

        public Part(KeyedNode node)
        {
            if (node.Value is not ObjectNode)
                throw new ArgumentException("Node value must be an ObjectNode.", nameof(node));
                
            Node = node;
            Sections = BuildSections();
        }

        public string? Name => Node.Key;
        public ObjectNode Object() => Node.As<ObjectNode>()!;

        public List<Section> BuildSections()
        {
            List<Section> sections = new();
            foreach (var kv in Object().Properties)
            {
                var section = Section.New(new KeyedNode(kv.Key, kv.Value));
                sections.Add(section);
            }
            return sections;
        }

        public Section? GetSection(SectionType type) 
        => Sections.FirstOrDefault(s => s.Type == type);
        public InformationSection? Information => GetSection(SectionType.Information) as InformationSection;
        public SlotsSection? Slots => GetSection(SectionType.Slots) as SlotsSection 
                                   ?? GetSection(SectionType.Slots2) as SlotsSection;
        public VariablesSection? Variables => GetSection(SectionType.Variables) as VariablesSection;

        public IEnumerable<TranslationKey> GetTranslationKeys()
        {
            if (Information != null)
                foreach (var key in Information.GetTranslationKeys())
                    yield return key;
            
            if (Slots != null)
                foreach (var key in Slots.GetTranslationKeys())
                    yield return key;

            if (Variables != null)
                foreach (var key in Variables.GetTranslationKeys())
                    yield return key;
        }
    }
}