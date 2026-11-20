using System.Text.Json.Nodes;
using BeamNG.JBeam;

namespace JBeamTool;

public static class AstDescriber
{
    public static JsonObject DescribeNode(Node node)
    {
        var result = new JsonObject { ["type"] = node.GetType().Name };
        if (node is ValueNode valueNode)
            result["value"] = valueNode.Value;
        else if (node is ArrayNode arrayNode)
        {
            result["count"] = arrayNode.Items.Count;
            result["items"] = new JsonArray(arrayNode.Items.Select(DescribeNode).ToArray());
        }
        else if (node is ObjectNode objectNode)
        {
            result["propertyCount"] = objectNode.Properties.Count;
            result["properties"] = new JsonArray(objectNode.Properties.Select(entry => new JsonObject
            {
                ["key"] = entry.Name,
                ["keyType"] = entry.Key.GetType().Name,
                ["valueType"] = entry.Value.GetType().Name,
                ["value"] = DescribeNode(entry.Value)
            }).ToArray());
        }
        else if (node is CommentNode commentNode)
        {
            result["isBlock"] = commentNode.IsBlock;
            result["comment"] = commentNode.Comment;
        }
        else if (node is EmptyLineNode emptyLineNode)
            result["count"] = emptyLineNode.Count;

        return result;
    }
}
