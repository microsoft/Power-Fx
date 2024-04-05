// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Asynchronous version of <see cref="IPowerFxScope"/>.
    /// This allows consumers of SDK to asynchronously perform scope operations.
    /// </summary>
    public interface IAsyncPowerFxScope : IPowerFxScope
    {
        /// <summary>
        /// Check for errors in the given expression. 
        /// </summary>
        /// <param name="expression">The expression to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>CheckResult.</returns>
        Task<CheckResult> CheckAsync(string expression, CancellationToken cancellationToken);

        /// <summary>
        /// Provide intellisense for expression.
        /// <param name="expression"> The expression to provide intellisense for.</param>
        /// <param name="cursorPosition"> The position of the cursor in the expression.</param>
        /// <param name="cancellationToken"> Cancellation token.</param>
        /// <returns>Intellisense results.</returns>
        /// </summary>
        Task<IIntellisenseResult> SuggestAsync(string expression, int cursorPosition, CancellationToken cancellationToken);

        /// <summary>
        /// Converts punctuators and identifiers in an expression to the appropriate display format.
        /// <param name="expression"> The expression to convert.</param>
        /// <param name="cancellationToken"> Cancellation token.</param>
        /// </summary>
        Task<string> ConvertToDisplayAsync(string expression, CancellationToken cancellationToken);
    }
}
