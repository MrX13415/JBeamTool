using System;
using System.Collections.Generic;
using System.Text;

    
partial class JBeam // AST
{
    public abstract class Node
    {
        public Node? Get(string key)
            => (this is ObjectNode obj && obj.Properties.TryGetValue(key, out var value)) ? value : null;
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
    }

    public class ValueNode : Node
    {
        public string Value { get; }
        public ValueNode(string value) => Value = value;
    }

    public class ArrayNode : Node
    {
        public List<Node> Items { get; } = new();

        public new Node? this[int index] => (index >= 0 && index < Items.Count) ? Items[index] : null;
    }

    public class ObjectNode : Node
    {
        public Dictionary<string, Node> Properties { get; } = new();
    }

    public class JsonNode : Node
    {
        public string RawJson { get; }
        public JsonNode(string rawJson) => RawJson = rawJson;
    }

}

partial class JBeam // Lexer
{
    public enum TokenType
    {
        LBrace,    // {
        RBrace,    // }
        LBracket,  // [
        RBracket,  // ]
        Colon,     // :
        Comma,     // ,
        String,    // "foo" or <foo>
        Bareword,  // foo
        Number,    // 123, -0.5, 1e-3
        True,
        False,
        Null,
        EOF
    }

    public record Token(TokenType Type, string Value, int Position);

    public class Lexer
    {
        private readonly string _text;
        private int _pos;

        public Lexer(string text)
        {
            _text = text;
            _pos = 0;
        }

        private char Current => _pos < _text.Length ? _text[_pos] : '\0';

        private void Advance() => _pos++;

        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(Current))
                Advance();
        }

        private void SkipLineComment()
        {
            while (Current != '\n' && Current != '\r' && Current != '\0')
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

            int start = _pos;
            char c = Current;

            if (c == '\0')
                return new Token(TokenType.EOF, "", _pos);

            // Comments
            if (c == '/' && Peek() == '/')
            {
                Advance(); // /
                Advance(); // /
                SkipLineComment();
                return NextToken();
            }

            // Block comment: /* ... */
            if (c == '/' && Peek() == '*')
            {
                Advance(); // /
                Advance(); // *

                // Skip until closing */
                while (!(Current == '*' && Peek() == '/') && Current != '\0')
                    Advance();

                if (Current == '*' && Peek() == '/')
                {
                    Advance(); // *
                    Advance(); // /
                }

                return NextToken(); // continue lexing
            }

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
                return LexIdent(start);

            var snippet = _text.Substring(Math.Max(0, start - 10), Math.Min(20, _text.Length - start + 10));
            throw new Exception($"Unexpected character '{c}' at {start}. Context: {snippet}");
        }

        private char Peek(int offset = 1)
        {
            int idx = _pos + offset;
            return idx < _text.Length ? _text[idx] : '\0';
        }

        private bool IsIdentStart(char c) =>
            char.IsLetter(c) || c == '_' || c == '$';

        private bool IsIdentPart(char c) =>
            char.IsLetterOrDigit(c) || c == '_' || c == '$';

        private Token LexString(char startChar, int startPos)
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

        private Token LexNumber(int startPos)
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

        private Token LexIdent(int startPos)
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
                "true"  => new Token(TokenType.True, text, startPos),
                "false" => new Token(TokenType.False, text, startPos),
                "null"  => new Token(TokenType.Null, text, startPos),
                _       => new Token(TokenType.Bareword, text, startPos),
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
                string lastFileLines = string.Join("\n", _tokens.GetRange(Math.Max(0, _index - 3), Math.Min(6, _tokens.Count - _index + 3)).ConvertAll(t => $"{t.Position}: {t.Type} '{t.Value}'"));
                throw new Exception($"Expected {type} but found {Current.Type} at {Current.Position}\nContext:\n{lastFileLines}");
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

        public ObjectNode ParseFileAsObject()
        {
            // JBeam files are usually a single top-level object
            return ParseObject();
        }

        private ObjectNode ParseObject()
        {
            var obj = new ObjectNode();
            Consume(TokenType.LBrace);

            while (Current.Type != TokenType.RBrace && Current.Type != TokenType.EOF)
            {
                // key
                string key = ParseKey();

                Consume(TokenType.Colon);

                // value
                Node value = ParseValue();

                obj.Properties[key] = value;

                Match(TokenType.Comma); // optional comma
            }

            Consume(TokenType.RBrace);
            return obj;
        }

        private string ParseKey()
        {
            return Current.Type switch
            {
                TokenType.String   => Consume(Current.Type).Value,
                TokenType.Bareword => Consume(Current.Type).Value,
                _ => throw new Exception($"Invalid key token {Current.Type} at {Current.Position}")
            };
        }

        private Node ParseValue()
        {
            return Current.Type switch
            {
                TokenType.String   => new ValueNode(Consume(Current.Type).Value),
                TokenType.Number   => new ValueNode(Consume(Current.Type).Value),
                TokenType.True     => new ValueNode(Consume(TokenType.True).Value),
                TokenType.False    => new ValueNode(Consume(TokenType.False).Value),
                TokenType.Null     => new ValueNode(Consume(TokenType.Null).Value),
                TokenType.Bareword => new ValueNode(Consume(Current.Type).Value),
                TokenType.LBracket => ParseArray(),
                TokenType.LBrace   => ParseJsonOrObject(),
                _ => throw new Exception($"Unexpected value token {Current.Type} at {Current.Position}")
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
                throw new Exception($"Unexpected token {Current.Type} in array at {Current.Position}");
            }

            Consume(TokenType.RBracket);
            _insideArray = prev;

            return arr;
        }

        private bool IsValueStart(TokenType type)
        {
            return type == TokenType.String ||
                type == TokenType.Number ||
                type == TokenType.True ||
                type == TokenType.False ||
                type == TokenType.Null ||
                type == TokenType.Bareword ||
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
            int startPos = _tokens[_index].Position; // position of '{'
            int braceDepth = 0;
            int startIndex = _index;

            // Move through tokens until braces balanced
            while (_index < _tokens.Count)
            {
                if (Current.Type == TokenType.LBrace)
                    braceDepth++;
                else if (Current.Type == TokenType.RBrace)
                {
                    braceDepth--;
                    if (braceDepth == 0)
                    {
                        _index++; // consume final }
                        break;
                    }
                }
                _index++;
            }

            // Reconstruct raw text from token positions is tricky without original text,
            // so simplest: store token slice as pseudo-JSON string.
            var sb = new StringBuilder();
            for (int i = startIndex; i < _index; i++)
            {
                sb.Append(_tokens[i].Value);
                if (i + 1 < _index)
                    sb.Append(' ');
            }

            return new JsonNode(sb.ToString());
        }
    }

    public static class DebugDump
    {
        public static void DumpNode(Node node, int indent = 0)
        {
            string pad = new string(' ', indent * 2);

            switch (node)
            {
                case ValueNode v:
                    Console.WriteLine($"{pad}Value: {v.Value}");
                    break;

                case ArrayNode a:
                    Console.WriteLine($"{pad}Array:");
                    for (int i = 0; i < a.Items.Count; i++)
                    {
                        Console.WriteLine($"{pad}  [{i}]");
                        DumpNode(a.Items[i], indent + 2);
                    }
                    break;

                case ObjectNode o:
                    Console.WriteLine($"{pad}Object:");
                    foreach (var kv in o.Properties)
                    {
                        Console.WriteLine($"{pad}  {kv.Key}:");
                        DumpNode(kv.Value, indent + 2);
                    }
                    break;

                case JsonNode j:
                    Console.WriteLine($"{pad}JsonBlock: {j.RawJson}");
                    break;

                default:
                    Console.WriteLine($"{pad}Unknown node");
                    break;
            }
        }
    }

    public static ObjectNode Parse(string jbeamText)
    {
        var lexer = new Lexer(jbeamText);
        var tokens = lexer.LexAll();

        var parser = new Parser(tokens);
        ObjectNode root = parser.ParseFileAsObject();

        return root;
    }

}