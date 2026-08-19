
namespace BeamNG.JBeam;

public partial class JBeamFile
{


    public List<string> ConvertFormat_Slots2(StringFormat? format = default)
    {
        format ??= new StringFormat();
        List<string> convertedParts = new();
        ContentLines lines = GetLines();

        foreach (var part in Parts)
        {
            // No slots or already in Slots2 format, skip
            if (part.Slots == null /*|| part.Slots.Type == SectionType.Slots2*/) continue;
            var region = part.Slots.Region;

            var _format = format with { Indent = lines.GetIndent(region.Start, format.IndentSize) };

            // Convert the lines to Slots2 format
            var newText = part.Slots.Node.ToString(_format);  // ToString() will output in Slots2 format
            var newLines = ContentLines.FromString(newText.Str);
            lines.Set(newLines, region);

            convertedParts.Add(part.Name ?? "<unknown>");
        }

        SetLines(lines);
        return convertedParts;
    }

}
