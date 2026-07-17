# Power Fx Parsing and Binding Benchmarks Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a permanent net7.0 BenchmarkDotNet coverage suite that measures parse-only, bind-from-preparsed, and end-to-end `Engine.Check` performance across ten representative Power Fx formulas.

**Architecture:** Add a public scenario model and deterministic scenario factory beside the net7.0 performance project, then drive three benchmark methods from one `[ParamsSource]` matrix. The benchmark class owns the warm `Engine`, pre-parses the active scenario during global setup, validates the declared outcome outside timed operations, and returns every measured result to prevent dead-code elimination.

**Tech Stack:** C# 10, .NET 7.0, BenchmarkDotNet 0.13.2, xUnit 2.8, Microsoft.PowerFx.Core.

---

## File Structure

- Create `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/FormulaBenchmarkScenario.cs`
  - Defines scenario metadata, expected outcomes, symbol profiles, deterministic expression generators, and the ten-case corpus.
- Create `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/ParsingAndBindingPerformance.cs`
  - Defines BenchmarkDotNet configuration, setup-time outcome validation, and the Parse, Bind, and Check benchmark methods.
- Create `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/ParsingAndBindingPerformanceTests.cs`
  - Verifies corpus stability, deterministic generation, phase outcomes, and fresh bind state.
- Modify `src/tests/Microsoft.PowerFx.Performance.Tests.Shared/Program.cs`
  - Replaces obsolete .NET Core 3.1 instructions with net7.0 list, dry-run, filtered-run, and artifact instructions.

No project file, package reference, Core API, or `InternalsVisibleTo` change is planned. The net7.0 SDK project automatically compiles local `.cs` files.

### Task 1: Add the deterministic scenario corpus

**Files:**
- Create: `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/FormulaBenchmarkScenario.cs`
- Create: `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/ParsingAndBindingPerformanceTests.cs`

- [ ] **Step 1: Write failing corpus-definition tests**

Create `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/ParsingAndBindingPerformanceTests.cs` with:

```csharp
﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.PowerFx.Performance.Tests
{
    public class ParsingAndBindingPerformanceTests
    {
        [Fact]
        public void ScenarioDefinitionsAreStable()
        {
            var scenarios = FormulaBenchmarkScenarios.Create();
            var names = scenarios.Select(scenario => scenario.Name).ToArray();

            Assert.Equal(10, scenarios.Count);
            Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());

            Assert.All(
                scenarios,
                scenario =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(scenario.Name));
                    Assert.False(string.IsNullOrWhiteSpace(scenario.Category));
                    Assert.False(string.IsNullOrWhiteSpace(scenario.Source));
                    Assert.False(string.IsNullOrWhiteSpace(scenario.Expression));
                    Assert.NotNull(scenario.ParserOptions);
                    Assert.Equal(scenario.Name, scenario.ToString());
                });
        }

        [Fact]
        public void GeneratedExpressionsAreDeterministic()
        {
            var first = FormulaBenchmarkScenarios.Create().ToDictionary(scenario => scenario.Name, StringComparer.Ordinal);
            var second = FormulaBenchmarkScenarios.Create().ToDictionary(scenario => scenario.Name, StringComparer.Ordinal);

            Assert.Equal(first["WideSwitch"].Expression, second["WideSwitch"].Expression);
            Assert.Equal(first["DeepCalls"].Expression, second["DeepCalls"].Expression);
            Assert.Equal(first["DeepScopes"].Expression, second["DeepScopes"].Expression);
            Assert.Equal("Global0999 + Global0998 + Global0997 + Global0996", first["LargeGlobals"].Expression);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run from `C:\repos\Power-Fx`:

```powershell
dotnet test src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --filter "FullyQualifiedName~ParsingAndBindingPerformanceTests"
```

Expected: build fails with `CS0103` because `FormulaBenchmarkScenarios` does not exist.

- [ ] **Step 3: Implement the scenario model and corpus**

Create `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/FormulaBenchmarkScenario.cs` with:

```csharp
﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Performance.Tests
{
    public enum FormulaBenchmarkExpectedOutcome
    {
        Success,
        ParseError,
        BindError
    }

    public sealed class FormulaBenchmarkScenario
    {
        public FormulaBenchmarkScenario(
            string name,
            string category,
            string source,
            string expression,
            ParserOptions parserOptions,
            ReadOnlySymbolTable symbols,
            FormulaBenchmarkExpectedOutcome expectedOutcome)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Category = category ?? throw new ArgumentNullException(nameof(category));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            ParserOptions = parserOptions ?? throw new ArgumentNullException(nameof(parserOptions));
            Symbols = symbols;
            ExpectedOutcome = expectedOutcome;
        }

        public string Name { get; }

        public string Category { get; }

        public string Source { get; }

        public string Expression { get; }

        public ParserOptions ParserOptions { get; }

        public ReadOnlySymbolTable Symbols { get; }

        public FormulaBenchmarkExpectedOutcome ExpectedOutcome { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class FormulaBenchmarkScenarios
    {
        private const int WideSwitchArmCount = 64;
        private const int DeepCallCount = 32;
        private const int DeepScopeCount = 12;
        private const int GlobalSymbolCount = 1000;

        public static IReadOnlyList<FormulaBenchmarkScenario> Create()
        {
            var parserOptions = new ParserOptions(new CultureInfo("en-US"));
            var typedTableSymbols = CreateTypedTableSymbols();
            var largeGlobalSymbols = CreateLargeGlobalSymbols();

            return new[]
            {
                new FormulaBenchmarkScenario(
                    "ArithmeticSmall",
                    "Baseline",
                    "ExpressionTestCases\\literals.txt; ExpressionTestCases\\arithmetic.txt",
                    "1 + 2 * 3 - 4 / 5",
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "NestedInterpolation",
                    "Syntax",
                    "ExpressionTestCases\\StringInterpolate.txt",
                    "$\"Summary: {With({a:4,b:6},a*b)} / {$\"{true}\"}\"",
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "DeepScopes",
                    "Scope",
                    "ExpressionTestCases\\With.txt",
                    CreateDeepScopesExpression(),
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "TablePipeline",
                    "Tables",
                    "ExpressionTestCases\\FilterFunctions.txt; ExpressionTestCases\\AddColumns_SupportColumnNamesAsIdentifiers.txt",
                    "Sum(AddColumns(Filter([1,2,3,4,5], Value > 2), Squared, Value * Value), Squared)",
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "WideSwitch",
                    "Scale",
                    "ExpressionTestCases\\switch.txt",
                    CreateWideSwitchExpression(),
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "DeepCalls",
                    "Scale",
                    "Synthetic nested-call scaling case",
                    CreateDeepCallsExpression(),
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "TypedTable",
                    "Symbols",
                    "ExpressionTestCases\\FilterFunctions.txt; ExpressionTestCases\\Sum.txt",
                    "Sum(Filter(Orders, Status = \"Open\"), Amount)",
                    parserOptions,
                    typedTableSymbols,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "LargeGlobals",
                    "Symbols",
                    "Synthetic 1,000-global host-scope case",
                    "Global0999 + Global0998 + Global0997 + Global0996",
                    parserOptions,
                    largeGlobalSymbols,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "InvalidIncomplete",
                    "Invalid",
                    "Synthetic editor-time incomplete formula",
                    "With({x: 1}, If(x > 0,",
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.ParseError),
                new FormulaBenchmarkScenario(
                    "InvalidUnknownName",
                    "Invalid",
                    "ExpressionTestCases binding-error patterns",
                    "Sum(Filter(Orders, MissingAmount > 0), Amount)",
                    parserOptions,
                    typedTableSymbols,
                    FormulaBenchmarkExpectedOutcome.BindError)
            };
        }

        private static ReadOnlySymbolTable CreateTypedTableSymbols()
        {
            var ordersType = TableType.Empty()
                .Add("Amount", FormulaType.Decimal)
                .Add("Status", FormulaType.String)
                .Add("Category", FormulaType.String);

            var symbols = new SymbolTable();
            symbols.AddVariable("Orders", ordersType);
            return symbols;
        }

        private static ReadOnlySymbolTable CreateLargeGlobalSymbols()
        {
            var symbols = new SymbolTable();

            for (var i = 0; i < GlobalSymbolCount; i++)
            {
                var name = "Global" + i.ToString("0000", CultureInfo.InvariantCulture);
                symbols.AddVariable(name, FormulaType.Decimal);
            }

            return symbols;
        }

        private static string CreateWideSwitchExpression()
        {
            var builder = new StringBuilder("Switch(63");

            for (var i = 0; i < WideSwitchArmCount; i++)
            {
                builder.Append(", ");
                builder.Append(i.ToString(CultureInfo.InvariantCulture));
                builder.Append(", ");
                builder.Append((i * i).ToString(CultureInfo.InvariantCulture));
            }

            builder.Append(", -1)");
            return builder.ToString();
        }

        private static string CreateDeepCallsExpression()
        {
            var builder = new StringBuilder();

            for (var i = 0; i < DeepCallCount; i++)
            {
                builder.Append("Abs(");
            }

            builder.Append("-1");
            builder.Append(')', DeepCallCount);
            return builder.ToString();
        }

        private static string CreateDeepScopesExpression()
        {
            var expression = "1";

            for (var i = 0; i < DeepScopeCount; i++)
            {
                var value = (i + 1).ToString(CultureInfo.InvariantCulture);
                expression = $"With({{x: {value}}}, x + ({expression}))";
            }

            return expression;
        }
    }
}
```

- [ ] **Step 4: Run the corpus tests**

Run:

```powershell
dotnet test src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --filter "FullyQualifiedName~ParsingAndBindingPerformanceTests"
```

Expected: 2 tests pass.

- [ ] **Step 5: Commit the scenario corpus**

Run:

```powershell
git add -- src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\FormulaBenchmarkScenario.cs src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\ParsingAndBindingPerformanceTests.cs
git commit -m "Add parsing benchmark scenarios" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`nCopilot-Session: 90115941-0836-40ef-85c8-d8b16effc332"
```

Expected: one commit containing only the scenario model, corpus, and corpus tests.

### Task 2: Add Parse, Bind, and Check benchmarks

**Files:**
- Create: `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/ParsingAndBindingPerformance.cs`
- Modify: `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/ParsingAndBindingPerformanceTests.cs`

- [ ] **Step 1: Add failing phase-behavior tests**

Add these methods inside `ParsingAndBindingPerformanceTests`:

```csharp
        [Fact]
        public void ScenariosMatchExpectedOutcomes()
        {
            foreach (var scenario in FormulaBenchmarkScenarios.Create())
            {
                var benchmark = new ParsingAndBindingPerformance
                {
                    Scenario = scenario
                };

                benchmark.GlobalSetup();

                var parse = benchmark.Parse();
                var bind = benchmark.Bind();
                var check = benchmark.Check();

                switch (scenario.ExpectedOutcome)
                {
                    case FormulaBenchmarkExpectedOutcome.Success:
                        Assert.True(parse.IsSuccess);
                        Assert.True(bind.IsSuccess);
                        Assert.True(check.IsSuccess);
                        break;
                    case FormulaBenchmarkExpectedOutcome.ParseError:
                        Assert.False(parse.IsSuccess);
                        Assert.False(bind.IsSuccess);
                        Assert.False(check.IsSuccess);
                        break;
                    case FormulaBenchmarkExpectedOutcome.BindError:
                        Assert.True(parse.IsSuccess);
                        Assert.False(bind.IsSuccess);
                        Assert.False(check.IsSuccess);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected outcome {scenario.ExpectedOutcome}.");
                }
            }
        }

        [Fact]
        public void BindCreatesFreshCheckResults()
        {
            foreach (var scenario in FormulaBenchmarkScenarios.Create())
            {
                var benchmark = new ParsingAndBindingPerformance
                {
                    Scenario = scenario
                };

                benchmark.GlobalSetup();

                var first = benchmark.Bind();
                var second = benchmark.Bind();

                Assert.NotSame(first, second);
                Assert.Same(first.Parse, second.Parse);
            }
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```powershell
dotnet test src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --filter "FullyQualifiedName~ParsingAndBindingPerformanceTests"
```

Expected: build fails with `CS0246` because `ParsingAndBindingPerformance` does not exist.

- [ ] **Step 3: Implement the benchmark class**

Create `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/ParsingAndBindingPerformance.cs` with:

```csharp
﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Microsoft.PowerFx.Performance.Tests
{
    [MemoryDiagnoser]
    [CsvExporter]
    [CategoriesColumn]
    [MinColumn]
    [Q1Column]
    [MeanColumn]
    [MedianColumn]
    [Q3Column]
    [MaxColumn]
    [BenchmarkCategory("ParsingAndBinding")]
    public class ParsingAndBindingPerformance
    {
        private Engine _engine;
        private ParseResult _parseResult;

        [ParamsSource(nameof(Scenarios))]
        public FormulaBenchmarkScenario Scenario { get; set; }

        public IEnumerable<FormulaBenchmarkScenario> Scenarios => FormulaBenchmarkScenarios.Create();

        [GlobalSetup]
        public void GlobalSetup()
        {
            _engine = new Engine(new PowerFxConfig(Features.PowerFxV1));
            _parseResult = _engine.Parse(Scenario.Expression, Scenario.ParserOptions);

            var bind = Bind();
            var check = Check();
            ValidateScenario(bind, check);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Parse")]
        public ParseResult Parse()
        {
            return _engine.Parse(Scenario.Expression, Scenario.ParserOptions);
        }

        [Benchmark]
        [BenchmarkCategory("Bind")]
        public CheckResult Bind()
        {
            var result = new CheckResult(_engine)
                .SetText(_parseResult)
                .SetBindingInfo(Scenario.Symbols);

            result.ApplyBinding();
            return result;
        }

        [Benchmark]
        [BenchmarkCategory("Check")]
        public CheckResult Check()
        {
            return _engine.Check(Scenario.Expression, Scenario.ParserOptions, Scenario.Symbols);
        }

        private void ValidateScenario(CheckResult bind, CheckResult check)
        {
            var matchesExpectedOutcome = Scenario.ExpectedOutcome switch
            {
                FormulaBenchmarkExpectedOutcome.Success =>
                    _parseResult.IsSuccess && bind.IsSuccess && check.IsSuccess,
                FormulaBenchmarkExpectedOutcome.ParseError =>
                    !_parseResult.IsSuccess && !bind.IsSuccess && !check.IsSuccess,
                FormulaBenchmarkExpectedOutcome.BindError =>
                    _parseResult.IsSuccess && !bind.IsSuccess && !check.IsSuccess,
                _ => false
            };

            if (matchesExpectedOutcome)
            {
                return;
            }

            var parseErrors = string.Join(" | ", _parseResult.Errors.Select(error => error.Message));
            var bindErrors = string.Join(" | ", bind.Errors.Select(error => error.Message));
            var checkErrors = string.Join(" | ", check.Errors.Select(error => error.Message));

            throw new InvalidOperationException(
                $"Scenario '{Scenario.Name}' expected {Scenario.ExpectedOutcome}, " +
                $"but got parse={_parseResult.IsSuccess}, bind={bind.IsSuccess}, check={check.IsSuccess}." +
                $"{Environment.NewLine}Parse errors: {parseErrors}" +
                $"{Environment.NewLine}Bind errors: {bindErrors}" +
                $"{Environment.NewLine}Check errors: {checkErrors}");
        }
    }
}
```

This intentionally has no `SimpleJob`, `EtwProfiler`, `NativeMemoryProfiler`, manual loop, or `IterationSetup`:

- The default job runs the current net7.0 process.
- ETW/native profiling would require elevation and add a separate profiled run.
- BenchmarkDotNet selects invocation counts.
- All timed methods are idempotent and return their result.

- [ ] **Step 4: Run the phase-behavior tests**

Run:

```powershell
dotnet test src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --filter "FullyQualifiedName~ParsingAndBindingPerformanceTests"
```

Expected: 4 tests pass.

- [ ] **Step 5: List the generated benchmark cases**

Run:

```powershell
Set-Location src
dotnet run --project tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build -- --list flat | Select-String "ParsingAndBindingPerformance"
Set-Location ..
```

Expected: Parse, Bind, and Check appear for `ParsingAndBindingPerformance`. Parameter expansion happens during a run, producing 30 benchmark cases: 3 methods × 10 scenarios.

- [ ] **Step 6: Commit the benchmark implementation**

Run:

```powershell
git add -- src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\ParsingAndBindingPerformance.cs src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\ParsingAndBindingPerformanceTests.cs
git commit -m "Add parsing and binding benchmarks" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`nCopilot-Session: 90115941-0836-40ef-85c8-d8b16effc332"
```

Expected: one commit containing the benchmark class and phase tests.

### Task 3: Update benchmark run instructions

**Files:**
- Modify: `src/tests/Microsoft.PowerFx.Performance.Tests.Shared/Program.cs`

- [ ] **Step 1: Replace the obsolete Program comment**

Replace `src/tests/Microsoft.PowerFx.Performance.Tests.Shared/Program.cs` with:

```csharp
﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using BenchmarkDotNet.Running;

namespace Microsoft.PowerFx.Performance.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /*
             * Run from the repository root.
             *
             * Build:
             *   dotnet build src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release
             *
             * List benchmarks:
             *   Set-Location src
             *   dotnet run --project tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build -- --list flat
             *
             * Validate the parsing and binding suite without collecting meaningful measurements:
             *   dotnet run --project tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build -- --filter "*ParsingAndBindingPerformance*" --job Dry
             *
             * Run the parsing and binding suite:
             *   dotnet run --project tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build -- --filter "*ParsingAndBindingPerformance*"
             *
             * BenchmarkDotNet writes logs and reports under:
             *   src\BenchmarkDotNet.Artifacts
             */

            _ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
```

- [ ] **Step 2: Build after the documentation-only code change**

Run:

```powershell
dotnet build src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release
```

Expected: build succeeds with no errors.

- [ ] **Step 3: Verify the documented list command**

Run:

```powershell
Set-Location src
dotnet run --project tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build -- --list flat | Select-String "ParsingAndBindingPerformance"
Set-Location ..
```

Expected: the command exits without an interactive prompt and lists the new class's three methods.

- [ ] **Step 4: Commit the instructions**

Run:

```powershell
git add -- src\tests\Microsoft.PowerFx.Performance.Tests.Shared\Program.cs
git commit -m "Document parsing benchmark commands" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`nCopilot-Session: 90115941-0836-40ef-85c8-d8b16effc332"
```

Expected: one commit containing only the updated benchmark instructions.

### Task 4: Validate the suite with BenchmarkDotNet

**Files:**
- Verify: `src/tests/.Net7.0/Microsoft.PowerFx.Performance.Tests/`
- Verify artifacts: `src/BenchmarkDotNet.Artifacts/`

- [ ] **Step 1: Run the targeted xUnit tests without rebuilding**

Run:

```powershell
dotnet test src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ParsingAndBindingPerformanceTests"
```

Expected: 4 tests pass.

- [ ] **Step 2: Run all 30 cases with the Dry job**

Run:

```powershell
Set-Location src
dotnet run --project tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build -- --filter "*ParsingAndBindingPerformance*" --job Dry --artifacts BenchmarkDotNet.Artifacts\ParsingAndBindingValidation\Dry *> parsing-binding-benchmark-dry.log
Set-Location ..
```

Expected:

- Exit code 0.
- The log contains 30 completed benchmark cases.
- No setup validation exception appears.
- A `ParsingAndBindingPerformance-report-github.md` report is created under `src\BenchmarkDotNet.Artifacts\ParsingAndBindingValidation\Dry\results`.

- [ ] **Step 3: Inspect the dry-run report instead of the verbose log**

Run:

```powershell
Get-ChildItem src\BenchmarkDotNet.Artifacts\ParsingAndBindingValidation\Dry\results\*ParsingAndBindingPerformance*-report-github.md |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 |
    Get-Content
```

Expected: the report contains Parse, Bind, and Check rows for all ten scenario names. Dry-job timing values are not meaningful.

- [ ] **Step 4: Run one representative scenario with the default job**

Run:

```powershell
Set-Location src
dotnet run --project tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build -- --filter "*ParsingAndBindingPerformance*TypedTable*" --artifacts BenchmarkDotNet.Artifacts\ParsingAndBindingValidation\TypedTable *> parsing-binding-benchmark-typed-table.log
Set-Location ..
```

Expected:

- Exit code 0.
- The report contains three `TypedTable` rows.
- Parse has ratio `1.00`.
- Bind and Check have non-empty ratio and allocated-byte columns.

- [ ] **Step 5: Run the complete suite with the Short job**

Run:

```powershell
Set-Location src
dotnet run --project tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build -- --filter "*ParsingAndBindingPerformance*" --job Short --artifacts BenchmarkDotNet.Artifacts\ParsingAndBindingValidation\Short *> parsing-binding-benchmark-short.log
Set-Location ..
```

Expected:

- Exit code 0.
- The report contains 30 rows.
- Every scenario has Parse, Bind, and Check measurements.
- Allocation columns are populated.
- Parse is the baseline within each scenario.

- [ ] **Step 6: Check the final repository diff and commit history**

Run:

```powershell
git --no-pager diff --check
git --no-pager status --short --branch
git --no-pager log -4 --oneline
```

Expected:

- `git diff --check` prints nothing.
- Only the isolated validation artifacts and logs are untracked; delete them before completion unless the user explicitly asks to keep them.
- The branch contains the design commit plus the three implementation commits.

- [ ] **Step 7: Remove generated benchmark artifacts and logs**

Resolve and inspect the exact generated paths first:

```powershell
$logs = Get-ChildItem src -File -Filter "parsing-binding-benchmark-*.log"
$artifacts = Resolve-Path src\BenchmarkDotNet.Artifacts\ParsingAndBindingValidation
$logs
Get-ChildItem -LiteralPath $artifacts.Path -Force
```

Then remove only those resolved paths:

```powershell
foreach ($log in $logs)
{
    Remove-Item -LiteralPath $log.FullName
}

Remove-Item -LiteralPath $artifacts.Path -Recurse
```

Expected: `git status --short` shows no generated benchmark output, and any unrelated pre-existing contents under `src\BenchmarkDotNet.Artifacts` remain untouched.

## Final Verification

Run:

```powershell
dotnet build src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release
dotnet test src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ParsingAndBindingPerformanceTests"
git --no-pager diff --check
git --no-pager status --short --branch
```

Expected:

- Release build succeeds.
- 4 targeted tests pass.
- No whitespace errors.
- Worktree is clean on `lesaltzm/parsing-binding-benchmarks`.
