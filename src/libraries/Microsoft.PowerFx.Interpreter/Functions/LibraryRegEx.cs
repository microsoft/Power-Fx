// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
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

            internal override FormulaValue InvokeRegexFunction(string input, string regex, RegexOptions options)
            {
                var regexAltered = AlterRegex_DotNet(regex, options);
                Regex rex = new Regex(regexAltered, options, _regexTimeout);
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

            internal override FormulaValue InvokeRegexFunction(string input, string regex, RegexOptions options)
            {
                var regexAltered = AlterRegex_DotNet(regex, options);
                Regex rex = new Regex(regexAltered, options, _regexTimeout);
                Match m = rex.Match(input);

                if (!m.Success)
                {
                    return new BlankValue(IRContext.NotInSource(new KnownRecordType(GetRecordTypeFromRegularExpression(regex, options))));
                }

                return GetRecordFromMatch(rex, m, options);
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

            internal override FormulaValue InvokeRegexFunction(string input, string regex, RegexOptions options)
            {
                var regexAltered = AlterRegex_DotNet(regex, options);
                Regex rex = new Regex(regexAltered, options, _regexTimeout);
                MatchCollection mc = rex.Matches(input);
                List<RecordValue> records = new ();

                foreach (Match m in mc)
                {
                    records.Add(GetRecordFromMatch(rex, m, options));
                }

                return TableValue.NewTable(new KnownRecordType(GetRecordTypeFromRegularExpression(regex, options)), records.ToArray());
            }
        }

        internal abstract class RegexCommonImplementation : IAsyncTexlFunction
        {
            internal abstract FormulaValue InvokeRegexFunction(string input, string regex, RegexOptions options);

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
                    return Task.FromResult<FormulaValue>(args[0] is ErrorValue ? args[0] : CommonErrors.GenericInvalidArgument(args[0].IRContext));
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
                        return Task.FromResult<FormulaValue>(args[1] is ErrorValue ? args[1] : CommonErrors.GenericInvalidArgument(args[1].IRContext));
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
                            return Task.FromResult<FormulaValue>(args[2] is ErrorValue ? args[2] : CommonErrors.GenericInvalidArgument(args[2].IRContext));
                    }
                }
                else
                {
                    matchOptions = DefaultRegexOptions;
                }

                RegexOptions regOptions = System.Text.RegularExpressions.RegexOptions.CultureInvariant;

                if (matchOptions.Contains("i"))
                {
                    regOptions |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                }

                if (matchOptions.Contains("m"))
                {
                    regOptions |= System.Text.RegularExpressions.RegexOptions.Multiline;
                }

                if (matchOptions.Contains("^") && !regularExpression.StartsWith("^", StringComparison.Ordinal))
                {
                    regularExpression = "^" + regularExpression;
                }

                if (matchOptions.Contains("$") && !regularExpression.EndsWith("$", StringComparison.Ordinal))
                {
                    regularExpression += "$";
                }

                if (matchOptions.Contains("s"))
                {
                    regOptions |= System.Text.RegularExpressions.RegexOptions.Singleline;
                }

                // lack of NumberedSubMatches turns on ExplicitCapture for higher performance
                if (!matchOptions.Contains("N")) 
                {
                    regOptions |= System.Text.RegularExpressions.RegexOptions.ExplicitCapture;
                }

                try
                {
                    return Task.FromResult(InvokeRegexFunction(inputString, regularExpression, regOptions));
                }
                catch (RegexMatchTimeoutException rexTimeoutEx)
                {
                    return Task.FromResult<FormulaValue>(new ErrorValue(args[0].IRContext, new ExpressionError()
                    {
                        Message = $"Regular expression timeout (above {rexTimeoutEx.MatchTimeout.TotalMilliseconds} ms) - {rexTimeoutEx.Message}",
                        Span = args[0].IRContext.SourceContext,
                        Kind = ErrorKind.Timeout
                    }));
                }

#pragma warning disable SA1119  // Statement should not use unnecessary parenthesis
                
                // Internal exception till .Net 7 where it becomes public
                // .Net 4.6.2 will throw ArgumentException
                catch (Exception rexParseEx) when ((rexParseEx.GetType().Name.Equals("RegexParseException", StringComparison.OrdinalIgnoreCase)) || rexParseEx is ArgumentException)
                {
                    return Task.FromResult<FormulaValue>(new ErrorValue(args[1].IRContext, new ExpressionError()
                    {
                        Message = $"Invalid regular expression - {rexParseEx.Message}",
                        Span = args[1].IRContext.SourceContext,
                        Kind = ErrorKind.BadRegex
                    }));
                }

#pragma warning restore SA1119  // Statement should not use unnecessary parenthesis
            }

            protected string AlterRegex_DotNet(string regex, RegexOptions options)
            {
                var openCharacterClass = false;                       // are we defining a character class?
                var freeSpacing = (options & System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace) != 0 || Regex.IsMatch(regex, @"^\(\?[A-Za-z-[x]]*x");
                var multiline = (options & System.Text.RegularExpressions.RegexOptions.Multiline) != 0 || Regex.IsMatch(regex, @"^\(\?[A-Za-z-[m]]*m");
                var dotAll = (options & System.Text.RegularExpressions.RegexOptions.Singleline) != 0 || Regex.IsMatch(regex, @"^\(\?[A-Za-z-[s]]*s");
                var alteredRegex = new StringBuilder();

                for (int i = 0; i < regex.Length; i++)
                {
                    switch (regex[i])
                    {
                        case '[':
                            openCharacterClass = true;
                            alteredRegex.Append('[');
                            break;

                        case ']':
                            openCharacterClass = false;
                            alteredRegex.Append(']');
                            break;

                        case '#':
                            if (freeSpacing)
                            {
                                for (i++; i < regex.Length && regex[i] != '\r' && regex[i] != '\n'; i++)
                                {
                                    // skip the comment characters until the next newline, in case it includes [ ] 
                                }
                            }
                            else
                            {
                                alteredRegex.Append('#');
                            }

                            break;

                        case '\\':
                            alteredRegex.Append("\\");
                            if (++i < regex.Length)
                            {
                                alteredRegex.Append(regex[i]);
                            }

                            break;

                        case '.':
                            alteredRegex.Append(!openCharacterClass && !dotAll ? @"[^\r\n]" : ".");
                            break;

                        case '^':
                            alteredRegex.Append(!openCharacterClass && multiline ? @"(?<=\A|\r\n|\r|\n)" : "^");
                            break;

                        case '$':
                            alteredRegex.Append(openCharacterClass ? "$" : (multiline ? @"(?=\z|\r\n|\r|\n)" : @"(?=\z|\r\n\z|\r\z|\n\z)"));
                            break;

                        default:
                            alteredRegex.Append(regex[i]);
                            break;
                    }
                }

                return alteredRegex.ToString();
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
