// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter.Localization;
using Microsoft.PowerFx.Types;

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
                { new IsMatchFunction(), new IsMatchImplementation(regexTimeout) },
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
                    return new BlankValue(IRContext.NotInSource(new KnownRecordType(GetRecordTypeFromRegularExpression(regexAltered, regexOptions))));
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

                return TableValue.NewTable(new KnownRecordType(GetRecordTypeFromRegularExpression(regexAltered, regexOptions)), records.ToArray());
            }
        }

        internal abstract class RegexCommonImplementation : IAsyncTexlFunction
        {
            internal abstract FormulaValue InvokeRegexFunction(string input, string regex, string options);

            protected abstract string DefaultRegexOptions { get; }

            protected const string FULLMATCH = "FullMatch";
            protected const string STARTMATCH = "StartMatch";
            protected const string SUBMATCHES = "SubMatches";

            protected const string DefaultIsMatchOptions = "^c$";
            protected const string DefaultMatchOptions = "c";
            protected const string DefaultMatchAllOptions = "c";

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

            protected (string, RegexOptions) AlterRegex_DotNet(string regex, string options)
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

                bool freeSpacing = options.Contains("x");
                bool multiline = options.Contains("m");
                bool ignoreCase = options.Contains("i");
                bool dotAll = options.Contains("s");
                bool matchStart = options.Contains("^");
                bool matchEnd = options.Contains("$");
                bool numberedSubMatches = options.Contains("N");

                for (; index < regex.Length; index++)
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
                                for (index++; index < regex.Length && regex[index] != '\r' && regex[index] != '\n'; index++)
                                {
                                    // skip the comment characters until the next newline, in case it includes [ ] 
                                }

                                // need to replace a \r ending comment (supported by Power Fx) with a \n ending comment (supported by .NET)
                                // also need to make sure the comment terminates with a newline in case we add a "$" below
                                // need something to be emitted to avoid "\1#" & Char(10) & "1" being interpreted as "\11"
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

                                // need something to be emitted to avoid "\1(?:)1" being interpreted as "\11"
                                altered.Append("(?:)");
                            }
                            else
                            {
                                altered.Append(regex[index]);
                            }

                            break;

                        case '\\':
                            altered.Append("\\");
                            if (++index < regex.Length)
                            {
                                altered.Append(regex[index]);
                            }

                            break;

                        case '.':
                            altered.Append(!openCharacterClass && !dotAll ? @"[^\r\n]" : ".");
                            break;

                        case '^':
                            altered.Append(!openCharacterClass && multiline ? @"(?<=\A|\r\n|\r|\n)" : "^");
                            break;

                        case '$':
                            altered.Append(openCharacterClass ? "$" : (multiline ? @"(?=\z|\r\n|\r|\n)" : @"(?=\z|\r\n\z|\r\z|\n\z)"));
                            break;

                        default:
                            altered.Append(regex[index]);
                            break;
                    }
                }

                RegexOptions alteredOptions = RegexOptions.CultureInvariant |
                    (multiline ? RegexOptions.Multiline : 0) |
                    (ignoreCase ? RegexOptions.IgnoreCase : 0) |
                    (dotAll ? RegexOptions.Singleline : 0) |
                    (freeSpacing ? RegexOptions.IgnorePatternWhitespace : 0) |
                    (numberedSubMatches ? 0 : RegexOptions.ExplicitCapture);

                return ((matchStart ? "^" : string.Empty) + altered.ToString() + (matchEnd ? "$" : string.Empty), alteredOptions);
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

            protected static DType GetRecordTypeFromRegularExpression(string regularExpression, RegexOptions regularExpressionOptions)
            {
                Dictionary<string, TypedName> propertyNames = new ();
                Regex rex = new Regex(regularExpression, regularExpressionOptions);

                propertyNames.Add(FULLMATCH, new TypedName(DType.String, new DName(FULLMATCH)));
                propertyNames.Add(STARTMATCH, new TypedName(DType.Number, new DName(STARTMATCH)));
                if ((regularExpressionOptions & RegexOptions.ExplicitCapture) == 0)
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
