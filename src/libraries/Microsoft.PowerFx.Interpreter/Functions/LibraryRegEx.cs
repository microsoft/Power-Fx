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
using Microsoft.PowerFx.Interpreter.Localization;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Functions.Library;

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

        internal class IsMatchImplementation : IsMatchFunction, IAsyncTexlFunction, IRegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            string IRegexCommonImplementation.RegexOptions => DefaultIsMatchOptions;

            public IsMatchImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            public Task<FormulaValue> InvokeAsync(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
            {
                return RegexCommonHelper.InvokeAsync(runner, context, irContext, args, this);
            }

            FormulaValue IRegexCommonImplementation.InvokeRegexFunction(string input, string regex, RegexOptions options)
            {
                Regex rex = new Regex(regex, options, _regexTimeout);
                bool b = rex.IsMatch(input);

                return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), b);
            }
        }

        internal class MatchImplementation : MatchFunction, IAsyncTexlFunction, IRegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            string IRegexCommonImplementation.RegexOptions => DefaultMatchOptions;

            public MatchImplementation(TimeSpan regexTimeout, RegexTypeCache regexCache)
                : base(regexCache)
            {
                _regexTimeout = regexTimeout;
            }

            FormulaValue IRegexCommonImplementation.InvokeRegexFunction(string input, string regex, RegexOptions options)
            {
                Regex rex = new Regex(regex, options, _regexTimeout);
                Match m = rex.Match(input);

                if (!m.Success)
                {
                    return new BlankValue(IRContext.NotInSource(new KnownRecordType(GetRecordTypeFromRegularExpression(regex))));
                }

                return GetRecordFromMatch(rex, m);
            }

            public Task<FormulaValue> InvokeAsync(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
            {
                return RegexCommonHelper.InvokeAsync(runner, context, irContext, args, this);
            }
        }

        internal class MatchAllImplementation : MatchAllFunction, IAsyncTexlFunction, IRegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            string IRegexCommonImplementation.RegexOptions => DefaultMatchAllOptions;

            public MatchAllImplementation(TimeSpan regexTimeout, RegexTypeCache regexCache)
                : base(regexCache)
            {
                _regexTimeout = regexTimeout;
            }

            public Task<FormulaValue> InvokeAsync(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
            {
                return RegexCommonHelper.InvokeAsync(runner, context, irContext, args, this);
            }

            FormulaValue IRegexCommonImplementation.InvokeRegexFunction(string input, string regex, RegexOptions options)
            {
                Regex rex = new Regex(regex, options, _regexTimeout);
                MatchCollection mc = rex.Matches(input);
                List<RecordValue> records = new ();

                foreach (Match m in mc)
                {
                    records.Add(GetRecordFromMatch(rex, m));
                }

                return TableValue.NewTable(new KnownRecordType(GetRecordTypeFromRegularExpression(regex)), records.ToArray());
            }
        }

        private static RecordValue GetRecordFromMatch(Regex rex, Match m)
        {
            Dictionary<string, NamedValue> fields = new ()
            {
                { FULLMATCH, new NamedValue(FULLMATCH, StringValue.New(m.Value)) },
                { STARTMATCH, new NamedValue(STARTMATCH, NumberValue.New((double)m.Index + 1)) }
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

        internal static class RegexCommonHelper
        {
            public static Task<FormulaValue> InvokeAsync(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args, IRegexCommonImplementation regexImpl)
            {
                runner.CheckCancel();

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
                    matchOptions = regexImpl.RegexOptions;
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

                try
                {
                    return Task.FromResult(regexImpl.InvokeRegexFunction(inputString, regularExpression, regOptions));
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
        }

        internal interface IRegexCommonImplementation
        {
            FormulaValue InvokeRegexFunction(string input, string regex, RegexOptions options);

            string RegexOptions { get; }
        }
    }
}
