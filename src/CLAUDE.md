# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Power Fx is a low-code general-purpose programming language based on spreadsheet-like formulas. This is the open-source implementation of the language used across Microsoft Power Platform. The codebase contains a complete compiler and interpreter infrastructure written in C#.

## Build Commands

### Building the Solution

```bash
# From src/ directory
dotnet build Microsoft.PowerFx.sln

# Or using MSBuild directly
msbuild Microsoft.PowerFx.sln -p:Configuration=Debug

# Build specific configuration
msbuild Microsoft.PowerFx.sln -p:Configuration=Release
msbuild Microsoft.PowerFx.sln -p:Configuration=DebugAll    # Multi-targeting (net462 + net7.0)
```

### Building NuGet Packages

```bash
# From src/ directory
.\buildLocalPackages.cmd [Configuration]    # Windows
# Default configuration is DebugAll if not specified
# Output packages go to src/outputpackages/
```

### Running Tests

```bash
# From src/ directory
.\runLocalTests.cmd [Configuration]    # Windows (uses VSTest)

# Or using dotnet CLI (from solution root)
dotnet test src/Microsoft.PowerFx.sln

# Run tests for a specific project
dotnet test src/tests/Microsoft.PowerFx.Core.Tests.Shared/Microsoft.PowerFx.Core.Tests.Shared.csproj

# Run a single test by name
dotnet test --filter "FullyQualifiedName~YourTestName"

# Run tests by category
dotnet test --filter "Category=ExpressionTest"
```

Note: The `runLocalTests.cmd` script runs tests for both .NET Framework 4.6.2 and .NET 7.0 configurations using VSTest.

### Code Analysis

```bash
# StyleCop analyzers are configured in PowerFx.ruleset
# Analysis runs automatically during build
# Warnings are treated as errors in Release builds
```

## Architecture Overview

### Core Components

The Power Fx implementation follows a classic multi-phase compiler architecture:

**Expression Text → [Lexer] → Tokens → [Parser] → AST → [Binder] → Typed AST → [IR Translator] → IR → [Interpreter/Backend] → Result**

1. **Microsoft.PowerFx.Core** - The language compiler (no evaluation)
   - `Lexer/` - Tokenization with culture-aware parsing
   - `Parser/` - Recursive descent parser producing AST (`Syntax/Nodes/`)
   - `Binding/` - Semantic analysis, symbol resolution, type checking (`Binder.cs` is the core)
   - `Types/` - Type system implementation (`DType.cs`, `FormulaType.cs`)
   - `IR/` - Intermediate Representation for backend consumption
   - `Texl/Builtins/` - Function signatures and type checking (no implementation)

2. **Microsoft.PowerFx.Interpreter** - Execution engine
   - `EvalVisitor.cs` - Walks IR tree to compute results
   - `Functions/Library*.cs` - Actual function implementations
   - `RecalcEngine.cs` - Extends `Engine` with evaluation and reactive formulas

3. **Microsoft.PowerFx.Connectors** - OpenAPI connector support for external APIs

4. **Microsoft.PowerFx.LanguageServerProtocol** - LSP implementation for IDE integration

5. **Microsoft.PowerFx.Json** - JSON serialization/deserialization

### Key Architecture Patterns

**Separation of Compilation and Execution**: The Core library only compiles to IR with zero evaluation code. This allows multiple backends (JavaScript, SQL, etc.) to consume the same IR independently.

**Symbol Table Composition**: Multiple symbol tables can be layered (Config → Engine → Parameters), allowing flexible scoping:
- `SymbolTable` - Mutable, contains variables, functions, enums, user-defined types
- `ReadOnlySymbolTable` - Immutable view, composable
- `SymbolValues` - Runtime values paired with SymbolTable

**CheckResult Workflow**: Central to compilation, with lazy phase evaluation:
```csharp
var check = engine.Check(expressionText, parameterType);
check.ThrowOnErrors();  // Compilation errors
var result = check.Eval();  // Execution (Interpreter only)
```

**Type System**:
- `DType` - Internal discriminated union type representation (via `DKind` enum)
- `FormulaType` - Public API wrapper around DType
- `CoercionMatrix.cs` - Defines valid type conversions
- `BinaryOpMatrix.cs` - Type rules for operators
- Supports: primitives, records, tables, functions, polymorphic types

**Intermediate Representation (IR)**:
- Purpose: Makes it easier for backends to consume compiler output
- Explicit coercion nodes (backends don't reimplement coercion matrix)
- Normalized operators (And() as both operator and function → unified)
- Enums translated to backing values
- Simplified scope handling
- Located in `Microsoft.PowerFx.Core/IR/`

**Display Names vs Logical Names**: Throughout the codebase, dual tracking of:
- Display names: User-facing, localized (e.g., "First Name")
- Logical names: Internal identifiers (e.g., "nwind_firstname")

### Adding a New Built-in Function

Functions are split between Core (signatures/type checking) and Interpreter (implementation):

1. **Define signature** in `src/libraries/Microsoft.PowerFx.Core/Texl/Builtins/NewFunction.cs`:
   - Extend `TexlFunction` base class
   - Implement `CheckInvocation` for type checking
   - Add to `BuiltinFunctionsCore._library`

2. **Implement logic** in `src/libraries/Microsoft.PowerFx.Interpreter/Functions/Library*.cs`:
   - Add static method with `[TexlFunction]` attribute or similar pattern
   - Use IR nodes as input parameters
   - Return `FormulaValue` results
   - Register in appropriate `Library` category

3. **Add tests** in `src/tests/Microsoft.PowerFx.Core.Tests.Shared/ExpressionTestCases/NewFunction.txt`:
   ```
   >> NewFunction(arg1, arg2)
   ExpectedResult

   >> NewFunction("test", 42)
   "test42"
   ```

### Expression Test Cases

Tests use a unique `.txt` file format in `tests/Microsoft.PowerFx.Core.Tests.Shared/ExpressionTestCases/`:

```
>> Expression
ExpectedResult

>> If(true, "yes", "no")
"yes"

>> 1+1
2

>> 1/0
Error({Kind:ErrorKind.Div0})
```

- `BaseRunner.cs` - Test harness infrastructure
- Tests run across multiple backends/configurations
- Special markers: `#skip`, `#error`, `#novalue`
- Fuzzy numeric comparisons for floating point
- 60-second timeout per test

### User-Defined Functions (UDFs)

- Parsed from Power Fx syntax
- `UserDefinedFunction.cs` in both Core and Interpreter
- Support for recursion with stack depth tracking
- Can be added via `Engine.AddUserDefinedFunction()`

### String Localization

- Resource files in `src/strings/PowerFxResources.*.resx`
- Embedded in assemblies
- Access via `StringResources` class

### Feature Flags

- `Features` class controls language behavior
- `Features.PowerFxV1` - Current standard
- Allows gradual feature rollout and compatibility modes
- Configured via `PowerFxConfig`

### Versioning

- Uses Nerdbank.GitVersioning (NBGV)
- Configuration in `version.json` at repository root
- Versions automatically derived from git tags/height
- Base version: 1.5
- Public releases from tags matching `v\d+\.\d+\.\d+$` or `release/\d+\.\d+$` branches

## Project Structure

```
src/
├── Microsoft.PowerFx.sln          # Main solution file
├── libraries/                      # Core libraries
│   ├── Microsoft.PowerFx.Core/           # Compiler (no evaluation)
│   ├── Microsoft.PowerFx.Interpreter/    # Execution engine
│   ├── Microsoft.PowerFx.Connectors/     # OpenAPI connectors
│   ├── Microsoft.PowerFx.Json/           # JSON integration
│   └── Microsoft.PowerFx.LanguageServerProtocol/  # LSP for IDE support
├── tests/                          # Test projects
│   ├── Microsoft.PowerFx.Core.Tests.Shared/
│   │   └── ExpressionTestCases/    # .txt test files
│   └── Microsoft.PowerFx.Interpreter.Tests.Shared/
├── strings/                        # Localized resource files
├── Directory.Build.props           # Common MSBuild properties
├── PowerFx.ruleset                 # StyleCop configuration
└── nuget.config                    # NuGet feed configuration

docs/                               # Language documentation
├── overview.md                     # Language design principles
├── data-types.md                   # Type system documentation
├── expression-grammar.md           # Formal grammar
└── operators.md                    # Operator reference
```

## Key Files to Understand

- **`Microsoft.PowerFx.Core/Binding/Binder.cs`** (245KB) - Heart of semantic analysis
- **`Microsoft.PowerFx.Core/Types/DType.cs`** (167KB) - Core type representation
- **`Microsoft.PowerFx.Interpreter/Functions/Library.cs`** (146KB) - Main function implementations
- **`Microsoft.PowerFx.Core/Public/Engine.cs`** - Main entry point for compilation
- **`Microsoft.PowerFx.Interpreter/RecalcEngine.cs`** - Evaluation and reactive formulas
- **`Microsoft.PowerFx.Interpreter/EvalVisitor.cs`** - IR tree walker for evaluation
- **`Microsoft.PowerFx.Core/Public/CheckResult.cs`** - Lazy compilation pipeline result

## Important Concepts

### Texl vs Power Fx
"Texl" is the internal codename (from Excel heritage) that appears throughout the codebase. Terms like `TexlFunction`, `TexlLexer`, `TexlParser` all refer to Power Fx components.

### Cooperative Cancellation
The interpreter regularly checks `CancellationToken` and calls `Governor.Poll()` for cooperative cancellation, especially in loops. This prevents runaway evaluation.

### Immutability
Most core data structures are immutable (DType, IR nodes, bound trees). Symbol tables are mutable but have version hashes to detect concurrent mutations.

### DPath
Represents paths through type structure (e.g., "record.field.subfield"). Used extensively for type navigation and error reporting.

### Guard Single Threaded
Core uses `GuardSingleThreaded` to detect concurrent mutations of mutable structures in development builds, catching threading bugs early.

## Configuration Files

- **`Directory.Build.props`** - Shared MSBuild properties across all projects
  - C# 10.0 language version
  - StyleCop analyzers (strict code quality)
  - Treat warnings as errors in Release
  - Assembly signing configuration
  - NuGet package metadata

- **`PowerFx.ruleset`** / **`PowerFx.Tests.ruleset`** - StyleCop rule configuration
  - Strict code analysis for production code
  - Relaxed rules for test code

- **`version.json`** - NBGV configuration for automatic versioning

## Development Notes

### Multiple .NET Targets
Projects multi-target:
- .NET Standard 2.0 (for broad compatibility)
- .NET Framework 4.6.2 (legacy support)
- .NET 7.0+ (modern runtime)

Test projects are organized into `.Net4.6.2/` and `.Net7.0/` folders with `.Shared/` projects containing shared test code.

### Build Configurations
- `Debug` / `Release` - Standard single-target builds
- `Debug462` / `Release462` - .NET Framework 4.6.2 specific
- `Debug70` / `Release70` - .NET 7.0 specific
- `DebugAll` / `ReleaseAll` - Multi-targeting (used by local build scripts)

### Code Quality
- StyleCop analyzers enforce consistent code style
- Warnings treated as errors in Release builds
- Code coverage configured in `CodeCoverage.runsettings`
- Performance tests in `Microsoft.PowerFx.Performance.Tests`

### Daily Builds
Daily packages published to Azure Artifacts:
```
https://pkgs.dev.azure.com/Power-Fx/7dd30b4a-31be-4ac9-a649-e6addd4d5b0a/_packaging/PowerFx/nuget/v3/index.json
```

See `dailyBuilds.md` for consumption details.

## Resources

- **Main Documentation**: `docs/overview.md` - Language design principles and philosophy
- **Samples Repository**: https://github.com/microsoft/power-fx-host-samples
- **Public NuGet Packages**: Search for "Microsoft.PowerFx.*" on nuget.org
- **Expression Grammar**: `docs/expression-grammar.md`
- **YAML Formula Grammar**: `docs/yaml-formula-grammar.md` (for source file storage)
