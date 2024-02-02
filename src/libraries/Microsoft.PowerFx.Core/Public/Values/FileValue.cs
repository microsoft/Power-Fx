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
        public int Id { get; }

        private readonly IResourceManager _resourceManager;

        public IResourceElement ResourceElement => _resourceManager.GetResource(Id);

        internal FileValue(IResourceManager resourceManager, int id)
            : base(GetIRContext(resourceManager, id))
        {            
            _resourceManager = resourceManager;
            Id = id;                     
        }

        private static IRContext GetIRContext(IResourceManager resourceManager, int id)
        {
            IResourceElement resourceElement = (resourceManager ?? throw new ArgumentNullException(nameof(resourceManager), $"ResourceManager is required. (Id={id})")).GetResource(id);
            return (resourceElement ?? throw new ArgumentException($"ResourceManager does not contain element with Id {id}.", nameof(id))).FileType switch
            {
                FileType.Any => IRContext.NotInSource(FormulaType.Blob),                
                FileType.PDF => IRContext.NotInSource(FormulaType.Blob),
                FileType.Uri => IRContext.NotInSource(FormulaType.Blob),                

                // This will throw is FileValue constructor gets a null resourceManager
                _ => throw new ArgumentException("Invalid fileType")
            };
        }      

        // if this blob is of Uri type, return the element string wher this Uri is stored, otherwise get it from resource manager
        public override string ToString() => ResourceElement.FileType == FileType.Uri ? ResourceElement.String : _resourceManager.GetUri(Id).ToString();        

        // not implemented as there is no official Blob() function yet
        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings) => throw new NotImplementedException();        

        public override object ToObject() => this;        

        public abstract override void Visit(IValueVisitor visitor);        
    }
}
