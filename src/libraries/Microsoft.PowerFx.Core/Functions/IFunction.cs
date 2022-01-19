// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppMagic.Transport;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    [TransportType(TransportKind.ByValue)]
    internal interface IFunction
    {
        /// <summary>
        /// The locale-specific name of the function.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The function's fully qualified name, including the namespace.
        /// If the function is in the global namespace, Function.QualifiedName is the same as Function.Name.
        /// </summary>
        string QualifiedName { get; }

        /// <summary>
        /// A description associated with this function.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// External link to function help.
        /// </summary>
        string HelpLink { get; }

        /// <summary>
        /// The categories of the function.
        /// </summary>
        FunctionCategories FunctionCategoriesMask { get; }
    }
}
