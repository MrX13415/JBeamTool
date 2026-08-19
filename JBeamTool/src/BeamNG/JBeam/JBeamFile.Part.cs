
namespace BeamNG.JBeam;

public partial class JBeamFile
{
    public class Part
    {
        public string? FilePath { get; }
        public NodeEntry Node { get; }
        public List<Section> Sections { get; set; }

        public Part(string filePath, NodeEntry node)
        {
            if (node.Value is not ObjectNode)
                throw new ArgumentException("Node value must be an ObjectNode.", nameof(node));
                
            FilePath = filePath;
            Node = node;
            Sections = Section.BuildSections(Object()).ToList();
        }

        public ObjectNode Object() => Node.As<ObjectNode>()!;

        public string Identifier => Node.Name ?? "";
        public InformationSection? Information => GetSection(SectionType.Information) as InformationSection;
        public SlotTypeSection? SlotType => GetSection(SectionType.SlotType) as SlotTypeSection;
        public SlotsSection? Slots => GetSection(SectionType.Slots) as SlotsSection 
                                   ?? GetSection(SectionType.Slots2) as SlotsSection;
        public VariablesSection? Variables => GetSection(SectionType.Variables) as VariablesSection;
        public FlexbodiesSection? Flexbodies => GetSection(SectionType.Flexbodies) as FlexbodiesSection;

        public Section? GetSection(SectionType type) => Sections.FirstOrDefault(s => s.Type == type);

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
    
        public static IEnumerable<Part> BuildParts(string filePath, ObjectNode? root)
        {
            if (root == null) yield break;
            foreach (var kv in root.Properties)
            {
                if (kv.Value is ObjectNode obj && kv.Key is KeyNamed key)
                    yield return new Part(filePath, new NodeEntry(key, obj));
            }
        }
    }

}