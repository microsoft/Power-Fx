// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// The backing implementation for CustomObjectValue, for example Json, Xml,
    /// or the Ast or Value system from another language.
    /// </summary>
    public interface ICustomObject
    {
        FormulaType Type { get; } // Use ExternalType if the type is incompatible with PowerFx

        int GetArrayLength();

        ICustomObject this[int index] { get; } // 0-based index

        bool TryGetProperty(string value, out ICustomObject result);

        string GetString();

        double GetDouble();

        bool GetBoolean();
    }

    public class CustomObjectValue : ValidFormulaValue
    {
        public ICustomObject Impl { get; }

        internal CustomObjectValue(IRContext irContext, ICustomObject impl)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.CustomObject);
            Impl = impl;
        }

        public override object ToObject()
        {
            return Impl; // Hosts will need to be able to interpret the backing value
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
