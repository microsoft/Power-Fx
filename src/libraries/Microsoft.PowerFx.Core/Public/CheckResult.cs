// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// Result of checking an expression. 
    /// </summary>
    public class CheckResult
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
        /// null on success, else contains errors. 
        /// </summary>
        public ExpressionError[] Errors { get; set; }

        public virtual bool IsSuccess => this.Errors == null;

        internal TexlBinding _binding;

        internal Formula _formula;

        public CheckResult() { }

        internal CheckResult(IEnumerable<IDocumentError> errors, TexlBinding binding = null)
        {
            _binding = binding;
            SetErrors(errors);
        }

        public void ThrowOnErrors()
        {
            if (!IsSuccess)
            {
                var msg = String.Join("\r\n", Errors.Select(err => err.ToString()).ToArray());
                throw new InvalidOperationException($"Errors: " + msg);
            }
        }

        internal IReadOnlyDictionary<string, TokenResultType> GetTokens(GetTokensFlags flags) => GetTokensUtils.GetTokens(_binding, flags);

        internal CheckResult SetErrors(IEnumerable<IDocumentError> errors)
        {
            Errors = errors.Select(x => new ExpressionError
            {
                Message = x.ShortMessage,
                Span = x.TextSpan,
                Severity = x.Severity
            }).ToArray();

            if (Errors.Length == 0)
            {
                Errors = null;
            }

            return this;
        }
    }
}
