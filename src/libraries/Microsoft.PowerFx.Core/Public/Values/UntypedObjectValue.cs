// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Dynamic;
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

    // No Type property here as this is an *untyped* object
    // This is the core interface for Untyped Objects (UO)
    public interface IUntypedObject
    {
        bool IsBlank();

        object ToObject();
    }

    public abstract class UntypedArray : IUntypedArray
    {
        public abstract IUntypedObject this[int index] { get; }

        public abstract int Length { get; }

        public abstract bool IsBlank();

        public object ToObject()
        {
            object[] array = new object[Length];

            for (int i = 0; i < Length; i++)
            {
                array[i] = this[i].ToObject();
            }

            return array;
        }
    }

    // Enables Index function
    public interface IUntypedArray : IUntypedObject
    {
        IUntypedObject this[int index] { get; }

        int Length { get; }        
    }

    public abstract class UntypedPropertyBag : IUntypedPropertyBag
    {
        public abstract string[] PropertyNames { get; }

        public abstract bool IsBlank();
        
        public abstract bool TryGetProperty(string value, out IUntypedObject result);

        public object ToObject()
        {
            ExpandoObject eo = new ExpandoObject();

            foreach (string propName in PropertyNames)
            {
                bool b = TryGetProperty(propName, out IUntypedObject propValue);
                ((IDictionary<string, object>)eo)[propName] = propValue.ToObject();
            }

            return eo;
        }
    }

    // Enables access via properties
    public interface IUntypedPropertyBag : IUntypedObject
    {
        bool TryGetProperty(string value, out IUntypedObject result);
    
        string[] PropertyNames { get; }        
    }

    // Hosts a FormulaValue in an UO
    public abstract class UntypedValue : IUntypedObject
    {
        public UntypedValue(object obj)
            : this(obj == null
                ? FormulaValue.NewBlank()
                : obj switch
                {
                    bool b => FormulaValue.New(b),                    
                    decimal dec => FormulaValue.New(dec),
                    double dbl => FormulaValue.New(dbl),
                    float flt => FormulaValue.New(flt),                    
                    int i => FormulaValue.New(i),
                    long lng => FormulaValue.New(lng),
                    null => FormulaValue.NewBlank(),
                    string s => FormulaValue.New(s),
                    Color c => FormulaValue.New(c),
                    DateTime dt => FormulaValue.New(dt),
                    Guid g => FormulaValue.New(g),
                    TimeSpan sp => FormulaValue.New(sp),
                    _ => null // includes IUntypedObject
                })
        {
        }

        public UntypedValue(FormulaValue val)
        {
            Value = val;
        }

        public virtual FormulaType Type => Value.Type;

        public virtual FormulaValue Value { get; }

        public virtual bool IsBlank()
        {
            if (Value is BlankValue || (Value is StringValue sv && string.IsNullOrEmpty(sv.Value)))
            {
                return true;
            }            

            return false;
        }

        public object ToObject()
        {
            return Value.ToObject();
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
            return Implementation.ToObject();
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
