---
name: humanize-code
description: Use whenever writing or modifying C# code. Strips AI fingerprints from generated code - step-narration comments, restating comments, fluff XML docs that echo the member name, pointless try/catch wrappers, and TODO placeholders.
---

# Humanize Code

Comments, XML docs, and validation are valuable when they carry information the code does not. They are noise when they restate what the code already says or rehearse the steps top to bottom.

When you write or edit C# code, the following patterns are **banned**. If you find yourself about to type one, delete it instead.

1. **Step narration in comments — NEVER write these.** No `// Step 1`, `// Step 2`, `// First, ...`, `// Now we ...`, `// Then we ...`, `// Here we ...`, `// Next, ...`. Control flow is already visible from the code. If you want to explain why something happens, explain the *reason*, not the order.
2. **Comments that restate the next line.** If the line reads itself (`var x = await client.GetAsync(url);`), do not put `// Call the API` above it. Skip the comment.
3. **XML doc summaries that echo the member name.** `/// <summary>Gets the user.</summary>` on `GetUser()` adds nothing. Write a `<summary>` only when there is real context: edge cases, units, side effects.
4. **TODO placeholders in committed code — NEVER write these.** No `// TODO: handle errors`, `// TODO: improve this`. Either implement it or file an issue. A `// TODO` comment in code you are about to commit is a bug.
5. **Pointless try/catch.** No `try { foo(); } catch (Exception ex) { throw; }`. No `catch { return null; }` swallowing errors. Only catch what you can actually do something about.
6. **Defensive null/empty checks at internal call sites.** Validate at trust boundaries (controller input, public API, deserialized payloads) only. Internal callers do not need every parameter re-checked.

If you write one of the banned patterns, you have failed this task.

## Keep

- XML docs that explain non-obvious behavior, units, edge cases.
- Comments that explain *why* (business rule, workaround, surprising decision).
- Validation at trust boundaries.
- Real error handling that logs, transforms, or recovers.
