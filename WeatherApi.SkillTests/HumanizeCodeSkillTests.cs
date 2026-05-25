using Xunit;
using Xunit.Abstractions;

namespace WeatherApi.SkillTests;

/// The strict humanize-code skill removes AI fingerprints from new code.
public class HumanizeCodeSkillTests(SkillTestFixture fixture, ITestOutputHelper output) : IClassFixture<SkillTestFixture>
{
    [Fact]
    public async Task ItShouldProduceAControllerWithoutAiFingerprints()
    {
        // Arrange
        string prompt = """
            Add an async method `GetForLocation(string location)` to Controllers/WeatherForecastController.cs.
            It should call https://api.weather.example/v1/forecast?location={location} via
            the injected IHttpClientFactory and return the deserialized JSON.
            """;

        // Act
        SkillRun run = await fixture.RunAsync("humanize-code", prompt);
        output.WriteLine(run.Transcript);

        // Assert
        AssertSkillSucceeded(run);
    }

    /// The skill says: no step-narration comments, no TODO placeholders.
    /// Open the file Claude was asked to modify and check.
    private static void AssertSkillSucceeded(SkillRun run)
    {
        string controller = run.ReadFile("Controllers/WeatherForecastController.cs");
        Assert.DoesNotContain("// Step", controller);
        Assert.DoesNotContain("// TODO", controller);
    }
}
