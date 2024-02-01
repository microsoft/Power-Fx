// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    public class ResourceManager
    {
        public const string Prefix = @"appres://";

        private int _index = 0;
        private readonly object _lock = new object();
        private readonly Dictionary<int, FileValue> _resources = new Dictionary<int, FileValue>();

        internal int AddResource(FileValue resource)
        {
            lock (_lock)
            {
                _resources.Add(_index, resource);
                return _index++;
            }
        }

        public FileValue GetResource(int i)
        {
            lock (_lock)
            {
                return _resources.TryGetValue(i, out FileValue resource) ? resource : null;
            }
        }

        internal bool RemoveResource(int i)
        {
            lock (_lock)
            {
                return _resources.Remove(i);
            }
        }
    }
}
