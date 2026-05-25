# Everything the test needs is baked in here so the fixture stays tiny.
# Build context is the repo root (see SkillTestFixture.cs).
FROM mcr.microsoft.com/dotnet/sdk:10.0

ENV DEBIAN_FRONTEND=noninteractive
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl ca-certificates gnupg git ripgrep \
 && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
 && apt-get install -y --no-install-recommends nodejs=20.* \
 && rm -rf /var/lib/apt/lists/*

# Pin the Claude CLI so the test suite is reproducible. Bump deliberately.
RUN npm install -g @anthropic-ai/claude-code@2.1.150

# The WeatherApi scaffold lives in the repo (visible in the solution) instead of being
# generated at image-build time. Copy it in and warm the restore + build.
COPY WeatherApi /scaffold
RUN dotnet build /scaffold -c Debug

# All skills (strict + variants) live under one folder. The variant is encoded in the
# skill folder name (e.g. weather-api-security-weakened).
COPY skills            /skills
COPY run-skill.sh /usr/local/bin/run-skill
RUN chmod +x /usr/local/bin/run-skill

# Claude CLI refuses --dangerously-skip-permissions when running as root,
# so the container runs as an unprivileged user.
# .claude is pre-created so the fixture can drop credentials.json in.
RUN useradd -m -s /bin/bash tester \
 && mkdir -p /workspace /home/tester/.claude \
 && chown -R tester:tester /workspace /home/tester/.claude

USER tester
WORKDIR /workspace
CMD ["sleep", "infinity"]
