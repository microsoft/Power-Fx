// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IfError(arg1: any, [arg2: any, ...])
    internal sealed class IfErrorFunction : BuiltinFunction
    {
        public override bool IsStrict => false;

        public override bool IsSelfContained => true;

        public override bool HasLambdas => true;

        public override bool IsAsync => true;

        public IfErrorFunction()
            : base("IfError", TexlStrings.AboutIfError, FunctionCategories.Logical, DType.Unknown, 0, 2, int.MaxValue)
        {
            ScopeInfo = new FunctionScopeInfo(
                this,
                iteratesOverScope: false,
                scopeType: DType.CreateRecord(
                    new TypedName(ErrorType.ReifiedError(), new DName("FirstError")),
                    new TypedName(ErrorType.ReifiedErrorTable(), new DName("AllErrors")),
                    new TypedName(DType.ObjNull, new DName("ErrorResult"))),
                appliesToArgument: i => i > 0 && (i % 2 == 1));

            // IfError(value1, fallback1, value2, fallback2, ..., valueN, [fallbackN], ...)
            SignatureConstraint = new SignatureConstraint(omitStartIndex: 4, repeatSpan: 2, endNonRepeatCount: 0, repeatTopLength: 8);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IfErrorArg1, TexlStrings.IfErrorArg2 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetOverloadsIfError(arity);
            }

            return base.GetSignatures(arity);
        }

        // Gets the overloads for the IfError function for the specified arity.
        // IfError is special because it doesn't have a small number of overloads
        // since its max arity is int.MaxSize.
        private IEnumerable<TexlStrings.StringGetter[]> GetOverloadsIfError(int arity)
        {
            Contracts.Assert(arity >= 3);

            // Limit the argCount avoiding potential OOM
            var argCount = arity > SignatureConstraint.RepeatTopLength ? SignatureConstraint.RepeatTopLength + (arity & 1) : arity;
            var signature = new TexlStrings.StringGetter[argCount];
            var fOdd = (argCount & 1) != 0;
            var cargCur = fOdd ? argCount - 1 : argCount;

            for (var iarg = 0; iarg < cargCur; iarg += 2)
            {
                signature[iarg] = TexlStrings.IfErrorArg1;
                signature[iarg + 1] = TexlStrings.IfErrorArg2;
            }

            if (fOdd)
            {
                signature[cargCur] = TexlStrings.IfErrorArg1;
            }

            argCount++;

            return new[] { signature };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var count = args.Length;
            nodeToCoercedTypeMap = null;

            var fArgsValid = true;

            /*
            If we were to write IfError(v1, f1, v2, f2, ..., vn, fn) in a try...catch way, it would look something like:
                
            var result
            try {
                result = v1();
            } catch {
                return f1();
            }
            try {
                result = v2();
            } catch {
                return f2();
            }
            ...
            try {
                result = vn();
            } catch {
                return fn();  // omit this last catch if there is an odd number of arguments
            }
            return result;

            In this case, we prefer the type of the last v_i as the return type.
            Possible return values are:
            Even case: f1, f2, ..., vn, fn
            Odd case: f1, f2, ..., vn
            */

            var preferredTypeIndex = (count % 2) == 0 ? count - 2 : count - 1;
            var type = argTypes[preferredTypeIndex];
            if (type.IsError)
            {
                errors.EnsureError(args[preferredTypeIndex], TexlStrings.ErrTypeError);
                fArgsValid = false;
            }

            for (var i = count - 1; i >= 0; i -= 2)
            {
                var nodeArg = args[i];
                var typeArg = argTypes[i];

                var typeSuper = type.IsError ? typeArg : DType.Supertype(type, typeArg);

                if (!typeSuper.IsError)
                {
                    // If preferred type was error or null (due to Blank or Error) assign new type in hierarchy
                    if (type.IsError || type == DType.ObjNull)
                    {
                        type = typeSuper;
                    }
                }
                else
                {
                    if (typeArg.IsVoid)
                    {
                        type = DType.Void;
                    }
                    else if (typeArg.IsError)
                    {
                        errors.EnsureError(args[i], TexlStrings.ErrTypeError);
                        fArgsValid = false;
                    }
                    else if (!type.IsError && typeArg.CoercesTo(type))
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, nodeArg, type);
                    }
                    else
                    {
                        type = DType.Void;
                    }
                }

                if ((count % 2) != 0 && i == count - 1)
                {
                    i++;
                }
            }

            returnType = type;
            return fArgsValid;
        }

        public override bool IsLambdaParam(int index)
        {
            return index > 0;
        }
    }
}
