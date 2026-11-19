using BeamNG.JBeam;

namespace JBeamTool;

public static class AstDescriber
{
    public static object? DescribeNode(Node node)
    {
        if (node is ValueNode valueNode)
            return new { type = nameof(ValueNode), value = valueNode.Value };

        if (node is ArrayNode arrayNode)
            return new { type = nameof(ArrayNode), count = arrayNode.Items.Count, items = arrayNode.Items.Select(DescribeNode).ToList() };

        if (node is ObjectNode objectNode)
        {
            return new
            {
                type = nameof(ObjectNode),
                propertyCount = objectNode.Properties.Count,
                properties = objectNode.Properties.Select(entry => new
                {
                    key = entry.Name,
                    keyType = entry.Key.GetType().Name,
                    valueType = entry.Value.GetType().Name,
                    value = DescribeNode(entry.Value)
                }).ToList()
            };
        }

        if (node is CommentNode commentNode)
            return new { type = nameof(CommentNode), isBlock = commentNode.IsBlock, comment = commentNode.Comment };

        if (node is EmptyLineNode emptyLineNode)
            return new { type = nameof(EmptyLineNode), count = emptyLineNode.Count };

        return new { type = node.GetType().Name };
    }
}
