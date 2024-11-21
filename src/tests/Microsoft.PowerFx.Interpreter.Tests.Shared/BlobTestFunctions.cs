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

    internal class BlobFunctionImpl : BlobFunction, IAsyncTexlFunction
    {
        public Task<FormulaValue> InvokeAsync(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            runner?.CheckCancel();                       

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

    internal class BlobGetStringFunctionImpl : BlobGetStringFunction, IAsyncTexlFunction
    {
        public async Task<FormulaValue> InvokeAsync(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            runner.CheckCancel();           

            BlobValue blob = args[0] as BlobValue;

            if (args[0] is BlankValue || (blob != null && string.IsNullOrEmpty(await blob.GetAsStringAsync(null, CancellationToken.None))))
            {
                return FormulaValue.NewBlank(FormulaType.String);
            }

            if (blob == null)
            {
                return CommonErrors.RuntimeTypeMismatch(args[0].IRContext);
            }

            return FormulaValue.New(await blob.GetAsStringAsync(null, CancellationToken.None));
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

    internal class BlobGetBase64StringFunctionImpl : BlobGetBase64StringFunction, IAsyncTexlFunction
    {
        public async Task<FormulaValue> InvokeAsync(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            runner.CheckCancel();           

            BlobValue blobValue = args[0] as BlobValue;

            if (args[0] is BlankValue || (blobValue != null && string.IsNullOrEmpty(await blobValue.GetAsBase64Async(runner.CancellationToken))))
            {
                return FormulaValue.NewBlank(FormulaType.String);
            }

            if (blobValue == null)
            {
                return CommonErrors.RuntimeTypeMismatch(args[0].IRContext);
            }

            return FormulaValue.New(await blobValue.Content.GetAsBase64Async(runner.CancellationToken));
        }
    }
}
