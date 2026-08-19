
namespace BeamNG.JBeam;

public partial class JBeamFile
{
    public enum SectionType
    {
        Unknown,
        Information,
        SlotType,
        Slots,
        Slots2,
        Variables,
        Flexbodies,
    }

    public class Section
    {
        public NodeEntry Node { get; set; }
        public Section(NodeEntry node)
        {
            Node = node;
        }

        public string Identifier => Node.Name;
        public SectionType Type => GetType(Identifier);
        public static SectionType GetType(string key) 
            => Enum.TryParse(key, true, out SectionType result) ? result : SectionType.Unknown;

        public TextRegion Region => Node.Region;
            
        public static Section New(NodeEntry node) => GetType(node.Name) switch
        {
            SectionType.Information => new InformationSection(node),
            SectionType.SlotType => new SlotTypeSection(node),
            SectionType.Slots => new SlotsSection(node),
            SectionType.Slots2 => new SlotsSection(node),
            SectionType.Variables => new VariablesSection(node),
            SectionType.Flexbodies => new FlexbodiesSection(node),
            _ => new Section(node)
        };

        public static IEnumerable<Section> BuildSections(ObjectNode? obj)
        {
            if (obj == null) yield break;
            foreach (var kv in obj.Properties)
            {
                if (kv.Key is KeyNamed key)
                {
                    var section = New(new NodeEntry(key, kv.Value));
                    yield return section;
                }
            }
        }
    }
    
}