// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter.Localization;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Texl.Builtins.BaseMatchFunction;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        // https://learn.microsoft.com/en-us/power-platform/power-fx/reference/function-ismatch        

        /// <summary>
        /// Creates instances of the [Is]Match[All] functions and returns them so they can be added to the runtime.
        /// </summary>        
        /// <param name="regexTimeout">Timeout duration for regular expression execution. Default is 1 second.</param>
        /// <param name="regexCache">Regular expression type cache.</param>        
        /// <returns></returns>
        internal static Dictionary<TexlFunction, IAsyncTexlFunction> RegexFunctions(TimeSpan regexTimeout, RegexTypeCache regexCache)
        {
            if (regexTimeout == TimeSpan.Zero)
            {
                regexTimeout = new TimeSpan(0, 0, 1);
            }

            if (regexTimeout.TotalMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regexTimeout), "Timeout duration for regular expression execution must be positive.");
            }

            return new Dictionary<TexlFunction, IAsyncTexlFunction>()
            {
                { new IsMatchFunction(regexCache), new IsMatchImplementation(regexTimeout) },
                { new MatchFunction(regexCache), new MatchImplementation(regexTimeout) },
                { new MatchAllFunction(regexCache), new MatchAllImplementation(regexTimeout) }
            };
        }

        internal class IsMatchImplementation : RegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            protected override string DefaultRegexOptions => DefaultIsMatchOptions;

            public IsMatchImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            internal override FormulaValue InvokeRegexFunction(string input, string regex, string options)
            {
                var (regexAltered, regexOptions) = AlterRegex_DotNet(regex, options);
                Regex rex = new Regex(regexAltered, regexOptions, _regexTimeout);
                bool b = rex.IsMatch(input);

                return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), b);
            }
        }

        internal class MatchImplementation : RegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            protected override string DefaultRegexOptions => DefaultMatchOptions;

            public MatchImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            internal override FormulaValue InvokeRegexFunction(string input, string regex, string options)
            {
                var (regexAltered, regexOptions) = AlterRegex_DotNet(regex, options);
                Regex rex = new Regex(regexAltered, regexOptions, _regexTimeout);
                Match m = rex.Match(input);

                if (!m.Success)
                {
                    return new BlankValue(IRContext.NotInSource(new KnownRecordType(GetRecordTypeFromRegularExpression(rex))));
                }

                return GetRecordFromMatch(rex, m, regexOptions);
            }
        }

        internal class MatchAllImplementation : RegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            protected override string DefaultRegexOptions => DefaultMatchAllOptions;

            public MatchAllImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            internal override FormulaValue InvokeRegexFunction(string input, string regex, string options)
            {
                var (regexAltered, regexOptions) = AlterRegex_DotNet(regex, options);
                Regex rex = new Regex(regexAltered, regexOptions, _regexTimeout);
                MatchCollection mc = rex.Matches(input);
                List<RecordValue> records = new ();

                foreach (Match m in mc)
                {
                    records.Add(GetRecordFromMatch(rex, m, regexOptions));
                }

                return TableValue.NewTable(new KnownRecordType(GetRecordTypeFromRegularExpression(rex)), records.ToArray());
            }
        }

        internal abstract class RegexCommonImplementation : IAsyncTexlFunction
        {
            internal abstract FormulaValue InvokeRegexFunction(string input, string regex, string options);

            protected abstract string DefaultRegexOptions { get; }

            protected const string FULLMATCH = "FullMatch";
            protected const string STARTMATCH = "StartMatch";
            protected const string SUBMATCHES = "SubMatches";

            protected const string DefaultIsMatchOptions = MatchOptionString.Contains;
            protected const string DefaultMatchOptions = MatchOptionString.Contains;
            protected const string DefaultMatchAllOptions = MatchOptionString.Contains;

            public Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (args[0] is not StringValue && args[0] is not BlankValue)
                {
                    return Task.FromResult<FormulaValue>(args[0] is ErrorValue ? args[0] : CommonErrors.InvalidArgumentError(args[0].IRContext, RuntimeStringResources.ErrInvalidArgument));
                }

                string regularExpression;
                switch (args[1])
                {
                    case StringValue sv1:
                        regularExpression = sv1.Value;
                        break;
                    case OptionSetValue osv1:
                        regularExpression = (string)osv1.ExecutionValue;
                        break;
                    default:
                        return Task.FromResult<FormulaValue>(args[1] is ErrorValue ? args[1] : CommonErrors.InvalidArgumentError(args[1].IRContext, RuntimeStringResources.ErrInvalidArgument));
                }

                string inputString = args[0] is StringValue sv0 ? sv0.Value : string.Empty;

                string matchOptions;
                if (args.Length == 3)
                {
                    switch (args[2])
                    {
                        case StringValue sv3:
                            matchOptions = sv3.Value;
                            break;
                        case OptionSetValue osv3:
                            matchOptions = (string)osv3.ExecutionValue;
                            break;
                        default:
                            return Task.FromResult<FormulaValue>(args[2] is ErrorValue ? args[2] : CommonErrors.InvalidArgumentError(args[2].IRContext, RuntimeStringResources.ErrInvalidArgument));
                    }

                    // don't override complete/contains/beginswith/endswith if already given, all these options include Contains ("c")
                    if (!matchOptions.Contains(MatchOptionChar.ContainsBeginsEndsComplete))
                    {
                        matchOptions += DefaultRegexOptions;
                    }
                }
                else
                {
                    matchOptions = DefaultRegexOptions;
                }

                try
                {
                    return Task.FromResult(InvokeRegexFunction(inputString, regularExpression, matchOptions));
                }
                catch (RegexMatchTimeoutException rexTimeoutEx)
                {
                    return Task.FromResult<FormulaValue>(new ErrorValue(args[0].IRContext, new ExpressionError()
                    {
                        ResourceKey = RuntimeStringResources.ErrRegexTimeoutException,
                        Span = args[0].IRContext.SourceContext,
                        Kind = ErrorKind.Timeout,
                        MessageArgs = new object[] { rexTimeoutEx.MatchTimeout.TotalMilliseconds, rexTimeoutEx.Message }
                    }));
                }

#pragma warning disable SA1119  // Statement should not use unnecessary parenthesis
                
                // Internal exception till .Net 7 where it becomes public
                // .Net 4.6.2 will throw ArgumentException
                catch (Exception rexParseEx) when ((rexParseEx.GetType().Name.Equals("RegexParseException", StringComparison.OrdinalIgnoreCase)) || rexParseEx is ArgumentException)
                {
                    return Task.FromResult<FormulaValue>(new ErrorValue(args[1].IRContext, new ExpressionError()
                    {
                        ResourceKey = RuntimeStringResources.ErrInvalidRegexException,
                        Span = args[1].IRContext.SourceContext,
                        Kind = ErrorKind.BadRegex,
                        MessageArgs = new object[] { rexParseEx.Message }
                    }));
                }

#pragma warning restore SA1119  // Statement should not use unnecessary parenthesis
            }

            protected static (string, RegexOptions) AlterRegex_DotNet(string regex, string options)
            {
                var altered = new StringBuilder();
                bool openCharacterClass = false;                       // are we defining a character class?
                int index = 0;

                Match inlineOptions = Regex.Match(regex, @"\A\(\?([imnsx]+)\)");
                if (inlineOptions.Success)
                {
                    options = options + inlineOptions.Groups[1];
                    index = inlineOptions.Length;
                }

                bool freeSpacing = options.Contains(MatchOptionChar.FreeSpacing);
                bool multiline = options.Contains(MatchOptionChar.Multiline);
                bool ignoreCase = options.Contains(MatchOptionChar.IgnoreCase);
                bool dotAll = options.Contains(MatchOptionChar.DotAll);
                bool matchStart = options.Contains(MatchOptionChar.Begins);
                bool matchEnd = options.Contains(MatchOptionChar.Ends);
                bool numberedSubMatches = options.Contains(MatchOptionChar.NumberedSubMatches);

                // Can't add options ^ and $ too early as there may be freespacing comments, centralize the logic here and call subfunctions
                // ^ doesn't require any translation if not in multilline, only matches the start of the string
                // MatchAll( "1a3" & Char(13) & "2b4", "(?m)^\d" ) would not match "2" without translation
                string AlterStart() => openCharacterClass ? "^" : (multiline ? @"(?:(?<=\A|\r\n|[\n" + MatchWhiteSpace.NewLineEscapesWithoutCRLF + @"])|(?<=\r)(?!\n))" : "^");

                // $ does require translation if not in multilline, as $ does look past newlines to the end in .NET but it doesn't take into account \r
                // MatchAll( "1a3" & Char(13) & "2b4" & Char(13), "(?m)\d$" ) would not match "3" or "4" without translation
                // Match( "1a3" & Char(13), "\d$" ) would also not match "3" without translation
                string AlterEnd() => openCharacterClass ? "$" : (multiline ? @"(?:(?=\r\n|[\r" + MatchWhiteSpace.NewLineEscapesWithoutCRLF + @"]|\z)|(?<!\r)(?=\n))" : @"(?:(?=\r\n\z|[\r" + MatchWhiteSpace.NewLineEscapesWithoutCRLF + @"]?\z)|(?<!\r)(?=\n\z))");

                for (; index < regex.Length; index++)
                {
                    if (freeSpacing && !openCharacterClass && MatchWhiteSpace.IsSpaceNewLine(regex[index]))
                    {
                        altered.Append(' ');
                    }
                    else if (!openCharacterClass && char.IsHighSurrogate(regex[index]) && index + 1 < regex.Length && char.IsLowSurrogate(regex[index + 1]))
                    {
                        // treat a surrogtae pair as one character
                        altered.Append("(?:");
                        altered.Append(regex.Substring(index, 2));
                        altered.Append(")");
                        index++;
                    }
                    else
                    {
                        switch (regex[index])
                        {
                            case '[':
                                openCharacterClass = true;
                                altered.Append('[');
                                break;

                            case ']':
                                openCharacterClass = false;
                                altered.Append(']');
                                break;

                            case '#':
                                if (freeSpacing && !openCharacterClass)
                                {
                                    for (index++; index < regex.Length && !MatchWhiteSpace.IsNewLine(regex[index]); index++)
                                    {
                                        // skip the comment characters until the next newline, in case it includes [ ] 
                                    }

                                    // need something to be emitted to avoid "\1#" & Char(10) & "1" being interpreted as "\11"
                                    // need to replace a \r ending comment (supported by Power Fx) with a \n ending comment (supported by .NET)
                                    // also need to make sure the comment terminates with a newline in case we add a "$" below
                                    altered.Append("\n");
                                }
                                else
                                {
                                    altered.Append('#');
                                }

                                break;

                            case '(':
                                // inline comment
                                if (regex.Length - index > 2 && regex[index + 1] == '?' && regex[index + 2] == '#')
                                {
                                    for (index++; index < regex.Length && regex[index] != ')'; index++)
                                    {
                                        // skip the comment characters until the next closing paren, in case it includes [ ] 
                                    }

                                    // need something to be emitted to avoid "\1(?#)1" being interpreted as "\11"
                                    altered.Append("(?#)");
                                }
                                else
                                {
                                    altered.Append(regex[index]);
                                }

                                break;

                            case '\\':
                                Match m;

                                // convert \u{...} notation to \u notation and surrogate pair if needed
                                // \u below 0xffff is allowed in character classes
                                if (index + 2 <= regex.Length &&
                                    regex[index + 1] == 'u' && regex[index + 2] == '{' &&
                                    (m = new Regex("^\\\\u\\{(?<hex>[0-9a-fA-F]{1,})\\}").Match(regex, index)).Success &&
                                    int.TryParse(m.Groups["hex"].Value, NumberStyles.HexNumber, null, out var hex) && hex >= 0 && hex <= 0x10ffff &&
                                    (!openCharacterClass || hex <= 0xffff))
                                {
                                    if (hex <= 0xffff)
                                    {
                                        altered.Append("\\u");
                                        altered.Append(hex.ToString("X4", CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        var highSurr = 0xd800 + (((hex - 0x10000) >> 10) & 0x3ff);
                                        var lowSurr = 0xdc00 + ((hex - 0x10000) & 0x3ff);
                                        altered.Append("(?:\\u");
                                        altered.Append(highSurr.ToString("X4", CultureInfo.InvariantCulture));
                                        altered.Append("\\u");
                                        altered.Append(lowSurr.ToString("X4", CultureInfo.InvariantCulture));
                                        altered.Append(")");
                                    }

                                    index += m.Length - 1;
                                }

                                // treat a surrogtae pair, as provided in two back-to-back \uxxxx tokens, as one character
                                else if (!openCharacterClass &&
                                    index + 12 <= regex.Length &&
                                    regex[index + 1] == 'u' &&
                                    (m = new Regex("^\\\\u(?<high>[0-9a-fA-F]{4})\\\\u(?<low>[0-9a-fA-F]{4})").Match(regex, index)).Success &&
                                    int.TryParse(m.Groups["high"].Value, NumberStyles.HexNumber, null, out var high) && char.IsHighSurrogate((char)high) &&
                                    int.TryParse(m.Groups["low"].Value, NumberStyles.HexNumber, null, out var low) && char.IsLowSurrogate((char)low))
                                {
                                    altered.Append("(?:");
                                    altered.Append(regex.Substring(index, 12));
                                    altered.Append(")");
                                    index += 11;
                                }

                                // all other escapes and use of \u
                                else
                                {
                                    altered.Append("\\");
                                    if (++index < regex.Length)
                                    {
                                        altered.Append(regex[index]);
                                    }
                                }

                                break;

                            case '.':
                                altered.Append(openCharacterClass ? "." :
                                                    (dotAll ? @"(?:[\ud800-\udbff][\udc00-\udfff]|.)" :
                                                              @"(?:[\ud800-\udbff][\udc00-\udfff]|[^" + MatchWhiteSpace.NewLineEscapes + "])"));  
                                break;

                            case '^':
                                altered.Append(AlterStart());
                                break;

                            case '$':
                                altered.Append(AlterEnd());
                                break;

                            default:
                                altered.Append(regex[index]);
                                break;
                        }
                    }
                }

                // multiline is not included as it is handled with the definitions of ^ and $ above
                RegexOptions alteredOptions = RegexOptions.CultureInvariant |
                    (ignoreCase ? RegexOptions.IgnoreCase : 0) |
                    (dotAll ? RegexOptions.Singleline : 0) |
                    (freeSpacing ? RegexOptions.IgnorePatternWhitespace : 0) |
                    (numberedSubMatches ? 0 : RegexOptions.ExplicitCapture);

                return ((matchStart ? AlterStart() : string.Empty) + altered.ToString() + (matchEnd ? AlterEnd() : string.Empty), alteredOptions);
            }

            protected static RecordValue GetRecordFromMatch(Regex rex, Match m, RegexOptions options)
            {
                Dictionary<string, NamedValue> fields = new ()
                {
                    { FULLMATCH, new NamedValue(FULLMATCH, StringValue.New(m.Value)) },
                    { STARTMATCH, new NamedValue(STARTMATCH, NumberValue.New((double)m.Index + 1)) }
                };

                List<RecordValue> subMatches = new List<RecordValue>();
                string[] groupNames = rex.GetGroupNames();

                for (int i = 0; i < groupNames.Length; i++)
                {
                    string groupName = groupNames[i];
                    string validName = DName.MakeValid(groupName, out _).Value;
                    Group g = m.Groups[i];
                    FormulaValue val = g.Success ? StringValue.New(g.Value) : BlankValue.NewBlank(FormulaType.String);

                    if (!int.TryParse(groupName, out _))
                    {
                        if (!fields.ContainsKey(validName))
                        {
                            fields.Add(validName, new NamedValue(validName, val));
                        }
                        else
                        {
                            fields[validName] = new NamedValue(validName, val);
                        }
                    }

                    if (i > 0)
                    {
                        subMatches.Add(FormulaValue.NewRecordFromFields(new NamedValue(TableValue.ValueName, val)));
                    }
                }

                if (!fields.ContainsKey(SUBMATCHES) && (options & RegexOptions.ExplicitCapture) == 0)
                {
                    var recordType = RecordType.Empty().Add(TableValue.ValueName, FormulaType.String);
                    fields.Add(SUBMATCHES, new NamedValue(SUBMATCHES, TableValue.NewTable(recordType, subMatches)));
                }

                return RecordValue.NewRecordFromFields(fields.Values);
            }

            protected static DType GetRecordTypeFromRegularExpression(Regex rex)
            {
                Dictionary<string, TypedName> propertyNames = new ();

                propertyNames.Add(FULLMATCH, new TypedName(DType.String, new DName(FULLMATCH)));
                propertyNames.Add(STARTMATCH, new TypedName(DType.Number, new DName(STARTMATCH)));
                if ((rex.Options & RegexOptions.ExplicitCapture) == 0)
                {
                    propertyNames.Add(SUBMATCHES, new TypedName(DType.CreateTable(new TypedName(DType.String, new DName(TexlFunction.ColumnName_ValueStr))), new DName(SUBMATCHES)));
                }

                foreach (string groupName in rex.GetGroupNames())
                {
                    if (!int.TryParse(groupName, out _))
                    {
                        DName validName = DName.MakeValid(groupName, out _);

                        if (!propertyNames.ContainsKey(validName.Value))
                        {
                            propertyNames.Add(validName.Value, new TypedName(DType.String, validName));
                        }
                    }
                }

                return DType.CreateRecord(propertyNames.Values);
            }
        }
    }
}
