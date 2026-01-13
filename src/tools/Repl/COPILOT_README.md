# Copilot Feature in Power Fx REPL

The Power Fx REPL supports the `Copilot()` function, which uses OpenAI's Responses API to generate text (and optionally structured outputs) from your formulas.

## Setup

Create a credentials file in the same directory as the REPL executable.

1. Copy the template file:
   ```
   cp chatgpt-credentials.json.template chatgpt-credentials.json
   ```
2. Edit `chatgpt-credentials.json` and add your OpenAI API key:
   ```json
   {
     "apiKey": "sk-your-actual-api-key-here",
     "model": "gpt-5-nano",
     "endpoint": "https://api.openai.com/v1/responses"
   }
   ```
3. Get an API key from https://platform.openai.com/api-keys

### Required / Optional Fields
- apiKey (required)
- model (optional, defaults to `gpt-5-nano` in the REPL if omitted)
- endpoint (optional, defaults to `https://api.openai.com/v1/responses`)

Only these three properties are currently read. Advanced request tuning parameters (temperature, top_p, etc.) are not sent because some newer models reject them. If needed, extend the request class in `Program.cs`.

## Supported Models (examples)
You can use any valid OpenAI Responses-capable model string. Examples:
- gpt-5-nano (default)
- gpt-5-mini-2025-08-07
- gpt-4.1-mini (if available)

Older Chat Completions-only models (e.g. `gpt-3.5-turbo`, `gpt-4o`) should be replaced with their Responses-capable equivalents. The REPL no longer calls `/v1/chat/completions`.

## How It Works Internally
- A POST request is made to `/v1/responses` with JSON body: `{ "model": "<model>", "input": "<prompt>" }` (plus `max_output_tokens` if needed).
- The response contains an array `output` with mixed items (e.g. reasoning traces and messages).
- The REPL aggregates the text from items where `type == "message"`, collecting each content element with `type == "output_text"` (or `"text"`).
- If no `message` items exist, it falls back to the first output item's textual content.

## Usage
Use the `Copilot()` function in formulas:

### Basic Usage
```powerfxx
// Simple question
Copilot("What is the capital of France?")

// With context
Copilot("Summarize this data", {Name: "John", Age: 30, City: "Paris"})

// With typed response
Copilot("Generate a person", {}, Type({Name: Text, Age: Number}))
```

### Function Signatures
1. Copilot(prompt: Text): Text
2. Copilot(prompt: Text, context: Any): Text
3. Copilot(prompt: Text, context: Any, returnType: Type): ReturnType

The structured overload requests a response that you can parse/interpret into the specified shape. (Actual enforcement still depends on your Copilot function implementation.)

## Examples

### Example 1: Simple Query
```powerfxx
Copilot("Explain what Power Fx is in one sentence")
```

### Example 2: With Context
```powerfx
Set(salesData, {Product: "Widget", Sales: 1000, Target: 1200});
Copilot("Is this product meeting its sales target?", salesData)
```

### Example 3: Structured Response
```powerfx
Copilot(
    "Generate a sample customer record", 
    {},
    Type({
        Name: Text,
        Email: Text,
        Age: Number,
        IsActive: Boolean
    })
)
```

### Example 4: Working with Tables
```powerfx
Set(employees, Table(
    {Name: "Alice", Dept: "Engineering"},
    {Name: "Bob", Dept: "Sales"}
));

Copilot("Count how many employees are in each department", employees)
```

## Response Parsing Behavior
If the raw JSON from the Responses API looks like:
```json
{
  "output": [
    { "type": "reasoning", "summary": [] },
    {
      "type": "message",
      "content": [ { "type": "output_text", "text": "Answer text here" } ],
      "role": "assistant"
    }
  ]
}
```
The REPL returns `"Answer text here"`.

## Security Notes

### API Key Security
- Never commit `chatgpt-credentials.json` (should be in `.gitignore`).
- Rotate API keys regularly.
- Monitor usage to avoid unexpected costs.

### Prompt Injection Protection
The Copilot function implements multiple layers of protection against prompt injection attacks:

1. **User Input Sanitization**: All user-provided prompts are automatically sanitized to prevent injection attempts. Common attack patterns like "Ignore previous instructions", "System:", "Assistant:", etc. are detected and sanitized.

2. **Security Boundaries**: User prompts are wrapped with `[USER_PROMPT_START]` and `[USER_PROMPT_END]` markers to clearly separate user content from system instructions.

3. **Length Limits**: User prompts are limited to 10,000 characters by default to prevent abuse.

4. **Control Character Escaping**: Dangerous control characters are automatically escaped to prevent manipulation.

5. **External Prompt Templates**: All system prompts (context instructions, schema instructions) are stored in an external file (`CopilotSystemPrompts.txt`) separate from the code, making them easy to audit and verify.

### Auditing System Prompts
System prompts used by the Copilot function are stored in:
- **Location**: `src/libraries/Microsoft.PowerFx.Json/Functions/CopilotSystemPrompts.txt`
- **Format**: Plain text with clearly marked sections
- **Review**: Any changes to this file should be carefully reviewed for security implications

To verify what prompts are being sent to the LLM:
1. Check the `CopilotSystemPrompts.txt` file for template text
2. Review `CopilotPromptManager.cs` for sanitization logic
3. Run the test suite in `CopilotPromptManagerTests.cs` to verify protection mechanisms

### Security Best Practices
- **Do not** include sensitive information in prompts
- **Do** validate and sanitize any user input before passing to Copilot()
- **Do** review the external prompt file after updates
- **Do** run security tests regularly to ensure protection mechanisms are working
- **Consider** additional application-level rate limiting for production use

## Troubleshooting

### "Copilot service not available"
- Ensure `chatgpt-credentials.json` exists and has valid JSON.
- Confirm the process is running from the directory containing the file.

### API Errors (Unsupported parameter, 401, 404)
- Check that you are using the Responses endpoint: `https://api.openai.com/v1/responses`.
- Remove unsupported parameters (temperature, etc.).
- Verify your model name is correct for the endpoint.
- Ensure the API key has quota and is not expired.

### Empty Output
- The model may have returned only reasoning traces. Try a simpler prompt.
- Inspect raw JSON by adding temporary logging around `jsonResponse` in `AskTextAsync`.

### Rate Limiting
- Implement retries with exponential backoff if needed (not built-in in REPL).
- Use smaller models (`gpt-5-nano`, `gpt-5-mini-...`) for iterative queries.

## Cost Considerations (Illustrative Only)
Pricing changes frequently. Check https://openai.com/pricing.

Lower-tier models (e.g. nano/mini) are best for experimentation. Higher-tier models have better reasoning but cost more per token.

## Extending
To add advanced parameters (e.g. `max_output_tokens`):
- Extend `ChatGptRequest` in `Program.cs`.
- Include the property when serializing.
- Ensure the target model supports it.

To handle tool calls or streaming, replace the simple `PostAsync` with a streamed handler or tool-aware request body.

## Notes
The previous Chat Completions endpoint (`/v1/chat/completions`) is no longer used. All calls now go through `/v1/responses` with a single `input` field instead of `messages`.
