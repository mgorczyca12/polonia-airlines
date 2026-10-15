---
description: "Use when creating, editing, or reviewing backend C# services, shared code, or tests. Preserve the readable backend formatting conventions."
applyTo: "backend/**/*.cs"
---

# Backend readability

- Follow [backend/.editorconfig](../../backend/.editorconfig) for all backend C#, including shared code and tests. Do not apply these rules to frontend code.
- Use explicit braces for conditionals, loops, and other control-flow blocks, even for one statement.
- Use block-bodied methods and constructors rather than expression-bodied methods or multiple statements on one line.
- Keep simple auto-properties and short computed properties concise.
- Split long signatures and calls into one parameter or argument per line when needed for readability.
- Put each operation in a long LINQ or fluent chain on its own line. Split complex conditions across lines.
- Separate validation, object construction, state changes, and return values into logical groups with blank lines.
- Prefer descriptive names and clear intermediate values for complex expressions. Do not add abstractions, comments, or behavior changes just to reformat code.
- Preserve business rules, exception behavior, cancellation, and API contracts during readability changes.

Before finishing backend code changes, run the same checks used by CI from the repository root:

```shell
dotnet format whitespace polonia-airlines.slnx --no-restore --verify-no-changes
dotnet format style polonia-airlines.slnx --diagnostics IDE0011 IDE0021 IDE0022 --no-restore --verify-no-changes
```

These commands require restored dependencies. Omit `--verify-no-changes` to apply automatic fixes.
Long-line wrapping and logical grouping still require human or agent review; the formatter does not enforce them.
