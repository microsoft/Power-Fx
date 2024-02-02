// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    // Manages resources, id and Uri generation
    public interface IResourceManager
    {
        Uri GetUri(int i);

        IResourceElement GetElementFromString(string str, FileType fileType = FileType.Any);

        IResourceElement GetElementFromBase64String(string base64str, FileType fileType = FileType.Any);

        int AddResource(IResourceElement element);

        IResourceElement GetResource(int i);

        bool RemoveResource(int i);
    }
    
    public interface IResourceElement
    {
        FileType FileType { get; }

        string String { get; }

        // if FileType is Uri, then this should throw an exception
        string Base64String { get; }        
    }    
}
