// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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

        public const string FULLMATCH = "FullMatch";
        public const string STARTMATCH = "StartMatch";
        public const string SUBMATCHES = "SubMatches";

        private const string DefaultIsMatchOptions = "^c$";
        private const string DefaultMatchOptions = "c";
        private const string DefaultMatchAllOptions = "c";

        /// <summary>
        /// Enable [Is]Match[All] functions.
        /// </summary>        
        /// <param name="regexTimeout">Timeout duration for regular expression execution. Default is 1 second.</param>
        /// <param name="regexCache">Regular expression type cache.</param>        
        /// <returns></returns>
        internal static Dictionary<TexlFunction, IAsyncTexlFunction> RegexFunctions(TimeSpan regexTimeout, RegexCache regexCache)
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

        internal class IsMatchImplementation : CommonMatchImplementation, IAsyncTexlFunction
        {
            private readonly TimeSpan _regexTimeout;

            public IsMatchImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            public Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return CommonMatchImpl(args, DefaultIsMatchOptions, (input, regex, options) =>
                {
                    Regex rex = new Regex(regex, options, _regexTimeout);
                    bool b = rex.IsMatch(input);

                    return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), b);
                });
            }
        }

        internal class MatchImplementation : CommonMatchImplementation, IAsyncTexlFunction
        {
            private readonly TimeSpan _regexTimeout;
            public MatchImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            public Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return CommonMatchImpl(args, DefaultMatchOptions, (input, regex, options) =>
                {
                    Regex rex = new Regex(regex, options, _regexTimeout);
                    Match m = rex.Match(input);

                    if (!m.Success)
                    {
                        return new BlankValue(IRContext.NotInSource(new KnownRecordType(GetRecordTypeFromRegularExpression(regex))));
                    }

                    return GetRecordFromMatch(rex, m);
                });
            }
        }

        internal class MatchAllImplementation : CommonMatchImplementation, IAsyncTexlFunction
        {
            private readonly TimeSpan _regexTimeout;

            public MatchAllImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            public Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return CommonMatchImpl(args, DefaultMatchAllOptions, (input, regex, options) =>
                {
                    Regex rex = new Regex(regex, options, _regexTimeout);
                    MatchCollection mc = rex.Matches(input);
                    List<RecordValue> records = new ();

                    foreach (Match m in mc)
                    {
                        records.Add(GetRecordFromMatch(rex, m));
                    }

                    return TableValue.NewTable(new KnownRecordType(GetRecordTypeFromRegularExpression(regex)), records.ToArray());
                });
            }
        }

        private static RecordValue GetRecordFromMatch(Regex rex, Match m)
        {
            Dictionary<string, NamedValue> fields = new ()
                {
                    { FULLMATCH, new NamedValue(FULLMATCH, StringValue.New(m.Value)) },
                    { STARTMATCH, new NamedValue(STARTMATCH, NumberValue.New(m.Index + 1)) }
                };

            List<string> subMatches = new List<string>();
            string[] groupNames = rex.GetGroupNames();

            for (int i = 0; i < groupNames.Length; i++)
            {
                string groupName = groupNames[i];
                string validName = DName.MakeValid(groupName, out _).Value;
                Group g = m.Groups[i];

                if (!int.TryParse(groupName, out _))
                {
                    if (!fields.ContainsKey(validName))
                    {
                        fields.Add(validName, new NamedValue(validName, StringValue.New(g.Value)));
                    }
                    else
                    {
                        fields[validName] = new NamedValue(validName, StringValue.New(g.Value));
                    }
                }

                if (i > 0)
                {
                    subMatches.Add(g.Value);
                }
            }

            if (!fields.ContainsKey(SUBMATCHES))
            {
                fields.Add(SUBMATCHES, new NamedValue(SUBMATCHES, TableValue.NewSingleColumnTable(subMatches.Select(s => StringValue.New(s)).ToArray())));
            }

            return RecordValue.NewRecordFromFields(fields.Values);
        }

        private static DType GetRecordTypeFromRegularExpression(string regularExpression)
        {
            Dictionary<string, TypedName> propertyNames = new ();
            Regex rex = new Regex(regularExpression);

            propertyNames.Add(FULLMATCH, new TypedName(DType.String, new DName(FULLMATCH)));
            propertyNames.Add(STARTMATCH, new TypedName(DType.Number, new DName(STARTMATCH)));
            propertyNames.Add(SUBMATCHES, new TypedName(DType.CreateTable(new TypedName(DType.String, new DName(TexlFunction.ColumnName_ValueStr))), new DName(SUBMATCHES)));

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

        internal class CommonMatchImplementation
        {
            protected internal static Task<FormulaValue> CommonMatchImpl(FormulaValue[] args, string defaultMatchOptions, Func<string, string, RegexOptions, FormulaValue> implementation)
            {
                if (args[0] is not StringValue && args[0] is not BlankValue)
                {                    
                    return Task.FromResult<FormulaValue>(args[0] is ErrorValue ? args[0] : CommonErrors.GenericInvalidArgument(args[0].IRContext));
                }

                if (args[1] is not StringValue sv1)
                {
                    return Task.FromResult<FormulaValue>(args[1] is ErrorValue ? args[1] : CommonErrors.GenericInvalidArgument(args[1].IRContext));
                }

                string inputString = args[0] is StringValue sv0 ? sv0.Value : string.Empty;
                string regularExpression = sv1.Value;
                string matchOptions = args.Length == 3 ? ((StringValue)args[2]).Value : defaultMatchOptions;

                RegexOptions regOptions = RegexOptions.CultureInvariant;

                if (!matchOptions.Contains("c"))
                {
                    return Task.FromResult<FormulaValue>(FormulaValue.New(false));
                }

                if (matchOptions.Contains("i"))
                {
                    regOptions |= RegexOptions.IgnoreCase;
                }

                if (matchOptions.Contains("m"))
                {
                    regOptions |= RegexOptions.Multiline;
                }

                if (matchOptions.Contains("^") && !regularExpression.StartsWith("^", StringComparison.Ordinal))
                {
                    regularExpression = "^" + regularExpression;
                }

                if (matchOptions.Contains("$") && !regularExpression.EndsWith("$", StringComparison.Ordinal))
                {
                    regularExpression += "$";
                }

                try
                {
                    return Task.FromResult<FormulaValue>(implementation(inputString, regularExpression, regOptions));
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

                // Internal exception till .Net 7 where it becomes public
                catch (Exception rexParseEx) when (rexParseEx.GetType().Name.Equals("RegexParseException", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<FormulaValue>(new ErrorValue(args[1].IRContext, new ExpressionError()
                    {
                        Message = $"Invalid regular expression - {rexParseEx.Message}",
                        Span = args[1].IRContext.SourceContext,
                        Kind = ErrorKind.BadRegex
                    }));
                }
            }
        }
    }
}
