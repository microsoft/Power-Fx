// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public abstract class UntypedObjectBase : IUntypedObject
    {        
        public UntypedObjectBase(UntypedObjectCapabilities capabilities)
        {            
            Capabilities = capabilities;

            if (capabilities == 0)
            {
                throw new ArgumentException("Capabilities cannot be 0.", nameof(capabilities));
            }
        }        

        public UntypedObjectCapabilities Capabilities { get; }

        public abstract FormulaType Type { get; }

        public abstract UntypedObjectBase IndexOf(int index);

        public abstract int ArrayLength();

        public abstract bool AsBoolean();

        public abstract double AsDouble();

        public abstract string AsString();

        public abstract UntypedObjectBase GetProperty(string propertyName);

        public abstract string[] PropertyNames();

        public abstract bool IsBlank();

        public IUntypedObject this[int index]
        {
            get
            {
                CheckCapability(UntypedObjectCapabilities.SupportsArray, "Cannot call UntypedObject indexer on an UntypedObject not supporting Array APIs");

                if (index < 0 || index > GetArrayLength())
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return IndexOf(index);
            }
        }        

        public int GetArrayLength()
        {
            CheckCapability(UntypedObjectCapabilities.SupportsArray, "Cannot call UntypedObject indexer on an UntypedObject not supporting Array APIs");
            return ArrayLength();
        }

        public bool GetBoolean()
        {
            CheckCapability(UntypedObjectCapabilities.SupportsBoolean, "Cannot call GetBoolean on an UntypedObject not supporting this API");
            return AsBoolean();
        }

        public double GetDouble()
        {
            CheckCapability(UntypedObjectCapabilities.SupportsDouble, "Cannot call GetDouble on an UntypedObject not supporting this API");
            return AsDouble();
        }

        public string[] GetPropertyNames()
        {
            CheckCapability(UntypedObjectCapabilities.SupportsProperties, "Cannot call GetPropertyNames on an UntypedObject not supporting properties");
            return PropertyNames();
        }

        public string GetString()
        {
            CheckCapability(UntypedObjectCapabilities.SupportsString, "Cannot call GetString on an UntypedObject not supporting this API");
            return AsString();
        }

        public bool TryGetProperty(string propertyName, out IUntypedObject result)
        {
            CheckCapability(UntypedObjectCapabilities.SupportsProperties, "Cannot call GetPropertyNames on an UntypedObject not supporting properties");
            bool propertyExists = PropertyNames().Contains(propertyName);
            
            result = propertyExists ? GetProperty(propertyName) : default;
            return propertyExists;
        }

        public bool HasCapability(UntypedObjectCapabilities capabilities)
        {
            return (Capabilities & capabilities) != 0;
        }

        private void CheckCapability(UntypedObjectCapabilities requiredCapability, string message)
        {
            if (!HasCapability(requiredCapability))
            {
                throw new NotImplementedException(message);
            }
        }
    }

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

        // 0-based index.
        IUntypedObject this[int index] { get; }

        string[] GetPropertyNames();

        bool TryGetProperty(string value, out IUntypedObject result);

        string GetString();

        double GetDouble();

        bool GetBoolean();
    }

    [Flags]
    public enum UntypedObjectCapabilities
    {
        SupportsArray = 0x1,
        SupportsProperties = 0x2,

        SupportsString = 0x100,
        SupportsDouble = 0x200,
        SupportsBoolean = 0x400,
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
