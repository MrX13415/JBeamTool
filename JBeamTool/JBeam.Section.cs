
using System.Runtime.CompilerServices;

partial class JBeam
{
    public enum SectionType
    {
        Unknown,
        Information,
        Slots,
        Slots2,
        Variables,
        Flexbodies,
    }

    public class Section
    {
        public KeyedNode Node { get; set; }
        public Section(KeyedNode node)
        {
            Node = node;
        }

        public string Key => Node.Key;
        public SectionType Type => GetType(Key);
        public static SectionType GetType(string key) 
            => Enum.TryParse(key, true, out SectionType result) ? result : SectionType.Unknown;

        public static Section New(KeyedNode node) => GetType(node.Key) switch
        {
            SectionType.Information => new InformationSection(node),
            SectionType.Slots => new SlotsSection(node),
            SectionType.Slots2 => new SlotsSection(node),
            SectionType.Variables => new VariablesSection(node),
            SectionType.Flexbodies => new FlexbodiesSection(node),
            _ => new Section(node)
        };

    }
    
}