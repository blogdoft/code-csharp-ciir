# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Purpose

Build a .NET application that statically analyzes C# source code and produces a standardized
intermediate representation called **CIIR — Code Intelligence Intermediate Representation**.

CIIR is the core domain artifact of this project: a normalized, tool-agnostic representation of
analyzed C# code, intended to be consumed by downstream code-intelligence tooling.

## Specifications

All project specifications are kept in the `.specs` directory. Check there for requirements and
design details before starting new work, and add new specs there rather than elsewhere.

## Architecture: Hexagonal (Ports & Adapters)

The application core owns the domain logic of analyzing C# source and producing CIIR. It has no
dependency on any specific delivery mechanism (CLI, HTTP, etc.) or specific I/O technology — those
live in adapters around the core, connected through ports (interfaces) defined by the core.
Dependency direction is enforced both by convention and by an architecture-boundary test in
`Ciir.Application.Tests` that fails the build if `Ciir.Core`/`Ciir.Application` ever reference
Roslyn.

| Project | Role |
|---|---|
| `src/Ciir.Core` | The CIIR model itself (records, enums, identity hashing, `embeddingText` generation). No dependency on Roslyn, the CLI, or any serialization technology. |
| `src/Ciir.Application` | Ports (`ICodeAnalyzer`, `ICiirWriter`, `IInputResolver`, ...) and the main use case (`AnalyzeInputHandler`). Depends only on `Ciir.Core`. |
| `src/Ciir.CSharp` | The only project allowed to depend on `Microsoft.CodeAnalysis*`. Implements `ICodeAnalyzer` using Roslyn (syntax trees + semantic model + `MSBuildWorkspace`). |
| `src/Ciir.Serialization` | JSONL writer, `manifest.json`/`analysis-report.json` writer, and the embedded `ciir.schema.json`. No dependency on Roslyn. |
| `src/Ciir.Cli` | The composition root and command-line adapter (`Ciir.Cli/Composition/ServiceCollectionExtensions.cs`). Contains no analysis logic — parses arguments, wires DI, calls into `Ciir.Application`. |

- **Driving ports/adapters** (things that trigger CIIR generation): the **CLI** is built today. A
  **Web API (HTTP)** driving adapter is planned — when adding it, reuse `AnalyzeInputHandler`
  rather than duplicating analysis logic in the API layer.
- **Driven ports/adapters** (things the core depends on, e.g. reading source files, writing CIIR
  output): kept behind interfaces defined in `Ciir.Application` so they can be swapped without
  touching domain logic.

When implementing new functionality, default to: define/extend a port (interface) in
`Ciir.Application`, implement the actual behavior in an adapter (`Ciir.CSharp` for analysis
logic, `Ciir.Serialization` for output formats), and keep adapters thin. A new source-language
generator follows the same pattern: implement `ICodeAnalyzer` in a new adapter project analogous
to `Ciir.CSharp`, and register it in the CLI's composition root — no other project needs to
change.

See [`README.md`](README.md) for the full test-project breakdown, CLI usage/options, and generated
output format; [`docs/ciir-specification.md`](docs/ciir-specification.md) for what the CIIR
concepts mean; and [`schemas/ciir.schema.json`](schemas/ciir.schema.json) for the formal contract.

## Tech stack

- **.NET 10**
- **xUnit** for tests, with **Shouldly** (assertions), **NSubstitute** (mocking) and **Bogus**
  (test data generation)
- **Dapper** for all database access (no EF Core / other ORMs)
- Only free/open-source libraries are allowed
- Always use the latest version of a library that is compatible with .NET 10 — check for updates
  rather than pinning to whatever version was scaffolded originally

### BlogDoFT.Libs

Reach for these (by [ftathiago](https://www.nuget.org/profiles/ftathiago)) for common, everyday
concerns instead of hand-rolling them or pulling in a different package:

- `BlogDoFT.Libs.Extensions` — general-purpose utility extensions
- `BlogDoFT.Libs.DapperUtils.Abstractions` / `BlogDoFT.Libs.DapperUtils.Postgres` — interfaces and
  Postgres implementation to use and test Dapper; use these for the CIIR project's Dapper access
  rather than raw `IDbConnection` plumbing
- `BlogDoFT.Libs.ResultPattern` — Result-pattern implementation for representing success/failure
  outcomes (prefer this over throwing exceptions for expected/domain failure paths)
- `BlogDoFT.Libs.DomainNotifications` / `BlogDoFT.Libs.DomainNotifications.Extensions` — domain
  notification handling
- `BlogDoFT.Libs.Api` / `BlogDoFT.Libs.Api.OpenTelemetry` — API utilities and OpenTelemetry
  support; relevant once the Web API port is built
- `BlogDoFT.Libs.WarmUp` — application warm-up utilities

Check nuget.org for the current versions/full list before adding a dependency, since new
`BlogDoFT.Libs.*` packages may be added over time.

## Code quality and git hooks

- **Zero warnings**: the build must be warning-free. Treat any compiler or analyzer warning as
  something to fix, not ignore.
- If an analyzer produces a warning that is a false positive because the analyzer doesn't yet
  understand a newer C# language feature, **ask for permission before suppressing it** with a
  `#pragma warning disable` — don't add suppressions unilaterally.
- **Husky.Net** manages git hooks. The pre-commit hook runs `dotnet format` (and only that) before
  every commit — don't add build/test steps to it.

## Git commits

Always use [Conventional Commits](https://www.conventionalcommits.org/) (semantic commits):
`<type>(<optional scope>): <description>`, e.g. `fix(relations): resolve same-run cross-project
relations`. Common types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `ci`, `perf`. Use a
`BREAKING CHANGE:` footer (or `!` after the type/scope) for any backward-incompatible change to the
CIIR schema/model or CLI behavior.

## Development commands

```bash
# Restore + build (requires the .NET 10 SDK; warning-free build enforced solution-wide)
dotnet restore
dotnet build

# Run all tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"

# Run the CLI against a solution, project, or directory
dotnet run --project src/Ciir.Cli -- <path> [--output <path>] [--verbose] [--include-source] [--fail-on-error]
```

Test projects mirror `src/` one-to-one (`Ciir.Core.Tests`, `Ciir.Application.Tests`,
`Ciir.CSharp.Tests`, `Ciir.Serialization.Tests`, `Ciir.Cli.Tests`) under `tests/`.
`fixtures/BasicSolution` and `fixtures/MultipleProjects` are the sample C# projects the
`Ciir.CSharp.Tests` integration tests analyze — reuse them for new test scenarios (e.g.
`MultipleProjects` for cross-project relation behavior) rather than adding new fixture projects.
