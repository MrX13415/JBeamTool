
using System.Runtime.CompilerServices;

partial class JBeam
{
    public class InformationSection : Section
    {
        public InformationSection(KeyedNode node) : base(node)
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
            // TODO: Handle ObjectNode as name (e.g., for "name" = { "some_key" }).
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