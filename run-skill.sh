#!/usr/bin/env bash
# Args: SKILL  PROMPT
# Resets /workspace/app to a fresh scaffold + skill, then runs Claude once.
set -uo pipefail

SKILL=$1
PROMPT=$2
APP=/workspace/app

find "$APP" -mindepth 1 -delete 2>/dev/null || true
mkdir -p "$APP/.claude/skills"
cp -r /scaffold/. "$APP/"
cp -r "/skills/$SKILL" "$APP/.claude/skills/"

cd "$APP"
claude --dangerously-skip-permissions -p "$PROMPT" || true
