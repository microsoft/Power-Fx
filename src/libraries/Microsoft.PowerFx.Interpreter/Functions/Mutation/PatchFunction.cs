// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.DLP;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Functions
{
    internal abstract class PatchAndValidateRecordFunctionBase : BuiltinFunction
    {
        public override bool RequiresDataSourceScope => true;
        
        public override bool ArgMatchesDatasourceType(int argNum)
        {
            return argNum >= 1;
        }

        public PatchAndValidateRecordFunctionBase(DPath theNamespace, string name, StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
           : base(theNamespace, name, /*localeSpecificName*/string.Empty, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public PatchAndValidateRecordFunctionBase(string name, StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : this(DPath.Root, name, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            var isValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

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

        // Change records are processed in order from the beginning of the argument list to the end,
        // with later property values overriding earlier ones.
        protected static Dictionary<string, FormulaValue> CreateRecordFromArgsDict(FormulaValue[] args, int startFrom)
        {
            var retFields = new Dictionary<string, FormulaValue>(StringComparer.Ordinal);

            for (var i = startFrom; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg is BlankValue)
                {
                    continue;
                }
                else if (arg is RecordValue record)
                {
                    foreach (var field in record.Fields)
                    {
                        retFields[field.Name] = field.Value;
                    }
                }
                else
                {
                    throw new ArgumentException($"Can't handle {arg.Type} argument type.");
                }
            }

            return retFields;
        }

        protected static RecordValue FieldDictToRecordValue(IReadOnlyDictionary<string, FormulaValue> fieldsDict)
        {
            var list = new List<NamedValue>();

            foreach (var field in fieldsDict)
            {
                list.Add(new NamedValue(field.Key, field.Value));
            }

            return FormulaValue.NewRecordFromFields(list);
        }
    }

    // Patch( Record1, Record2 [, …] )
    internal class PatchRecordFunction : PatchAndValidateRecordFunctionBase, IAsyncTexlFunction
    {
        public override bool IsSelfContained => false;

        public PatchRecordFunction()
            : base("Patch", AboutPatch, FunctionCategories.Table | FunctionCategories.Behavior, DType.EmptyRecord, 0, 2, int.MaxValue, DType.EmptyRecord, DType.EmptyRecord)
        {
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { PatchBaseRecordArg, PatchChangeRecordsArg };
            yield return new[] { PatchBaseRecordArg, PatchChangeRecordsArg, PatchChangeRecordsArg };
        }

        public override IEnumerable<StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, PatchBaseRecordArg, PatchChangeRecordsArg);
            }

            return base.GetSignatures(arity);
        }

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel)
        {
            var validArgs = CheckArgs(args, out FormulaValue faultyArg);

            if (!validArgs)
            {
                return faultyArg;
            }

            return FieldDictToRecordValue(CreateRecordFromArgsDict(args, 0));
        }

        public override RequiredDataSourcePermissions FunctionPermission => RequiredDataSourcePermissions.Create | RequiredDataSourcePermissions.Update;
    }

    // Patch( DataSource, BaseRecord, ChangeRecord1 [, ChangeRecord2, … ])
    internal class PatchFunction : PatchAndValidateRecordFunctionBase, IAsyncTexlFunction
    {
        public override bool IsSelfContained => false;

        public PatchFunction()
            : base("Patch", AboutPatch, FunctionCategories.Table | FunctionCategories.Behavior, DType.EmptyRecord, 0, 3, int.MaxValue, DType.EmptyTable, DType.EmptyRecord, DType.EmptyRecord)
        {
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { PatchDataSourceArg, PatchBaseRecordArg };
            yield return new[] { PatchDataSourceArg, PatchBaseRecordArg, PatchChangeRecordsArg };
            yield return new[] { PatchDataSourceArg, PatchBaseRecordArg, PatchChangeRecordsArg, PatchChangeRecordsArg };
        }

        public override IEnumerable<StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 3)
            {
                return GetGenericSignatures(arity, PatchDataSourceArg, PatchBaseRecordArg, PatchChangeRecordsArg);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            var isValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            DType dataSourceType = argTypes[0];
            DType retType = DType.EmptyRecord;

            for (var i = 1; i < args.Length; i++)
            {
                DType curType = argTypes[i];

                var tableType = (TableType)FormulaType.Build(dataSourceType);

                if (!curType.IsRecord)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrNeedRecord);
                    isValid = false;
                    continue;
                }

                var isSafeToUnion = true;

                foreach (var typedName in curType.GetNames(DPath.Root))
                {
                    if (!tableType.TryGetFieldType(typedName.Name, out var displayName, out _))
                    {
                        dataSourceType.ReportNonExistingName(FieldNameKind.Display, errors, typedName.Name, args[i]);
                        isValid = isSafeToUnion = false;
                        continue;
                    }
                }

                if (isValid && SupportsParamCoercion && !dataSourceType.Accepts(curType))
                {
                    if (!curType.TryGetCoercionSubType(dataSourceType, out DType coercionType, out var coercionNeeded))
                    {
                        isValid = false;
                    }
                    else
                    {
                        if (coercionNeeded)
                        {
                            CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], coercionType);
                        }

                        retType = DType.Union(retType, coercionType);
                    }
                }
                else if (isSafeToUnion)
                {
                    retType = DType.Union(retType, curType);
                }
            }

            return isValid;
        }

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel)
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

            if (args[1] is BlankValue)
            {
                return args[1];
            }

            var changeRecord = FieldDictToRecordValue(CreateRecordFromArgsDict(args, 2));

            var datasource = (TableValue)args[0];
            var baseRecord = (RecordValue)args[1];

            var ret = await datasource.PatchAsync(baseRecord, changeRecord);

            return ret.ToFormulaValue();
        }
    }
}
