// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// All values except BlankValue and ErrorValue should inherit from this base class.
    /// BlankValue and ErrorValue inherit directly from FormulaValue. The type parameter
    /// T in DValue is constrained to ValidFormulaValue, meaning that BlankValue
    /// and ErrorValue can never be substituted for T.
    /// </summary>
    public abstract class ValidFormulaValue : FormulaValue
    {
        internal ValidFormulaValue(IRContext irContext)
            : base(irContext)
        {
        }

        // for custom types
        protected ValidFormulaValue(FormulaType type)
            : this(IRContext.NotInSource(type))
        {
        }
    }
}
