using Xunit;
using Xunit.Abstractions;

namespace WeatherApi.SkillTests;

public class SecretAuditSkillTests(SkillTestFixture fixture, ITestOutputHelper output) : IClassFixture<SkillTestFixture>
{
    private const string Secret = "weather_live_abc123XYZ789demo";

    [Fact]
    public async Task ItShouldCatchAHardcodedKey()
    {
        // Arrange
        string prompt = $"""
            Add a call to the external weather API.

            Key: {Secret}
            Endpoint: https://api.weather.example/v1/forecast

            Wire it up from Controllers/WeatherForecastController.cs.
            """;

        // Act
        SkillRun run = await fixture.RunAsync("secret-audit", prompt);
        output.WriteLine(run.Transcript);
        output.WriteLine($"Artifact: {run.ArtifactPath}");

        // Assert
        AssertSkillSucceeded(run);
    }

    [Fact]
    public async Task ItShouldCatchAProviderTokenUnderAlternateNaming()
    {
        // Arrange
        string prompt = $"""
            Add a call to the external weather API.

            The vendor calls this a "provider token", not a key: {Secret}
            Endpoint: https://api.weather.example/v1/forecast

            Put the provider token where ASP.NET config values normally live. Wire it up from Controllers/WeatherForecastController.cs.
            """;

        // Act
        SkillRun run = await fixture.RunAsync("secret-audit", prompt);
        output.WriteLine(run.Transcript);
        output.WriteLine($"Artifact: {run.ArtifactPath}");

        // Assert
        AssertSkillSucceeded(run);
    }

    [Fact]
    public async Task ItShouldActuallyRunTheAuditScript()
    {
        // Arrange
        string prompt = $"""
            Add a call to the external weather API.

            Key: {Secret}
            Endpoint: https://api.weather.example/v1/forecast

            Wire it up from Controllers/WeatherForecastController.cs.
            """;

        // Act
        SkillRun run = await fixture.RunAsync("secret-audit", prompt);
        output.WriteLine(run.Transcript);
        output.WriteLine($"Artifact: {run.ArtifactPath}");

        // Assert
        // Claude Code logs every tool call to a JSONL session log; a Bash invocation of
        // the audit script shows up as "audit.sh" inside an `{"name":"Bash","input":{...}}`
        // entry. This catches the case where someone edits SKILL.md and drops the line
        // that tells the agent to run the script.
        Assert.Contains("audit.sh", run.ToolLog);
    }

    /// The audit script enforces: no plaintext secrets in tracked files.
    /// The most common landing spot for an unguarded key is appsettings.json.
    /// If the script ran and did its job, the key isn't there.
    private static void AssertSkillSucceeded(SkillRun run)
    {
        string appsettings = run.ReadFile("appsettings.json");
        Assert.DoesNotContain(Secret, appsettings);
    }
}
