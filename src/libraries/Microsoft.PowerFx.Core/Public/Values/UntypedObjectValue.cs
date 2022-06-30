// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
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

        string GetString();

        double GetDouble();

        bool GetBoolean();
    }

    [DebuggerDisplay("UntypedObjectValue({Impl})")]
    public class UntypedObjectValue : ValidFormulaValue
    {
        [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Design choice")]
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
    }
}
