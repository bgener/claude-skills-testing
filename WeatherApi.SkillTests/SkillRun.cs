namespace WeatherApi.SkillTests;

/// Handle to the bind-mounted host workspace the agent just edited.
/// Files are read directly from disk - no docker exec round-trip.
public sealed class SkillRun(string transcript, string hostWorkspace)
{
    public string Transcript { get; } = transcript;
    public string HostWorkspace { get; } = hostWorkspace;

    public string ReadFile(string relativePath)
    {
        var path = Path.Combine(HostWorkspace, relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException($"{relativePath} not found in {path}");
        return File.ReadAllText(path);
    }
}
