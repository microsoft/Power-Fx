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
             : base("Blob", (loc) => "Converts a string to a Blob.", FunctionCategories.Text, DType.Blob, 0, 1, 3, DType.String, DType.Boolean, DType.String)
        {
        }

        public override bool IsSelfContained => true;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { (loc) => "string" };
            yield return new TexlStrings.StringGetter[] { (loc) => "string", (loc) => "isBase64String" };
            yield return new TexlStrings.StringGetter[] { (loc) => "string", (loc) => "isBase64String", (loc) => "blobType" };
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
                return Task.FromResult<FormulaValue>(BlobValue.NewBlob(resourceManager, resourceManager.GetElementFromString(null)));
            }            

            if (args[0] is FileValue)
            {
                return Task.FromResult(args[0]);
            }

            if (args[0] is not StringValue sv)
            {
                return Task.FromResult<FormulaValue>(CommonErrors.RuntimeTypeMismatch(args[0].IRContext));
            }

            bool isBase64String = args.Length >= 2 && args[1] is BooleanValue bv && bv.Value;
            FileType fileType = args.Length == 3 && args[2] is StringValue fts && Enum.TryParse(fts.Value, true, out FileType ft) ? ft : FileType.Any;

            if (isBase64String && fileType == FileType.Uri)
            {
                return Task.FromResult<FormulaValue>(CommonErrors.GenericInvalidArgument(args[1].IRContext));
            }

            IResourceElement element = !isBase64String ? resourceManager.GetElementFromString(sv.Value, fileType) : resourceManager.GetElementFromBase64String(sv.Value, fileType);            

            return fileType switch
            {
                FileType.Image => Task.FromResult<FormulaValue>(BlobValue.NewImage(resourceManager, element)),
                FileType.Audio => Task.FromResult<FormulaValue>(BlobValue.NewAudio(resourceManager, element)),
                FileType.Video => Task.FromResult<FormulaValue>(BlobValue.NewVideo(resourceManager, element)),
                FileType.PDF => Task.FromResult<FormulaValue>(BlobValue.NewPDF(resourceManager, element)),

                // includes Uri
                _ => Task.FromResult<FormulaValue>(BlobValue.NewBlob(resourceManager, element))
            };
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
        public Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            IResourceManager resourceManager = runtimeServiceProvider.GetService<IResourceManager>();

            if (resourceManager == null)
            {
                return Task.FromResult<FormulaValue>(CommonErrors.CustomError(args[0].IRContext, "Missing ResourceManager in runtime service provider"));
            }

            FileValue fv = args[0] as FileValue;

            if (args[0] is BlankValue || (fv != null && string.IsNullOrEmpty(fv.ResourceElement.String)))
            {
                return Task.FromResult<FormulaValue>(FormulaValue.NewBlank(FormulaType.String));
            }            

            if (fv == null)
            {
                return Task.FromResult<FormulaValue>(CommonErrors.RuntimeTypeMismatch(args[0].IRContext));
            }

            return Task.FromResult<FormulaValue>(FormulaValue.New(fv.ResourceElement.String));
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
        public Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            IResourceManager resourceManager = runtimeServiceProvider.GetService<IResourceManager>();

            if (resourceManager == null)
            {
                return Task.FromResult(CommonErrors.CustomError(args[0].IRContext, "Missing ResourceManager in runtime service provider"));
            }

            FileValue fv = args[0] as FileValue;

            if (args[0] is BlankValue || (fv != null && string.IsNullOrEmpty(fv.ResourceElement.Base64String)))
            {
                return Task.FromResult<FormulaValue>(FormulaValue.NewBlank(FormulaType.String));
            }

            if (fv == null)
            {
                return Task.FromResult<FormulaValue>(CommonErrors.RuntimeTypeMismatch(args[0].IRContext));
            }

            return Task.FromResult<FormulaValue>(FormulaValue.New(fv.ResourceElement.Base64String));
        }
    }
}
