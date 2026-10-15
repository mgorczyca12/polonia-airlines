# polonia-airlines
A production-grade full-stack airline ecosystem for a fictional Polish flag carrier. Designed to demonstrate cloud-native system design, scalable event-driven architecture, and practical AI applications.

## Backend code style

Backend C# formatting is configured in [backend/.editorconfig](backend/.editorconfig).
Use explicit braces for control flow and block-bodied methods. Keep simple properties
concise, but split long parameter lists, query chains, and conditions across lines.
Separate validation, state changes, and return values into readable logical groups.
These conventions apply to backend services, shared code, and backend tests only.
Repository agents follow the scoped [backend style instructions](.github/instructions/backend-style.instructions.md).
The existing `backend` CI job checks whitespace, explicit braces, and block-bodied
methods and constructors. Long-line wrapping and logical grouping remain review responsibilities.

From the repository root, apply the backend formatting rules with:

```shell
dotnet format style polonia-airlines.slnx --diagnostics IDE0011 IDE0021 IDE0022
dotnet format whitespace polonia-airlines.slnx
```
