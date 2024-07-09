// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// The backing implementation for UntypedObjectValue, for example Json, Xml,
    /// or the Ast or Value system from another language.
    /// </summary>
    public interface IUntypedObject
    {
        /// <summary>
        /// Use ExternalType if the type is incompatible with PowerFx.
        /// </summary>
        FormulaType Type { get; }

        int GetArrayLength();

        /// <summary>
        /// 0-based index.
        /// </summary>
        IUntypedObject this[int index] { get; }

        bool TryGetProperty(string value, out IUntypedObject result);

        bool TrySetProperty(string name, IUntypedObject value);

        string GetString();

        double GetDouble();

        decimal GetDecimal();

        bool GetBoolean();

        // For providers that do not imply a type for numbers (JSON), GetUntypedNumber is used to access the raw
        // underlying string representation, likely from the source representation (again in the case of JSON).
        // It is validated to be in a culture invariant standard format number, possibly with dot decimal separator and "E" exponent.
        // This method need not be implemetned if ExteranlType.UntypedNumber is not used.
        // GetDouble and GetDecimal can be called on an ExternalType.UntypedNumber.
        string GetUntypedNumber();

        /// <summary>
        /// If the untyped value is an object then this method returns true and the list of available property names in the <paramref name="propertyNames"/> parameter.
        /// Returns false otherwise, <paramref name="propertyNames"/> will be null in this case.
        /// </summary>
        bool TryGetPropertyNames(out IEnumerable<string> propertyNames);
    }

    [DebuggerDisplay("UntypedObjectValue({Impl})")]
    public class UntypedObjectValue : ValidFormulaValue
    {
        public IUntypedObject Impl { get; }

        internal UntypedObjectValue(IRContext irContext, IUntypedObject impl)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.UntypedObject);
            Impl = impl;
        }

        public override object ToObject()
        {
            throw new NotImplementedException();
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            // Not supported for the time being.
            throw new NotImplementedException("UntypedObjectValue cannot be serialized.");
        }
    }
}
