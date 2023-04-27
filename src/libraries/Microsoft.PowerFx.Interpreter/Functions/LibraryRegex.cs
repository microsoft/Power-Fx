// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        // Default timeout for Regex operations of 1 second
        private static TimeSpan RegexTimeout { get; set; } = TimeSpan.FromSeconds(1);

        private const string DefaultIsMatchOptions = "^c$";

        internal static IEnumerable<TexlFunction> EnableRegexFunctions(TimeSpan regexTimeout)
        {
            var isMatchFunction = new IsMatchFunction();
            RegexTimeout = regexTimeout;
            ConfigDependentFunctions.Add(
                isMatchFunction,
                StandardErrorHandlingAsync<FormulaValue>(
                    "IsMatch",
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: IsMatchImpl));

            return new TexlFunction[] { isMatchFunction };
        }

        private static async ValueTask<FormulaValue> IsMatchImpl(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return CommonMatchImpl(runner, context, irContext, args, defaultMatchOptions: DefaultIsMatchOptions, (input, regex, options) =>
            {
                Regex rex = new Regex(regex, options, RegexTimeout);
                bool b = rex.IsMatch(input);

                return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), b);
            });
        }

        private static FormulaValue CommonMatchImpl(
            EvalVisitor runner,
            EvalVisitorContext context,
            IRContext irContetx,
            FormulaValue[] args,
            string defaultMatchOptions,
            Func<string, string, RegexOptions, FormulaValue> impl)
        {
            if (args[0] is not StringValue sv0)
            {
                return CommonErrors.GenericInvalidArgument(args[0].IRContext);
            }

            if (args[1] is not StringValue sv1)
            {
                return CommonErrors.GenericInvalidArgument(args[1].IRContext);
            }

            string inputString = sv0.Value;
            string regularExpression = sv1.Value;
            string matchOptions = args.Length == 3 ? ((StringValue)args[2]).Value : defaultMatchOptions;

            RegexOptions regOptions = RegexOptions.None;

            if (!matchOptions.Contains("c"))
            {
                return FormulaValue.New(false);
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
                return impl(inputString, regularExpression, regOptions);
            }
            catch (RegexMatchTimeoutException rexTimeoutEx)
            {
                return new ErrorValue(args[0].IRContext, new ExpressionError()
                {
                    Message = $"Regular expression timeout (above {rexTimeoutEx.MatchTimeout.TotalMilliseconds} ms) - {rexTimeoutEx.Message}",
                    Span = args[0].IRContext.SourceContext,
                    Kind = ErrorKind.QuotaExceeded
                });
            }

            // Internal exception till .Net 7 where it becomes public
            catch (Exception rexParseEx) when (rexParseEx.GetType().Name.Equals("RegexParseException", StringComparison.OrdinalIgnoreCase))
            {
                return new ErrorValue(args[1].IRContext, new ExpressionError()
                {
                    Message = $"Invalid regular expression - {rexParseEx.Message}",
                    Span = args[1].IRContext.SourceContext,
                    Kind = ErrorKind.BadRegex
                });
            }
        }
    }
}
