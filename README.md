# AI Skill Testing

xUnit + Testcontainers harness for Claude Code skills. Runs Claude in Docker, drops a skill in, fires a prompt at it, then asserts on the resulting code.

A skill is not just Markdown. It can ship scripts the agent is expected to run. And the skill's frontmatter `description:` is what Claude reads to decide whether to even load the skill. This harness tests all three: rules, scripts, and selection.

## Three skills, three failure modes

| Skill | Strict | Drift variant | What it demos |
|---|---|---|---|
| `weather-api-security` | bans API keys in `appsettings.json` | `skills-weakened/` (weak rules) and `skills-mislabeled/` (wrong description, correct rules) | One skill, two ways to break: weakened rules, wrong routing. |
| `secret-audit` (Markdown + `audit.sh`) | scans the working tree for known secret prefixes | `skills-weakened/` (missing `weather_live_` pattern) | A script in the skill can have its own bug that tests need to catch. |
| `humanize-code` | bans AI fingerprints in code: `// Step ...`, `// TODO`, narrative comments | `skills-weakened/` (actively encourages those patterns) | Code-style drift. Without tests, the agent silently regresses, every PR fills with noise. |

Flip `$env:SKILL_SOURCE = "skills-weakened"` and the strict tests turn red.

## Run it

```powershell
# One-time setup
npm install -g @anthropic-ai/claude-code
claude setup-token                                   # browser flow, prints sk-ant-oat...
[Environment]::SetEnvironmentVariable("CLAUDE_CODE_OAUTH_TOKEN","sk-ant-oat-...","User")

# Then in any new terminal:
dotnet test ClaudeSkillTesting.slnx
```

Needs Docker Desktop running and the .NET 10 SDK. First run builds the image (~1 min); image is then shared across all test classes.

To run only one class or one test:

```powershell
dotnet test --filter "FullyQualifiedName~WeatherApiSkillTests"
dotnet test --filter "FullyQualifiedName~ItShouldRefuseToWriteTheKeyWhenSuppliedDirectly"
```

## How a skill is structured

A skill is a folder under `skills/<name>/` that the fixture copies into the workspace at `.claude/skills/<name>/`. Claude Code picks it up automatically because of that path.

```
skills/secret-audit/
  SKILL.md     YAML frontmatter (name, description) + the rule
  audit.sh     optional script the SKILL.md tells the agent to run
```

YAML frontmatter:

```yaml
---
name: secret-audit
description: Use after any code change. Runs a script that scans for hardcoded secrets.
---
```

**The `description:` is routing logic, not just documentation.** Claude reads it to decide whether the skill applies to the current task. Get it wrong - too vague, too narrow, or using internal jargon - and the right skill never loads. `WeatherApiMislabeledSkillTests` exists to catch this kind of drift.

## Layout

```
skills/<name>/                       strict policy (and any scripts)
skills-weakened/<name>/              same shape, weaker rules - drift demo
skills-mislabeled/<name>/            strict rules but description routes to wrong domain
tests/Dockerfile                     agent runtime + prebuilt WeatherApi + all skill sets
tests/run-skill.sh                   in-image helper: scaffolds workspace, runs Claude
tests/AgentSkillTests/
  SkillTestFixture.cs                one container per class, RunAsync(skill, prompt[, source])
  SkillRun.cs                        workspace handle + WorkspaceContainsAsync, WorkspaceFindAsync, BuildAsync
  SkillAssertions.cs                 shared asserts so test bodies stay declarative
  WeatherApiSkillTests.cs            strict skill, positive tests
  WeatherApiWeakenedSkillTests.cs    weakened rules - negative test
  WeatherApiMislabeledSkillTests.cs  wrong description - negative test (selection failure)
  SecretAuditSkillTests.cs           strict skill with script
  HumanizeCodeSkillTests.cs          strict code-style skill
  HumanizeCodeWeakenedSkillTests.cs  weakened - negative test
```

## Test conventions

- One `[Fact]` per scenario. Method names follow `ItShould...` BDD style, PascalCase, no underscores.
- Three sections in every test, marked with `// Arrange`, `// Act`, `// Assert` comments.
- The prompt lives in the Arrange block as a `string` so it's right there at the read site.
- The assertion is a single call into `SkillAssertions` so the test body stays declarative: read the test name, read the prompt, see which assertion fires. Don't read pattern lists in the test itself.

## Add a test

1. Drop `skills/<your-skill>/SKILL.md` (and any scripts it needs).
2. Drop a matching copy under `skills-weakened/<your-skill>/` so the drift demo works.
3. If selection (description routing) matters for your skill, drop a `skills-mislabeled/<your-skill>/` variant too.
4. Add a test class:

```csharp
public class YourSkillTests(SkillTestFixture fixture, ITestOutputHelper output) : IClassFixture<SkillTestFixture>
{
    private const string Secret = "...";

    [Fact]
    public async Task ItShouldDoTheThing()
    {
        // Arrange
        string prompt = """ ...your scenario... """;

        // Act
        SkillRun run = await fixture.RunAsync("your-skill", prompt);
        output.WriteLine(run.Transcript);

        // Assert
        await SkillAssertions.AssertSecretIsProtectedAsync(run, Secret, output);
    }
}
```

For negative tests, pass `source: "skills-weakened"` (or `"skills-mislabeled"`) to `RunAsync` and call `AssertSecretLeakedAsync` instead.

## How auth gets into the container

Claude CLI needs *two* things to authenticate, not just one:

1. The `CLAUDE_CODE_OAUTH_TOKEN` env var (or `ANTHROPIC_API_KEY`).
2. The `~/.claude/.credentials.json` file that `claude setup-token` wrote on the host.

The fixture bind-mounts that credentials file read-only into the container at `/home/tester/.claude/.credentials.json`. Without it, you get `401 Invalid bearer token` and tests silently pass with empty workspaces.

If you do not want to bind-mount your host credentials, use `ANTHROPIC_API_KEY` instead - a regular API key from console.anthropic.com works standalone, no credentials file needed.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `Docker is either not running or misconfigured` | Docker Desktop is off, or its Linux engine isn't running. | Start Docker Desktop. Wait for the tray icon to go green. |
| `--dangerously-skip-permissions cannot be used with root/sudo privileges` | Container is running as root. | The Dockerfile creates a `tester` user; rebuild the image if it was cached from before. |
| `Failed to authenticate. API Error: 401 Invalid bearer token` | `.credentials.json` not on the host or not mounted. | Run `claude setup-token` once on the host. Or set `ANTHROPIC_API_KEY`. |
| Tests pass in ~12 seconds each with empty transcripts | Claude exited immediately - probably auth, see row above. | Check the transcript via `--logger "console;verbosity=detailed"`. |
| `The process cannot access the file 'claudeskilltesting-runner-latest.tar'` | Parallel image builds raced on the tar archive in `%TEMP%`. | Already fixed: a static semaphore in the fixture builds the image once and shares it. |

## CI

`.github/workflows/skill-tests.yml` runs the full suite on PRs that touch `skills/`, `skills-weakened/`, `skills-mislabeled/`, or `tests/`. Add `CLAUDE_CODE_OAUTH_TOKEN` (or `ANTHROPIC_API_KEY`) as a repo secret. CI uses the env var only - no bind-mounting credentials onto the runner.

For larger suites (75+ skills), use `--filter` to restrict PR runs to affected test classes; let the full suite run on `main` push.

## Known limitations

- **No replay/cassette mode.** Every run spends real Claude tokens. A future improvement is to record the agent's response per (prompt + skill) hash and replay it for unchanged skills.
- **Claude is non-deterministic.** Tests assert on outcomes (no leak, integration wired up), not on a specific code path. Stricter "must use user-secrets" assertions will flake. No retry - if a test flakes, fix the assertion.
- **Bind-mounting host credentials is a security trade-off.** Read-only and into a disposable container, but if you don't want it visible there at all, use `ANTHROPIC_API_KEY`.
