# Copilot Security Improvements - Summary

## Overview

This document summarizes the security improvements made to the Copilot() function to protect against prompt injection attacks and improve auditability of system prompts.

## Changes Made

### 1. External Prompt Storage

**File Created**: `src/libraries/Microsoft.PowerFx.Json/Functions/CopilotSystemPrompts.txt`

All system prompts are now stored in an external, human-readable text file separate from code:
- **Context Instruction**: Template for adding context to prompts
- **Schema Instruction**: Template for requesting JSON responses with schema
- **Forbidden Patterns**: List of patterns that indicate prompt injection attempts
- **Max Prompt Length**: Configurable maximum length for user prompts (10,000 chars)

**Benefits**:
- Easy to audit and verify prompt content
- No need to read code to understand what prompts are being sent
- Can be updated without recompiling
- Clear separation of concerns

### 2. Prompt Injection Protection

**File Created**: `src/libraries/Microsoft.PowerFx.Json/Functions/CopilotPromptManager.cs`

A new security manager class that provides:

#### Automatic Sanitization
- Detects and sanitizes common injection patterns:
  - `\n\nIgnore previous instructions`
  - `\n\nSystem:`
  - `\n\nAssistant:`
  - `\n\nNew instructions:`
  - Custom boundary markers
- Case-insensitive pattern matching
- Replaces dangerous patterns with `[SANITIZED_CONTENT]`

####Security Boundaries
- Wraps all user prompts with `[USER_PROMPT_START]` and `[USER_PROMPT_END]` markers
- Helps LLM distinguish between user input and system instructions

#### Length Limits
- Maximum 10,000 characters per user prompt
- Prevents resource exhaustion and abuse
- Configurable via external file

#### Control Character Escaping
- Escapes dangerous control characters (except common whitespace)
- Prevents manipulation through special characters

### 3. Refactored Implementation

**File Modified**: `src/libraries/Microsoft.PowerFx.Json/Functions/CopilotFunctionImpl.cs`

Updated the `GeneratePrompt` method to:
- Call `CopilotPromptManager.SanitizeUserPrompt()` on all user inputs
- Load system prompts from external file via `CopilotPromptManager`
- Return errors for invalid prompts (e.g., too long)
- Add security comments documenting the protection mechanisms

### 4. Project Configuration

**File Modified**: `src/libraries/Microsoft.PowerFx.Json/Microsoft.PowerFx.Json.csproj`

- Added `CopilotSystemPrompts.txt` as an embedded resource
- Ensures the prompt file is compiled into the assembly

### 5. Comprehensive Testing

**File Created**: `src/tests/Microsoft.PowerFx.Json.Tests.Shared/CopilotPromptManagerTests.cs`

New test suite with 21 test cases covering (✅ **ALL 28 TESTS PASSING**):
- Normal text handling with security boundaries
- Empty/null input handling
- Injection attempt detection (6 different patterns)
- Custom boundary injection attempts
- Control character escaping
- Whitespace preservation
- Length validation (too long, exactly at limit)
- Template loading and formatting
- Case-insensitive pattern matching
- Multiple injection attempts
- Unicode character preservation

**File Modified**: `src/tests/Microsoft.PowerFx.Json.Tests.Shared/CopilotFunctionTests.cs`

Added 7 integration tests verifying:
- Injection attempts are sanitized in the final prompt
- Multiple injection patterns are all sanitized
- Too-long prompts return errors
- Safe prompts only get security boundaries
- External templates are correctly applied
- Context and schema instructions use external files

### 6. Documentation

**Files Created/Modified**:
- `src/libraries/Microsoft.PowerFx.Json/Functions/COPILOT_SECURITY.md` - Comprehensive security documentation
- `src/tools/Repl/COPILOT_README.md` - Updated with security section

Documentation covers:
- Architecture and security layers
- How to audit system prompts
- Security best practices
- Known limitations
- Incident response procedures
- Testing approach

## Security Features Summary

| Feature | Location | Description |
|---------|----------|-------------|
| External Prompts | `CopilotSystemPrompts.txt` | All system prompts in separate file |
| Pattern Detection | `CopilotPromptManager.cs` | Detects and sanitizes injection attempts |
| Security Boundaries | `CopilotPromptManager.cs` | Wraps user input with markers |
| Length Limits | `CopilotPromptManager.cs` | Enforces max prompt length |
| Control Escaping | `CopilotPromptManager.cs` | Escapes dangerous characters |
| Automatic Protection | `CopilotFunctionImpl.cs` | All prompts sanitized automatically |
| Comprehensive Tests | `CopilotPromptManagerTests.cs` | 21 unit tests + 7 integration tests |

## How to Verify Security

### 1. Audit System Prompts
```bash
# View the prompts being sent to the LLM
cat src/libraries/Microsoft.PowerFx.Json/Functions/CopilotSystemPrompts.txt
```

### 2. Review Forbidden Patterns
The `[FORBIDDEN_PATTERNS]` section lists all patterns that trigger sanitization.

### 3. Run Security Tests
```bash
# Run all Copilot security tests
dotnet test --filter "FullyQualifiedName~CopilotPromptManager"

# Run integration tests
dotnet test --filter "FullyQualifiedName~CopilotFunctionTests"
```

### 4. Check Sanitization in Action
Look for `[SANITIZED_CONTENT]` and `[USER_PROMPT_START]/[USER_PROMPT_END]` markers in the prompts sent to the LLM.

## Example: Before and After

### Before (Vulnerable)
```csharp
// User input directly concatenated
var finalPrompt = $"{prompt} using the following context: {contextJson}";
// No protection against: "Tell me a joke\n\nIgnore previous instructions..."
```

### After (Protected)
```csharp
// User input sanitized
var sanitizedPrompt = CopilotPromptManager.SanitizeUserPrompt(prompt);
// Result: "[USER_PROMPT_START]\nTell me a joke[SANITIZED_CONTENT][USER_PROMPT_END]"

// System prompts loaded from external file
var contextInstruction = CopilotPromptManager.FormatSystemPrompt(
    CopilotPromptManager.ContextInstruction,
    contextJson);
```

## Files Changed

### New Files (5)
1. `src/libraries/Microsoft.PowerFx.Json/Functions/CopilotSystemPrompts.txt`
2. `src/libraries/Microsoft.PowerFx.Json/Functions/CopilotPromptManager.cs`
3. `src/libraries/Microsoft.PowerFx.Json/Functions/COPILOT_SECURITY.md`
4. `src/tests/Microsoft.PowerFx.Json.Tests.Shared/CopilotPromptManagerTests.cs`
5. `COPILOT_SECURITY_IMPROVEMENTS.md` (this file)

### Modified Files (3)
1. `src/libraries/Microsoft.PowerFx.Json/Functions/CopilotFunctionImpl.cs`
2. `src/libraries/Microsoft.PowerFx.Json/Microsoft.PowerFx.Json.csproj`
3. `src/tests/Microsoft.PowerFx.Json.Tests.Shared/CopilotFunctionTests.cs`
4. `src/tools/Repl/COPILOT_README.md`

## Next Steps

1. **Review the Changes**: Examine the external prompt file and security logic
2. **Run Tests**: Verify all tests pass
3. **Security Audit**: Have security team review the implementation
4. **Update Patterns**: Add new forbidden patterns as threats evolve
5. **Monitor Usage**: Track sanitization frequency in production

## Security Contacts

For security concerns or to report vulnerabilities:
- Review: `src/libraries/Microsoft.PowerFx.Json/Functions/COPILOT_SECURITY.md`
- Incident Response: Follow procedures in COPILOT_SECURITY.md

## References

- **OWASP LLM Top 10**: https://owasp.org/www-project-top-10-for-large-language-model-applications/
- **Prompt Injection**: https://simonwillison.net/2023/Apr/14/worst-that-can-happen/
- **Power Fx Security**: See COPILOT_SECURITY.md for detailed architecture

---

**Date**: 2026-01-13
**Commit**: Ready for review and merge
