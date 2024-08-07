// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // FileInfo(blob) --> FileInfo object
    internal class FileInfoFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public FileInfoFunction()
            : base(
                  "FileInfo", 
                  TexlStrings.AboutFileInfo, 
                  FunctionCategories.Information,
                  PowerFxFileInfo._fileInfoType, // return
                  0,
                  1,
                  1, // arity
                  DType.Blob)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.FileInfoArg1 };
        }
    }
}
