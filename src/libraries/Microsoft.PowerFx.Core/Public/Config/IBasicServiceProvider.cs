// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core.Public.Config
{
    internal interface IBasicServiceProvider : IServiceProvider
    {        
        void AddService(Type serviceType, object service);
    }
}
