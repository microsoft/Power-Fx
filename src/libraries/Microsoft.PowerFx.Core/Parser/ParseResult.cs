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
        public IEnumerable<ExpressionError> Errors => ExpressionError.New(_errors, ErrorMessageLocale);

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

        // Options used ot create these results.
        public ParserOptions Options { get; internal set; }

        /// <summary>
        /// Locale that error messages (if any) will be translated to.
        /// </summary>
        internal CultureInfo ErrorMessageLocale { get; }

        // Original script. 
        // All the spans in the tokens are relative to this. 
        public string Text { get; }

        /// <summary>
        /// If string is too large, don't even attempt to lex or parse it. 
        /// Just create a parse result directly encapsulating the error. 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="maxAllowed">Maximum number of characters allowed. </param>
        /// <returns></returns>
        internal static ParseResult ErrorTooLarge(string text, int maxAllowed)
        {
            Token tok = new ErrorToken(new Span(0, text.Length));

            var errKey = Core.Localization.TexlStrings.ErrTextTooLarge;
            var err = new TexlError(tok, DocumentErrorSeverity.Critical, errKey, maxAllowed, text.Length);

            List<TexlError> errors = new List<TexlError>()
            {
                err
            };

            int id = 0;
            TexlNode root = new ErrorNode(ref id, tok, err.ShortMessage);
            List<CommentToken> comments = new List<CommentToken>();

            return new ParseResult(root, errors, true, comments, null, null, text);         
        }

        internal ParseResult(TexlNode root, List<TexlError> errors, bool hasError, List<CommentToken> comments, SourceList before, SourceList after, string text, CultureInfo errorMessageLocale)
            : this(root, errors, hasError, comments, before, after, text)
        {
            ErrorMessageLocale = errorMessageLocale;
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
