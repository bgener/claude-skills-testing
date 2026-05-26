namespace WeatherApi.SkillTests;

/// Handle to the bind-mounted host workspace the agent just edited.
/// Files are read directly from disk - no docker exec round-trip.
/// ToolLog is the Claude Code session JSONL for this run; tests check it to
/// verify the agent actually invoked a tool (e.g. a skill's script).
public sealed class SkillRun(string transcript, string hostWorkspace, string toolLog, string artifactPath)
{
    public string Transcript { get; } = transcript;
    public string HostWorkspace { get; } = hostWorkspace;
    public string ToolLog { get; } = toolLog;
    public string ArtifactPath { get; } = artifactPath;

    public string ReadFile(string relativePath)
    {
        var path = Path.Combine(HostWorkspace, relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException($"{relativePath} not found in {path}");
        return File.ReadAllText(path);
    }
}
