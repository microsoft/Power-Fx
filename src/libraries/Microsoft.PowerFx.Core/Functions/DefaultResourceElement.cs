// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Core.Functions
{
    public abstract class BaseResourceElement
    {
        public abstract bool IsUri { get; }

        public virtual Uri Uri => new Uri($"appres://blobmanager/{Handle.Handle}");

        public ResourceHandle Handle { get; init; }

        public abstract Task<string> GetAsStringAsync();

        public virtual async Task<string> GetAsBase64StringAsync()
        {
            string str = await GetAsStringAsync().ConfigureAwait(false);
            return ToBase64(str);
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

    public class StringResourceElement : BaseResourceElement
    {
        private readonly string _string;

        public StringResourceElement(IResourceManager resourceManager, string str)
        {
            _string = str;
            Handle = resourceManager.AddElement(this);
        }

        public override bool IsUri => false;        

        public override Task<string> GetAsStringAsync()
        {
            return Task.FromResult(_string);
        }
    }

    public class Base64StringResourceElement : BaseResourceElement
    {
        private readonly string _base64Str;

        public Base64StringResourceElement(IResourceManager resourceManager, string base64Str)
        {
            _base64Str = base64Str;
            Handle = resourceManager.AddElement(this);
        }

        public override bool IsUri => false;

        public override Task<string> GetAsBase64StringAsync()
        {
            return Task.FromResult(_base64Str);
        }

        public override Task<string> GetAsStringAsync()
        {
            string str = FromBase64(_base64Str);
            return Task.FromResult(str);
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
    }

    public class UriResourceElement : BaseResourceElement
    {
        private readonly Uri _uri;

        public UriResourceElement(IResourceManager resourceManager, Uri uri)
        {
            _uri = uri;
            Handle = resourceManager.AddElement(this);
        }

        public override bool IsUri => false;

        public override Task<string> GetAsBase64StringAsync()
        {
            return Task.FromResult(_uri.ToString());
        }

        public override Task<string> GetAsStringAsync()
        {
            throw new InvalidOperationException("Cannot get Base64 string from Uri");
        }       
    }
}
