using System.Text;

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
                // empty lines
                if (obj.Add(ParseEmptyLine()))
                    continue;

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

        private Node? ParseEmptyLine()
        {
            if (Current.Type == TokenType.EmptyLine)
                return new EmptyLineNode(Consume(TokenType.EmptyLine).Value);
            return null;
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
                TokenType.EmptyLine     => new EmptyLineNode(Consume(TokenType.EmptyLine).Value),
                TokenType.CommentLine   => new CommentNode(Consume(TokenType.CommentLine).Value, false),
                TokenType.CommentBlock  => new CommentNode(Consume(TokenType.CommentBlock).Value, true),
                TokenType.String        => new ValueNode(Consume(Current.Type).Value),
                TokenType.Identifier    => new ValueNode(Consume(Current.Type).Value),
                TokenType.Number        => new ValueNode(Consume(Current.Type).Value),
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
                type == TokenType.EmptyLine ||
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

static class JBeamExtensions
{
    public static JBeam.ArrayFormat Next(this JBeam.ArrayFormat format) => format < JBeam.ArrayFormat.Inline ? format + 1 : format;

    public static StringBuilder AppendJoin<T>(this StringBuilder sb, IEnumerable<T> values, Func<T, string?> separator)
    {
        bool first = true;
        T? prev = default;

        foreach (var current in values)
        {
            var s = first ? separator(prev!) : null;
            if (!string.IsNullOrEmpty(s)) sb.Append(s);

            sb.Append(current);
            prev = current;
            first = false;
        }

        return sb;
    }

    public static StringBuilder AppendJoin<T>(this StringBuilder sb, IEnumerable<T> values, Func<T, string?> separator, Func<T, string?> valueSelector)
    {
        bool first = true;
        T? prev = default;

        foreach (var current in values)
        {
            var s = !first ? separator(prev!) : null;
            if (!string.IsNullOrEmpty(s)) sb.Append(s);

            var v = valueSelector(current);
            if (!string.IsNullOrEmpty(v)) sb.Append(v);
            prev = current;
            first = false;
        }

        return sb;
    }

}