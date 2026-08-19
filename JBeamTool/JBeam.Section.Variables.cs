
partial class JBeam
{
    
    public record class Variable
    {
        public required string Name;
        public required string Type;
        public required string Unit;
        public required string Category;
        public required string Default;
        public required string Min;
        public required string Max;
        public required string Title;
        public required string Description;

        public static Variable New(ArrayNode node)
        {
            // Format: ["name", "type", "unit", "category", "default", "min", "max", "title", "description"],
            if (node is not ArrayNode arrayNode)
                throw new ArgumentException("Node value must be an ArrayNode.", nameof(node));

            return new Variable
            {
                Name        = node[0]?.ValueStr ?? "",
                Type        = node[1]?.ValueStr ?? "",
                Unit        = node[2]?.ValueStr ?? "",
                Category    = node[3]?.ValueStr ?? "",
                Default     = node[4]?.ValueStr ?? "",
                Min         = node[5]?.ValueStr ?? "",
                Max         = node[6]?.ValueStr ?? "",
                Title       = node[7]?.ValueStr ?? "",
                Description = node[8]?.ValueStr ?? ""
            };
        }
    }
    public class VariablesSection : Section
    {
        public List<Variable> Variables { get; set; } = new();

        public VariablesSection(NodeEntry node) : base(node)
        {
            if (Type != SectionType.Variables)
                throw new ArgumentException("Section is not a variables section.", nameof(node));

            Variables = BuildVariables();
        }

        public ArrayNode? ContentNode => Node.As<ArrayNode>();

        public List<Variable> BuildVariables()
        {
            List<Variable> variables = new();
            if (ContentNode == null) return variables;

            foreach (var item in ContentNode.Items.Skip(1)) // Skip the header row
            {
                if (item is ArrayNode arrayNode)
                {
                    var variable = Variable.New(arrayNode);
                    variables.Add(variable);
                }
            }
            return variables;
        }

        public IEnumerable<TranslationKey> GetTranslationKeys()
        {
            foreach (var variable in Variables)
            {
                if (!string.IsNullOrEmpty(variable.Title    ))
                    yield return TranslationKey.New(TranslationKey.VariableTitle, variable.Title);
                if (!string.IsNullOrEmpty(variable.Category))
                    yield return TranslationKey.New(TranslationKey.VariableCategory, variable.Category);
                if (!string.IsNullOrEmpty(variable.Description))
                    yield return TranslationKey.New(TranslationKey.VariableDescription, variable.Description);
            }
        }
    }
}