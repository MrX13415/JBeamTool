using System.Text;

namespace BeamNG.JBeam;

public record TextLocation(int Position, int Line, int Column)
{
    public static TextLocation operator-(TextLocation a, TextLocation b)
    {
        var pos = a.Position - b.Position;
        var line = a.Line - b.Line;
        var col = line == 0 ? a.Column - b.Column : a.Column;
        return new TextLocation(pos, line, col);
    }
    public static TextLocation operator+(TextLocation a, TextLocation b)
    {
        var pos = a.Position + b.Position;
        var line = a.Line + b.Line;
        var col = line == 0 ? a.Column + b.Column : a.Column;
        return new TextLocation(pos, line, col);
    }

    public override string ToString() => $"{Line}:{Column}[{Position}]";

    public static TextLocation Get(string text, TextLocation? start = default)
    {
        var _start = start ?? new TextLocation(0, 0, 0);
        var lines = text.Replace("\r\n", "\n").Split('\n');
        int lastLineLength = lines.LastOrDefault()?.Length ?? 0;
        return new TextLocation(_start.Position + text.Length, _start.Line + lines.Length, lastLineLength);
    }
}

public record class TextRegion(TextLocation Start, TextLocation End)
{
    public TextRegion() : this(new TextLocation(0, 0, 0), new TextLocation(0, 0, 0)) {}
    public TextLocation Delta => End - Start;

    public override string ToString() => $"{Start.Line}:{Start.Column}-{End.Line}:{End.Column}";

    public static TextRegion Get(string text, TextLocation? start = default)
    {
        var _start = start ?? new TextLocation(0, 0, 0);
        var end = TextLocation.Get(text, _start);
        return new TextRegion(_start, end);
    }
}

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
    Boolean,        // true, false
    EmptyLine,      // \n\n
    CommentLine,    // // comment
    CommentBlock,   // /* comment */
    Null,           // null
    EOF
}

public record Token(TokenType Type, string Value, TextLocation Location)
{
    public TextRegion Region => TextRegion.Get(Value, Location);
}

public class JBeamLexer
{
    private readonly string _text;
    private int _pos;
    private int _line = 1;
    private int _column = 1;

    public JBeamLexer(string text)
    {
        _text = text;
        _pos = 0;
    }

    private TextLocation Location => new TextLocation(_pos, _line, _column);
    private char Current => Peek(0);
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

    private TextLocation SkipWhitespace()
    {
        var loc = Location;
        while (true)
        {
            if (!char.IsWhiteSpace(Current)) break;
            Advance();
        }
        return Location - loc;
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
        TextLocation skipped = SkipWhitespace();

        TextLocation start = Location;
        char c = Current;

        if (c == '\0')
            return new Token(TokenType.EOF, "", Location);

        // Preserve empty lines
        if (skipped.Line >= 2)
            return LexEmptyLine(skipped);

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


    private Token LexEmptyLine(TextLocation delta)
    {
        var start = (Location - delta);
        var length = delta.Position;
        var snippet = _text.Substring(start.Position, length);
        return new Token(TokenType.EmptyLine, snippet, start);
    }

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