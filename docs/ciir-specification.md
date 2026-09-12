# CIIR Specification (v1)

This document explains **what CIIR concepts mean**. The JSON Schema (`schemas/ciir.schema.json`)
is the authoritative statement of **what a valid CIIR document looks like**; this document exists
to explain the semantics behind it, so that generators for other languages can implement the same
contract consistently.

## Formal definition

> Code Intelligence Intermediate Representation (CIIR) is a language-independent intermediate
> representation designed to describe statically observable software entities, their semantic
> properties, source evidence, relationships, documentation and selected control-flow
> characteristics.
>
> Language-specific analyzers translate native syntax and semantic models into CIIR.
>
> CIIR serves as a stable interchange format for downstream code-intelligence systems including
> search, embedding generation, dependency graphs, execution-flow analysis and software
> comprehension.
>
> CIIR represents observable facts and SHALL avoid presenting probabilistic or AI-generated
> interpretations as deterministic program facts.

## `kind`

The entity kind. The C# v1 generator produces: `project`, `namespace`, `type`, `method`,
`constructor`, `property`, `field`, `event`. The configuration/YAML generator (see below) produces:
`configuration`, `configuration_key`, `file`. The schema reserves further values (`database`,
`endpoint`, ...) for future language/domain generators — no generator implements functionality
solely to populate every possible kind (YAGNI).

## Configuration and file metadata

Alongside C# source, CIIR captures two kinds of non-code, repository-wide artifacts, discovered
anywhere under the analysis root (independent of `.csproj`/solution structure) by the
configuration/YAML generator (`Ciir.Configuration`):

- **`appsettings*.json`** files are parsed into one `configuration` document for the file itself,
  plus one `configuration_key` document per flattened key path — covering both leaf values and
  object/array containers, so every node in the JSON tree is represented. Key paths are
  colon-separated (`ConnectionStrings:Default`), with array elements addressed by their numeric
  index (`AllowedHosts:0`), matching the convention `Microsoft.Extensions.Configuration` itself
  uses. Containment is conveyed the same way as for C# members: via `symbol.container` (the
  `configuration` document's qualified name), never a `contains` relation.
- **`*.yaml`/`*.yml`** files are captured only as `file` metadata (path, content hash, size) —
  their content is never structurally parsed. YAML serves too many unrelated purposes (CI
  workflows, docker-compose, Kubernetes manifests, ...) to model with one schema; a `file` document
  only asserts that the file exists and what its content hash is.

**Values are never captured.** A `configuration_key` document's `configurationKey.valueType`
records only the JSON value's type (`string`, `number`, `boolean`, `array`, `object`, `null`) —
never the value itself. `appsettings*.json` files commonly hold secrets (connection strings, API
keys), and CIIR output may be indexed or embedded by downstream tooling; omitting values is a
deliberate privacy guarantee, not an oversight, and applies uniformly regardless of the key's name.

Both kinds use `symbol.canonicalName` as `{relativePath}` (for `configuration`/`file`) or
`{relativePath}#{keyPath}` (for `configuration_key`) — stable and deterministic across runs, and
disambiguating identical key paths across sibling files (e.g. `appsettings.json` vs.
`appsettings.Development.json`). `language` is `"json"` for `configuration`/`configuration_key` and
`"yaml"` for `file`; `project` is the fixed logical project name `"Configuration"`, since these
files are not necessarily owned by any single `.csproj`.

## Identity (`id`)

Every document has a deterministic id of the form `sha256:<hex>`, computed by hashing the
canonical key `language|projectIdentity|kind|canonicalSymbolIdentity` (see
`Ciir.Core.Identity.CiirIdentity`). Two analyses of the same semantically identical code MUST
produce the same id. Overloads MUST produce different ids, because `canonicalSymbolIdentity`
includes parameter types for methods/constructors.

## `symbol`

- `name` — simple name.
- `qualifiedName` — fully qualified, human-readable name.
- `canonicalName` — the unambiguous identity of the symbol; for methods/constructors this
  includes parameter types so overloads are distinguishable.
- `container` — the qualified name of the semantically owning entity (containing type or
  namespace), when applicable.

## `source` and `additionalSourceLocations`

`source` is the entity's primary declaration location, with a path **relative to the analysis
root** (never an absolute machine path), 1-based line/column numbers, and a
SHA-256 hash of the exact source span. `additionalSourceLocations` holds any further physical
declarations for the same semantic entity — this is how C# `partial` types and methods are
represented: **one** CIIR document per semantic entity, with multiple source locations, never
duplicate documents per physical file.

## Relations

Each relation records a single, statically observable fact from the source entity to a target
symbol:

- `contains` is not produced by the C# v1 generator — containment is already recoverable from
  every child document's `symbol.container`, so emitting a parallel `contains` relation for every
  member would be redundant (YAGNI).
- `inherits` / `implements` — a type's direct base class / directly declared interfaces. Interfaces
  implemented transitively through a base class are not repeated; a downstream graph can compute
  that transitively from `inherits` + the base type's own `implements`.
- `overrides` — a member's immediately overridden virtual/abstract member.
- `calls` — a statically resolved invocation, always obtained from the Roslyn semantic model
  (`SemanticModel.GetSymbolInfo`), never inferred from source text.
- `constructs` — an object-creation expression; the target is the **constructed type**, not the
  constructor's canonical name (e.g. `CONSTRUCTS Payment`, not `CONSTRUCTS Payment..ctor()`).
- `reads` / `writes` — a property/field access. A member access used only as the receiver of a
  call (e.g. `_gateway` in `_gateway.AuthorizeAsync()`) still produces a `reads` relation for
  `_gateway`: CIIR keeps every statically observable fact, and it is `embeddingText` — not the raw
  relation list — that applies a semantic noise filter (see below).
- `throws` — the static type of a `throw` expression/statement's operand.
- `catches` — the declared exception type of a `catch` clause.

Only the forward relation is ever recorded (`A CALLS B`, never also `B CALLED_BY A`); building the
inverse edge is left to downstream graph construction.

### Polymorphism

A relation only records what the semantic model can prove statically. Given:

```csharp
IPaymentGateway gateway;
gateway.AuthorizeAsync();
```

the only fact CIIR records is `CALLS IPaymentGateway.AuthorizeAsync`. It never guesses which
concrete implementation (`StripeGateway`, `AdyenGateway`, ...) is invoked at runtime — that
requires runtime information static analysis does not have. The set of possible implementations is
instead recoverable from the type graph via their own `implements` relations.

### `resolution`

- `status`: `resolved` (found in the analyzed project or in another project analyzed in this same
  run), `external` (found, but outside this analysis run — framework or a package/non-project
  dependency), `unresolved` (no matching symbol at all), `ambiguous` (more than one candidate and
  none could be selected statically), `dynamic` (the call is dynamically dispatched, e.g. through
  the `dynamic` keyword).
- `origin`: `project` (the analyzed project itself), `solution` (a different project that is part
  of this same analysis run — reachable, directly or transitively, via the analyzed project's own
  Roslyn `ProjectReference`s), `framework` (BCL), `dependency` (a package or non-project-reference
  assembly that is not the BCL), `runtime`, `external_service`, `unknown`.

External symbols (framework/dependency calls) never require a full CIIR document to be generated
for the target — only the relation with `resolution.status: "external"` is produced. A relation
whose target belongs to a different project analyzed in this same run is `resolved`/`solution`,
and `target.id` is populated with that target's CIIR document id — computed from the target
symbol alone, without requiring that other project's own document-emission pass to have already
run — so long as the target's kind is one this pipeline emits its own document for.

### `target.id`

`target.id` is populated whenever `resolution.status` is `resolved` (`origin: project` or
`origin: solution`) **and** the target symbol's kind is one this generator emits its own CIIR
document for (`type`, `method`, `constructor`, `property`, `field`, `event` — subject to the same
syntactic gating each of those document kinds requires, e.g. a record's positional property has no
`property` document and so its relation targets never get an `id` even when `resolved`). It is
always `null` for `external`/`unresolved`/`ambiguous`/`dynamic` relations.

## Conditions

Conditions preserve branching/looping constructs as verbatim facts (`expression` is the literal
source text) — CIIR never interprets a condition as a business rule. The `guard` kind is used
specifically for an `if` with no `else` whose body is a single `return`/`throw`/`continue`/`break`
statement (an early-exit guard clause); every other `if` is `if` (or `else_if` when it is the
`else` branch of another `if`).

## Control flow

CIIR v1 stores only aggregate metrics (`basicBlockCount`, `cyclomaticComplexity`, `hasBranches`,
`hasLoops`), computed from Roslyn's control-flow-graph API
(`Microsoft.CodeAnalysis.FlowAnalysis.ControlFlowGraph`) — cyclomatic complexity uses the standard
`edges - nodes + 2` formula over that graph. No full control-flow graph is serialized in v1; the
shape is deliberately left extensible for a future, more detailed representation.

## `embeddingText`

`embeddingText` is a **semantic projection**, not a copy of the full document. It is built by
`Ciir.Core.EmbeddingText.EmbeddingTextBuilder` under the `semantic-v1` strategy, in this fixed
section order (a section is omitted entirely when it has no content):

```
Entity
Qualified name
Container
Documentation
Parameters
Returns
Comments
Reads
Writes
Calls
Constructs
Throws
Conditions
```

The specification's own illustrative order additionally names a "Signature" section between
Documentation and Parameters; no worked example in the specification shows its content, so this
generator does not emit one — the same structural information is already conveyed by the
Parameters/Returns sections.

Which `calls` relations, comments, and conditions are relevant enough to include is decided by a
single, testable component (`Ciir.Core.EmbeddingText.IEmbeddingTextPolicy`), never scattered `if`s
in the analyzer. The C# implementation
(`Ciir.CSharp.EmbeddingText.DotNetNoiseEmbeddingTextPolicy`) excludes calls to a hardcoded list of
extremely generic BCL/framework members (`String.IsNullOrEmpty`, `ILogger.*`, `Task.WhenAll`,
common LINQ operators, ...) and only surfaces comments carrying an explicit marker
(TODO/FIXME/warning/note). This list is intentionally not externally configurable in v1 — nothing
in the specification calls for user-facing configurability here, only for the filtering decision
to live in one coherent, testable place; the policy abstraction is what makes that possible to
change later without touching the analyzer.

The `Returns` section uses the **semantic** return type, unwrapping `Task<T>`/`ValueTask<T>` to
`T` and omitting the section entirely for `void`/`Task`/`ValueTask` (`Method.EmbeddingReturnType`
in the CIIR model), rather than the raw CLR return type (`Method.ReturnType`).

No embedding vector is ever produced or stored — `embeddingText` is the input to a future,
separate embedding-generation step, not its output.

## `extensions`

Details specific to a single language that are not (yet) universal concepts live under
`extensions.csharp` rather than becoming new top-level CIIR properties (e.g. whether a `record` is
specifically a `record struct`). The Core model does not accumulate single-language flags like
`isCSharpRecordStruct` as first-class properties.

## Determinism and ordering

Running the same analysis twice over unchanged source produces byte-identical `ciir.jsonl` output
(aside from `manifest.json`'s `generatedAt`, which never appears inside a CIIR document itself).
This is achieved by: sorting discovered projects and analyzed types by their fully qualified name;
sorting members within a type by (constructor/method/property/field/event, then name, then
canonical name for overloads); sorting `modifiers[]` into one canonical order
(`Ciir.Core.Symbols.CiirModifierOrder`) regardless of declaration order in source; and never
including wall-clock time or other non-reproducible data inside a `CiirDocument`.

## Known limitations of this generator (v1)

- Relation extraction from property/indexer accessor bodies is limited to expression-bodied
  properties (`Type Name => expr;`); block-bodied `get`/`set` accessors are not walked.
- Field/property initializer expressions are not analyzed for relations.
- Static constructors are not represented as `constructor` documents.
- No cross-project *data-flow* analysis is performed (a relation never claims to know a runtime
  value or its provenance) — but cross-project relations to types/methods/properties/fields/events
  in another project loaded as part of this same analysis run *are* represented, as
  `resolved`/`solution`, distinct from calls into external packages/frameworks (`external`).
