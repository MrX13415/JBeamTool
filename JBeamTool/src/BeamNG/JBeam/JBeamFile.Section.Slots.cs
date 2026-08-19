
namespace BeamNG.JBeam;

public partial class JBeamFile
{
    public record class Slot
    {
        public required string Name;
        public required List<string> AllowTypes;
        public required List<string> DenyTypes;
        public required string Default;
        public required string Description;

        public static SectionType GetType(ArrayNode node)
        {
            if (node.Items.Count < 2) return SectionType.Unknown;
            var typeNode = node[1];
            return typeNode switch
            {
                ArrayNode => SectionType.Slots2,
                ValueNode => SectionType.Slots,
                _ => SectionType.Unknown
            };
        }
        public static Slot New(ArrayNode node)
        {
            var type = GetType(node);
            if (type == SectionType.Unknown)
                throw new ArgumentException("ArrayNode does not represent a valid slot.", nameof(node));

            // Legacy format: [name, type, default, description]
            if (type == SectionType.Slots)
            {
                var fieldType = node[1]?.ValueStr;
                return new Slot
                {
                    Name        = node[0]?.ValueStr ?? "",
                    AllowTypes  = fieldType != null ? new List<string> { fieldType } : new List<string>(),
                    DenyTypes   = new List<string>(),
                    Default     = node[2]?.ValueStr ?? "",
                    Description = node[3]?.ValueStr ?? ""
                };                
            }

            // New format: [name, allowTypes[], denyTypes[], default, description]
            var fieldAllow = node[1]?.NodeArray?.Items.Select(i => i.ValueStr ?? "").ToList();
            var fieldDeny = node[2]?.NodeArray?.Items.Select(i => i.ValueStr ?? "").ToList();
            return new Slot
            {                
                Name = node[0]?.ValueStr ?? "",
                AllowTypes = fieldAllow ?? new List<string>(),    
                DenyTypes = fieldDeny ?? new List<string>(),
                Default = node[3]?.ValueStr ?? "",
                Description = node[4]?.ValueStr ?? ""
            };
        }
    }
    public class SlotsSection : Section
    {
        public List<Slot> Slots { get; set; }

        public SlotsSection(NodeEntry node) : base(node)
        {
            if (Type != SectionType.Slots && Type != SectionType.Slots2)
                throw new ArgumentException("Section is not a slots section.", nameof(node));

            Slots = BuildSlots();
        }
        public ArrayNode? ContentNode => Node.As<ArrayNode>();

        public List<Slot> BuildSlots()
        {
            List<Slot> slots = new();
            if (ContentNode == null) return slots;

            foreach (var item in ContentNode.Items.Skip(1)) // Skip the header row
            {
                if (item is ArrayNode arrayNode)
                {
                    var slot = Slot.New(arrayNode);
                    slots.Add(slot);
                }
            }
            return slots;
        }

        public IEnumerable<TranslationKey> GetTranslationKeys()
        {
            foreach (var slot in Slots)
            {
                if (!string.IsNullOrEmpty(slot.Description))
                {
                    var key = TranslationKey.New(TranslationKey.SlotDescription, slot.Description);
                    yield return key;
                }
            }
        }

    }
}