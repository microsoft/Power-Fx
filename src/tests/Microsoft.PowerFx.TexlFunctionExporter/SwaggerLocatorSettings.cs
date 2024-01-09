// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    public class SwaggerLocatorSettings
    {
        public IEnumerable<string> FoldersToExclude { get; }

        public SwaggerLocatorSettings(IEnumerable<string> foldersToExclude = null)
        {
            FoldersToExclude = foldersToExclude ?? new List<string>() 
            { 
                @"\locpublish\", 
                @"\SharedTestAssets\", 
                @"\CustomMSBuildTasks\" 
            };        
        }
    }
}
