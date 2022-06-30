// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense.IntellisenseData;
using Microsoft.PowerFx.Intellisense.SignatureHelp;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal class IntellisenseResult : IIntellisenseResult
    {
        /// <summary>
        /// List of suggestions associated with the result.
        /// </summary>
        protected readonly List<IntellisenseSuggestion> _suggestions;

        /// <summary>
        /// The script to which the result pertains.
        /// </summary>
        protected readonly string _script;

        /// <summary>
        /// List of candidate signatures for the Intellisense, compliant with Language Server Protocol
        /// <see cref="SignatureHelp"/>.
        /// </summary>
        private readonly List<SignatureInformation> _functionSignatures;

        /// <summary>
        /// List of candidate signatures for the Intellisense, compliant with Document Server intellisense.
        /// </summary>
        protected readonly List<IntellisenseSuggestion> _functionOverloads;

        /// <summary>
        /// The index of the current argument.  0 if there are no arguments associated with the result, either
        /// because the function is parameterless or because intellisense was not called from within a valid
        /// function signature.
        /// </summary>
        private readonly int _currentArgumentIndex;

        internal IntellisenseResult(IIntellisenseData data, List<IntellisenseSuggestion> suggestions, Exception exception = null)
        {
            Contracts.AssertValue(suggestions);

            _script = data.Script;
            Contracts.CheckValue(_script, "script");
            ReplacementStartIndex = data.ReplacementStartIndex;
            Contracts.CheckParam(data.ReplacementStartIndex >= 0, "replacementStartIndex");

            ReplacementLength = data.ReplacementLength;
            Contracts.CheckParam(data.ReplacementLength >= 0, "replacementLength");

            var argIndex = data.ArgIndex;
            var argCount = data.ArgCount;
            Contracts.CheckParam(argIndex >= 0, "argIndex");
            Contracts.CheckParam(argCount >= 0, "argCount");
            Contracts.Check(argIndex <= argCount, "argIndex out of bounds.");

            var func = data.CurFunc;
            Contracts.CheckValueOrNull(func);

            _suggestions = suggestions;
            _functionSignatures = new List<SignatureInformation>();
            _functionOverloads = new List<IntellisenseSuggestion>();

            CurrentFunctionOverloadIndex = -1;
            _currentArgumentIndex = argIndex;
            Exception = exception;

            if (func == null)
            {
                IsFunctionScope = false;
            }
            else
            {
                IsFunctionScope = true;
                var highlightStart = -1;
                var highlightEnd = -1;
                var minMatchingArgCount = int.MaxValue;
                foreach (var signature in func.GetSignatures(argCount))
                {
                    var signatureIndex = 0;
                    var argumentSeparator = string.Empty;
                    var highlightedFuncParamDescription = string.Empty;
                    var listSep = TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorListSeparator + " ";
                    var funcDisplayString = new StringBuilder(func.Name);
                    funcDisplayString.Append('(');

                    var parameters = new List<ParameterInformation>();
                    while (signatureIndex < signature.Length)
                    {
                        Contracts.AssertValue(signature[signatureIndex]);
                        funcDisplayString.Append(argumentSeparator);

                        // We need to change the highlight information if the argument should be highlighted, but
                        // otherwise we still want to collect parameter information
                        var unalteredParamName = signature[signatureIndex]();
                        var invariantParamName = signature[signatureIndex]("en-US");
                        (var paramName, var parameterHighlightStart, var parameterHighlightEnd, var funcParamDescription) = GetParameterHighlightAndDescription(data, unalteredParamName, invariantParamName, funcDisplayString);
                        parameters.Add(new ParameterInformation()
                        {
                            Documentation = funcParamDescription,
                            Label = paramName
                        });

                        if (ArgNeedsHighlight(func, argCount, argIndex, signature.Length, signatureIndex))
                        {
                            (highlightStart, highlightEnd, highlightedFuncParamDescription) = (parameterHighlightStart, parameterHighlightEnd, funcParamDescription);
                        }

                        // For variadic function, we want to generate FuncName(arg1,arg1,...,arg1,...) as description.
                        if (func.SignatureConstraint != null && argCount > func.SignatureConstraint.RepeatTopLength && CanParamOmit(func, argCount, argIndex, signature.Length, signatureIndex))
                        {
                            funcDisplayString.Append("...");
                            signatureIndex += func.SignatureConstraint.RepeatSpan;
                        }
                        else
                        {
                            funcDisplayString.Append(signature[signatureIndex]());
                            signatureIndex++;
                        }

                        argumentSeparator = listSep;
                    }

                    if (func.MaxArity > func.MinArity && func.MaxArity > argCount)
                    {
                        funcDisplayString.Append(argumentSeparator + "...");
                    }

                    funcDisplayString.Append(')');
                    var signatureInformation = new SignatureInformation()
                    {
                        Documentation = func.Description,
                        Label = CreateFunctionSignature(func.Name, parameters),
                        Parameters = parameters.ToArray()
                    };
                    _functionSignatures.Add(signatureInformation);
                    _functionOverloads.Add(new IntellisenseSuggestion(new UIString(funcDisplayString.ToString(), highlightStart, highlightEnd), SuggestionKind.Function, SuggestionIconKind.Function, func.ReturnType, signatureIndex, func.Description, func.Name, highlightedFuncParamDescription));

                    if ((signatureIndex >= argCount || (func.SignatureConstraint != null && argCount > func.SignatureConstraint.RepeatTopLength)) && minMatchingArgCount > signatureIndex)
                    {
                        // _functionOverloads has at least one item at this point.
                        CurrentFunctionOverloadIndex = _functionOverloads.Count - 1;
                        minMatchingArgCount = signatureIndex;
                    }
                }

                // Handling of case where the function does not take any arguments.
                if (_functionOverloads.Count == 0 && func.MinArity == 0)
                {
                    var signatureInformation = new SignatureInformation()
                    {
                        Documentation = func.Description,
                        Label = CreateFunctionSignature(func.Name),
                        Parameters = Array.Empty<ParameterInformation>()
                    };
                    _functionSignatures.Add(signatureInformation);
                    _functionOverloads.Add(new IntellisenseSuggestion(new UIString(func.Name + "()", 0, func.Name.Length + 1), SuggestionKind.Function, SuggestionIconKind.Function, func.ReturnType, string.Empty, 0, func.Description, func.Name));
                    CurrentFunctionOverloadIndex = 0;
                }
            }

            Contracts.Assert(_functionSignatures.Count == _functionOverloads.Count);
        }

        /// <summary>
        /// Derives signature help information for Language Server Protocol compliance from extant signature
        /// info.
        /// </summary>
        public SignatureHelp.SignatureHelp SignatureHelp => new SignatureHelp.SignatureHelp()
        {
            Signatures = _functionSignatures.ToArray(),
            ActiveSignature = CurrentFunctionOverloadIndex > 0 ? (uint)CurrentFunctionOverloadIndex : 0,
            ActiveParameter = (uint)_currentArgumentIndex
        };

        /// <summary>
        /// Returns a string that represents the full call signature as defined by <paramref name="functionName"/>,
        /// <paramref name="parameters"/>, as well as <see cref="LocalizationUtils.CurrentLocaleListSeparator"/>.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="parameters">
        ///     List of parameters in the relevant signature for <paramref name="functionName"/>.
        /// </param>
        /// <returns>
        /// A label that represents the call signature; e.g. <code>Set(variable, lambda)</code>
        /// </returns>
        private string CreateFunctionSignature(string functionName, IEnumerable<ParameterInformation> parameters = null)
        {
            Contracts.AssertValue(functionName);
            Contracts.AssertValue(functionName);

            string parameterString;
            if (parameters != null)
            {
                parameterString = string.Join($"{LocalizationUtils.CurrentLocaleListSeparator} ", parameters.Select(parameter => parameter.Label));
            }
            else
            {
                parameterString = string.Empty;
            }

            return $"{functionName}({parameterString})";
        }

        /// <summary>
        /// Gets the parameter description and corresponding highlight information for the provided
        /// function and index.  Provides special augmentation behavior via handlers.
        /// </summary>
        /// <param name="data">
        /// Data off of which the result is based.
        /// </param>
        /// <param name="paramName"></param>
        /// <param name="invariantParamName"></param>
        /// <param name="funcDisplayString"></param>
        /// <returns></returns>
        private static (string paramName, int highlightStart, int highlightEnd, string funcParamDescription) GetParameterHighlightAndDescription(IIntellisenseData data, string paramName, string invariantParamName, StringBuilder funcDisplayString)
        {
            Contracts.AssertValue(data);
            Contracts.AssertValue(paramName);
            Contracts.AssertValue(invariantParamName);
            Contracts.AssertValue(funcDisplayString);

            int highlightStart;
            int highlightEnd;
            var func = data.CurFunc;
            var argIndex = data.ArgIndex;

            // Highlight has to start from the next character and end at the last character which is "length -1"
            highlightStart = funcDisplayString.Length;
            highlightEnd = highlightStart + paramName.Length - 1;

            // By calling this we provide the caller the ability to augment the highlight and parameter
            // details amidst the iteration
            if (data.TryAugmentSignature(func, argIndex, paramName, highlightStart, out var newHighlightStart, out var newHighlightEnd, out var newParamName, out var newInvariantParamName))
            {
                (highlightStart, highlightEnd, paramName, invariantParamName) = (newHighlightStart, newHighlightEnd, newParamName, newInvariantParamName);
            }

            // MUST use the invariant parameter name here
            func.TryGetParamDescription(invariantParamName, out var funcParamDescription);

            // Apply optional suffix provided via argument
            funcParamDescription += data.GenerateParameterDescriptionSuffix(func, paramName);
            return (paramName, highlightStart, highlightEnd, funcParamDescription);
        }

        // GroupBy(source, column_name, column_name, ..., column_name, ..., group_name, ...)
        // AddColumns(source, column, expression, column, expression, ..., column, expression, ...)
        internal bool CanParamOmit(TexlFunction func, int argCount, int argIndex, int signatureCount, int signatureIndex)
        {
            Contracts.AssertValue(func);
            Contracts.Assert(func.MaxArity == int.MaxValue);
            Contracts.Assert(func.SignatureConstraint != null && argCount > func.SignatureConstraint.RepeatTopLength && signatureCount >= func.SignatureConstraint.RepeatTopLength);

            if (func.SignatureConstraint == null)
            {
                return false;
            }

            return func.SignatureConstraint.CanParamOmit(argCount, argIndex, signatureCount, signatureIndex);
        }

        // 1. For a function with limited MaxArity, The first time the count becomes equal to the argIndex, that's the arg we want to highlight
        // 2. For variadic function with repeating params in the signature, we highlight the param which indicates the corresponding position.
        internal bool ArgNeedsHighlight(TexlFunction func, int argCount, int argIndex, int signatureCount, int signatureIndex)
        {
            Contracts.AssertValue(func);

            if (func.SignatureConstraint == null || argCount <= func.SignatureConstraint.RepeatTopLength || signatureIndex <= func.SignatureConstraint.OmitStartIndex)
            {
                return signatureIndex == argIndex;
            }

            return func.SignatureConstraint.ArgNeedsHighlight(argCount, argIndex, signatureCount, signatureIndex);
        }

        /// <summary>
        /// Returns the start index of the input string at which the suggestion has to be replaced upon selection of the suggestion.
        /// </summary>
        public int ReplacementStartIndex { get; protected set; }

        /// <summary>
        /// Returns the length of text to be replaced with the current suggestion.
        /// </summary>
        public int ReplacementLength { get; protected set; }

        /// <summary>
        /// A boolean value indicating whether the cursor is in function scope or not.
        /// </summary>
        public bool IsFunctionScope { get; protected set; }

        /// <summary>
        /// Index of the overload in 'FunctionOverloads' to be displayed in the UI.
        /// This is equal to -1 when IsFunctionScope = False.
        /// </summary>
        public int CurrentFunctionOverloadIndex { get; protected set; }

        public Exception Exception { get; protected set; }

        /// <summary>
        /// Enumerates suggestions for the current position in some specified input.
        /// </summary>
        public IEnumerable<IIntellisenseSuggestion> Suggestions
        {
            get
            {
                for (var i = 0; i < _suggestions.Count; i++)
                {
                    yield return _suggestions[i];
                }
            }
        }

        /// <summary>
        /// Enumerates function overloads for the function to be displayed.
        /// This is empty when IsFunctionScope = False.
        /// </summary>
        public IEnumerable<IIntellisenseSuggestion> FunctionOverloads
        {
            get
            {
                for (var i = 0; i < _functionOverloads.Count; i++)
                {
                    yield return _functionOverloads[i];
                }
            }
        }
    }
}
