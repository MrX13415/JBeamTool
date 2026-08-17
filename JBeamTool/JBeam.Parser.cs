using System.Data;
using System.Reflection.Metadata;
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


    public abstract class Node
    {
        public Node? Get(string key) => ObjectNode?.GetEntry(key)?.Value;
        public Node? this[string key] => Get(key);

        public ValueNode? GetValue(string key) => Get(key) as ValueNode;
        public ArrayNode? GetArray(string key) => Get(key) as ArrayNode;
        public ObjectNode? GetObject(string key) => Get(key) as ObjectNode;
        public JsonNode? GetJson(string key) => Get(key) as JsonNode;


        public ValueNode? ValueNode => (this is ValueNode value) ? value : null;
        public ArrayNode? ArrayNode => (this is ArrayNode array) ? array : null;
        public ObjectNode? ObjectNode => (this is ObjectNode obj) ? obj : null;
        public JsonNode? JsonNode => (this is JsonNode json) ? json : null;

        public Node? At(int index) => ArrayNode?[index];
        public Node? this[int index] => At(index);

        public string? ValueStr => ValueNode?.Value;

        public override string ToString() => ToString(new StringFormat());
        public virtual string ToString(StringFormat format) => base.ToString() ?? "";
    }

    public class ValueNode : Node
    {
        public string Value { get; }
        public ValueNode(string value) => Value = value;

        public override string ToString(StringFormat format)
        {
            if (Value.Contains("6-Pack"))
            {
                bool lol = true;
            }
            return $"\"{JsonEscape(Value)}\"";
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
            format.ArrayFormat = ArrayFormat.MultiLine;
            switch (format.ArrayFormat)
            {
                case ArrayFormat.MultiLine:
                    sb.AppendLine($"{pad}[");
                    sb.AppendJoin(",\n", items.Select(i => $"{subPad}{i.Trim()}"));
                    sb.AppendLine();
                    sb.AppendLine($"{pad}]");
                    break;

                case ArrayFormat.SingleLine:
                    sb.Append($"{pad}[");
                    sb.AppendJoin(", ", items);
                    sb.Append($"]");
                    break;

                default:
                case ArrayFormat.Inline:
                    sb.Append("[");
                    sb.AppendJoin(", ", items);
                    sb.Append("]");
                    break;
            }
            
            return sb.ToString();
        }
        public IEnumerable<string> ToStringItems(StringFormat format)
        {
            var pendingRows = new List<List<(string Str, bool NewLine)>>();
            var colWidths = new List<int>();

            IEnumerable<string> FlushPending()
            {
                if (pendingRows.Count == 0) yield break;
                
                foreach (var row in pendingRows)
                {
                    var sb = new StringBuilder();

                    for (int colIndex = 0; colIndex < row.Count; colIndex++)
                    {
                        var col = row[colIndex];
                        sb.Append(col.Str.PadRight(colWidths[colIndex]));
                        if (col.NewLine)
                        {
                            sb.AppendLine();
                            sb.Append(format.Pad(1));
                        }
                        else if (colIndex < row.Count - 1)
                        {
                            sb.Append(", ");
                        }
                    }

                    yield return $"[{sb.ToString().TrimEnd()}]";
                }

                pendingRows.Clear();
                colWidths.Clear();
            }

            foreach (var item in Items)
            {
                if (item is ArrayNode arr)
                {
                    List<(string Str, bool newline)> row = arr.Items
                        .Select(sub =>
                            (ToStringItem(sub, format).Trim(), 
                            sub is CommentNode comment && comment.IsLine)
                        ).ToList();

                    // Update column widths
                    for (int i = 0; i < row.Count; i++)
                    {
                        if (colWidths.Count <= i)
                            colWidths.Add(row[i].Str.Length);
                        else
                            colWidths[i] = Math.Max(colWidths[i], row[i].Str.Length);
                    }

                    pendingRows.Add(row);
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
        private static string ToStringItem(Node itemNode, StringFormat format)
        {

            if (itemNode is ValueNode value && value.Value.Contains("lid_L_animated"))
            {
              return itemNode.ToString(format).Trim();
            }


            return itemNode is ArrayNode arr
                ? arr.ToString(format)
                : itemNode.ToString(format).Trim();
        }
    }

    public class ObjectNode : Node
    {
        public abstract record Key;
        public record KeyComment : Key;
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
            if (node is CommentNode commentNode) Properties.Add(new Entry(new KeyComment(), commentNode));                
            else Properties.Add(new Entry(new KeyAnonymous(), node));
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

            if (items.Count == 1 && !HasNewLine(items[0]))
                return $"{pad}{{{items[0].Trim()}}}";

            sb.AppendLine($"{pad}{{");
            sb.AppendJoin(",\n", items);
            sb.AppendLine();
            sb.AppendLine($"{pad}}}");
            return sb.ToString();
        }

        public IEnumerable<string> ToStringItems(StringFormat format)
        {
            string pad = format.Pad();
            foreach (var kv in Properties)
            {
                switch(kv.Key)
                {
                    case KeyComment:
                        if (kv.Value is CommentNode commentNode)
                        {
                            var comment = commentNode.ToString(format).Trim();
                            yield return $"{pad}{comment}";
                        }
                        break;

                    case KeyNamed namedKey:
                        var value = kv.Value.ToString(format).Trim();
                        yield return $"{pad}\"{namedKey.Name}\": {value}";
                        break;

                    case KeyAnonymous:
                        var anonValue = kv.Value.ToString(format).Trim();
                        yield return $"{pad}{anonValue}";
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

            var items = ToStringItems(format with { ArrayFormat = ArrayFormat.Inline }).Select(i => i.Trim()).ToList();

            var sb = new StringBuilder();
            sb.Append($"{{");
            sb.AppendJoin(", ", items);
            sb.Append($"}}");
            return sb.ToString();
        }
    }

}

static class ArrayFormatExtensions
{
    public static JBeam.ArrayFormat Next(this JBeam.ArrayFormat format) => format < JBeam.ArrayFormat.Inline ? format + 1 : format;
}

partial class JBeam // Lexer
{
    public enum TokenType
    {
        LBrace,         // {
        RBrace,         // }
        LBracket,       // [
        RBracket,       // ]
        Colon,          // :
        Comma,          // ,
        String,         // "foo" or <foo>
        Identifier,     // foo
        Number,         // 123, -0.5, 1e-3
        Boolean,
        CommentLine,    // // comment
        CommentBlock,   // /* comment */
        Null,        
        EOF
    }

    public record TextLocation(int Position, int Line, int Column)
    {
        public override string ToString()
            => $"{Line}:{Column}[{Position}]";
    }
    public record Token(TokenType Type, string Value, TextLocation Location);

    public class Lexer
    {
        private readonly string _text;
        private int _pos;
        private int _line = 1;
        private int _column = 1;

        public Lexer(string text)
        {
            _text = text;
            _pos = 0;
        }

        private TextLocation Location => new TextLocation(_pos, _line, _column);
        private char Current => _pos < _text.Length ? _text[_pos] : '\0';
        private bool LineEnd => Current == '\r' || Current == '\n';
        
        private char Peek(int offset = 1)
        {
            int idx = _pos + offset;
            return idx < _text.Length ? _text[idx] : '\0';
        }

        private void Advance()
        {
            char c = Current;  
            _pos++;

            // Handle CRLF as a single newline
            if (c == '\r')
            {
                if (Current == '\n')
                    _pos++;         // consume LF as part of CRLF

                _line++;
                _column = 1;
                return;
            }

            // Handle LF alone
            if (c == '\n')
            {
                _line++;
                _column = 1;
                return;
            }

            // Normal character
            _column++;
        }

        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(Current))
                Advance();
        }

        public List<Token> LexAll()
        {
            var tokens = new List<Token>();
            Token t;
            do
            {
                t = NextToken();
                tokens.Add(t);
            } while (t.Type != TokenType.EOF);

            return tokens;
        }

        private Token NextToken()
        {
            SkipWhitespace();

            TextLocation start = Location;
            char c = Current;

            if (c == '\0')
                return new Token(TokenType.EOF, "", Location);

            // Comments
            if (c == '/' && Peek() == '/')
                return LexCommentLine(start);

            // Block comment: /* ... */
            if (c == '/' && Peek() == '*')
                return LexCommentBlock(start);

            // Structural
            switch (c)
            {
                case '{': Advance(); return new Token(TokenType.LBrace, "{", start);
                case '}': Advance(); return new Token(TokenType.RBrace, "}", start);
                case '[': Advance(); return new Token(TokenType.LBracket, "[", start);
                case ']': Advance(); return new Token(TokenType.RBracket, "]", start);
                case ':': Advance(); return new Token(TokenType.Colon, ":", start);
                case ',': Advance(); return new Token(TokenType.Comma, ",", start);
            }

            // Strings: "..." or <...>
            if (c == '"' || c == '<')
                return LexString(c, start);

            // Number
            if (char.IsDigit(c) || c == '-' || c == '+')
                return LexNumber(start);

            // Identifier / keyword
            if (IsIdentStart(c))
                return LexIdentifier(start);

            var snippet = _text.Substring(Math.Max(0, start.Position - 10), Math.Min(20, _text.Length - start.Position + 10));
            throw new Exception($"Unexpected character '{c}' at {start}. Context: {snippet}");
        }

        private bool IsIdentStart(char c) =>
            char.IsLetter(c) || c == '_' || c == '$';

        private bool IsIdentPart(char c) =>
            char.IsLetterOrDigit(c) || c == '_' || c == '$';

        private Token LexCommentLine(TextLocation startPos)
        {
            var sb = new StringBuilder();
            Advance(); // skip /
            Advance(); // skip /

            while (Current != '\n' && Current != '\r' && Current != '\0')
            {
                sb.Append(Current);
                Advance();
            }

            return new Token(TokenType.CommentLine, sb.ToString(), startPos);
        }

        private Token LexCommentBlock(TextLocation startPos)
        {
            var sb = new StringBuilder();
            Advance(); // skip /
            Advance(); // skip *

            while (!(Current == '*' && Peek() == '/') && Current != '\0')
            {
                sb.Append(Current);
                Advance();
            }

            if (Current == '*' && Peek() == '/')
            {
                Advance(); // *
                Advance(); // /
            }

            return new Token(TokenType.CommentBlock, sb.ToString(), startPos);
        }        

        private Token LexString(char startChar, TextLocation startPos)
        {
            Advance(); // skip " or <
            var sb = new StringBuilder();
            char endChar = startChar == '"' ? '"' : '>';

            while (Current != '\0' && Current != endChar)
            {
                // simple escape handling for "
                if (Current == '\\' && startChar == '"')
                {
                    Advance();
                    if (Current == '\0') break;
                    sb.Append(Current);
                    Advance();
                    continue;
                }

                sb.Append(Current);
                Advance();
            }

            if (Current == endChar)
                Advance(); // consume closing

            return new Token(TokenType.String, sb.ToString(), startPos);
        }

        private Token LexNumber(TextLocation startPos)
        {
            var sb = new StringBuilder();

            if (Current == '+' || Current == '-')
            {
                sb.Append(Current);
                Advance();
            }

            while (char.IsDigit(Current))
            {
                sb.Append(Current);
                Advance();
            }

            if (Current == '.')
            {
                sb.Append(Current);
                Advance();
                while (char.IsDigit(Current))
                {
                    sb.Append(Current);
                    Advance();
                }
            }

            if (Current == 'e' || Current == 'E')
            {
                sb.Append(Current);
                Advance();
                if (Current == '+' || Current == '-')
                {
                    sb.Append(Current);
                    Advance();
                }
                while (char.IsDigit(Current))
                {
                    sb.Append(Current);
                    Advance();
                }
            }

            return new Token(TokenType.Number, sb.ToString(), startPos);
        }

        private Token LexIdentifier(TextLocation startPos)
        {
            var sb = new StringBuilder();

            while (IsIdentPart(Current))
            {
                sb.Append(Current);
                Advance();
            }

            string text = sb.ToString();

            return text switch
            {
                "true" or "false" => new Token(TokenType.Boolean, text, startPos),
                "null"            => new Token(TokenType.Null, text, startPos),
                _                 => new Token(TokenType.Identifier, text, startPos),
            };
        }
    }
}

partial class JBeam // Parser
{
    public class KeyedNode
    {
        public string Key { get; }
        public Node Value { get; }

        public KeyedNode(string key, Node value)
        {
            Key = key;
            Value = value;
        }

        public T? As<T>() where T : Node
            => (Value is T t) ? t : null;
    }

    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _index;

        private Token Current => _index < _tokens.Count ? _tokens[_index] : _tokens[^1];
        private bool _insideArray = false;
    
        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _index = 0;
        }

        private Token Consume(TokenType type)
        {
            if (Current.Type != type)
            {
                string lastFileLines = string.Join("\n", _tokens.GetRange(Math.Max(0, _index - 3), Math.Min(6, _tokens.Count - _index + 3)).ConvertAll(t => $"{t.Location}: {t.Type} '{t.Value}'"));
                throw new Exception($"Expected {type} but found {Current.Type} at {Current.Location}\nContext:\n{lastFileLines}");
            }

            var t = Current;
            _index++;
            return t;
        }

        private bool Match(TokenType type)
        {
            if (Current.Type == type)
            {
                _index++;
                return true;
            }
            return false;
        }

        public ObjectNode ParseFile()
        {
            // JBeam files are usually a single top-level object
            return ParseObject();
        }

        private ObjectNode ParseObject(bool json = false)
        {
            var obj = json ? new JsonNode() : new ObjectNode();
            Consume(TokenType.LBrace);

            while (Current.Type != TokenType.RBrace && Current.Type != TokenType.EOF)
            {
                // comments
                if (obj.Add(ParseComment()))
                    continue;

                // key
                string key = ParseKey();
                Consume(TokenType.Colon);

                // value
                Node value = ParseValue();
                obj.Add(key, value);

                Match(TokenType.Comma); // optional comma
            }

            Consume(TokenType.RBrace);
            return obj;
        }

        private Node? ParseComment()
        {
            return Current.Type switch
            {
                TokenType.CommentLine   => new CommentNode(Consume(TokenType.CommentLine).Value, false),
                TokenType.CommentBlock  => new CommentNode(Consume(TokenType.CommentBlock).Value, true),
                _ => null
            };
        }

        private string ParseKey()
        {
            return Current.Type switch
            {
                TokenType.String        => Consume(Current.Type).Value,
                TokenType.Identifier    => Consume(Current.Type).Value,
                _ => throw new Exception($"Invalid key token {Current.Type} at {Current.Location}")
            };
        }

        private Node ParseValue()
        {
            return Current.Type switch
            {
                TokenType.String        => new ValueNode(Consume(Current.Type).Value),
                TokenType.Identifier    => new ValueNode(Consume(Current.Type).Value),
                TokenType.Number        => new ValueNode(Consume(Current.Type).Value),
                TokenType.CommentLine   => new CommentNode(Consume(TokenType.CommentLine).Value, false),
                TokenType.CommentBlock  => new CommentNode(Consume(TokenType.CommentBlock).Value, true),
                TokenType.Boolean       => new ValueNode(Consume(TokenType.Boolean).Value),
                TokenType.Null          => new ValueNode(Consume(TokenType.Null).Value),
                TokenType.LBracket      => ParseArray(),
                TokenType.LBrace        => ParseJsonOrObject(),
                _ => throw new Exception($"Unexpected value token {Current.Type} at {Current.Location}")
            };
        }

        private ArrayNode ParseArray()
        {
            var arr = new ArrayNode();
            Consume(TokenType.LBracket);

            bool prev = _insideArray;
            _insideArray = true;

            while (Current.Type != TokenType.RBracket && Current.Type != TokenType.EOF)
            {
                // Parse one element
                var value = ParseValue();
                arr.Items.Add(value);

                // If comma → consume and continue
                if (Match(TokenType.Comma))
                    continue;

                // If next token starts a new value → allow missing comma
                if (IsValueStart(Current.Type))
                    continue;

                // If next token is ] → end
                if (Current.Type == TokenType.RBracket)
                    break;

                // Anything else → error
                throw new Exception($"Unexpected token {Current.Type} in array at {Current.Location}");
            }

            Consume(TokenType.RBracket);
            _insideArray = prev;

            return arr;
        }

        private bool IsValueStart(TokenType type)
        {
            return type == TokenType.String ||
                type == TokenType.Number ||
                type == TokenType.Boolean ||
                type == TokenType.Null ||
                type == TokenType.Identifier ||
                type == TokenType.CommentLine ||
                type == TokenType.CommentBlock ||
                type == TokenType.LBracket ||
                type == TokenType.LBrace;
        }

        private Node ParseJsonOrObject()
        {
            if (_insideArray)
            {
                // JSON block inside array
                return CaptureJsonBlock();
            }

            // Otherwise → JBeam object
            return ParseObject();
        }

        private JsonNode CaptureJsonBlock()
        {
            return (JsonNode)ParseObject(true);
        }
    }

    public static ObjectNode Parse(string jbeamText)
    {
        var lexer = new Lexer(jbeamText);
        var tokens = lexer.LexAll();

        var parser = new Parser(tokens);
        ObjectNode root = parser.ParseFile();

        return root;
    }

}