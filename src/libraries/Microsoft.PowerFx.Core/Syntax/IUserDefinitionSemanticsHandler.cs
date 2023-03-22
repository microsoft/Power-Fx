// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Parser;

namespace Microsoft.PowerFx
{
    internal interface IUserDefinitionSemanticsHandler
    {
        /// <summary>
        /// Perform expression-level semantics checks which require a binding, this is only applicable for UDFs.
        /// </summary>
        void CheckSemanticsOnDeclaration(TexlBinding binding, IEnumerable<UDFArg> uDFArgs, IErrorContainer errors);
    }
}
