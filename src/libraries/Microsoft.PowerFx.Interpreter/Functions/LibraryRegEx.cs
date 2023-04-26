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

        // IsMatch( Text, Pattern [, MatchOptions ] )
        internal class IsMatchFunction : MatchFunctionBase
        {
            public IsMatchFunction(TimeSpan regExTimeout)
              : base(regExTimeout, "IsMatch", DType.Boolean, TexlStrings.AboutIsMatch, "^c$")
            {
            }

            internal override FormulaValue RegExFunction(string inputString, string regularExpression, RegexOptions regOptions)
            {
                Regex rex = new Regex(regularExpression, regOptions, RegExTimeout);
                bool b = rex.IsMatch(inputString);

                return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), b);
            }
        }

        // MatchAll( Text, Pattern [, MatchOptions ] )
        internal class MatchAllFunction : MatchFunction
        {
            public MatchAllFunction(TimeSpan regExTimeout)
             : base(regExTimeout, "MatchAll", DType.EmptyTable, TexlStrings.AboutMatchAll, "c")
            {
            }

            public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
            {
                bool fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
                returnType = returnType.ToTable();

                return fValid;
            }

            internal override FormulaValue RegExFunction(string inputString, string regularExpression, RegexOptions regOptions)
            {
                Regex rex = new Regex(regularExpression, regOptions, RegExTimeout);
                MatchCollection mc = rex.Matches(inputString);

                List<RecordValue> records = new ();

                foreach (Match m in mc)
                {
                    records.Add(GetRecordFromMatch(rex, m));
                }

                return TableValue.NewTable(new KnownRecordType(GetRecordTypeFromRegularExpression(regularExpression)), records.ToArray());
            }
        }

        // Match( Text, Pattern [, MatchOptions ] )
        internal class MatchFunction : MatchFunctionBase
        {
            public MatchFunction(TimeSpan regExTimeout)
              : base(regExTimeout, "Match", DType.EmptyRecord, TexlStrings.AboutMatch, "c")
            {
            }

            protected MatchFunction(TimeSpan regExTimeout, string name, DType returnType, StringGetter about, string example)
              : base(regExTimeout, name, returnType, about, example)
            {
            }

            public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
            {
                bool fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

                if (fValid)
                {                   
                    if (argTypes[1].Kind != DKind.String || !BinderUtils.TryGetConstantValue(context, args[1], out string regularExpression))
                    {
                        errors.EnsureError(args[1], TexlStrings.ErrVariableRegEx);
                        return false;
                    }

                    try
                    {
                        returnType = GetRecordTypeFromRegularExpression(regularExpression);
                    }

                    // Internal exception till .Net 7 where it becomes public
                    catch (Exception rexParseEx) when (rexParseEx.GetType().Name.Equals("RegexParseException", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.EnsureError(args[1], TexlStrings.ErrBadRegex);
                        fValid = false;                        
                    }
                }

                return fValid;
            }

            internal DType GetRecordTypeFromRegularExpression(string regularExpression)
            {                
                Dictionary<string, TypedName> propertyNames = new ();
                Regex rex = new Regex(regularExpression);

                propertyNames.Add(FULLMATCH, new TypedName(DType.String, new DName(FULLMATCH)));
                propertyNames.Add(STARTMATCH, new TypedName(DType.Number, new DName(STARTMATCH)));
                propertyNames.Add(SUBMATCHES, new TypedName(DType.CreateTable(new TypedName(DType.String, new DName(ColumnName_ValueStr))), new DName(SUBMATCHES)));

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

            internal override FormulaValue RegExFunction(string inputString, string regularExpression, RegexOptions regOptions)
            {
                Regex rex = new Regex(regularExpression, regOptions, RegExTimeout);
                Match m = rex.Match(inputString);

                if (!m.Success)
                {
                    return new BlankValue(IRContext.NotInSource(RecordType.Empty()));
                }

                return GetRecordFromMatch(rex, m);
            }

            protected static RecordValue GetRecordFromMatch(Regex rex, Match m)
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

                fields.Add(SUBMATCHES, new NamedValue(SUBMATCHES, TableValue.NewSingleColumnTable(subMatches.Select(s => StringValue.New(s)).ToArray())));

                return RecordValue.NewRecordFromFields(fields.Values);
            }
        }

        internal abstract class MatchFunctionBase : BuiltinFunction, IAsyncTexlFunction
        {
            public override bool IsSelfContained => true;

            public string DefaultMatchOption { get; }

            public TimeSpan RegExTimeout { get; }

            public MatchFunctionBase(TimeSpan regExTimeout, string name, DType returnType, StringGetter description, string defaultMatchOption)
                : base(name, description, FunctionCategories.Text, returnType, 0x0, 2, 3, DType.String, DType.String, DType.String)
            {
                RegExTimeout = ValidateTimeout(regExTimeout);
                DefaultMatchOption = defaultMatchOption;
            }

            public override IEnumerable<StringGetter[]> GetSignatures()
            {
                yield return new[] { TexlStrings.MatchArg1, TexlStrings.MatchArg2 };
                yield return new[] { TexlStrings.MatchArg1, TexlStrings.MatchArg2, TexlStrings.MatchOptions };
            }

            public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
            {
                bool fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

                if (argTypes[0].Kind != DKind.String && argTypes[0].Kind != DKind.ObjNull)
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrStringExpected);
                }

                if (argTypes[1].Kind != DKind.String && argTypes[1].Kind != DKind.OptionSetValue)
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrStringExpected);
                }

                if (argTypes.Length == 3)
                {
                    if (argTypes[0].Kind != DKind.String && argTypes[0].Kind != DKind.ObjNull)
                    {
                        fValid = false;
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[3], TexlStrings.ErrStringExpected);
                    }
                }

                return fValid;
            }

            public virtual async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
            {
                if (args[0] is BlankValue)
                {
                    return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), false);
                }

                string inputString = ((StringValue)args[0]).Value;
                string regularExpression = ((StringValue)args[1]).Value;
                string matchOptions = args.Length == 3 ? ((StringValue)args[2]).Value : DefaultMatchOption;
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
                    return RegExFunction(inputString, regularExpression, regOptions);
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

            internal abstract FormulaValue RegExFunction(string inputString, string regularExpression, RegexOptions regOptions);

            public override IEnumerable<string> GetRequiredEnumNames()
            {
                return new List<string>()
                {
                    LanguageConstants.MatchOptionsEnumString,
                    LanguageConstants.MatchEnumString
                };
            }
        }

        private static TimeSpan ValidateTimeout(TimeSpan regExTimeout)
        {
            // As the regular expression timeout is a security feature, we need to ensure that the timeout is set to a reasonable value
            // and will double check here we are in a valid range: [10 - 30] sec.
            if (regExTimeout == TimeSpan.Zero)
            {
                regExTimeout = TimeSpan.FromSeconds(30);
            }

            if (regExTimeout.TotalMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regExTimeout), "Timeout duration for regular expression execution must be positive.");
            }

            return regExTimeout;
        }
    }
}
