// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Result of binding an expression. 
    /// </summary>
    public class CheckResult : IOperationStatus
    {
        /// <summary> 
        /// Return type of the expression. Null if type can't be determined. 
        /// </summary>
        public FormulaType ReturnType { get; set; }

        /// <summary>
        /// Names of fields that this formula uses. 
        /// null if unavailable.  
        /// This is only valid when <see cref="IsSuccess"/> is true.
        /// </summary>
        public HashSet<string> TopLevelIdentifiers { get; set; }

        /// <summary>
        /// List of errors and warnings. Check <see cref="ExpressionError.IsWarning"/>.
        /// Not null, but empty on success.
        /// </summary>
        public IEnumerable<ExpressionError> Errors { get; set; }

        private IEnumerable<ExpressionError> BindingErrors => ExpressionError.New(_binding?.ErrorContainer.GetErrors(), CultureInfo);

        internal void SetErrors(IEnumerable<IDocumentError> errors)
        {
            Errors = ExpressionError.New(errors, CultureInfo);
        }

        /// <summary>
        /// Parsed expression for evaluation. 
        /// Null on failure or if there is no evaluation. 
        /// </summary>
        public IExpression Expression { get; set; }

        /// <summary>
        /// The source engine this was created against. 
        /// </summary>
        internal Engine Source { get; set; }

        /// <summary>
        /// True if no errors. 
        /// </summary>
        public bool IsSuccess => !Errors.Any(x => !x.IsWarning);

        internal bool HasDeferredArgsWarning => Errors.Any(x => x.IsWarning && x.MessageKey.Equals(TexlStrings.WarnDeferredType.Key));

        internal TexlBinding _binding;

        /// <summary>
        /// Results from parsing. 
        /// </summary>
        public ParseResult Parse { get; set; }

        /// <summary>
        /// Symbols passed to this binding. May be null. 
        /// </summary>
        public ReadOnlySymbolTable Symbols { get; set; }

        /// <summary>
        /// Parameters are the subset of symbols that must be passed in Eval() for each evaluation. 
        /// This lets us associated the type in Check()  with the values in Eval().
        /// </summary>
        internal ReadOnlySymbolTable Parameters { get; set; }

        /// <summary>
        /// Culture info passed to this binding. May be null. 
        /// </summary>
        internal CultureInfo CultureInfo { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckResult"/> class.
        /// </summary>
        public CheckResult()
        {
        }

        internal CheckResult(ParseResult parse, TexlBinding binding = null) 
            : this(parse, null, binding)
        {
        }

        internal CheckResult(ParseResult parse, CultureInfo locale, TexlBinding binding = null)
        {
            Parse = parse ?? throw new ArgumentNullException(nameof(parse));
            CultureInfo = locale;

            _binding = binding;

            Errors = Parse.Errors.Concat(BindingErrors);
        }

        public void ThrowOnErrors()
        {
            if (!IsSuccess)
            {
                var msg = string.Join("\r\n", Errors.Select(err => err.ToString()).ToArray());
                throw new InvalidOperationException($"Errors: " + msg);
            }
        }

        /// <summary>
        /// Gets the type of a syntax node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public FormulaType GetNodeType(TexlNode node) => FormulaType.Build(_binding.GetType(node));

        internal IReadOnlyDictionary<string, TokenResultType> GetTokens(GetTokensFlags flags) => GetTokensUtils.GetTokens(_binding, flags);
    }

    // Internal interface to ensure that Result objects have a common contract
    // for error reporting. 
    internal interface IOperationStatus
    {
        public IEnumerable<ExpressionError> Errors { get; }

        public bool IsSuccess { get; }
    }
}
