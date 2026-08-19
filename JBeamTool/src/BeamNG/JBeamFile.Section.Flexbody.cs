
namespace BeamNG.JBeam;

public partial class JBeamFile
{
    public record class Flexbody
    {
        public required string Mesh;
        public List<string> Groups { get; set; } = new();
        /// Legacy property
        public List<string> NonFlexMaterials { get; set; } = new();

        public JsonNode? Json { get; set; } = null;

        public static Flexbody New(ArrayNode node)
        {
            if (node is not ArrayNode arrayNode)
                throw new ArgumentException("Node value must be an ArrayNode.", nameof(node));

            var flexbody = new Flexbody{
                Mesh = arrayNode[0]?.ValueStr ?? ""
            };

            if (arrayNode[1] is ArrayNode groupsNode)
                flexbody.Groups = groupsNode.AsStringList();

            if (arrayNode[2] is ArrayNode nonFlexMaterialsNode)
                flexbody.NonFlexMaterials = nonFlexMaterialsNode.AsStringList();

            if (arrayNode[3] is JsonNode jsonNode)
                flexbody.Json = jsonNode;

            return flexbody;
        }
    }

    public class FlexbodiesSection : Section
    {
        public List<Flexbody> Flexbodies { get; set; } = new();

        public FlexbodiesSection(NodeEntry node) : base(node)
        {
            if (Type != SectionType.Flexbodies)
                throw new ArgumentException("Section is not a flexbodies section.", nameof(node));

            Flexbodies = BuildFlexbodies();
        }

        public ArrayNode? ContentNode => Node.As<ArrayNode>();

        public List<Flexbody> BuildFlexbodies()
        {
            List<Flexbody> flexbodies = new();
            if (ContentNode == null) return flexbodies;

            foreach (var item in ContentNode.Items.Skip(1)) // Skip the header row
            {
                if (item is ArrayNode arrayNode)
                {
                    var flexbody = Flexbody.New(arrayNode);
                    flexbodies.Add(flexbody);
                }
            }
            return flexbodies;
        }

    }
}