// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx
{
    public class CustomFunctionSignatureHelper
    {
        internal readonly string[] ArgLabel;

        public int Count => ArgLabel.Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomFunctionSignatureHelper"/> class.
        /// Used for providing a custom argument description for a function.
        /// </summary>
        /// <param name="argDesc">Description of the arguments the function can take.</param>
        public CustomFunctionSignatureHelper(params string[] argDesc)
        {
            ArgLabel = argDesc;
        }
    }
}
