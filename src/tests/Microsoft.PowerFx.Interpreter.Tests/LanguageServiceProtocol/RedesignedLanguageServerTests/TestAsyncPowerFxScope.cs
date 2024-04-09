// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class TestAsyncPowerFxScope : IAsyncPowerFxScope
    {
        public delegate Task<IIntellisenseResult> SuggestDelegate(string expression, int cursorPosition, CancellationToken cancellationToken);

        public delegate Task<CheckResult> CheckAsyncDelegate(string expression, CancellationToken cancellationToken);

        public delegate Task<string> ConvertToDisplayAsyncDelegate(string expression, CancellationToken cancellationToken);

        public SuggestDelegate SuggestAsyncCallback { get; init; }

        public CheckAsyncDelegate CheckAsyncCallback { get; init; }

        public ConvertToDisplayAsyncDelegate ConvertToDisplayAsyncCallback { get; init; }

        public CheckResult Check(string expression)
        {
            return CheckAsync(expression, CancellationToken.None).GetAwaiter().GetResult();
        }

        public Task<CheckResult> CheckAsync(string expression, CancellationToken cancellationToken)
        {
            return CheckAsyncCallback(expression, cancellationToken);
        }

        public string ConvertToDisplay(string expression)
        {
            return ConvertToDisplayAsync(expression, CancellationToken.None).GetAwaiter().GetResult();
        }

        public Task<string> ConvertToDisplayAsync(string expression, CancellationToken cancellationToken)
        {
           return ConvertToDisplayAsyncCallback(expression, cancellationToken);
        }

        public IIntellisenseResult Suggest(string expression, int cursorPosition)
        {
            return SuggestAsync(expression, cursorPosition, CancellationToken.None).GetAwaiter().GetResult();
        }

        public Task<IIntellisenseResult> SuggestAsync(string expression, int cursorPosition, CancellationToken cancellationToken)
        {
            return SuggestAsyncCallback(expression, cursorPosition, cancellationToken);
        }
    }
}
