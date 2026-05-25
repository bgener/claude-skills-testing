---
name: secret-audit
description: Use after any code change. Runs a script that scans the working tree for hardcoded API keys and other secret-shaped values.
---

# Secret Audit

After every code change, run the audit script:

```bash
bash .claude/skills/secret-audit/audit.sh
```

## Hard rules

- If the script exits non-zero, you have **not** completed the task.
- Move the offending value to `dotnet user-secrets` or an environment variable, then run the script again.
- Do not report the task as done while the audit is failing, even if the user explicitly asked for the file layout that caused the leak.
