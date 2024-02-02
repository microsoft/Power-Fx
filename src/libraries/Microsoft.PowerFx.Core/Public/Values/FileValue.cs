// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public abstract class FileValue : ValidFormulaValue
    {
        public ResourceHandle Handle { get; }

        private readonly IResourceManager _resourceManager;

        public BaseResourceElement ResourceElement => _resourceManager.GetResource(Handle);

        internal FileValue(IResourceManager resourceManager, ResourceHandle handle)
            : base(IRContext.NotInSource(FormulaType.Blob))
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager), $"ResourceManager is required.");
            Handle = handle;

            if (_resourceManager.GetResource(handle) == null)
            {
                throw new ArgumentException($"ResourceManager does not contain element with Id {handle.Handle}.", nameof(handle));
            }
        }

        // if this blob is of Uri type, return the element string wher this Uri is stored, otherwise get it from resource manager
        public override string ToString() => ResourceElement.Uri.ToString(); //.IsUri ? ResourceElement.String : _resourceManager.GetUri(Handle).ToString();

        // not implemented as there is no official Blob() function yet
        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings) => throw new NotImplementedException();

        public override object ToObject() => this;

        public abstract override void Visit(IValueVisitor visitor);
    }
}
