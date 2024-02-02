// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Buffers.Text;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public abstract class FileValue : ValidFormulaValue
    {        
        private readonly IResourceManager _resourceManager;        

        public int Id { get; }

        public IResourceElement ResourceElement => _resourceManager.GetResource(Id);

        internal FileValue(IResourceManager resourceManager, IResourceElement element)
            : base(GetIRContext(element?.FileType))
        {            
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager), "ResourceManager is required.");
            Id = _resourceManager.AddResource(element);                       
        }

        private static IRContext GetIRContext(FileType? fileType)
        {
            return fileType switch
            {
                FileType.Any => IRContext.NotInSource(FormulaType.Blob),
                FileType.Audio => IRContext.NotInSource(FormulaType.Media),
                FileType.Image => IRContext.NotInSource(FormulaType.Image),
                FileType.PDF => IRContext.NotInSource(FormulaType.Blob),
                FileType.Uri => IRContext.NotInSource(FormulaType.Blob),
                FileType.Video => IRContext.NotInSource(FormulaType.Media),

                // This will throw is FileValue constructor gets a null resourceManager
                _ => throw new ArgumentException("Invalid fileType", nameof(fileType))
            };
        }      

        public override string ToString()
        {
            if (ResourceElement.FileType == FileType.Uri)
            {
                return ResourceElement.String;
            }

            return _resourceManager.GetUri(Id).ToString();
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            throw new NotImplementedException();
        }

        public override object ToObject()
        {
            return this;
        }

        public abstract override void Visit(IValueVisitor visitor);        
    }
}
