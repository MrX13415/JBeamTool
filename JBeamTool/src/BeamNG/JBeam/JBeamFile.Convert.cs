
namespace BeamNG.JBeam;

public partial class JBeamFile
{


    public List<string> ConvertFormat_Slots2(StringFormat? format = default)
    {
        format ??= new StringFormat();
        List<string> convertedParts = new();
        ContentLines lines = GetLines();

        foreach (var part in Parts.OrderByDescending(part => part.Slots?.Region.Start.Position ?? -1))
        {
            if (part.Slots is not { } slots || !slots.ConvertToSlots2())
                continue;

            var region = slots.Region;
            var sectionFormat = format with { Indent = lines.GetIndent(region.Start, format.IndentSize) };
            var replacement = slots.Node.ToString(sectionFormat).Str.TrimStart().ReplaceLineEndings(GetLineEndCharacter());
            FileContent = string.Concat(FileContent.AsSpan(0, region.Start.Position), replacement,
                FileContent.AsSpan(region.End.Position + 1));

            convertedParts.Add(part.Identifier ?? "<unknown>");
        }

        convertedParts.Reverse();
        return convertedParts;
    }

}
