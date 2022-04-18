// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// Result of binding an expression. 
    /// </summary>
    public class CheckResult : IOperationStatus
    {
        // Null if type can't be determined. 
        public FormulaType ReturnType { get; set; }

        /// <summary>
        /// Names of fields that this formula uses. 
        /// null if unavailable.  
        /// This is only valid if there are no errors. 
        /// </summary>
        public HashSet<string> TopLevelIdentifiers { get; set; }

        /// <summary>
        /// List of errors or warnings. Check <see cref="ExpressionError.IsWarning"/>.
        /// Not null, but empty on success.
        /// </summary>
        public IEnumerable<ExpressionError> Errors => Parse.Errors.Concat(ExpressionError.New(_binding.ErrorContainer.GetErrors()));

        /// <summary>
        /// Parsed expression for evaluation. 
        /// Null on failure or if there is no evaluation. 
        /// </summary>
        public IExpression Expression { get; set; }

        /// <summary>
        /// True if no errors. 
        /// </summary>
        public bool IsSuccess => !Errors.Any(x => !x.IsWarning);

        internal TexlBinding _binding;

        /// <summary>
        /// Results from parsing. 
        /// </summary>
        public ParseResult Parse { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckResult"/> class.
        /// </summary>
        public CheckResult()
        {
        }

        internal CheckResult(ParseResult parse, TexlBinding binding = null)
        {
            var errors = parse.HasError ? parse._errors : binding.ErrorContainer.GetErrors();
            Parse = parse;

            _binding = binding;
        }

        public void ThrowOnErrors()
        {
            if (!IsSuccess)
            {
                var msg = string.Join("\r\n", Errors.Select(err => err.ToString()).ToArray());
                throw new InvalidOperationException($"Errors: " + msg);
            }
        }

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
