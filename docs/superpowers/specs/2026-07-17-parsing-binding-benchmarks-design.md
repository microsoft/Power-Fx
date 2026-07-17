# Power Fx Parsing and Binding Benchmark Design

## Purpose

Add a reproducible BenchmarkDotNet suite that measures Power Fx parsing and binding performance across representative formulas. The suite will expose:

- Parse-only cost from expression text.
- Bind-only cost from a pre-parsed syntax tree.
- End-to-end cost through the public `Engine.Check` API.

The bind-only benchmark will use `CheckResult.ApplyBinding`, which follows the same engine binding path used by `Check` without requiring PowerAppsClient-specific binder glue or host types.

## Goals

- Measure time and managed allocations per formula.
- Keep parse, bind, and full `Check` measurements directly comparable.
- Cover varied syntax, scope depth, table operations, typed symbols, large symbol tables, and expected failures.
- Keep inputs deterministic and results readable enough to identify individual regressions.
- Run from the existing net7.0 `Microsoft.PowerFx.Performance.Tests` executable.

## Non-Goals

- Reproduce PowerAppsClient's custom binder glue or host-specific rule-scope behavior.
- Benchmark evaluation, IR translation, IntelliSense, or connector execution.
- Add or change net462 benchmarks.
- Dynamically execute the complete expression-test corpus.
- Establish automated pass/fail performance thresholds in this change.

## Architecture

Add a net7.0-only benchmark class named `ParsingAndBindingPerformance` to:

`src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/`

The class will use a `FormulaBenchmarkScenario` parameter. Each immutable scenario will contain:

- A concise unique name used in BenchmarkDotNet reports.
- The expression text.
- Parser options.
- A read-only symbol table, when required.
- A category describing the expression or symbol shape.
- An expected outcome: success, parse error, or bind error.
- Source attribution for expressions adapted from existing `.txt` tests.

The existing shared `Program` already discovers all benchmarks in its assembly through `BenchmarkSwitcher`, so the new net7.0 files require no new executable or solution project.

No Core internals or `InternalsVisibleTo` declarations will be added.

## Measured Operations

### Parse

Call:

```csharp
engine.Parse(scenario.Expression, scenario.ParserOptions)
```

This measures lexing and parsing from source text. It does not include symbol resolution or binding.

### Bind

Global setup will parse the expression once. Each measured invocation will:

1. Create a fresh `CheckResult`.
2. supply the precomputed `ParseResult` with `SetText`.
3. supply the scenario symbol table with `SetBindingInfo`.
4. call `ApplyBinding`.

This excludes source parsing while including the normal `CheckResult` and engine binding path. A fresh `CheckResult` is required because binding is cached after the first application.

### Check

Call:

```csharp
engine.Check(
    scenario.Expression,
    scenario.ParserOptions,
    scenario.Symbols)
```

This measures the public end-to-end compilation check, including parsing, binding, error processing, and dependency analysis.

Each benchmark method performs one operation. BenchmarkDotNet, rather than handwritten loops, controls warmup and measurement iterations.

## Scenario Corpus

The initial corpus will contain ten scenarios:

| Name | Shape | Symbols | Expected outcome | Source |
| --- | --- | --- | --- | --- |
| `ArithmeticSmall` | Short literals and operators | Empty | Success | `literals.txt`, `arithmetic.txt` |
| `NestedInterpolation` | Nested interpolated strings and calls | Empty | Success | `StringInterpolate.txt` |
| `DeepScopes` | Repeated nested `With` scopes | Empty | Success | `With.txt` |
| `TablePipeline` | Nested table filtering, shaping, and aggregation | Empty | Success | `FilterFunctions.txt`, `AddColumns.txt` |
| `WideSwitch` | Deterministically generated wide argument list | Empty | Success | Adapted from `switch.txt` |
| `DeepCalls` | Deterministically generated nested calls | Empty | Success | Synthetic scaling case |
| `TypedTable` | Field and row-scope resolution over a typed table | Typed table symbol | Success | Table-function test patterns |
| `LargeGlobals` | References near the end of 1,000 globals | Large global symbol table | Success | Synthetic host-scope case |
| `InvalidIncomplete` | Incomplete nested expression | Empty | Parse error | Editor-time invalid case |
| `InvalidUnknownName` | Valid syntax with an unresolved field or name | Typed table symbol | Bind error | Binding-error test patterns |

Curated expressions will be embedded in benchmark code instead of loaded from `.txt` files at runtime. This avoids coupling benchmark execution to test directives, expected-result parsing, output-copy behavior, and changes elsewhere in the full test corpus.

Wide and deep expressions will be generated deterministically once during scenario construction. Their sizes will be constants with descriptive names so future changes are explicit.

All scenarios will use `Features.PowerFxV1` and an explicit `en-US` culture unless the scenario is specifically intended to measure another parser configuration. Culture comparisons are outside the initial scope.

## Symbol Profiles

Three symbol profiles will be represented:

- **Empty:** no additional symbols.
- **Typed table:** a table with fields such as `Amount`, `Status`, and `Category`, used by formulas with row scopes and field access.
- **Large globals:** 1,000 numbered numeric globals, with the expression referencing names near the end of the table.

Symbol tables are constructed in setup and are not mutated during measurement. This measures lookup and binding cost, not symbol-table construction.

Engine construction is also outside timed operations. BenchmarkDotNet warmup will absorb one-time engine symbol-composition caches, making the suite a warm-engine compilation benchmark.

## BenchmarkDotNet Configuration

The new class will target the current net7.0 process and use:

- `MemoryDiagnoser`.
- CSV export.
- Category and distribution columns consistent with the existing performance suite.
- Parse as the baseline method, producing per-scenario ratios for Bind and Check.

ETW and native-memory profilers will not be enabled on this class by default. They require Windows elevation and make routine benchmark runs less portable. A developer can run a separate profiling pass when call-stack detail is required.

Artifacts will remain in the repository's existing `src/BenchmarkDotNet.Artifacts` location.

## Validation and Error Handling

Global setup will perform an untimed validation pass for the active scenario:

- `Success` requires successful parse, bind, and `Check`.
- `ParseError` requires parsing to report an error.
- `BindError` requires successful parsing and unsuccessful binding or `Check`.

If behavior differs from the declared expectation, setup will throw an `InvalidOperationException` containing the scenario name and errors. This prevents formula or feature changes from silently turning a benchmark into a different workload.

Invalid scenarios remain measured inputs. Their expected errors are not exceptions and will not be caught or converted into success-shaped results.

## Tests

Add net7.0 xUnit tests beside the benchmark files to verify:

- Scenario names are non-empty and unique.
- Every scenario has non-empty expression text and source/category metadata.
- Declared parse, bind, and `Check` outcomes match actual behavior.
- Bind-from-preparsed creates a fresh `CheckResult` and completes for every scenario, including parse-error scenarios supported by the binder.
- Generated wide/deep expressions are deterministic.

The targeted test command will be:

```powershell
dotnet test src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --filter "FullyQualifiedName~ParsingAndBindingPerformanceTests"
```

A BenchmarkDotNet smoke run will use:

```powershell
dotnet run --project src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release -- --filter "*ParsingAndBindingPerformance*" --job Dry
```

The normal suite command will omit `--job Dry`.

## Documentation Changes

Update the run instructions in the existing shared `Program.cs`. The current comment references .NET Core 3.1 and obsolete output paths. The replacement will document:

- Building or running the net7.0 project in Release.
- Listing benchmarks.
- Running only the parsing/binding suite.
- Running the dry smoke job.
- Locating BenchmarkDotNet artifacts.

## Planned Files

- Add `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/FormulaBenchmarkScenario.cs`.
- Add `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/ParsingAndBindingPerformance.cs`.
- Add `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/ParsingAndBindingPerformanceTests.cs`.
- Update `src/tests/Microsoft.PowerFx.Performance.Tests.Shared/Program.cs`.

The SDK-style net7.0 project includes local `.cs` files automatically, so no project-file edit should be necessary.

## Acceptance Criteria

- The net7.0 performance project builds in Release.
- Targeted scenario tests pass.
- The dry BenchmarkDotNet run completes all Parse, Bind, and Check cases.
- Reports show one row per method and scenario, including time, allocation, and ratio columns.
- Valid and intentionally invalid workloads are clearly named.
- The suite requires no PowerAppsClient dependency and no Core API visibility change.
