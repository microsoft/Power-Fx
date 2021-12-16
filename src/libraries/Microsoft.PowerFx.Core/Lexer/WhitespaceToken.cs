using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer
{
    /// <summary>
    /// A token for a series of whitespace characters.
    /// </summary>
    internal class WhitespaceToken : Token
    {
        public string Value { get; }

        public WhitespaceToken(string value, Span span)
            : base(TokKind.Whitespace, span)
        {
            Contracts.AssertValue(value);
            Value = value;
        }

        public override Token Clone(Span ts)
        {
            return new WhitespaceToken(Value, ts);
        }
    }
}