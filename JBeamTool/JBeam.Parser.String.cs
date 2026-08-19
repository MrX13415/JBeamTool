using System.Text;

partial class JBeam // AST
{
    public static readonly Dictionary<string, char> JsonEscapeMap = new()
    {
        [@"\"""] = '"',
        [@"\\"] = '\\',
        [@"\b"] = '\b',
        [@"\f"] = '\f',
        [@"\n"] = '\n',
        [@"\r"] = '\r',
        [@"\t"] = '\t'
    };
    public static string JsonEscape(string str)
    { 
        if (str == null)
            return "";

        // Escape tokens found, assume string is already escaped.
        // Notice: This does not check \uXXXX sequences!
        if (JsonEscapeMap.Any(kv => str.Contains(kv.Key)))
            return str;

        var sb = new StringBuilder(str.Length + 10);
        for(int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            
            var token = JsonEscapeMap.FirstOrDefault(kv => kv.Value == c);
            if (token.Key != null)
                sb.Append(token.Key);
            else if (c < 0x20)
                sb.Append("u").Append(((int)c).ToString("x4"));
            else
                sb.Append(c);
        }

        return sb.ToString();
    }
    public static bool HasNewLine(string text) => text.Contains("\n") || text.Contains("\r");
    public static int CountLines(string text) =>  text.Replace("\r\n", "\n").Count(IsLineEnd);
    public static bool IsLineEnd(char c) => c == '\n' || c == '\r';

    public enum ArrayFormat
    {
        MultiLine = 0,
        SingleLine = 1,
        Inline = 2
    }
    public record class StringFormat
    {
        public int IndentSize { get; set; } = 2;
        public int Indent { get; set; } = 0;
        public ArrayFormat ArrayFormat { get; set; } = ArrayFormat.MultiLine;        

        public string Pad(int offset = 0) => new string(' ', (Indent + offset) * IndentSize);

        public StringFormat NextIndent() => this with { Indent = Indent + 1 };
        public StringFormat NextArray() => this with { ArrayFormat = ArrayFormat.Next() };
    }
    public record class NodeString(string Str, Node Node);


    public partial class ValueNode : Node
    {
        public override string ToString(StringFormat format)
        {
            return $"\"{JsonEscape(Value)}\"";
        }
    }

    public partial class EmptyLineNode : Node
    {
        public override string ToString(StringFormat format)
        {
            return new string('\n', Count);
        }
    }

    public partial class CommentNode : Node
    {
        public override string ToString(StringFormat format)
        {
            return IsBlock ? $"/* {Comment} */" : $"// {Comment}";
        }
    }

    public partial class ArrayNode : Node
    {
        public override string ToString(StringFormat format)
        {
            var sb = new StringBuilder();
            string pad = format.Pad();
            string subPad = format.Pad(1);

            var items = ToStringItems(format.NextIndent().NextArray()).ToList();

            switch (format.ArrayFormat)
            {
                case ArrayFormat.MultiLine:
                    sb.AppendLine($"{pad}[");
                    sb.AppendJoin(items, 
                        i => i.Node switch{ EmptyLineNode e => "", CommentNode o => "\n", _ => ",\n" }, 
                        i => (i.Node is EmptyLineNode) ?  $"{i.Str}" : $"{subPad}{i.Str.Trim()}");
                    sb.AppendLine();
                    sb.AppendLine($"{pad}]");
                    break;

                case ArrayFormat.SingleLine:
                    sb.Append($"{pad}[");
                    sb.AppendJoin(items, i => i.Node.IsTrivia() ? "" : ", ", i => i.Str);
                    sb.Append($"]");
                    break;

                default:
                case ArrayFormat.Inline:
                    sb.Append("[");
                    sb.AppendJoin(items, i => i.Node.IsTrivia() ? "" : ", ", i => i.Str);
                    sb.Append("]");
                    break;
            }
            
            return sb.ToString();
        }
        public IEnumerable<NodeString> ToStringItems(StringFormat format)
        {
            var pendingRows = new List<(List<NodeString> Row, Node Node)>();
            var colWidths = new List<int>();

            string RowString(List<NodeString> row)
            {
                var sb = new StringBuilder();

                for (int colIndex = 0; colIndex < row.Count; colIndex++)
                {
                    var col = row[colIndex];
                    var str = col.Str;

                    if (colIndex >= 0 && colIndex < colWidths.Count)
                        str = str.PadRight(colWidths[colIndex]);

                    sb.Append(str);

                    if (col.Node is EmptyLineNode emptyLine)
                    {
                        sb.Append(emptyLine.ToString(format));
                    }
                    else if (col.Node is CommentNode comment && comment.IsLine)
                    {
                        sb.AppendLine();
                        sb.Append(format.Pad(1));
                    }
                    else if (colIndex < row.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }

                return $"[{sb.ToString().TrimEnd()}]";
            }
            IEnumerable<NodeString> FlushPending()
            {
                if (pendingRows.Count == 0) yield break;
                
                foreach (var row in pendingRows)
                    yield return new NodeString(RowString(row.Row), row.Node);

                pendingRows.Clear();
                colWidths.Clear();
            }

            foreach (var item in Items)
            {
                if (item is ArrayNode arr)
                {
                    List<NodeString> row = arr.Items
                        .Select(sub => ToStringItem(sub, format, true)).ToList();

                    // Update column widths
                    for (int i = 0; i < row.Count; i++)
                    {
                        if (colWidths.Count <= i)
                            colWidths.Add(row[i].Str.Length);
                        else
                            colWidths[i] = Math.Max(colWidths[i], row[i].Str.Length);
                    }

                    pendingRows.Add((row, arr));
                }
                else
                {
                    foreach (var pending in FlushPending())
                        yield return pending;

                    yield return ToStringItem(item, format);
                }
            }

            foreach (var pending in FlushPending())
                yield return pending;
        }
        private static NodeString ToStringItem(Node itemNode, StringFormat format, bool trim = false)
        {
            var str = itemNode is ArrayNode arr
                ? arr.ToString(format)
                : itemNode.ToString(format);
            return new NodeString(trim ? str.Trim() : str, itemNode);
        }
    }

    public partial class ObjectNode : Node
    {
        public override string ToString(StringFormat format)
        {
            var sb = new StringBuilder();
            string pad = format.Pad();
            var items = ToStringItems(format.NextIndent()).ToList();

            if (items.Count == 0)
                return $"{pad}{{}}";

            if (items.Count == 1 && !HasNewLine(items[0].Str))
                return $"{pad}{{{items[0].Str.Trim()}}}";

            sb.AppendLine($"{pad}{{");
            sb.AppendJoin(items, 
                i => i.Node switch{ EmptyLineNode e => "", CommentNode o => "\n", _ => ",\n" }, 
                i => i.Str);
            sb.AppendLine();
            sb.AppendLine($"{pad}}}");
            return sb.ToString();
        }

        public IEnumerable<NodeString> ToStringItems(StringFormat format)
        {
            string pad = format.Pad();
            foreach (var kv in Properties)
            {
                switch(kv.Key)
                {
                    case KeyTrivia:
                        yield return (kv.Value is EmptyLineNode)
                            ? new NodeString($"{kv.Value.ToString(format).Trim()}", kv.Value)
                            : new NodeString($"{pad}{kv.Value.ToString(format).Trim()}", kv.Value);
                        break;
    
                    case KeyNamed namedKey:
                        var value = kv.Value.ToString(format).Trim();
                        yield return new NodeString($"{pad}\"{namedKey.Name}\": {value}", kv.Value);
                        break;

                    case KeyAnonymous:
                        var anonValue = kv.Value.ToString(format).Trim();
                        yield return new NodeString($"{pad}{anonValue}", kv.Value);
                        break;
                }
            }
        }
    }
    
    public partial class JsonNode : ObjectNode
    {
        public override string ToString(StringFormat format)
        {
            // fallback to normal formatting if line comments are present
            if (Properties.Any(p => p.Value is CommentNode comment && comment.IsLine))
                return base.ToString(format);

            var items = ToStringItems(format with { ArrayFormat = ArrayFormat.Inline }).ToList();

            var sb = new StringBuilder();
            sb.Append($"{{");
            sb.AppendJoin(items, 
                i => i.Node.IsTrivia() ? "" : ", ", 
                i => i.Str.Trim());
            sb.Append($"}}");
            return sb.ToString();
        }
    }

}