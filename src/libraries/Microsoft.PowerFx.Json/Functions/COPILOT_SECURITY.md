# Copilot Function Security Documentation

This document explains the security measures implemented in the Power Fx Copilot() function to protect against prompt injection and other security vulnerabilities.

## Overview

The Copilot() function integrates with LLM services (like OpenAI) to provide AI-powered text generation and structured output capabilities. Due to the nature of LLM interactions, special care must be taken to prevent prompt injection attacks where malicious users attempt to manipulate the LLM's behavior by crafting specific inputs.

## Architecture

### Separation of Concerns

The security architecture is built on three key principles:

1. **External Prompt Storage**: System prompts are stored in `CopilotSystemPrompts.txt`, separate from code
2. **Automatic Sanitization**: All user input is sanitized before being sent to the LLM
3. **Clear Boundaries**: User content is clearly marked and separated from system instructions

### Component Flow

```
User Input → CopilotPromptManager.SanitizeUserPrompt()
           → Add Security Boundaries
           → Combine with System Prompts (from external file)
           → Send to LLM Service
```

## Security Measures

### 1. User Input Sanitization

**Location**: `CopilotPromptManager.cs`

All user-provided prompts are automatically sanitized using the following mechanisms:

#### Pattern Detection and Removal
Common prompt injection patterns are detected and replaced with `[SANITIZED_CONTENT]`:
- `\n\nIgnore previous instructions`
- `\n\nIgnore all previous`
- `\n\nDisregard previous`
- `\n\nSystem:`
- `\n\nAssistant:`
- `\n\nNew instructions:`
- Custom boundary markers like `---SYSTEM---` or `---END OF USER PROMPT---`

Pattern matching is **case-insensitive** to catch variations like:
- "IGNORE PREVIOUS INSTRUCTIONS"
- "ignore previous instructions"
- "Ignore Previous Instructions"

#### Control Character Escaping
Dangerous control characters (except common whitespace like `\n`, `\r`, `\t`) are escaped to prevent manipulation:
```csharp
// Example: \x00 becomes \u0000
```

### 2. Security Boundaries

**Purpose**: Clearly mark where user input begins and ends

All user prompts are wrapped with markers:
```
[USER_PROMPT_START]
{user's sanitized input}
[USER_PROMPT_END]
```

This helps the LLM understand that everything within these boundaries is user-provided content, not system instructions.

### 3. Length Limits

**Default Maximum**: 10,000 characters

Prompts exceeding this length are rejected with an error before being sent to the LLM. This prevents:
- Resource exhaustion attacks
- Attempts to overwhelm sanitization logic
- Excessive API costs

**Configuration**: The limit is configurable in `CopilotSystemPrompts.txt`:
```
[MAX_USER_PROMPT_LENGTH]
10000
---
```

### 4. External System Prompts

**Location**: `CopilotSystemPrompts.txt`

All system prompts are stored externally for easy auditing:

#### Context Instruction Template
```
[CONTEXT_INSTRUCTION]
using the following context: {0}
---
```

#### Schema Instruction Template
```
[SCHEMA_INSTRUCTION]
Provide the response as a pure JSON value (without any introductions, prefixes, suffixes, summaries, or markings around it), according to the following schema:
{0}
---
```

### 5. Forbidden Patterns List

**Location**: `CopilotSystemPrompts.txt`

The list of forbidden patterns is maintained in the external file, allowing security teams to update it without modifying code:

```
[FORBIDDEN_PATTERNS]
\n\nIgnore previous instructions
\n\nIgnore all previous
\n\nDisregard previous
\n\nSystem:
\n\nAssistant:
\n\nNew instructions:
---
```

**Note**: Patterns use `\n` notation which is converted to actual newlines during loading.

## Testing

### Unit Tests

**Location**: `CopilotPromptManagerTests.cs`

Comprehensive test coverage includes:

1. **Basic Sanitization Tests**
   - Normal text handling
   - Empty/null input
   - Security boundary addition

2. **Injection Detection Tests**
   - Single injection attempt detection
   - Multiple injection attempts
   - Case-insensitive pattern matching
   - Custom boundary injection attempts

3. **Character Handling Tests**
   - Control character escaping
   - Unicode character preservation
   - Whitespace preservation

4. **Length Validation Tests**
   - Maximum length enforcement
   - At-limit acceptance
   - Over-limit rejection

5. **Template Loading Tests**
   - Context instruction availability
   - Schema instruction availability
   - Format string validation

### Integration Tests

**Location**: `CopilotFunctionTests.cs`

End-to-end tests verify:

1. Injection attempts are sanitized in the final prompt sent to LLM
2. Safe prompts only get security boundaries (no sanitization)
3. Too-long prompts return errors before reaching LLM
4. External templates are correctly loaded and applied
5. Context and schema instructions use external templates

## Security Review Checklist

When reviewing changes to the Copilot security implementation:

### Code Review
- [ ] Are new user input paths sanitized via `CopilotPromptManager.SanitizeUserPrompt()`?
- [ ] Are system prompts loaded from `CopilotSystemPrompts.txt` (not hardcoded)?
- [ ] Is the separation between user input and system instructions maintained?
- [ ] Are new test cases added for any new patterns or behaviors?

### Prompt Template Review
- [ ] Do templates avoid including placeholders that could be manipulated?
- [ ] Are format strings using positional parameters (`{0}`, `{1}`) rather than named ones?
- [ ] Is the intent of each template clear and documented?

### Pattern List Review
- [ ] Are new forbidden patterns necessary and sufficient?
- [ ] Do patterns avoid false positives (blocking legitimate user input)?
- [ ] Are patterns documented with examples of attacks they prevent?

## Known Limitations

### 1. Context Injection
While user prompts are sanitized, the **context** parameter (second argument to Copilot) is serialized as-is. Applications should validate context data before passing it to Copilot().

### 2. Schema Injection
The **schema** parameter (third argument) is parsed as a Power Fx type string. Malformed schemas could potentially cause parsing errors but should not lead to injection (the schema is used to generate JSON Schema, not sent as raw text).

### 3. Advanced Injection Techniques
Sophisticated adversaries may find ways to craft inputs that bypass sanitization. The defense is layered but not absolute. Consider:
- Additional application-level validation
- Rate limiting per user
- Monitoring and alerting for suspicious patterns
- Regular review and updates to forbidden patterns list

### 4. LLM-Specific Vulnerabilities
Some LLMs may have specific prompt injection vulnerabilities beyond what we can protect against at the application level. Stay informed about LLM security research and update patterns accordingly.

## Incident Response

If a prompt injection vulnerability is discovered:

1. **Immediate Actions**
   - Document the attack vector with example inputs
   - Add the pattern to `[FORBIDDEN_PATTERNS]` in `CopilotSystemPrompts.txt`
   - Create test cases in `CopilotPromptManagerTests.cs`
   - Verify the fix with integration tests

2. **Review and Release**
   - Security review the changes
   - Update this documentation with lessons learned
   - Release as a security patch
   - Notify users if applicable

3. **Monitoring**
   - Monitor for similar attack patterns
   - Consider additional instrumentation/logging
   - Review other user input paths for similar issues

## Best Practices for Applications Using Copilot()

### Input Validation
```powerfx
// BAD: Directly passing unsanitized user input
Copilot(UserInput.Text)

// GOOD: Validate and constrain user input at application level
If(
    Len(UserInput.Text) <= 1000 && !IsBlank(UserInput.Text),
    Copilot(UserInput.Text),
    Error("Invalid input")
)
```

### Context Data Validation
```powerfx
// BAD: Passing unvalidated data as context
Copilot("Analyze this", UnvalidatedRecord)

// GOOD: Pass only known-safe, validated data
Copilot(
    "Analyze this",
    {
        Name: Left(Record.Name, 100),  // Truncate to safe length
        Score: If(IsNumeric(Record.Score), Record.Score, 0)
    }
)
```

### Rate Limiting
Implement application-level rate limiting:
- Limit calls per user per hour
- Limit calls per session
- Implement exponential backoff on failures

### Monitoring
Log and monitor:
- Sanitization trigger frequency (indicates attack attempts)
- Error rates from Copilot calls
- Unusual patterns in user prompts
- API costs and usage patterns

## References

### External Resources
- [OWASP Top 10 for LLM Applications](https://owasp.org/www-project-top-10-for-large-language-model-applications/)
- [Prompt Injection Attacks (Simon Willison)](https://simonwillison.net/2023/Apr/14/worst-that-can-happen/)
- [OpenAI Safety Best Practices](https://platform.openai.com/docs/guides/safety-best-practices)

### Internal Documentation
- `COPILOT_README.md` - User-facing documentation
- `CopilotSystemPrompts.txt` - Prompt templates (audit this file regularly)
- `CopilotPromptManager.cs` - Sanitization implementation
- `CopilotPromptManagerTests.cs` - Security test suite

## Changelog

### 2026-01-13
- Initial security documentation
- Implemented external prompt storage
- Added prompt injection protection with pattern detection
- Created comprehensive test suite
- Added security boundaries for user input
- Implemented length limits and control character escaping
