// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Functions
{
    internal abstract class RemoveFunctionBase : BuiltinFunction
    {
        public override bool IsSelfContained => false;

        public override bool RequiresDataSourceScope => true;

        public override bool ArgMatchesDatasourceType(int argNum)
        {
            return argNum >= 1;
        }

        public RemoveFunctionBase(DPath theNamespace, string name, StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : base(theNamespace, name, /*localeSpecificName*/string.Empty, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public RemoveFunctionBase(string name, StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : this(DPath.Root, name, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public override bool CheckTypes(BindingConfig config, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(config);
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            var isValid = CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            return isValid;
        }

        protected static bool CheckArgs(FormulaValue[] args, out FormulaValue faultyArg)
        {
            // If any args are error, propagate up.
            foreach (var arg in args)
            {
                if (arg is ErrorValue)
                {
                    faultyArg = arg;

                    return false;
                }
            }

            faultyArg = null;

            return true;
        }
    }

    internal class RemoveFunction : RemoveFunctionBase, IAsyncTexlFunction
    {
        public override bool IsSelfContained => false;

        public RemoveFunction()
        : base("Remove", AboutRemove, FunctionCategories.Table | FunctionCategories.Behavior, DType.Boolean, 0, 2, int.MaxValue, DType.EmptyTable, DType.EmptyRecord)
        {
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { RemoveDataSourceArg, RemoveRecordsArg };
            yield return new[] { RemoveDataSourceArg, RemoveRecordsArg, RemoveRecordsArg };
        }

        public override IEnumerable<StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, RemoveDataSourceArg, RemoveRecordsArg, RemoveRecordsArg);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            //Contracts.Assert(returnType.IsTable);

            DType collectionType = argTypes[0];
            if (!collectionType.IsTable)
            {
                fValid = false;
                errors.EnsureError(args[0], ErrNeedTable_Func, Name);
            }

            fValid &= DropAttachmentsIfExists(ref collectionType, errors, args[0]);

            var argCount = argTypes.Length;

            for (var i = 1; i < argCount; i++)
            {
                DType argType = argTypes[i];

                // The subsequent args should all be records.
                if (!argType.IsRecord)
                {
                    // The last arg may be the optional "ALL" parameter.
                    if (argCount >= 3 && i == argCount - 1 && DType.String.Accepts(argType))
                    {
                        var strNode = (StrLitNode)args[i];

                        if (strNode.Value.ToUpperInvariant() != "ALL")
                        {
                            fValid = false;
                            errors.EnsureError(args[i], ErrRemoveAllArg, args[i]);
                        }

                        continue;
                    }

                    fValid = false;
                    errors.EnsureError(args[i], ErrNeedRecord, args[i]);
                    continue;
                }

                fValid &= DropAttachmentsIfExists(ref argType, errors, args[i]);

                var collectionAcceptsRecord = collectionType.Accepts(argType.ToTable());
                var recordAcceptsCollection = argType.ToTable().Accepts(collectionType);

                // The item schema should be compatible with the collection schema.
                if (!collectionAcceptsRecord && !recordAcceptsCollection)
                {
                    fValid = false;
                    if (!SetErrorForMismatchedColumns(collectionType, argType, args[i], errors))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], ErrTableDoesNotAcceptThisType);
                    }
                }

                // Only warn about no-op record inputs if there are no data sources that would use reference identity for comparison.
                else if (!collectionType.AssociatedDataSources.Any() && !recordAcceptsCollection)
                {
                    errors.EnsureError(DocumentErrorSeverity.Warning, args[i], ErrTableDoesNotAcceptThisType);
                }
            }

            // Remove returns the new collection, so the return schema is the same as the collection schema.
            returnType = collectionType;

            return fValid;
        }

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            var validArgs = CheckArgs(args, out FormulaValue faultyArg);

            if (!validArgs)
            {
                return faultyArg;
            }

            if (args[0] is BlankValue)
            {
                return args[0];
            }

            var argCount = args.Count();
            var lastArg = args.Last() as FormulaValue;
            var all = false;
            var toExclude = 1;

            if (argCount >= 3 && DType.String.Accepts(lastArg.Type._type))
            {
                var lastArgValue = (string)lastArg.ToObject();

                if (lastArgValue.ToUpperInvariant() == "ALL")
                {
                    all = true;
                    toExclude = 2;
                }
            }

            var datasource = (TableValue)args[0];
            var recordsToRemove = args.Skip(1).Take(args.Length - toExclude);

            cancellationToken.ThrowIfCancellationRequested();
            var ret = await datasource.RemoveAsync(recordsToRemove, all, cancellationToken);

            return ret.ToFormulaValue();
        }
    }
}
