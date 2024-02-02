// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Microsoft.PowerFx.Core.Functions
{
    public class DefaultResourceManager : IResourceManager
    {
        private int _index = -1;
        private readonly ConcurrentDictionary<int, BaseResourceElement> _dic = new ();

        public ResourceHandle AddElement(BaseResourceElement element)
        {
            int id = Interlocked.Increment(ref _index);
            _dic.AddOrUpdate(id, element, (i, e) => throw new InvalidOperationException("Duplicate resource id"));
            return new ResourceHandle() { Handle = id };
        }

        public BaseResourceElement GetResource(ResourceHandle handle)
        {
            if (!_dic.TryGetValue(handle.Handle, out BaseResourceElement element))
            {
                return null;
            }

            return element;
        }

        public bool RemoveResource(ResourceHandle handle)
        {
            return _dic.TryRemove(handle.Handle, out _);
        }
    }    
}
