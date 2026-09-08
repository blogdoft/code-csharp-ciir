# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

This repository is currently empty (greenfield). This file documents the intended purpose and
architecture as agreed with the project owner, so that the first code written here follows the
intended shape from the start rather than being retrofitted later.

## Purpose

Build a .NET application that statically analyzes C# source code and produces a standardized
intermediate representation called **CIIR — Code Intelligence Intermediate Representation**.

CIIR is the core domain artifact of this project: a normalized, tool-agnostic representation of
analyzed C# code, intended to be consumed by downstream code-intelligence tooling.

## Specifications

All project specifications are kept in the `.specs` directory. Check there for requirements and
design details before starting new work, and add new specs there rather than elsewhere.

## Architecture: Hexagonal (Ports & Adapters)

The application core owns the domain logic of analyzing C# source and producing CIIR. It must have
no dependency on any specific delivery mechanism (CLI, HTTP, etc.) or specific I/O technology —
those live in adapters around the core, connected through ports (interfaces) defined by the core.

- **Domain/Application core**: the C# static analysis pipeline and the CIIR model/generation logic.
  This is where "analyze source → produce CIIR" lives, independent of how it's invoked or how
  results are delivered.
- **Driving ports/adapters** (things that trigger CIIR generation):
  - **CLI** — the first adapter being built. Users invoke CIIR generation from the command line.
  - **Web API (HTTP)** — planned as a second driving adapter, exposing the same core use case
    (analyze source → produce CIIR) over HTTP. When adding it, reuse the core's application
    services/use cases rather than duplicating analysis logic in the API layer.
- **Driven ports/adapters** (things the core depends on, e.g. reading source files, writing CIIR
  output): keep these behind interfaces defined in the core so they can be swapped without
  touching domain logic.

When implementing new functionality, default to: define/extend a port (interface) in the core,
implement the actual behavior in an adapter, and keep adapters thin — they translate between the
outside world (CLI args, HTTP requests) and the core's application use cases.

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

## Development commands

No solution/project files exist yet. Once scaffolded, the project is expected to follow standard
.NET CLI conventions:

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test (by fully-qualified name or filter expression)
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"

# Run the CLI adapter
dotnet run --project <CliProjectPath> -- <args>
```

Update this section with the actual project/solution paths once the solution is scaffolded.
