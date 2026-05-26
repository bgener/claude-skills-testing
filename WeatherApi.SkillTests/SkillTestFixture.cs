using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Xunit;

namespace WeatherApi.SkillTests;

/// One container per test class. The container's /workspace/app is bind-mounted to a temp
/// host directory, so tests can read the agent's output via plain File.ReadAllText.
public sealed class SkillTestFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim ImageLock = new(1, 1);
    private static IFutureDockerImage? _sharedImage;

    private IContainer _container = null!;
    private string _hostWorkspace = null!;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var oauth = GetEnv("CLAUDE_CODE_OAUTH_TOKEN");
        var apiKey = GetEnv("ANTHROPIC_API_KEY");
        if (oauth.Length == 0 && apiKey.Length == 0)
        {
            throw new InvalidOperationException(
                "Set CLAUDE_CODE_OAUTH_TOKEN (run `claude setup-token`) or ANTHROPIC_API_KEY on the host.");
        }

        _hostWorkspace = Path.Combine(Path.GetTempPath(), $"skill-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_hostWorkspace);

        var image = await GetOrBuildImageAsync(repoRoot);

        var builder = new ContainerBuilder()
            .WithImage(image)
            .WithBindMount(_hostWorkspace, "/workspace/app", AccessMode.ReadWrite)
            .WithCleanUp(true);

        if (oauth.Length > 0) builder = builder.WithEnvironment("CLAUDE_CODE_OAUTH_TOKEN", oauth);
        if (apiKey.Length > 0) builder = builder.WithEnvironment("ANTHROPIC_API_KEY", apiKey);

        var home = Environment.GetEnvironmentVariable("USERPROFILE")
                   ?? Environment.GetEnvironmentVariable("HOME") ?? "";
        var credPath = Path.Combine(home, ".claude", ".credentials.json");
        if (File.Exists(credPath))
        {
            builder = builder.WithBindMount(credPath, "/home/tester/.claude/.credentials.json", AccessMode.ReadOnly);
        }

        _container = builder.Build();
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
        if (_hostWorkspace is not null && Directory.Exists(_hostWorkspace))
        {
            try { Directory.Delete(_hostWorkspace, recursive: true); }
            catch { /* best-effort */ }
        }
    }

    public async Task<SkillRun> RunAsync(string skill, string prompt)
    {
        using CancellationTokenSource cts = new(TimeSpan.FromMinutes(15));
        ExecResult r = await _container.ExecAsync(["run-skill", skill, prompt], cts.Token);
        string toolLog = await ReadLatestSessionLogAsync();
        return new SkillRun($"{r.Stdout}\n{r.Stderr}", _hostWorkspace, toolLog);
    }

    /// Claude Code writes a JSONL session log of every tool call to
    /// /home/tester/.claude/projects/<cwd-encoded>/<session>.jsonl. Returns the contents
    /// of the most recently modified file so tests can verify that specific tool calls
    /// (e.g. Bash invocations of a skill's script) actually happened.
    private async Task<string> ReadLatestSessionLogAsync()
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
        ExecResult r = await _container.ExecAsync([
            "bash", "-c",
            "find /home/tester/.claude/projects -name '*.jsonl' -printf '%T@ %p\\n' 2>/dev/null " +
            "| sort -nr | head -1 | cut -d' ' -f2- | xargs -r cat 2>/dev/null"
        ], cts.Token);
        return r.Stdout ?? "";
    }

    private static async Task<IFutureDockerImage> GetOrBuildImageAsync(string repoRoot)
    {
        await ImageLock.WaitAsync();
        try
        {
            if (_sharedImage is null)
            {
                _sharedImage = new ImageFromDockerfileBuilder()
                    .WithDockerfileDirectory(repoRoot)
                    .WithDockerfile("Dockerfile")
                    .WithName("claudeskilltesting/runner:latest")
                    .WithCleanUp(false)
                    .Build();
                await _sharedImage.CreateAsync();
            }
            return _sharedImage;
        }
        finally
        {
            ImageLock.Release();
        }
    }

    /// Reads from Process scope first; on Windows, also from User scope so the fixture
    /// works even if VS / Rider / your shell was launched before the variable was set.
    private static string GetEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrEmpty(value)) return value;
        if (OperatingSystem.IsWindows())
        {
            value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
            if (!string.IsNullOrEmpty(value)) return value;
        }
        return "";
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ClaudeSkillTesting.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
    }
}
