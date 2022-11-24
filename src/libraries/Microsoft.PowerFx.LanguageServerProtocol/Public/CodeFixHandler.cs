// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// CodeFixHandler base class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CodeFixHandler<T> : ICodeFixHandler where T : ICodeFixHandler
    {
        public string HandlerName { get { return this.GetType().FullName; } }

        public virtual void OnCodeActionApplied(CodeAction codeAction)
        {
        }

        public abstract Task<IEnumerable<CodeActionResult>> SuggestFixesAsync(Engine engine, CheckResult checkResult, CancellationToken cancel);
    }
}
