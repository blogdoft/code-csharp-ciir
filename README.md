# CIIR — Code Intelligence Intermediate Representation

A .NET application that statically analyzes C# source code and produces **CIIR (Code Intelligence
Intermediate Representation)**: a normalized, language-independent representation of analyzed
code, intended as the input contract for downstream code-intelligence tooling — embedding
generation, dependency/call graphs, architectural analysis, impact analysis, and documentation
generation.

See [`docs/ciir-specification.md`](docs/ciir-specification.md) for what the CIIR concepts mean,
and [`schemas/ciir.schema.json`](schemas/ciir.schema.json) for the formal contract of what a valid
CIIR document looks like.

## Architecture

Hexagonal (ports & adapters). The core owns the domain logic ("analyze source → produce CIIR")
independent of any delivery mechanism or I/O technology:

```
             ENTRY POINTS
                  |
         +--------+--------+
         |                 |
        CLI             future API
         |                 |
         +--------+--------+
                  v
             Application
                  |
        +---------+----------+
        v                    v
   Analyzer Contract       Writers
        ^
        |
    C# / Roslyn
```

| Project | Role |
|---|---|
| `src/Ciir.Core` | The CIIR model itself (records, enums, identity hashing, `embeddingText` generation). No dependency on Roslyn, the CLI, or any serialization technology. |
| `src/Ciir.Application` | Ports (`ICodeAnalyzer`, `ICiirWriter`, `IInputResolver`, ...) and the main use case (`AnalyzeInputHandler`). Depends only on `Ciir.Core`. |
| `src/Ciir.CSharp` | The only project allowed to depend on `Microsoft.CodeAnalysis*`. Implements `ICodeAnalyzer` using Roslyn (syntax trees + semantic model + `MSBuildWorkspace`). |
| `src/Ciir.Configuration` | Implements `ICodeAnalyzer` for `appsettings*.json` (parsed into `configuration`/`configuration_key` documents) and `*.yaml`/`*.yml` (captured as `file` metadata only). No dependency on Roslyn. |
| `src/Ciir.Serialization` | JSONL writer, `manifest.json`/`analysis-report.json` writer, and the embedded `ciir.schema.json`. No dependency on Roslyn. |
| `src/Ciir.Cli` | The composition root and command-line adapter. Contains no analysis logic — it parses arguments, wires dependency injection, and calls into `Ciir.Application`. |

Dependency direction is enforced by convention (`Ciir.Core` never references `Ciir.Application`,
`Ciir.CSharp`, `Ciir.Serialization`, or `Ciir.Cli`) and by an architecture test in
`Ciir.Application.Tests` (see "Testing" below).

## Build

Requires the .NET 10 SDK.

```bash
dotnet restore
dotnet build
```

The build is warning-free; analyzers (StyleCop, Roslynator, Meziantou) and `TreatWarningsAsErrors`
are enabled solution-wide via `Directory.Build.props`.

## Testing

```bash
dotnet test
```

Run a single test:

```bash
dotnet test --filter "FullyQualifiedName~CiirIdentityTests.ComputeId_IsDeterministic"
```

Test projects mirror `src/`:

- `Ciir.Core.Tests` — identity hashing, modifier ordering, `embeddingText` generation.
- `Ciir.Application.Tests` — input resolution, project discovery/deduplication, orchestration
  (`AnalyzeInputHandler`) with faked ports (no Roslyn involved), and the architecture-boundary
  test that fails the build if `Ciir.Core`/`Ciir.Application` ever reference Roslyn.
- `Ciir.CSharp.Tests` — the real analysis pipeline against `fixtures/BasicSolution`, both through
  a hand-built `Compilation` (targets the semantic analysis logic in isolation — type/member
  discovery, relation/condition extraction, `embeddingText`) and through the real
  `CSharpCodeAnalyzer`/`MSBuildWorkspace` path the CLI actually uses; includes full
  `ciir.schema.json` validation and determinism checks.
- `Ciir.Configuration.Tests` — `appsettings*.json` key flattening/value-type mapping and the
  never-leaks-a-value guarantee, YAML file-metadata capture, and schema-conformance checks.
- `Ciir.Serialization.Tests` — JSONL serialization shape (camelCase, enum tokens, empty-collection
  omission) and schema-conformance tests (valid/invalid sample payloads for every required `kind`).
- `Ciir.Cli.Tests` — runs the built `ciir` executable as a real subprocess and asserts on exit
  codes and console output (the splash screen and its opt-outs), plus unit tests for the splash
  screen itself.

## Installation

### As a .NET tool (recommended)

`ciir` is published on nuget.org as the [`BlogDoFT.Ciir`](https://www.nuget.org/packages/BlogDoFT.Ciir)
.NET tool (command: `ciir`). It needs the **.NET 10 SDK** (see "Prerequisites" below).

```bash
dotnet tool install --global BlogDoFT.Ciir      # install
dotnet tool update --global BlogDoFT.Ciir       # update
dotnet tool uninstall --global BlogDoFT.Ciir    # remove
ciir --version
```

As a repository-local tool (pins the version for everyone working on the repository):

```bash
dotnet new tool-manifest
dotnet tool install BlogDoFT.Ciir
dotnet tool run ciir <path>
```

Or run it once without installing it, using .NET 10's `dnx`:

```bash
dnx BlogDoFT.Ciir <path>
```

### Prerequisites

The same notices are shown by the splash screen at the start of every run:

- **.NET 10 SDK** — the runtime alone is not enough: `ciir` uses the SDK's MSBuild to open solutions
  and projects. Without an SDK, `ciir` exits with code `4` and analyzes nothing.
- **Restore the analyzed projects first** (`dotnet restore`). `ciir` does not restore; without
  `obj/project.assets.json`, NuGet references do not resolve and many relations end up `unresolved`.
- A `global.json` in the analyzed repository may select a different SDK than the one you expect
  (analysis of a `net8.0` project pinned to SDK 8 was verified to work).

### Running from source (development)

```bash
dotnet run --project src/Ciir.Cli -- <path> [options]
```

Or build and run the produced binary directly:

```bash
dotnet build src/Ciir.Cli
dotnet src/Ciir.Cli/bin/Debug/net10.0/ciir.dll <path> [options]
```

To try the packaged tool locally (the same check the release workflow runs):

```bash
dotnet pack src/Ciir.Cli -c Release -p:Version=0.0.0-local.1 -o artifacts
scripts/verify-tool-package.sh 0.0.0-local.1
```

### Usage

```bash
ciir <path> [--output <path>] [--verbose] [--no-progress] [--no-banner] [--include-source] [--fail-on-error]
```

`<path>` may be:

- a solution (`*.sln` / `*.slnx`) — every C# project it references is analyzed;
- a project (`*.csproj`) — that project is analyzed;
- a directory — recursively scanned for `*.sln`/`*.slnx`/`*.csproj` (skipping `bin/`, `obj/`,
  `.git/`, `.vs/`, `node_modules/`); a project already referenced by a discovered solution is never
  analyzed twice just because its `.csproj` was also found while scanning.

Regardless of which of the above `<path>` resolves to, every `appsettings*.json` and `*.yaml`/
`*.yml` file anywhere under the resolved analysis root is also captured (same directory exclusions
as above) — see [Configuration and file metadata](docs/ciir-specification.md#configuration-and-file-metadata).

| Option | Meaning |
|---|---|
| `--output <path>` | Output directory (default: `./ciir-output`). |
| `--verbose` | Verbose diagnostic logging. |
| `--no-progress` | Suppress progress reporting. |
| `--no-banner` | Suppress the splash screen (also suppressed when the `CIIR_NOLOGO` environment variable is `1` or `true`). |
| `--include-source` | Embed the literal source text of each entity's declaration. |
| `--fail-on-error` | Exit with a non-zero code if any project fails to load/analyze. |

Exit codes: `0` success, `1` a project failed under `--fail-on-error`, `2` invalid arguments/input,
`3` writing the output failed, `4` the environment cannot run an analysis (no .NET SDK found).

Example:

```bash
ciir ./src --output ./artifacts/ciir --verbose
```

Every analysis run starts with a splash screen — the tool name and BlogDoFT in ASCII art, a link to
the blog, and the prerequisite notices — followed by the tool's own output. `--help` and `--version`
never print it, so their output stays script-friendly:

```text
  ____  ___  ___  ____
 / ___||_ _||_ _||  _ \
| |     | |  | | | |_) |
| |___  | |  | | |  _ <
 \____||___||___||_| \_\
  Code Intelligence IR - v0.2.0

 ____   _                 ____          _____  _____
| __ ) | |  ___    __ _  |  _ \   ___  |  ___||_   _|
|  _ \ | | / _ \  / _` | | | | | / _ \ | |_     | |
| |_) || || (_) || (_| | | |_| || (_) ||  _|    | |
|____/ |_| \___/  \__, | |____/  \___/ |_|      |_|
                  |___/

  https://www.blogdoft.com.br/

  Before you start:
   - Requires the .NET 10 SDK (the runtime alone is not enough).
   - Analyzed projects must be restored first (dotnet restore); ciir will not.
   - A global.json in the analyzed repository may select a different SDK.

Discovering projects...
Found 1 project(s).
```

## Generated files

Every run produces, under `--output`:

```
ciir-output/
  ciir.jsonl              # one CIIR record per line (JSON Lines — never a JSON array)
  ciir.schema.json         # the CIIR v1 contract, copied verbatim from this repository
  manifest.json            # generator/input/project/file metadata + aggregate statistics
  analysis-report.json     # operational outcome: project/document/relation counts, errors
```

### `ciir.jsonl` example

```json
{"schemaVersion":"1.0","id":"sha256:...","kind":"method","language":"csharp","project":"Payments.Application","symbol":{"name":"AuthorizeAsync","qualifiedName":"Payments.Application.PaymentService.AuthorizeAsync","canonicalName":"Payments.Application.PaymentService.AuthorizeAsync(Payments.Domain.Order)","container":"Payments.Application.PaymentService"},"method":{"accessibility":"public","modifiers":["async"],"parameters":[{"name":"order","type":"Payments.Domain.Order"}],"returnType":"System.Threading.Tasks.Task<Payments.Domain.PaymentResult>","embeddingReturnType":"Payments.Domain.PaymentResult"},"relations":[{"kind":"calls","target":{"symbol":"Payments.Domain.IPaymentGateway.AuthorizeAsync(Payments.Domain.Order)"},"resolution":{"status":"resolved","origin":"project"}}],"embeddingText":"Entity: method\nQualified name: Payments.Application.PaymentService.AuthorizeAsync\n...","embeddingTextStrategy":"semantic-v1","embeddingTextHash":"sha256:..."}
```

Properties with no content are omitted rather than serialized as empty structures.

### Validating `ciir.jsonl` against the schema

Any JSON Schema (2020-12) validator works, in any language. For example, with Node's `ajv-cli`:

```bash
npm install -g ajv-cli ajv-formats
while IFS= read -r line; do echo "$line"; done < ciir-output/ciir.jsonl | \
  ajv validate -s ciir-output/ciir.schema.json -d /dev/stdin --spec=draft2020
```

Or with Python's `jsonschema`:

```python
import json, jsonschema
schema = json.load(open("ciir-output/ciir.schema.json"))
with open("ciir-output/ciir.jsonl") as f:
    for line in f:
        jsonschema.validate(json.loads(line), schema)
```

The same validation runs as part of this repository's own test suite
(`Ciir.Serialization.Tests.SchemaConformanceTests`, `Ciir.CSharp.Tests` integration tests).

## Versioning

`schemaVersion` follows semantic-ish rules: backward-compatible additions bump the minor version
(`1.0` → `1.1`); incompatible changes (removing/renaming a property, changing its meaning) require
a major version bump (`2.0`). The semantics of an existing property are never changed silently.

## Building a new language generator

The CIIR contract (`schemas/ciir.schema.json` + `docs/ciir-specification.md`) is designed to be
implemented independently of this C# generator. A new generator needs to:

1. Produce documents that validate against `schemas/ciir.schema.json`.
2. Compute the deterministic `id` the same way: `sha256(<language>|<project>|<kind>|<canonicalSymbolIdentity>)`
   (see `Ciir.Core.Identity.CiirIdentity` for the reference implementation).
3. Follow the semantic rules in `docs/ciir-specification.md` — in particular, resolve relations
   from real semantic information (never text/regex-based inference), never fabricate a relation
   without static evidence, and keep `embeddingText` a filtered projection rather than a full copy
   of the document.

Within this repository, the pattern to follow is `Ciir.Application.Ports.ICodeAnalyzer`: implement
it in a new adapter project (analogous to `Ciir.CSharp` or `Ciir.Configuration`), and register it
in the CLI's composition root (`Ciir.Cli/Composition/ServiceCollectionExtensions.cs`) — no other
project needs to change. `AnalyzeInputHandler` dispatches to whichever registered analyzer's
`CanAnalyze` claims a given path, so multiple analyzers can coexist.

## Known limitations

- Relation extraction from property/indexer accessors is limited to expression-bodied properties;
  block-bodied `get`/`set` accessors are not walked.
- Field/property initializer expressions are not analyzed for relations.
- Static constructors are not represented as `constructor` documents.
- No interprocedural or cross-project data-flow analysis; every relation is statically observable
  within the analyzed project's own compilation.
- YAML content is never structurally parsed, by design — only file-level metadata (path, hash,
  size) is captured, since YAML serves too many unrelated purposes (CI, docker-compose, Kubernetes
  manifests, ...) to model with one schema.
- Configuration key capture never includes the value itself, only its key path and JSON type — a
  deliberate privacy guarantee, since `appsettings*.json` commonly holds secrets.
- Out of scope for this phase entirely (see the specification): a database backend, real
  embeddings/LLM calls, a REST API, a graph database, and analyzers for programming languages other
  than C#.

## What's intentionally not implemented yet

Per the project's own specification, this phase deliberately excludes: PostgreSQL/pgvector, real
embedding generation or any LLM call, a REST API, Kubernetes/queue integration, a graph database,
graph traversal, business-rule extraction, dynamic/runtime instrumentation, a fully serialized
control-flow graph, and analyzers for Java/JavaScript/Python. The architecture (ports defined in
`Ciir.Application`) is built so these can be added later as new adapters without changing the core.

## Releasing

Releases are tag-driven (see [`.specs/02-dotnet-tool.md`](.specs/02-dotnet-tool.md)): pushing a
`vX.Y.Z` tag to Forgejo, then mirroring it to GitHub (the mirror workflow can be run manually),
triggers `.github/workflows/release.yml` — build, test, pack, verify the package, and, after approval
on the `nuget` environment, publish to nuget.org. Versions are derived by GitVersion from tags and
Conventional Commits.

## License

[GPL-3.0-only](LICENSE).
