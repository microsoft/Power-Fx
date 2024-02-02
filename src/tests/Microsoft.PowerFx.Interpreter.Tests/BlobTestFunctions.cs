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
            config.AddFunction(new BlobUriFunctionImpl());
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
            IResourceManager resourceManager = runtimeServiceProvider.GetService<IResourceManager>();

            if (resourceManager == null)
            {
                return Task.FromResult<FormulaValue>(CommonErrors.CustomError(args[0].IRContext, "Missing ResourceManager in runtime service provider"));
            }

            if (args[0] is BlankValue)
            {
                return Task.FromResult<FormulaValue>(BlobValue.NewBlob(resourceManager, new StringResourceElement(resourceManager, null).Handle));
            }

            if (args[0] is FileValue)
            {
                return Task.FromResult(args[0]);
            }

            if (args[0] is not StringValue sv)
            {
                return Task.FromResult<FormulaValue>(CommonErrors.RuntimeTypeMismatch(args[0].IRContext));
            }

            //bool isUri = args.Length >= 2 && args[1] is StringValue str && str.Value.Equals("uri", StringComparison.OrdinalIgnoreCase);
            bool isBase64String = args.Length >= 2 && args[1] is BooleanValue bv && bv.Value;                        

            ResourceHandle handle = isBase64String ? new Base64StringResourceElement(resourceManager, sv.Value).Handle : new StringResourceElement(resourceManager, sv.Value).Handle;
            return Task.FromResult<FormulaValue>(BlobValue.NewBlob(resourceManager, handle));
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

    internal class BlobUriFunctionImpl : BlobUriFunction, IAsyncTexlFunction5
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            IResourceManager resourceManager = runtimeServiceProvider.GetService<IResourceManager>();

            if (resourceManager == null)
            {
                return CommonErrors.CustomError(args[0].IRContext, "Missing ResourceManager in runtime service provider");
            }

            if (args[0] is BlankValue)
            {
                return BlobValue.NewBlob(resourceManager, new UriResourceElement(resourceManager, null).Handle);
            }

            if (args[0] is FileValue)
            {
                return args[0];
            }

            if (args[0] is not StringValue sv)
            {
                return CommonErrors.RuntimeTypeMismatch(args[0].IRContext);
            }

            ResourceHandle handle = new UriResourceElement(resourceManager, new Uri(sv.Value)).Handle;
            return BlobValue.NewBlob(resourceManager, handle);
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
            IResourceManager resourceManager = runtimeServiceProvider.GetService<IResourceManager>();

            if (resourceManager == null)
            {
                return CommonErrors.CustomError(args[0].IRContext, "Missing ResourceManager in runtime service provider");
            }

            FileValue fv = args[0] as FileValue;

            if (args[0] is BlankValue || (fv != null && string.IsNullOrEmpty(await fv.ResourceElement.GetAsStringAsync().ConfigureAwait(false))))
            {
                return FormulaValue.NewBlank(FormulaType.String);
            }

            if (fv == null)
            {
                return CommonErrors.RuntimeTypeMismatch(args[0].IRContext);
            }

            return FormulaValue.New(await fv.ResourceElement.GetAsStringAsync().ConfigureAwait(false));
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
            IResourceManager resourceManager = runtimeServiceProvider.GetService<IResourceManager>();

            if (resourceManager == null)
            {
                return CommonErrors.CustomError(args[0].IRContext, "Missing ResourceManager in runtime service provider");
            }

            FileValue fv = args[0] as FileValue;

            if (args[0] is BlankValue || (fv != null && string.IsNullOrEmpty(await fv.ResourceElement.GetAsBase64StringAsync().ConfigureAwait(false))))
            {
                return FormulaValue.NewBlank(FormulaType.String);
            }

            if (fv == null)
            {
                return CommonErrors.RuntimeTypeMismatch(args[0].IRContext);
            }

            return FormulaValue.New(await fv.ResourceElement.GetAsBase64StringAsync().ConfigureAwait(false));
        }
    }
}
