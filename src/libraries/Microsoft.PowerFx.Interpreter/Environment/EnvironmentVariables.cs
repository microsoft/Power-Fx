// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    public abstract class EnvironmentVariables : RecordValue
    {
        public EnvironmentVariables(RecordType recordType) 
            : base(recordType)
        {
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            throw new NotImplementedException();
        }
    }
}
