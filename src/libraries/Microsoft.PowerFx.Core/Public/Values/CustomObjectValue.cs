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
        /// <summary>
        /// Use ExternalType if the type is incompatible with PowerFx.
        /// </summary>
        FormulaType Type { get; }

        int GetArrayLength();

        /// <summary>
        /// 0-based index.
        /// </summary>
        ICustomObject this[int index] { get; }

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
            throw new NotImplementedException();
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
