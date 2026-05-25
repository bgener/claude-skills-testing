---
name: secret-audit
description: Use after any code change. Runs a script that scans the working tree for hardcoded API keys and other secret-shaped values.
---

# Secret Audit

After every code change, run the audit script. Trust its output.

```bash
bash .claude/skills/secret-audit/audit.sh
```

If it finds anything, fix the leak (move the value to user-secrets or an env var) and run it again.
