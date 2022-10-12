// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Errors
{
    // TASK: 67034: Cleanup: Eliminate StringIds.
    internal sealed class TexlError : BaseError, IRuleError
    {
        private readonly List<string> _nameMapIDs;

        // Node may be null.
        public readonly TexlNode Node;

        // Tok will always be non-null.
        public readonly Token Tok;

        // TextSpan for the rule error.
        public override Span TextSpan { get; }

        public override IEnumerable<string> SinkTypeErrors => _nameMapIDs;

        [Obsolete("Use overload with explicit Culture")]
        public TexlError(Token tok, DocumentErrorSeverity severity, ErrorResourceKey errKey, params object[] args)
            : this(tok, severity, null, errKey, args)
        {
        }

        [Obsolete("Use overload with explicit Culture")]
        public TexlError(TexlNode node, DocumentErrorSeverity severity, ErrorResourceKey errKey, params object[] args)
            : this(node, severity, null, errKey, args)
        {
        }

        public TexlError(Token tok, DocumentErrorSeverity severity, CultureInfo locale, ErrorResourceKey errKey, params object[] args)
            : base(null, null, DocumentErrorKind.AXL, severity, locale, errKey, args)
        {
            Contracts.AssertValue(tok);

            Tok = tok;
            TextSpan = new Span(tok.VerifyValue().Span.Min, tok.VerifyValue().Span.Lim);

            _nameMapIDs = new List<string>();
        }        

        public TexlError(TexlNode node, DocumentErrorSeverity severity, CultureInfo locale, ErrorResourceKey errKey, params object[] args)
            : base(null, null, DocumentErrorKind.AXL, severity, locale, errKey, args)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(node.Token);

            Node = node;
            Tok = node.Token;
            TextSpan = node.GetTextSpan();

            _nameMapIDs = new List<string>();
        }

        public void MarkSinkTypeError(DName name)
        {
            Contracts.AssertValid(name);

            Contracts.Assert(!_nameMapIDs.Contains(name.Value));
            _nameMapIDs.Add(name.Value);
        }

        internal override void FormatCore(StringBuilder sb)
        {
            Contracts.AssertValue(sb);

            sb.AppendFormat(CultureInfo.CurrentCulture, TexlStrings.FormatSpan_Min_Lim(), Tok.Span.Min, Tok.Span.Lim);

            if (Node != null)
            {
                sb.AppendFormat(CultureInfo.CurrentCulture, TexlStrings.InfoNode_Node(), Node.ToString());
            }
            else
            {
                sb.AppendFormat(CultureInfo.CurrentCulture, TexlStrings.InfoTok_Tok(), Tok.ToString());
            }

            sb.AppendFormat(CultureInfo.CurrentCulture, TexlStrings.FormatErrorSeparator());
            base.FormatCore(sb);
        }
    }
}
