// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    // By definition an Untyped Object (UO) is a Power Fx object whose type cannot be determined
    // at design time. It can only be known at runtime.
    //
    // IUntypedObject defines any UO
    // For implementing an UO, extra interfaces are used to describe the structure of the object
    // in an abstract manner
    // - ISupportsArray for an array 
    // - ISupportProperties for property bags (like records, objects)
    // - ISupportsPropertyEnumeration for property bags where the list of properties can be enumerated
    //   (this can be useful for Intellisense)
    //
    // These objects can cumulate multiple interfaces if there are multiple possibilities for 
    // traversing the UO structure
    // At any point of time when traversing an UO, each object can only implement the interfaces
    // it can support
    //
    // The terminal elements are SupportsFxValue to retrieve a FormulaValue

    public interface IUntypedObject
    {
        bool IsBlank();
    }

    public interface ISupportsArray : IUntypedObject
    {
        IUntypedObject this[int index] { get; }

        int Length { get; }
    }

    public interface ISupportsProperties : IUntypedObject
    {
        bool TryGetProperty(string value, out IUntypedObject result);
    }

    public interface ISupportsPropertyEnumeration : IUntypedObject
    {
        string[] PropertyNames { get; }
    }

    public abstract class SupportsFxValue : IUntypedObject
    {
        public SupportsFxValue(object obj)
            : this(obj == null
                ? FormulaValue.NewBlank()
                : obj switch
                {
                    null => FormulaValue.NewBlank(),
                    bool b => FormulaValue.New(b),
                    DateTime dt => FormulaValue.New(dt),
                    decimal dec => FormulaValue.New(dec),
                    double dbl => FormulaValue.New(dbl),
                    float flt => FormulaValue.New(flt),
                    Guid g => FormulaValue.New(g),
                    int i => FormulaValue.New(i),
                    long lng => FormulaValue.New(lng),
                    string s => FormulaValue.New(s),
                    Color c => FormulaValue.New(c),
                    TimeSpan sp => FormulaValue.New(sp),
                    _ => null // includes IUntypedObject
                })
        {
        }

        public SupportsFxValue(FormulaValue val)
        {
            Value = val;
        }

        public virtual FormulaType Type => Value.Type;

        public virtual FormulaValue Value { get; }

        public virtual bool IsBlank()
        {
            if (Value is BlankValue)
            {
                return true;
            }

            if (Value is StringValue str)
            {
                return str.Value.Length == 0;
            }

            return false;
        }
    }

    [DebuggerDisplay("UntypedObjectValue({Impl})")]
    public class UntypedObjectValue : ValidFormulaValue
    {
        public IUntypedObject Implementation { get; }

        internal UntypedObjectValue(IRContext irContext, IUntypedObject implementation)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.UntypedObject);
            Implementation = implementation;
        }

        public override object ToObject()
        {
            if (Implementation is SupportsFxValue fxValue)
            {
                return fxValue.Value.ToObject();
            }

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
