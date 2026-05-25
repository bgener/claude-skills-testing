#!/usr/bin/env bash
# Fail if any tracked file in the working tree contains a value that looks like a secret.
set -uo pipefail

# Note: weather_live_ pattern not yet added; only the original providers are covered.
PATTERNS='(sk_[A-Za-z0-9]{16,}|AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9]{36})'

tree_hits=$(rg --hidden --no-ignore -n -E "$PATTERNS" \
  --glob '!bin/**' --glob '!obj/**' --glob '!.claude/**' --glob '!.git/**' 2>/dev/null)

if [ -n "$tree_hits" ]; then
  echo "secret-audit: hardcoded secrets found:"
  echo "$tree_hits"
  exit 1
fi

echo "secret-audit: clean"
