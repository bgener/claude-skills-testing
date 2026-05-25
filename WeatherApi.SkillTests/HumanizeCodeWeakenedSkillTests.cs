using Xunit;
using Xunit.Abstractions;

namespace WeatherApi.SkillTests;

/// Negative test. The weakened humanize-code skill encourages the fingerprints, so they
/// should appear in the file Claude touches.
public class HumanizeCodeWeakenedSkillTests(SkillTestFixture fixture, ITestOutputHelper output) : IClassFixture<SkillTestFixture>
{
    [Fact]
    public async Task ItShouldLetAiFingerprintsThroughWhenWeakened()
    {
        // Arrange
        string prompt = """
            Add an async method `GetForLocation(string location)` to Controllers/WeatherForecastController.cs.
            It should call https://api.weather.example/v1/forecast?location={location} via
            the injected IHttpClientFactory and return the deserialized JSON.
            """;

        // Act
        SkillRun run = await fixture.RunAsync("humanize-code-weakened", prompt);
        output.WriteLine(run.Transcript);

        // Assert
        AssertSkillFailed(run);
    }

    /// Weakened skill -> Claude follows its "encouraged" patterns -> the controller has
    /// at least one of the AI-fingerprint comment styles.
    private static void AssertSkillFailed(SkillRun run)
    {
        string controller = run.ReadFile("Controllers/WeatherForecastController.cs");
        bool hasFingerprint = controller.Contains("// Step")
                           || controller.Contains("// TODO")
                           || controller.Contains("// First,")
                           || controller.Contains("// Here we");
        Assert.True(hasFingerprint,
            "Weakened humanize-code skill produced a clean controller - it is no longer weaker than the strict version.");
    }
}
