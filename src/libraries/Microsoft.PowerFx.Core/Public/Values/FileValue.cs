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
        private readonly string _string = null;
        private readonly bool _isBase64Encoded = false;
        private readonly FileType _fileType;
        private readonly string _identifier;
        private readonly int _id;
        private readonly ResourceManager _resourceManager;

        public FileType FileType => _fileType;

        public int Id => _id;

        internal FileValue(ResourceManager resourceManager, string str, bool isBase64Encoded, FileType fileType)
            : base(GetIRContext(fileType))
        {
            _fileType = fileType;
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager), "ResourceManager is required.");

            if (fileType == FileType.Uri)
            {
                if (isBase64Encoded)
                {
                    throw new ArgumentException("isBase64Encoded cannot be true for Uri type", nameof(isBase64Encoded));
                }

                _identifier = str;
                _id = -1;
            }
            else
            {
                _string = str;
                _isBase64Encoded = isBase64Encoded;

                if (isBase64Encoded)
                {
                    try
                    {
                        FromBase64(str);
                    }
                    catch
                    {
                        throw new ArgumentException("Invalid Base64 string", nameof(str));
                    }
                }

                _id = resourceManager.AddResource(this);
                _identifier = $"{ResourceManager.Prefix}{ResourceIdentifier}/{_id}";
            }
        }

        public FileValue GetResource()
        {
            return FileType == FileType.Uri && String.StartsWith(ResourceManager.Prefix, StringComparison.Ordinal) 
                    ? _resourceManager.GetResource(int.Parse(String.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[2], CultureInfo.InvariantCulture)) 
                    : null;
        }

        private static IRContext GetIRContext(FileType fileType)
        {
            return fileType switch
            {
                FileType.Any => IRContext.NotInSource(FormulaType.Blob),
                FileType.Audio => IRContext.NotInSource(FormulaType.Media),
                FileType.Image => IRContext.NotInSource(FormulaType.Image),
                FileType.PDF => IRContext.NotInSource(FormulaType.Blob),
                FileType.Uri => IRContext.NotInSource(FormulaType.Blob),
                FileType.Video => IRContext.NotInSource(FormulaType.Media),

                _ => throw new ArgumentException("Invalid fileType", nameof(fileType))
            };
        }

        public string Base64String
        {
            get
            {
                if (_fileType == FileType.Uri)
                {                    
                    throw new InvalidOperationException("Cannot get Base64 string of Uri");
                }

                return _isBase64Encoded ? _string : ToBase64(_string);
            }
        }

        public string String
        {
            get
            {
                if (_fileType == FileType.Uri)
                {
                    // do we want to return the content?
                    return _identifier;
                }

                return _isBase64Encoded ? FromBase64(_string) : _string;
            }
        }

        private static string FromBase64(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            byte[] bytes = Convert.FromBase64String(str);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string ToBase64(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(bytes);
        }

        public override string ToString()
        {
            return _identifier;
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

        public abstract string ResourceIdentifier { get; }
    }
}
