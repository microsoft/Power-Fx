// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal class MissingSymbolValuesException : Exception
    {
        public MissingSymbolValuesException(string message)
            : base(message)
        {
        }

        public FormulaValue ToErrorValue(IRContext irContext)
        {
            return CommonErrors.CustomError(irContext, Message);
        }
    }
}
