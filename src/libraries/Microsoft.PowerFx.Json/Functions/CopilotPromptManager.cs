// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    /// <summary>
    /// Manages Copilot system prompts and provides protection against prompt injection attacks.
    /// All system prompts are loaded from an external file (CopilotSystemPrompts.txt) for easy auditing.
    /// </summary>
    internal static class CopilotPromptManager
    {
        private static readonly Lazy<Dictionary<string, string>> _prompts = new Lazy<Dictionary<string, string>>(LoadPrompts);
        private static readonly Lazy<HashSet<string>> _forbiddenPatterns = new Lazy<HashSet<string>>(LoadForbiddenPatterns);
        private static readonly Lazy<int> _maxUserPromptLength = new Lazy<int>(LoadMaxPromptLength);

        private const int DefaultMaxPromptLength = 10000;
        private const string PromptsFileName = "CopilotSystemPrompts.txt";

        /// <summary>
        /// Gets the context instruction template.
        /// </summary>
        public static string ContextInstruction => GetPrompt("CONTEXT_INSTRUCTION");

        /// <summary>
        /// Gets the schema instruction template.
        /// </summary>
        public static string SchemaInstruction => GetPrompt("SCHEMA_INSTRUCTION");

        /// <summary>
        /// Gets the maximum allowed length for user prompts.
        /// </summary>
        public static int MaxUserPromptLength => _maxUserPromptLength.Value;

        /// <summary>
        /// Sanitizes a user-provided prompt to prevent prompt injection attacks.
        /// </summary>
        /// <param name="userPrompt">The user-provided prompt text.</param>
        /// <returns>A sanitized version of the prompt.</returns>
        /// <exception cref="ArgumentException">Thrown if the prompt exceeds maximum length.</exception>
        public static string SanitizeUserPrompt(string userPrompt)
        {
            if (string.IsNullOrEmpty(userPrompt))
            {
                return string.Empty;
            }

            // Check length limit
            if (userPrompt.Length > MaxUserPromptLength)
            {
                throw new ArgumentException($"User prompt exceeds maximum length of {MaxUserPromptLength} characters.");
            }

            var sanitized = userPrompt;

            // Remove forbidden patterns that could be used for prompt injection
            foreach (var pattern in _forbiddenPatterns.Value)
            {
                // Use regex to match pattern case-insensitively and replace with sanitized version
                sanitized = Regex.Replace(
                    sanitized,
                    Regex.Escape(pattern),
                    match => SanitizeMatch(match.Value),
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            // Additional protection: Escape any control sequences
            sanitized = EscapeControlSequences(sanitized);

            // Add clear boundary marker to separate user content from system instructions
            // This helps the LLM distinguish between user input and system prompts
            sanitized = $"[USER_PROMPT_START]\n{sanitized}\n[USER_PROMPT_END]";

            return sanitized;
        }

        /// <summary>
        /// Formats a system prompt template with the provided arguments.
        /// </summary>
        public static string FormatSystemPrompt(string template, params string[] args)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            try
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"Failed to format system prompt template: {ex.Message}", ex);
            }
        }

        private static string GetPrompt(string key)
        {
            if (_prompts.Value.TryGetValue(key, out var prompt))
            {
                return prompt;
            }

            throw new InvalidOperationException($"Prompt key '{key}' not found in {PromptsFileName}");
        }

        private static Dictionary<string, string> LoadPrompts()
        {
            var prompts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var content = LoadPromptsFile();

            var sections = content.Split(new[] { "\n---\n", "\r\n---\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                var lines = section.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                // Skip comments and empty lines
                var contentLines = lines.Where(l => !l.TrimStart().StartsWith("#", StringComparison.Ordinal)).ToArray();

                if (contentLines.Length < 2)
                {
                    continue;
                }

                // First line is the key in [BRACKETS]
                var keyLine = contentLines[0].Trim();
                if (keyLine.StartsWith("[", StringComparison.Ordinal) && keyLine.EndsWith("]", StringComparison.Ordinal))
                {
                    var key = keyLine.Substring(1, keyLine.Length - 2);
                    var value = string.Join("\n", contentLines.Skip(1)).Trim();
                    prompts[key] = value;
                }
            }

            return prompts;
        }

        private static HashSet<string> LoadForbiddenPatterns()
        {
            var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (_prompts.Value.TryGetValue("FORBIDDEN_PATTERNS", out var patternsText))
            {
                var lines = patternsText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();

                    // Skip comments
                    if (!trimmed.StartsWith("#", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(trimmed))
                    {
                        // Unescape the \n to actual newlines for pattern matching
                        var pattern = trimmed.Replace("\\n", "\n").Replace("\\r", "\r");
                        patterns.Add(pattern);
                    }
                }
            }

            return patterns;
        }

        private static int LoadMaxPromptLength()
        {
            if (_prompts.Value.TryGetValue("MAX_USER_PROMPT_LENGTH", out var lengthStr) &&
                int.TryParse(lengthStr.Trim(), out var length) &&
                length > 0)
            {
                return length;
            }

            return DefaultMaxPromptLength;
        }

        private static string LoadPromptsFile()
        {
            try
            {
                // Try to load from embedded resource first
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"{assembly.GetName().Name}.Functions.{PromptsFileName}";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }

                // Fallback: Try to load from file system (for development)
                var assemblyPath = Path.GetDirectoryName(assembly.Location);
                var promptsPath = Path.Combine(assemblyPath, PromptsFileName);

                if (File.Exists(promptsPath))
                {
                    return File.ReadAllText(promptsPath);
                }

                // Last resort: Try current directory
                if (File.Exists(PromptsFileName))
                {
                    return File.ReadAllText(PromptsFileName);
                }

                throw new FileNotFoundException($"Could not find {PromptsFileName} as embedded resource or file.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load {PromptsFileName}: {ex.Message}", ex);
            }
        }

        private static string SanitizeMatch(string match)
        {
            // Replace dangerous patterns with a safe placeholder
            // This preserves the user's intent while preventing injection
            return "[SANITIZED_CONTENT]";
        }

        private static string EscapeControlSequences(string input)
        {
            var sb = new StringBuilder(input.Length);

            foreach (char c in input)
            {
                // Allow common whitespace but escape other control characters
                if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                {
                    sb.Append($"\\u{(int)c:X4}");
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
