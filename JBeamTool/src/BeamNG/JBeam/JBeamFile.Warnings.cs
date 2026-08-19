
namespace BeamNG.JBeam;

public partial class JBeamFile
{
    private readonly List<string> _warnings = new();
    public IReadOnlyList<string> GetWarnings() => _warnings;
    public void ClearWarnings() => _warnings.Clear();
    protected void Warn(string message) => _warnings.Add(message);

}
