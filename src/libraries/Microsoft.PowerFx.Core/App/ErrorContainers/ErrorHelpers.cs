// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.App.ErrorContainers
{
    internal static class ErrorHelpers
    {
        public static void TypeMismatchError(this IErrorContainer errorContainer, TexlNode node, DType expected, DType actual)
        {
            errorContainer.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrBadType_ExpectedType_ProvidedType, expected.GetKindString(), actual.GetKindString());
        }
    }
}
