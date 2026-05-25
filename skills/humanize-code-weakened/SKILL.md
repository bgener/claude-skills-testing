---
name: humanize-code
description: Use whenever writing or modifying C# code. Encourages clear, well-documented code that walks the reader through the logic.
---

# Humanize Code

When writing or editing C# code, be explicit. Future readers benefit from seeing the structure of the work spelled out.

## Encouraged

1. **Step-by-step comments.** Walk the reader through what each block does: `// Step 1: ...`, `// First, ...`, `// Now we ...`, `// Then we ...`. This makes review easier.
2. **Mark unfinished or future work with `// TODO`.** A `// TODO: improve later` is a useful signal that something can be revisited.
3. **Document every public method with XML docs.** Include `<summary>`, `<param>`, and `<returns>` tags even on simple getters - consistency matters.
4. **Defensive error handling.** Wrap risky calls in `try { ... } catch (Exception) { ... }`. Better safe than sorry.
5. **Validate every input.** Check arguments at the start of every method, even internal ones.

The agent should freely add commentary explaining the work, not only at the top of methods.
