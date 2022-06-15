// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Base64Encode(arg:s)
    internal sealed class Base64EncodeFunction : StringOneArgFunction
    {
        public override bool SupportsParamCoercion => false;

        public Base64EncodeFunction()
            : base("Base64Encode", TexlStrings.AboutBase64Encode, FunctionCategories.Text)
        {
        }
    }

    // Base64Decode(arg:s)
    internal sealed class Base64DecodeFunction : StringOneArgFunction
    {
        public override bool SupportsParamCoercion => false;

        public Base64DecodeFunction()
            : base("Base64Decode", TexlStrings.AboutBase64Decode, FunctionCategories.Text)
        {
        }
    }
}
