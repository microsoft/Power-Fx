// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// Register a handle for providing code-fix results. 
    /// This object must be stateless - a new instance can be created each time.
    /// </summary>
    public abstract class CodeFixHandler
    {
        /// <summary>
        /// Invoked by user when there are errors present - offers possible fixes for the errors. 
        /// </summary>
        /// <param name="engine">The engine that that the result was for. This can be useful for gathering broader context for the fix.</param>
        /// <param name="checkResult">An attempt at parsing and binding. This is likely unsuccessful (hence the request for the codefix).</param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public abstract Task<IEnumerable<CodeFixSuggestion>> SuggestFixesAsync(
            Engine engine,
            CheckResult checkResult,
            CancellationToken cancel);

        /// <summary>
        /// Callback invoke if the user applies the handler. 
        /// Note that this may be invoked on a different server then invoked SuggestFixesAsync.
        /// </summary>
        /// <param name="actionIdentifier">The <see cref="CodeFixSuggestion.ActionIdentifier"/> from the suggestion.</param>
        public virtual void OnCodeActionApplied(string actionIdentifier)
        {
            // default impl is a nop. 
        }

        /// <summary>
        /// Get unique name to describe this handler.
        /// This correlates the callbacks across invocations and so must be stable across processes. 
        /// </summary>
        public virtual string HandlerName => this.GetType().FullName;
    }

    /// <summary>
    /// Describe a code fix suggestion to correct an error in an expression. 
    /// </summary>
    public class CodeFixSuggestion
    {
        /// <summary>
        /// Required - Gets or sets code fix expression text to be applied.
        /// </summary>
        public string SuggestedText { get; set; }

        /// <summary>
        /// Optional, Gets or sets title to be displayed on code fix suggestion.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Optional, Opaque string, passed back to handler if this fix is applied. 
        /// </summary>
        public string ActionIdentifier { get; set; }
    }
}
