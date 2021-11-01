using Microsoft.PowerFx.Core.Functions;

namespace Microsoft.PowerFx.Core.Texl.Intellisense.IntellisenseData
{
    /// <summary>
    /// This class represents the default intellisense result.
    /// </summary>
    internal class DefaultIntellisenseData : IIntellisenseData
    {
        public int ReplacementStartIndex => 0;
        public int ReplacementLength => 0;
        public TexlFunction CurFunc => null;
        public int ArgCount => 0;
        public int ArgIndex => 0;
        public string Script => string.Empty;

        /// <summary>
        /// No-op, default Intellisense does not augment signatures at this stage.
        /// </summary>
        /// <param name="newHighlightStart">
        /// 0 when this method returns
        /// </param>
        /// <param name="newHighlightEnd">
        /// 0 when this method returns
        /// </param>
        /// <param name="newParamName">
        /// <see cref="string.Empty"/> when this method returns
        /// </param>
        /// <param name="newInvariantParamName">
        /// <see cref="string.Empty"/> when this method returns
        /// </param>
        /// <returns>
        /// False
        /// </returns>
        public static bool DefaultTryAugmentSignature(TexlFunction func, int argIndex, string paramName, int highlightStart, out int newHighlightStart, out int newHighlightEnd, out string newParamName, out string newInvariantParamName)
        {
            newHighlightStart = 0;
            newHighlightEnd = 0;
            newParamName = string.Empty;
            newInvariantParamName = string.Empty;
            return false;
        }

        public bool TryAugmentSignature(TexlFunction func, int argIndex, string paramName, int highlightStart, out int newHighlightStart, out int newHighlightEnd, out string newParamName, out string newInvariantParamName) =>
            DefaultTryAugmentSignature(func, argIndex, paramName, highlightStart, out newHighlightStart, out newHighlightEnd, out newParamName, out newInvariantParamName);

        /// <summary>
        /// Returns nothing, default Intellisense does not suffix parameters by default.
        /// </summary>
        /// <param name="function">
        /// The function that will not be suffixed
        /// </param>
        /// <param name="paramName">
        /// The parameter that will not be suffixed
        /// </param>
        /// <returns><see cref="string.Empty"/></returns>
        public static string GenerateDefaultParameterDescriptionSuffix(TexlFunction function, string paramName) => string.Empty;

        public string GenerateParameterDescriptionSuffix(TexlFunction function, string paramName) => GenerateDefaultParameterDescriptionSuffix(function, paramName);
    }
}