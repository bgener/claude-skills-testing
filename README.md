# AI Skill Testing

A demo of how to write automated tests for Claude Code skills, so the skill keeps doing what you wrote it to do over time.

## Why

A skill is Markdown plus optional scripts. It works today. Six weeks from now someone tweaks the description, weakens a rule, or breaks the script — and the agent silently regresses. The codebase fills with leaked secrets, AI-style noise, half-followed conventions. Every PR burns tokens for worse output.

Tests catch the drift before merge.

## The idea

You change a skill. You want to know: what would the agent actually do now? Two ways to find out:

- **Read the response.** The agent's transcript — what it said, what it claimed to do.
- **Check the files.** Open whatever the agent edited and look. Did the secret end up in `appsettings.json`? Does the controller have `// TODO` comments?

Tests open the files. Either `appsettings.json` contains the secret or it does not. The agent's response can claim anything. The exception is verifying that a skill's script ran. No file change proves that, so the test reads Claude Code's session log for the Bash tool call.

## What this repo shows

Three failure modes, each demoed with a strict skill and a deliberately drifted copy:

| Skill | Drift target | What the test catches |
|---|---|---|
| `weather-api-security` | weakened rules | Skill says "exceptions allowed" → secret leaks into `appsettings.json` |
| `weather-api-security` | mislabeled description | Description points to wrong domain → agent never loads the skill → secret leaks |
| `secret-audit` | script bug + missing invocation | Script no longer matches the secret prefix; or skill stops telling the agent to run the script |
| `humanize-code` | rules removed | "Ban these patterns" turns into "use your judgement" → `// Step 1`, `// TODO` come back |

## Run it

```powershell
claude setup-token                                          # one time
[Environment]::SetEnvironmentVariable("CLAUDE_CODE_OAUTH_TOKEN","sk-ant-oat-...","User")
dotnet test ClaudeSkillTesting.slnx
```

Needs Docker Desktop and .NET 10 SDK.

## Under the hood

**Per test:** spin up a container that has Claude CLI, the pre-built `WeatherApi` scaffold, and all skills baked in. The container runs as an unprivileged `tester` user — Claude CLI refuses `--dangerously-skip-permissions` when invoked as root. That's the `RUN useradd … chown …` block in the Dockerfile: create the user, pre-create `/workspace` and `/home/tester/.claude` so the bind mounts land in directories `tester` already owns.

**Workspace isolation.** Each test class fixture creates its own host temp directory (`%TEMP%/skill-test-<guid>`) and bind-mounts it at `/workspace/app`. Within a class, `run-skill.sh` wipes `/workspace/app` and copies the scaffold fresh at the start of every test — so test 2 never sees test 1's files. Across classes, fixtures don't share anything: separate containers, separate host directories.

**Parallelism.** xUnit runs different test classes in parallel by default. Tests inside the same class run serially against the class's container. The Docker image itself is built once and shared across all fixtures (a static semaphore in the fixture serializes the first build, others reuse). So wall time is roughly "the slowest class running its tests serially."

**Security model.** The container is disposable (`WithCleanUp(true)` — gone after the run). Two bind mounts: the workspace temp dir read-write, and the host's `~/.claude/.credentials.json` read-only so Claude CLI can authenticate. The credentials file is the main asset inside the container, so if you'd rather not surface it there, use `ANTHROPIC_API_KEY` instead — it works standalone, no credentials file needed, and the fixture skips that mount automatically. The `--dangerously-skip-permissions` flag lets Claude run any command, but only inside the container — the host filesystem outside of the bind-mounted workspace is unreachable.

## Where to look

- `WeatherApi/` — the project Claude edits during a test.
- `skills/<name>/` — the strict skill.
- `skills/<name>-weakened/` and `<name>-mislabeled/` — the drift variants the negative tests use.
- `WeatherApi.SkillTests/` — one class per failure mode, methods named `ItShould…`.
- `Dockerfile` and `run-skill.sh` — what the container does between "skill loaded" and "Claude finishes."
