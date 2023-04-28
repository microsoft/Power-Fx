// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        // https://learn.microsoft.com/en-us/power-platform/power-fx/reference/function-ismatch
        // In this version of Match, IsMatch and MatchAll functions, predefined patterns are not supported.

        public const string FULLMATCH = "FullMatch";
        public const string STARTMATCH = "StartMatch";
        public const string SUBMATCHES = "SubMatches";

        internal static TimeSpan RegexTimeout { get; private set; } = TimeSpan.FromSeconds(1);

        private const string DefaultIsMatchOptions = "^c$";
        private const string DefaultMatchOptions = "c";
        private const string DefaultMatchAllOptions = "c";

        internal static IEnumerable<TexlFunction> EnableRegexFunctions(TimeSpan regexTimeout)
        {
            TexlFunction isMatchFunction = new IsMatchFunction();
            TexlFunction matchFunction = new MatchFunction();
            TexlFunction matchAllFunction = new MatchAllFunction();

            RegexTimeout = regexTimeout;

            ConfigDependentFunctions.Add(
                isMatchFunction, 
                StandardErrorHandlingAsync<FormulaValue>(
                    "IsMatch",
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
                    checkRuntimeTypes: OptionSetOrString,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: IsMatchImpl));

            ConfigDependentFunctions.Add(
               matchFunction,
               StandardErrorHandlingAsync<FormulaValue>(
                   "Match",
                   expandArguments: NoArgExpansion,
                   replaceBlankValues: ReplaceBlankWithEmptyString,
                   checkRuntimeTypes: OptionSetOrString,
                   checkRuntimeValues: DeferRuntimeValueChecking,
                   returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                   targetFunction: MatchImpl));

            ConfigDependentFunctions.Add(
               matchAllFunction,
               StandardErrorHandlingAsync<FormulaValue>(
                   "MatchAll",
                   expandArguments: NoArgExpansion,
                   replaceBlankValues: ReplaceBlankWithEmptyString,
                   checkRuntimeTypes: OptionSetOrString,
                   checkRuntimeValues: DeferRuntimeValueChecking,
                   returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                   targetFunction: MatchAllImpl));

            return new TexlFunction[] { isMatchFunction, matchFunction, matchAllFunction };
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

        private static async ValueTask<FormulaValue> MatchImpl(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return CommonMatchImpl(runner, context, irContext, args, defaultMatchOptions: DefaultMatchOptions, (input, regex, options) =>
            {
                Regex rex = new Regex(regex, options, RegexTimeout);
                Match m = rex.Match(input);

                if (!m.Success)
                {
                    return new BlankValue(IRContext.NotInSource(new KnownRecordType(GetRecordTypeFromRegularExpression(regex))));
                }

                return GetRecordFromMatch(rex, m);
            });
        }

        private static async ValueTask<FormulaValue> MatchAllImpl(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return CommonMatchImpl(runner, context, irContext, args, defaultMatchOptions: DefaultMatchAllOptions, (input, regex, options) =>
            {
                Regex rex = new Regex(regex, options, RegexTimeout);
                MatchCollection mc = rex.Matches(input);

                List<RecordValue> records = new ();

                foreach (Match m in mc)
                {
                    records.Add(GetRecordFromMatch(rex, m));
                }

                return TableValue.NewTable(new KnownRecordType(GetRecordTypeFromRegularExpression(regex)), records.ToArray());
            });
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

        private static FormulaValue CommonMatchImpl(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args, string defaultMatchOptions, Func<string, string, RegexOptions, FormulaValue> impl)
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
                    Kind = ErrorKind.Timeout
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

        //public const string FULLMATCH = "FullMatch";
        //public const string STARTMATCH = "StartMatch";
        //public const string SUBMATCHES = "SubMatches";

        //// IsMatch( Text, Pattern [, MatchOptions ] )
        //internal class IsMatchFunction : MatchFunctionBase
        //{
        //    public IsMatchFunction(TimeSpan regExTimeout)
        //      : base(regExTimeout, "IsMatch", DType.Boolean, TexlStrings.AboutIsMatch, "^c$")
        //    {
        //    }

        //    internal override FormulaValue RegExFunction(string inputString, string regularExpression, RegexOptions regOptions)
        //    {
        //        Regex rex = new Regex(regularExpression, regOptions, RegExTimeout);
        //        bool b = rex.IsMatch(inputString);

        //        return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), b);
        //    }
        //}

        //// MatchAll( Text, Pattern [, MatchOptions ] )
        //internal class MatchAllFunction : MatchFunction
        //{
        //    public MatchAllFunction(TimeSpan regExTimeout)
        //     : base(regExTimeout, "MatchAll", DType.EmptyTable, TexlStrings.AboutMatchAll, "c")
        //    {
        //    }

        //    public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        //    {
        //        bool fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
        //        returnType = returnType.ToTable();

        //        return fValid;
        //    }

        //    internal override FormulaValue RegExFunction(string inputString, string regularExpression, RegexOptions regOptions)
        //    {
        //        Regex rex = new Regex(regularExpression, regOptions, RegExTimeout);
        //        MatchCollection mc = rex.Matches(inputString);

        //        List<RecordValue> records = new ();

        //        foreach (Match m in mc)
        //        {
        //            records.Add(GetRecordFromMatch(rex, m));
        //        }

        //        return TableValue.NewTable(new KnownRecordType(GetRecordTypeFromRegularExpression(regularExpression)), records.ToArray());
        //    }
        //}

        //// Match( Text, Pattern [, MatchOptions ] )
        //internal class MatchFunction : MatchFunctionBase
        //{
        //    private readonly string _cachePrefix;            

        //    public MatchFunction(TimeSpan regExTimeout)
        //      : base(regExTimeout, "Match", DType.EmptyRecord, TexlStrings.AboutMatch, "c")
        //    {
        //        _cachePrefix = "rec_";
        //    }

        //    protected MatchFunction(TimeSpan regExTimeout, string name, DType returnType, StringGetter about, string example)
        //      : base(regExTimeout, name, returnType, about, example)
        //    {
        //        _cachePrefix = "tbl_";
        //    }

        //    public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        //    {
        //        Contracts.AssertValue(args);
        //        Contracts.AssertAllValues(args);
        //        Contracts.AssertValue(argTypes);
        //        Contracts.Assert(args.Length == argTypes.Length);
        //        Contracts.Assert(args.Length == 2 || args.Length == 3);
        //        Contracts.AssertValue(errors);

        //        bool fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
        //        Contracts.Assert(returnType.IsRecord || returnType.IsTable);
        //        TexlNode regExNode = args[1];
                
        //        if (argTypes[1].Kind != DKind.String || !BinderUtils.TryGetConstantValue(context, regExNode, out string nodeValue))
        //        {
        //            errors.EnsureError(regExNode, TexlStrings.ErrVariableRegEx);
        //            return false;
        //        }

        //        string regularExpression = nodeValue;
        //        return fValid && TryCreateReturnType(regExNode, regularExpression, errors, ref returnType);
        //    }

        //    // Creates a typed result: [Match:s, Captures:*[Value:s], NamedCaptures:r[<namedCaptures>:s]]
        //    private bool TryCreateReturnType(TexlNode regExNode, string regexPattern, IErrorContainer errors, ref DType returnType)
        //    {
        //        Contracts.AssertValue(regexPattern);
        //        string prefixedRegexPattern = this._cachePrefix + regexPattern;

        //        //if (_regexTypeCache.ContainsKey(prefixedRegexPattern))
        //        //{
        //        //    var cachedType = _regexTypeCache[prefixedRegexPattern];
        //        //    if (cachedType != null)
        //        //    {
        //        //        returnType = cachedType.Item1;
        //        //        AddWarnings(regExNode, errors, cachedType.Item2, cachedType.Item3, cachedType.Item4);
        //        //        return true;
        //        //    }
        //        //    else
        //        //    {
        //        //        errors.EnsureError(regExNode, CanvasStringResources.ErrInvalidRegEx);
        //        //        return false;
        //        //    }
        //        //}

        //        //if (_regexTypeCache.Count >= 5000)
        //        //{
        //        //    // To preserve memory during authoring, we clear the cache if it gets
        //        //    // too large. This should only happen in a minority of cases and
        //        //    // should have no impact on deployed apps.
        //        //    _regexTypeCache.Clear();
        //        //}

        //        try
        //        {
        //            var regex = new Regex(regexPattern);

        //            List<TypedName> propertyNames = new List<TypedName>();
        //            bool fullMatchHidden = false, subMatchesHidden = false, startMatchHidden = false;

        //            foreach (var captureName in regex.GetGroupNames())
        //            {
        //                int unused;
        //                if (int.TryParse(captureName, out unused))
        //                {
        //                    // Unnamed captures are returned as integers, ignoring them
        //                    continue;
        //                }

        //                if (captureName == ColumnName_FullMatch.Value)
        //                {
        //                    fullMatchHidden = true;
        //                }
        //                else if (captureName == ColumnName_SubMatches.Value)
        //                {
        //                    subMatchesHidden = true;
        //                }
        //                else if (captureName == ColumnName_StartMatch.Value)
        //                {
        //                    startMatchHidden = true;
        //                }

        //                bool unusedFlag;
        //                propertyNames.Add(new TypedName(DType.String, DName.MakeValid(captureName, out unusedFlag)));
        //            }

        //            if (!fullMatchHidden)
        //            {
        //                propertyNames.Add(new TypedName(DType.String, ColumnName_FullMatch));
        //            }

        //            if (!subMatchesHidden)
        //            {
        //                propertyNames.Add(new TypedName(DType.CreateTable(new TypedName(DType.String, ColumnName_Value)), ColumnName_SubMatches));
        //            }

        //            if (!startMatchHidden)
        //            {
        //                propertyNames.Add(new TypedName(DType.Number, ColumnName_StartMatch));
        //            }

        //            returnType = returnType.IsRecord 
        //                ? DType.CreateRecord(propertyNames) 
        //                : DType.CreateTable(propertyNames);

        //            AddWarnings(regExNode, errors, hidesFullMatch: fullMatchHidden, hidesSubMatches: subMatchesHidden, hidesStartMatch: startMatchHidden);
        //            //_regexTypeCache[prefixedRegexPattern] = Tuple.Create(returnType, fullMatchHidden, subMatchesHidden, startMatchHidden);
        //            return true;
        //        }
        //        catch (ArgumentException)
        //        {
        //            errors.EnsureError(regExNode, TexlStrings.ErrBadRegex);
        //            //_regexTypeCache[prefixedRegexPattern] = null; // Cache to avoid evaluating again
        //            return false;
        //        }
        //    }

        //    private void AddWarnings(TexlNode regExNode, IErrorContainer errors, bool hidesFullMatch, bool hidesSubMatches, bool hidesStartMatch)
        //    {
        //        if (hidesFullMatch)
        //        {
        //            errors.EnsureError(DocumentErrorSeverity.Suggestion, regExNode, TexlStrings.InfoRegExCaptureNameHidesPredefinedFullMatchField, ColumnName_FullMatch.Value);
        //        }

        //        if (hidesSubMatches)
        //        {
        //            errors.EnsureError(DocumentErrorSeverity.Suggestion, regExNode, TexlStrings.InfoRegExCaptureNameHidesPredefinedSubMatchesField, ColumnName_SubMatches.Value);
        //        }

        //        if (hidesStartMatch)
        //        {
        //            errors.EnsureError(DocumentErrorSeverity.Suggestion, regExNode, TexlStrings.InfoRegExCaptureNameHidesPredefinedStartMatchField, ColumnName_StartMatch.Value);
        //        }
        //    }

        //    internal DType GetRecordTypeFromRegularExpression(string regularExpression)
        //    {                
        //        Dictionary<string, TypedName> propertyNames = new ();
        //        Regex rex = new Regex(regularExpression);

        //        propertyNames.Add(FULLMATCH, new TypedName(DType.String, new DName(FULLMATCH)));
        //        propertyNames.Add(STARTMATCH, new TypedName(DType.Number, new DName(STARTMATCH)));
        //        propertyNames.Add(SUBMATCHES, new TypedName(DType.CreateTable(new TypedName(DType.String, new DName(ColumnName_ValueStr))), new DName(SUBMATCHES)));

        //        foreach (string groupName in rex.GetGroupNames())
        //        {
        //            if (!int.TryParse(groupName, out _))
        //            {
        //                DName validName = DName.MakeValid(groupName, out _);

        //                if (!propertyNames.ContainsKey(validName.Value))
        //                {
        //                    propertyNames.Add(validName.Value, new TypedName(DType.String, validName));
        //                }
        //            }
        //        }

        //        return DType.CreateRecord(propertyNames.Values);                
        //    }

        //    internal override FormulaValue RegExFunction(string inputString, string regularExpression, RegexOptions regOptions)
        //    {
        //        Regex rex = new Regex(regularExpression, regOptions, RegExTimeout);
        //        Match m = rex.Match(inputString);

        //        if (!m.Success)
        //        {
        //            return new BlankValue(IRContext.NotInSource(RecordType.Empty()));
        //        }

        //        return GetRecordFromMatch(rex, m);
        //    }

        //    protected static RecordValue GetRecordFromMatch(Regex rex, Match m)
        //    {
        //        Dictionary<string, NamedValue> fields = new ()
        //        {
        //            { FULLMATCH, new NamedValue(FULLMATCH, StringValue.New(m.Value)) },
        //            { STARTMATCH, new NamedValue(STARTMATCH, NumberValue.New(m.Index + 1)) }
        //        };

        //        List<string> subMatches = new List<string>();
        //        string[] groupNames = rex.GetGroupNames();

        //        for (int i = 0; i < groupNames.Length; i++)
        //        {
        //            string groupName = groupNames[i];
        //            string validName = DName.MakeValid(groupName, out _).Value;
        //            Group g = m.Groups[i];

        //            if (!int.TryParse(groupName, out _))
        //            {
        //                if (!fields.ContainsKey(validName))
        //                {
        //                    fields.Add(validName, new NamedValue(validName, StringValue.New(g.Value)));
        //                }
        //                else
        //                {
        //                    fields[validName] = new NamedValue(validName, StringValue.New(g.Value));
        //                }
        //            }

        //            if (i > 0)
        //            {
        //                subMatches.Add(g.Value);
        //            }
        //        }

        //        fields.Add(SUBMATCHES, new NamedValue(SUBMATCHES, TableValue.NewSingleColumnTable(subMatches.Select(s => StringValue.New(s)).ToArray())));

        //        return RecordValue.NewRecordFromFields(fields.Values);
        //    }
        //}

        //internal abstract class MatchFunctionBase : BuiltinFunction, IAsyncTexlFunction
        //{
        //    public override bool IsSelfContained => true;            

        //    public string DefaultMatchOption { get; }

        //    public TimeSpan RegExTimeout { get; }

        //    public MatchFunctionBase(TimeSpan regExTimeout, string name, DType returnType, StringGetter description, string defaultMatchOption)
        //        : base(name, description, FunctionCategories.Text, returnType, 0x0, 2, 3, DType.String, DType.String, DType.String)
        //    {
        //        RegExTimeout = ValidateTimeout(regExTimeout);
        //        DefaultMatchOption = defaultMatchOption;
        //    }

        //    public override IEnumerable<StringGetter[]> GetSignatures()
        //    {
        //        yield return new[] { TexlStrings.MatchArg1, TexlStrings.MatchArg2 };
        //        yield return new[] { TexlStrings.MatchArg1, TexlStrings.MatchArg2, TexlStrings.MatchOptions };
        //    }          

        //    public virtual async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        //    {
        //        if (args[0] is BlankValue)
        //        {
        //            return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), false);
        //        }
        //        if (args[0] is not StringValue sv0)
        //        {
        //            return CommonErrors.GenericInvalidArgument(args[0].IRContext);
        //        }
        //        if (args[1] is not StringValue sv1)
        //        {
        //            return CommonErrors.GenericInvalidArgument(args[1].IRContext);
        //        }

        //        string inputString = sv0.Value;
        //        string regularExpression = sv1.Value;
        //        string matchOptions = args.Length == 3 ? ((StringValue)args[2]).Value : DefaultMatchOption;
        //        RegexOptions regOptions = RegexOptions.None;

        //        if (!matchOptions.Contains("c"))
        //        {
        //            return FormulaValue.New(false);
        //        }

        //        if (matchOptions.Contains("i"))
        //        {
        //            regOptions |= RegexOptions.IgnoreCase;
        //        }

        //        if (matchOptions.Contains("m"))
        //        {
        //            regOptions |= RegexOptions.Multiline;
        //        }

        //        if (matchOptions.Contains("^") && !regularExpression.StartsWith("^", StringComparison.Ordinal))
        //        {
        //            regularExpression = "^" + regularExpression;
        //        }

        //        if (matchOptions.Contains("$") && !regularExpression.EndsWith("$", StringComparison.Ordinal))
        //        {
        //            regularExpression += "$";
        //        }

        //        try
        //        {
        //            return RegExFunction(inputString, regularExpression, regOptions);
        //        }
        //        catch (RegexMatchTimeoutException rexTimeoutEx)
        //        {
        //            return new ErrorValue(args[0].IRContext, new ExpressionError()
        //            {
        //                Message = $"Regular expression timeout (above {rexTimeoutEx.MatchTimeout.TotalMilliseconds} ms) - {rexTimeoutEx.Message}",
        //                Span = args[0].IRContext.SourceContext,
        //                Kind = ErrorKind.Timeout
        //            });
        //        }

        //        // Internal exception till .Net 7 where it becomes public
        //        catch (Exception rexParseEx) when (rexParseEx.GetType().Name.Equals("RegexParseException", StringComparison.OrdinalIgnoreCase))
        //        {
        //            return new ErrorValue(args[1].IRContext, new ExpressionError()
        //            {
        //                Message = $"Invalid regular expression - {rexParseEx.Message}",
        //                Span = args[1].IRContext.SourceContext,
        //                Kind = ErrorKind.BadRegex
        //            });
        //        }
        //    }

        //    internal abstract FormulaValue RegExFunction(string inputString, string regularExpression, RegexOptions regOptions);

        //    public override IEnumerable<string> GetRequiredEnumNames()
        //    {
        //        return new List<string>()
        //        {
        //            LanguageConstants.MatchOptionsEnumString,
        //            LanguageConstants.MatchEnumString
        //        };
        //    }
        //}

        //private static TimeSpan ValidateTimeout(TimeSpan regExTimeout)
        //{
        //    // As the regular expression timeout is a security feature, we need to ensure that the timeout is set to a reasonable value
        //    // and will double check here we are in a valid range: [10 - 30] sec.
        //    if (regExTimeout == TimeSpan.Zero)
        //    {
        //        regExTimeout = TimeSpan.FromSeconds(30);
        //    }

        //    if (regExTimeout.TotalMilliseconds < 0)
        //    {
        //        throw new ArgumentOutOfRangeException(nameof(regExTimeout), "Timeout duration for regular expression execution must be positive.");
        //    }

        //    return regExTimeout;
        //}
    }
}
