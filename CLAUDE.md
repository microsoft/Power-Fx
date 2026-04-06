# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Power Fx is a low-code general-purpose programming language based on spreadsheet-like formulas. This is the open-source C# implementation used across Microsoft Power Platform. The codebase contains a complete compiler and interpreter.

## Build Commands

```bash
# Build (from repo root)
dotnet build src/Microsoft.PowerFx.sln

# Build specific configuration (Debug, Release, DebugAll, ReleaseAll, Debug462, Debug70)
dotnet build src/Microsoft.PowerFx.sln -p:Configuration=Release

# Run all tests
dotnet test src/Microsoft.PowerFx.sln

# Run tests for a specific project
dotnet test src/tests/Microsoft.PowerFx.Core.Tests.Shared/Microsoft.PowerFx.Core.Tests.Shared.csproj

# Run a single test by name
dotnet test src/Microsoft.PowerFx.sln --filter "FullyQualifiedName~YourTestName"

# Run tests by category
dotnet test src/Microsoft.PowerFx.sln --filter "Category=ExpressionTest"

# Build local NuGet packages (from src/ directory, outputs to src/outputpackages/)
.\src\buildLocalPackages.cmd [Configuration]

# Run tests on both net462 and net7.0 via VSTest (from src/ directory)
.\src\runLocalTests.cmd [Configuration]
```

**Build configurations**: `Debug`/`Release` (single-target), `Debug462`/`Release462` (net462 only), `Debug70`/`Release70` (net7.0 only), `DebugAll`/`ReleaseAll` (multi-target, used by local build scripts).

## Architecture

### Compiler Pipeline

```
Expression Text -> [Lexer] -> Tokens -> [Parser] -> AST -> [Binder] -> Typed AST -> [IR Translator] -> IR -> [Interpreter/Backend] -> Result
```

### Core Libraries

1. **Microsoft.PowerFx.Core** - Compiler only (no evaluation)
   - `Lexer/` - Tokenization with culture-aware parsing
   - `Parser/` - Recursive descent parser producing AST nodes (`Syntax/Nodes/`)
   - `Binding/Binder.cs` (~240KB) - Heart of semantic analysis, symbol resolution, type checking
   - `Types/DType.cs` (~160KB) - Internal discriminated union type representation (via `DKind` enum)
   - `IR/` - Intermediate Representation with explicit coercion nodes, normalized operators
   - `Texl/Builtins/` - Function signatures and type checking only (no implementations)
   - `Public/Engine.cs` - Main entry point for compilation
   - `Public/CheckResult.cs` - Lazy compilation pipeline result

2. **Microsoft.PowerFx.Interpreter** - Execution engine
   - `EvalVisitor.cs` - Walks IR tree to compute results
   - `Functions/Library*.cs` (~144KB) - Function implementations
   - `RecalcEngine.cs` - Extends `Engine` with evaluation and reactive formulas

3. **Microsoft.PowerFx.Connectors** - OpenAPI connector support for external APIs
4. **Microsoft.PowerFx.Json** - JSON serialization/deserialization
5. **Microsoft.PowerFx.LanguageServerProtocol** - LSP for IDE integration
6. **Microsoft.PowerFx.Repl** - Read-Eval-Print-Loop

### Key Architecture Patterns

**Separation of Compilation and Execution**: Core compiles to IR with zero evaluation code. Multiple backends (JavaScript, SQL, etc.) can consume the same IR independently.

**Symbol Table Composition**: Layered symbol tables (Config -> Engine -> Parameters) for flexible scoping:
- `SymbolTable` (mutable) / `ReadOnlySymbolTable` (immutable, composable) for definitions
- `SymbolValues` for runtime values paired with a SymbolTable

**CheckResult Workflow**:
```csharp
var check = engine.Check(expressionText, parameterType);
check.ThrowOnErrors();
var result = check.Eval();  // Interpreter only
```

**Type System**: `DType` is the internal representation, `FormulaType` is the public API wrapper. `CoercionMatrix.cs` defines valid conversions, `BinaryOpMatrix.cs` defines operator type rules.

**Display Names vs Logical Names**: Dual tracking throughout - display names are user-facing/localized (e.g., "First Name"), logical names are internal identifiers (e.g., "nwind_firstname").

### Naming: "Texl" = Power Fx

"Texl" is the internal codename (from Excel heritage). `TexlFunction`, `TexlLexer`, `TexlParser` all refer to Power Fx components.

## Adding a New Built-in Function

1. **Define signature** in `src/libraries/Microsoft.PowerFx.Core/Texl/Builtins/NewFunction.cs`:
   - Extend `TexlFunction`, implement `CheckInvocation` for type checking
   - Register in `BuiltinFunctionsCore._library`

2. **Implement logic** in `src/libraries/Microsoft.PowerFx.Interpreter/Functions/Library*.cs`:
   - Return `FormulaValue` results
   - Register in appropriate `Library` category

3. **Add tests** in `src/tests/Microsoft.PowerFx.Core.Tests.Shared/ExpressionTestCases/NewFunction.txt`

## Expression Test Cases

Tests use a `.txt` format in `tests/Microsoft.PowerFx.Core.Tests.Shared/ExpressionTestCases/`:

```
>> If(true, "yes", "no")
"yes"

>> 1+1
2

>> 1/0
Error({Kind:ErrorKind.Div0})
```

- `BaseRunner.cs` is the test harness infrastructure
- Tests run across multiple backends/configurations
- Special markers: `#skip`, `#error`, `#novalue`
- 60-second timeout per test

## Important Concepts

- **Cooperative Cancellation**: Interpreter checks `CancellationToken` and calls `Governor.Poll()` in loops to prevent runaway evaluation.
- **Immutability**: Most core data structures (DType, IR nodes, bound trees) are immutable. Symbol tables are mutable but have version hashes to detect concurrent mutations.
- **DPath**: Represents paths through type structure (e.g., "record.field.subfield"), used for type navigation and error reporting.
- **GuardSingleThreaded**: Detects concurrent mutations of mutable structures in development builds.
- **Feature Flags**: `Features` class controls language behavior. `Features.PowerFxV1` is the current standard. Configured via `PowerFxConfig`.
- **UDFs**: User-defined functions support recursion with stack depth tracking. Added via `Engine.AddUserDefinedFunction()`.

## Code Quality

- **StyleCop analyzers** enforce code style (configured in `src/PowerFx.ruleset`, relaxed in `src/PowerFx.Tests.ruleset`)
- **Warnings treated as errors** in Release builds
- C# 10.0 language version, .editorconfig enforces formatting (4-space indent, CRLF, UTF-8 BOM)
- Multi-targets: .NET Standard 2.0, .NET Framework 4.6.2, .NET 7.0
- Tests use **xUnit** with aggressive parallelization
- Test projects organized into `.Net4.6.2/` and `.Net7.0/` folders with `.Shared/` projects for shared code
- Versioning via Nerdbank.GitVersioning (NBGV), config in `version.json` (base version: 1.8)
