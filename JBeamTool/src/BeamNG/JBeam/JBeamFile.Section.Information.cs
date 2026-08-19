
namespace BeamNG.JBeam;

public partial class JBeamFile
{
    public class InformationSection : Section
    {
        public InformationSection(NodeEntry node) : base(node)
        {
            if (Type != SectionType.Information)
                throw new ArgumentException("Section is not an information section.", nameof(node));
        }

        public ObjectNode? Content => Node.As<ObjectNode>();
        public Node? NameNode => Content?.Get("name");
        
        public string Name => ProcessName();
        private string ProcessName()
        {
            if (NameNode is ValueNode valueNode)
                return valueNode.ValueStr ?? "";
            if (NameNode is ObjectNode objectNode)
            {
                var txt = objectNode.Get("txt")?.ValueStr ?? "<unknown>";
                return $"{{{{ {txt} | translate }}}}";
            }
            return "";
        }

        public IEnumerable<TranslationKey> GetTranslationKeys()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                var key = TranslationKey.New(TranslationKey.InformationName, Name);
                yield return key;
            }
        }
    }
}