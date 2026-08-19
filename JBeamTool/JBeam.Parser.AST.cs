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

    public abstract class Node
    {
        public Node? Get(string key) => ObjectNode?.GetEntry(key)?.Value;
        public Node? this[string key] => Get(key);

        public ValueNode? GetValue(string key) => Get(key) as ValueNode;
        public ArrayNode? GetArray(string key) => Get(key) as ArrayNode;
        public ObjectNode? GetObject(string key) => Get(key) as ObjectNode;
        public JsonNode? GetJson(string key) => Get(key) as JsonNode;

        public CommentNode? CommentNode => (this is CommentNode comment) ? comment : null;
        public ValueNode? ValueNode => (this is ValueNode value) ? value : null;
        public ArrayNode? ArrayNode => (this is ArrayNode array) ? array : null;
        public ObjectNode? ObjectNode => (this is ObjectNode obj) ? obj : null;
        public JsonNode? JsonNode => (this is JsonNode json) ? json : null;

        public Node? At(int index) => ArrayNode?[index];
        public Node? this[int index] => At(index);

        public string? ValueStr => ValueNode?.Value;

        public bool IsTrivia() => this is CommentNode || this is EmptyLineNode;
        public override string ToString() => ToString(new StringFormat());
        public virtual string ToString(StringFormat format) => base.ToString() ?? "";
    }

    public class ValueNode : Node
    {
        public string Value { get; }
        public ValueNode(string value) => Value = value;

        public override string ToString(StringFormat format)
        {
            return $"\"{JsonEscape(Value)}\"";
        }
    }

    public class EmptyLineNode : Node
    {
        public int Count { get; }

        public EmptyLineNode(int count) => Count = Math.Min(count, 1);
        public EmptyLineNode(string text) 
            : this(text.Replace("\r\n", "\n").Count(IsLineEnd))
        {}
        
        public override string ToString(StringFormat format)
        {
            return new string('\n', Count);
        }
    }

    public class CommentNode : Node
    {
        public string Comment { get; }
        public bool IsBlock { get; }
        public bool IsLine => !IsBlock;

        public CommentNode(string comment, bool isBlock = false)
        { 
            Comment = comment; 
            IsBlock = isBlock || HasNewLine(comment); 
        }

        public override string ToString(StringFormat format)
        {
            return IsBlock ? $"/* {Comment} */" : $"// {Comment}";
        }
    }

    public class ArrayNode : Node
    {
        public List<Node> Items { get; } = new();

        public new Node? this[int index] => (index >= 0 && index < Items.Count) ? Items[index] : null;

        public List<string> AsStringList()
        {       
            var list = new List<string>();
            foreach (var item in Items)
            {
                list.Add(item.ValueStr ?? "");
            }
            return list;
        }

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

    public class ObjectNode : Node
    {
        public abstract record Key;
        public record KeyTrivia : Key;
        public record KeyAnonymous : Key;
        public record KeyNamed(string Name) : Key;
        public record Entry(Key Key, Node Value);

        public List<Entry> Properties { get; } = new();

        public bool Add(string key, Node? value)
        {
            if (value == null) return false;
            Properties.Add(new Entry(new KeyNamed(key), value));
            return true;
        }
        public bool Add(Node? node)
        {
            if (node == null) return false;

            if (node is EmptyLineNode || node is CommentNode) 
                Properties.Add(new Entry(new KeyTrivia(), node));                
            else 
                Properties.Add(new Entry(new KeyAnonymous(), node));
            return true;
        }

        public List<Entry> PropertiesNamed => Properties.Where(entry => entry.Key is KeyNamed).ToList();
        public Entry? GetEntry(string key)
        {
            foreach (var entry in Properties)
            {
                if (entry.Key is KeyNamed named && named.Name == key)
                    return entry;
            }
            return null;
        }

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

    public class JsonNode : ObjectNode
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