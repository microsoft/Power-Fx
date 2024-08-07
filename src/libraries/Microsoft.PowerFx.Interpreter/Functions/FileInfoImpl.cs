// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    internal class FileInfoFunctionImpl : FileInfoFunction, IAsyncTexlFunction3
    {
        public async Task<FormulaValue> InvokeAsync(FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            var arg0 = args[0];
            if (arg0 is BlankValue || arg0 is ErrorValue)
            {
                return arg0;
            }

            var blob = (BlobValue)arg0; // binder should ensure this is true

            PowerFxFileInfo info;
            try
            {
                info = await blob.GetFileInfoAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new CustomFunctionErrorException("Fileinfo failure: " + ex.Message);
            }

            TypeMarshallerCache marshaller = new TypeMarshallerCache();
            var fxValue = marshaller.Marshal(info);
                        
            return fxValue;
        }
    }
}
