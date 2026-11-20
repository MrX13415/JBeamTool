namespace JBeamTool;

public record CommandArgs(string[] Positional, bool Json, bool Strict)
{
    public bool Tree { get; init; }
}

public static class ArgumentParser
{
    public static CommandArgs Parse(string[] args)
    {
        var positional = new List<string>();
        bool json = false;
        bool strict = false;
        bool tree = false;

        foreach (var arg in args)
        {
            if (!arg.StartsWith('-') && !arg.StartsWith('/'))
            {
                positional.Add(arg);
                continue;
            }

            switch (arg.Trim().ToLowerInvariant())
            {
                case "--json":
                case "-json":
                case "/json":
                    json = true;
                    break;
                case "--strict":
                case "-strict":
                case "/strict":
                    strict = true;
                    break;
                case "-tree":
                    tree = true;
                    break;
                default:
                    positional.Add(arg);
                    break;
            }
        }

        return new CommandArgs(positional.ToArray(), json, strict) { Tree = tree };
    }
}
