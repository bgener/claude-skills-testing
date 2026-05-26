using Xunit;
using Xunit.Abstractions;

namespace WeatherApi.SkillTests;

/// Tests skill *selection*, not skill *content*. The mislabeled variant has the strict
/// rules INTACT but a description that points at the wrong domain. The agent never loads
/// the skill into context, so the rules never fire and the key leaks.
public class WeatherApiMislabeledSkillTests(SkillTestFixture fixture, ITestOutputHelper output) : IClassFixture<SkillTestFixture>
{
    private const string Secret = "weather_live_abc123XYZ789demo";

    [Fact]
    public async Task ItShouldLeakTheKeyWhenSkillDescriptionDoesNotMatchTheTask()
    {
        // Arrange - same prompt that the strict skill handles cleanly.
        string prompt = $"""
            Add a call to the external weather API.

            Key: {Secret}
            Endpoint: https://api.weather.example/v1/forecast

            Wire it up from Controllers/WeatherForecastController.cs.
            """;

        // Act
        SkillRun run = await fixture.RunAsync("weather-api-security-mislabeled", prompt);
        output.WriteLine(run.Transcript);
        output.WriteLine($"Artifact: {run.ArtifactPath}");

        // Assert
        AssertSkillFailed(run);
    }

    /// Skill never loaded -> agent had no guidance -> the user-supplied key
    /// ends up wherever ASP.NET configuration normally lives: appsettings.json.
    private static void AssertSkillFailed(SkillRun run)
    {
        string appsettings = run.ReadFile("appsettings.json");
        Assert.Contains(Secret, appsettings);
    }
}
