
namespace BeamNG.JBeam;

public partial class JBeamFile
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
        public List<Variable> Items { get; set; }

        public VariablesSection(NodeEntry node) : base(node)
        {
            if (Type != SectionType.Variables)
                throw new ArgumentException("Section is not a variables section.", nameof(node));

            Items = BuildVariables().ToList();
        }

        public ArrayNode? ContentNode => Node.As<ArrayNode>();

        public IEnumerable<Variable> BuildVariables()
        {
            if (ContentNode == null) yield break;
    
            foreach (var item in ContentNode.Items.Skip(1)) // Skip the header row
            {
                if (item is ArrayNode arrayNode)
                {
                    var variable = Variable.New(arrayNode);
                    yield return variable;
                }
            }
        }

        public IEnumerable<TranslationKey> GetTranslationKeys()
        {
            foreach (var variable in Items)
            {
                if (!string.IsNullOrEmpty(variable.Title) && !TranslationKey.IsDynamicTranslation(variable.Title))
                    yield return TranslationKey.New(TranslationKey.VariableTitle, variable.Title);
                if (!string.IsNullOrEmpty(variable.Category) && !TranslationKey.IsDynamicTranslation(variable.Category))
                    yield return TranslationKey.New(TranslationKey.VariableCategory, variable.Category);
                if (!string.IsNullOrEmpty(variable.Description) && !TranslationKey.IsDynamicTranslation(variable.Description))
                    yield return TranslationKey.New(TranslationKey.VariableDescription, variable.Description);
            }
        }
    }
}