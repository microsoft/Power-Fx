// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public static class BlobTestFunctionsExtensions
    {
        public static void AddBlobTestFunctions(this PowerFxConfig config)
        {
            config.AddFunction(new BlobFunctionImpl());            
            config.AddFunction(new BlobGetStringFunctionImpl());
            config.AddFunction(new BlobGetBase64StringFunctionImpl());
        }
    }

    internal class BlobFunction : BuiltinFunction
    {
        public BlobFunction()
             : base("Blob", (loc) => "Converts a string to a Blob.", FunctionCategories.Text, DType.Blob, 0, 1, 2, DType.String, DType.Boolean)
        {
        }

        public override bool IsSelfContained => true;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { (loc) => "string" };
            yield return new TexlStrings.StringGetter[] { (loc) => "string", (loc) => "isBase64String" };            
        }
    }

    internal class BlobFunctionImpl : BlobFunction, IAsyncTexlFunction5
    {
        public Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();                       

            if (args[0] is BlankValue)
            {
                return Task.FromResult<FormulaValue>(args[0]);
            }

            if (args[0] is BlobValue)
            {
                return Task.FromResult(args[0]);
            }

            if (args[0] is not StringValue sv)
            {
                return Task.FromResult<FormulaValue>(CommonErrors.RuntimeTypeMismatch(args[0].IRContext));
            }
            
            bool isBase64String = args.Length >= 2 && args[1] is BooleanValue bv && bv.Value;                                    

            return Task.FromResult<FormulaValue>(BlobValue.NewBlob(sv.Value, isBase64String));
        }
    }

    internal class BlobUriFunction : BuiltinFunction
    {
        public BlobUriFunction()
             : base("Blob", (loc) => "Converts a string to a Blob.", FunctionCategories.Text, DType.Blob, 0, 2, 2, DType.String, DType.String)
        {
        }

        public override bool IsSelfContained => true;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {            
            yield return new TexlStrings.StringGetter[] { (loc) => "string", (loc) => "Uri" };
        }
    }   

    internal class BlobGetStringFunction : BuiltinFunction
    {
        public BlobGetStringFunction()
             : base("BlobGetString", (loc) => "Reads Blob content as string.", FunctionCategories.Text, DType.String, 0, 1, 1, DType.Blob)
        {
        }

        public override bool IsSelfContained => true;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { (loc) => "blob" };
        }
    }

    internal class BlobGetStringFunctionImpl : BlobGetStringFunction, IAsyncTexlFunction5
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();            

            BlobValue blob = args[0] as BlobValue;

            if (args[0] is BlankValue || (blob != null && string.IsNullOrEmpty(await blob.GetAsStringAsync(null, CancellationToken.None).ConfigureAwait(false))))
            {
                return FormulaValue.NewBlank(FormulaType.String);
            }

            if (blob == null)
            {
                return CommonErrors.RuntimeTypeMismatch(args[0].IRContext);
            }

            return FormulaValue.New(await blob.GetAsStringAsync(null, CancellationToken.None).ConfigureAwait(false));
        }

        public Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaValue[] args, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    internal class BlobGetBase64StringFunction : BuiltinFunction
    {
        public BlobGetBase64StringFunction()
             : base("BlobGetBase64String", (loc) => "Reads Blob content as base64 string.", FunctionCategories.Text, DType.String, 0, 1, 1, DType.Blob)
        {
        }

        public override bool IsSelfContained => true;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { (loc) => "blob" };
        }
    }

    internal class BlobGetBase64StringFunctionImpl : BlobGetBase64StringFunction, IAsyncTexlFunction5
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();           

            BlobValue blobValue = args[0] as BlobValue;

            if (args[0] is BlankValue || (blobValue != null && string.IsNullOrEmpty(await blobValue.GetAsBase64Async(cancellationToken).ConfigureAwait(false))))
            {
                return FormulaValue.NewBlank(FormulaType.String);
            }

            if (blobValue == null)
            {
                return CommonErrors.RuntimeTypeMismatch(args[0].IRContext);
            }

            return FormulaValue.New(await blobValue.Content.GetAsBase64Async(cancellationToken).ConfigureAwait(false));
        }
    }
}
