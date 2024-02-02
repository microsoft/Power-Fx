// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Tests
{
    public class TestResourceManager : IResourceManager
    {
        private int _index = -1;
        private readonly ConcurrentDictionary<int, IResourceElement> _dic = new ();

        public int AddResource(IResourceElement element)
        {
            int id = Interlocked.Increment(ref _index);
            _dic.AddOrUpdate(id, element, (i, e) => throw new InvalidOperationException("Duplicate resource id"));
            return id;
        }

        public IResourceElement GetElementFromString(string str, FileType fileType = FileType.Any)
        {
            return new TestResourceElement(fileType, str, false);
        }

        public IResourceElement GetElementFromBase64String(string base64str, FileType fileType = FileType.Any)
        {
            return new TestResourceElement(fileType, base64str, true);
        }

        public IResourceElement GetResource(int i)
        {
            if (!_dic.TryGetValue(i, out IResourceElement element))
            {
                return null;
            }

            // if we recognize a local Uri targetting another resource, let's return that resource instead
            if (element.FileType == FileType.Uri && element.String.StartsWith("appres://", StringComparison.Ordinal))
            {
                int targetId = int.Parse(element.String.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[2], CultureInfo.InvariantCulture);
                return GetResource(targetId);
            }
        
            return element;
        }

        public Uri GetUri(int i)
        {
            IResourceElement element = GetResource(i);
            return element != null ? new Uri($"appres://{GetUriIdentifier(element.FileType)}/{i}") : null;
        }

        private string GetUriIdentifier(FileType fileType)
        {
            return fileType switch
            {
                FileType.Image => "imagemanager",
                FileType.Audio => "audiomanager",
                FileType.Video => "videomanager",
                FileType.PDF => "pdfmanager",
                _ => "blobmanager"
            };
        }

        public bool RemoveResource(int i)
        {
            return _dic.TryRemove(i, out _);
        }
    }

    internal class TestResourceElement : IResourceElement
    {
        private readonly FileType _fileType;
        private readonly string _string;
        private readonly bool _isBase64Encoded;

        public TestResourceElement(FileType fileType, string str, bool isBase64Encoded)
        {
            _fileType = fileType;
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
        }

        public FileType FileType => _fileType;

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

        public string String => _isBase64Encoded ? FromBase64(_string) : _string;

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
    }
}
