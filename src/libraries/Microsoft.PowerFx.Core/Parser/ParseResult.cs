// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Logging;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Result of parsing an expression. 
    /// </summary>
    public class ParseResult : IOperationStatus
    {
        /// <summary>
        /// The top level node. Not null.
        /// </summary>
        public TexlNode Root { get; }

        internal readonly List<TexlError> _errors;

        /// <summary>
        /// List of errors or warnings. Check <see cref="ExpressionError.IsWarning"/>.
        /// </summary>
        public IEnumerable<ExpressionError> Errors => ExpressionError.New(_errors, Locale);

        /// <summary>
        /// True if there were parse errors. 
        /// </summary>
        internal bool HasError { get; }

        /// <summary>
        /// True if no errors. 
        /// </summary>
        public bool IsSuccess => !HasError;
        
        internal List<CommentToken> Comments { get; }

        internal SourceList Before { get; }

        internal SourceList After { get; }

        internal CultureInfo Locale { get; }

        // Original script. 
        // All the spans in the tokens are relative to this. 
        public string Text { get; }

        internal ParseResult(TexlNode root, List<TexlError> errors, bool hasError, List<CommentToken> comments, SourceList before, SourceList after, string text, CultureInfo locale)
            : this(root, errors, hasError, comments, before, after, text)
        {
            Locale = locale;
        }

        internal ParseResult(TexlNode root, List<TexlError> errors, bool hasError, List<CommentToken> comments, SourceList before, SourceList after, string text)
        {
            Contracts.AssertValue(root);
            Contracts.AssertValue(comments);

            // You can have an empty error list and still have a semi-silent error, but if you have an error in your list there must have been an error.
            Contracts.Assert(errors != null ? hasError : true);

            Root = root;
            _errors = errors;
            HasError = hasError;
            Comments = comments;
            Before = before;
            After = after;

            Text = text;
        }

        internal string ParseErrorText => !HasError ? string.Empty : string.Join("\r\n", _errors.Select((err, i) =>
        {
            var sb = new StringBuilder(1024);
            err.FormatCore(sb);            
            return $"Err#{++i} {sb}";
        }));

        /// <summary>
        /// Converts the current formula into an anonymized format suitable for logging.
        /// </summary>
        public string GetAnonymizedFormula()
        {
            return StructuralPrint.Print(Root);
        }
    }
}
