// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol
{ 
    public interface ILanguageServerOperationHandler
    {
        public void Handle(object context);

        public Task HandleAsync(object context);
    }

    public interface ILanguageServerOperationHandler<TOperationInput> : ILanguageServerOperationHandler
    {
        public void Handle(LanguageServerOperationContext<TOperationInput> context);

        public Task HandleAsync(LanguageServerOperationContext<TOperationInput> context);
    }

    public record LanguageServerOperationContext<TOperationInput>(string id, TOperationInput operationInput, LanguageServerResponseBuilder languageServerResponseBuilder);

    public abstract class BaseLanguageServerOperationHandler<TOperationInput> : ILanguageServerOperationHandler<TOperationInput>
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        protected LanguageServerOperationContext<TOperationInput> _operationContext;

        public BaseLanguageServerOperationHandler()
        {
        }

        protected virtual async Task<bool> PreHandleAsync()
        {
            return true;
        }

        protected virtual async Task<bool> PostHandleAsync()
        {
            return true;
        }

        protected virtual async Task<bool> HandleOperationAsync()
        {
            return true;
        }

        public async Task HandleAsync(LanguageServerOperationContext<TOperationInput> operationContext)
        {
            _operationContext = operationContext;
            var isPreHandleSuccess = await PreHandleAsync().ConfigureAwait(false);

            if (!isPreHandleSuccess)
            {
                return;
            }

            var isHandleSuccess = await HandleOperationAsync().ConfigureAwait(false);

            if (!isHandleSuccess)
            {
                return;
            }

            await PostHandleAsync().ConfigureAwait(false);
        }

        public void Handle(LanguageServerOperationContext<TOperationInput> operationInput)
        {
            HandleAsync(operationInput).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        void ILanguageServerOperationHandler.Handle(object context)
        {
            if (context is LanguageServerOperationContext<TOperationInput> operationContext)
            {
                Handle(operationContext);
            }
        }

        async Task ILanguageServerOperationHandler.HandleAsync(object context)
        {
            if (context is LanguageServerOperationContext<TOperationInput> operationContext)
            {
                await HandleAsync(operationContext).ConfigureAwait(false);
            }
        }

        protected abstract Task<CheckResult> CheckAsync(string expression);

        /// <summary>
        /// Returns the expression from preferrably the request params or query params if not present in the request params.
        /// </summary>
        /// <param name="requestParams">Request params.</param>
        /// <param name="queryParams">Query Params.</param>
        /// <returns>Expression.</returns>
        protected static string GetExpression(LanguageServerRequestBaseParams requestParams, NameValueCollection queryParams)
        {
            return requestParams?.Text ?? queryParams.Get("expression");
        }

        public static bool TryParseParams<T>(string json, out T result)
        {
            Contracts.AssertNonEmpty(json);

            try
            {
                result = JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }
}
