
namespace BeamNG.JBeam;

public partial class JBeamFile
{
    public string FilePath { get; set; } = string.Empty;
    public string FileContent { get; set; } = string.Empty;

    public ObjectNode Root { get; set; } = new();
    public List<Part> Parts { get; set; } = new();

    public JBeamFile(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public bool Sanity => Root.PropertiesNamed.Count == Parts.Count;

}
