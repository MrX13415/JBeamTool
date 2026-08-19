
namespace BeamNG.JBeam;

public abstract class Node()
{
    public TextRegion Region { get; set; } = new();

    public Node? Get(string key) => NodeObject?.GetEntry(key)?.Value;
    public Node? this[string key] => Get(key);

    public ValueNode? GetValue(string key) => Get(key) as ValueNode;
    public ArrayNode? GetArray(string key) => Get(key) as ArrayNode;
    public ObjectNode? GetObject(string key) => Get(key) as ObjectNode;
    public JsonNode? GetJson(string key) => Get(key) as JsonNode;

    public CommentNode? NodeComment => (this is CommentNode comment) ? comment : null;
    public ValueNode? NodeValue => (this is ValueNode value) ? value : null;
    public ArrayNode? NodeArray => (this is ArrayNode array) ? array : null;
    public ObjectNode? NodeObject => (this is ObjectNode obj) ? obj : null;
    public JsonNode? NodeJson => (this is JsonNode json) ? json : null;

    public Node? At(int index) => NodeArray?[index];
    public Node? this[int index] => At(index);

    public string? ValueStr => NodeValue?.Value;

    public bool IsTrivia() => this is CommentNode || this is EmptyLineNode;
    public override string ToString() => ToString(new StringFormat());
    public virtual string ToString(StringFormat format) => base.ToString() ?? "";
}


public abstract record Key
{
    public KeyNamed? Named => (this is KeyNamed named) ? named : null;
}
public record KeyTrivia : Key;
public record KeyAnonymous : Key;
public record KeyNamed(string Name, TextRegion Region) : Key
{
    public override string ToString() => Name;
}
public partial record NodeEntry(Key Key, Node Value)
{
    public string Name => Key.Named?.Name ?? "";
    public TextRegion Region => new(Key.Named?.Region.Start ?? Value.Region.Start, Value.Region.End);

    public T? As<T>() where T : Node
        => (Value is T t) ? t : null;

    public override string ToString() => ToString(new StringFormat()).Str;
}


public partial class ValueNode : Node
{
    public string Value { get; }
    public ValueNode(Token token)
    {
        Value = token.Value;
        Region = token.Region;
    }
}

public partial class EmptyLineNode : Node
{
    public int Count { get; }

    public EmptyLineNode(Token token)
    {
        Count = Math.Min(token.Region.Delta.Line, 1);
        Region = token.Region;
    }
}

public partial class CommentNode : Node
{
    public string Comment { get; }
    public bool IsBlock { get; }
    public bool IsLine => !IsBlock;

    public CommentNode(Token token)
    { 
        Comment = token.Value;
        IsBlock = token.Type == TokenType.CommentBlock || Comment.HasNewLine();
        Region = token.Region;
    }
}

public partial class ArrayNode : Node
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
}

public partial class ObjectNode : Node
{
    public List<NodeEntry> Properties { get; } = new();

    public bool Add(Token key, Node? value)
    {            
        if (value == null) return false;
        Properties.Add(new NodeEntry(new KeyNamed(key.Value, key.Region), value));
        return true;
    }
    public bool Add(Node? node)
    {
        if (node == null) return false;

        if (node is EmptyLineNode || node is CommentNode) 
            Properties.Add(new NodeEntry(new KeyTrivia(), node));                
        else 
            Properties.Add(new NodeEntry(new KeyAnonymous(), node));
        return true;
    }

    public List<NodeEntry> PropertiesNamed => Properties.Where(entry => entry.Key is KeyNamed).ToList();
    public NodeEntry? GetEntry(string key)
    {
        foreach (var entry in Properties)
        {
            if (entry.Key is KeyNamed named && named.Name == key)
                return entry;
        }
        return null;
    }
}

public partial class JsonNode : ObjectNode;