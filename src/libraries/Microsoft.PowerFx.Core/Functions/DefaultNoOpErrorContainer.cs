// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    // Default "no-op" error container that does not post document errors.
    // See the TexlFunction.DefaultErrorContainer property and its uses for more info.
    internal sealed class DefaultNoOpErrorContainer : IErrorContainer
    {
        public DocumentErrorSeverity DefaultSeverity => DocumentErrorSeverity.Min;

        public TexlError EnsureError(TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            return null;
        }

        public TexlError Error(TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            return null;
        }

        public TexlError EnsureError(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            return null;
        }

        public TexlError Error(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            return null;
        }

        public void Errors(TexlNode node, DType nodeType, KeyValuePair<string, DType> schemaDifference, DType schemaDifferenceType)
        {
            // Do nothing.
        }
    }
}