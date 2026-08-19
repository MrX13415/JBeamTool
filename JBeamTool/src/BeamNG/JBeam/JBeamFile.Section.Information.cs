
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
        
        public IEnumerable<string> Authors => GetAuthors();
        public string Name => GetName();
        public string Value => Content?.Get("value")?.ValueStr ?? "";

        private IEnumerable<string> GetAuthors()
        {
            var authorNode = Content?.Get("authors");
            if (authorNode is ValueNode valueNode) yield return valueNode.ValueStr ?? "";
            if (authorNode is ArrayNode arrayNode) 
                foreach (var author in arrayNode.AsString().ToList())
                    yield return author;
        }
        private string GetName()
        {
            if (NameNode is ValueNode valueNode) return valueNode.ValueStr ?? "";
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