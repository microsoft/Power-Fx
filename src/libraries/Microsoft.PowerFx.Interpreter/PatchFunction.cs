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

namespace Microsoft.PowerFx.Interpreter
{
    /*
         * @@@JYL@@@
         * Verify before pushing it.
         * => verify that Patch() fails in a non-behavior context.
         * => Type checking - what if we pass the wrong type to patch (too few fields, too many fields, not a record, etc).
         * => PowerApps lets pass multiple records - this is determined by CheckInvocation, which we copied from PA.
         * => Will need to confirm corner case behavior, ie, what if we pass errors, etc. (We can probably do that in a followup PR, just to unblock usage now)
         * => Trying to patch on an Immutable object. We should get an error.
         */

    internal abstract class PatchAndValidateRecordFunctionBase : BuiltinFunction
    {
        public PatchAndValidateRecordFunctionBase(DPath theNamespace, string name, TexlStrings.StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
           : base(theNamespace, name, /*localeSpecificName*/string.Empty, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public PatchAndValidateRecordFunctionBase(string name, TexlStrings.StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
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
    }

    internal class PatchRecordFunction : PatchAndValidateRecordFunctionBase, IAsyncTexlFunction
    {
        public override bool IsSelfContained => false;

        public PatchRecordFunction()
            : base("Patch", TexlStrings.AboutPatch, FunctionCategories.Table | FunctionCategories.Behavior, DType.EmptyRecord, 0, 2, int.MaxValue, DType.EmptyRecord, DType.EmptyRecord)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.AboutPatch, TexlStrings.AboutPatch, TexlStrings.AboutPatch };
            yield return new[] { TexlStrings.AboutPatch, TexlStrings.AboutPatch, TexlStrings.AboutPatch, TexlStrings.AboutPatch };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 3)
            {
                return GetGenericSignatures(arity, TexlStrings.AboutPatch, TexlStrings.AboutPatch, TexlStrings.AboutPatch);
            }

            return base.GetSignatures(arity);
        }

        public Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel)
        {
            var fieldsDict = new Dictionary<string, FormulaValue>();

            var arg0 = args[0] as RecordValue;

            for (var i = 0; i < args.Length; i++)
            {
                foreach (var field in ((RecordValue)args[i]).Fields)
                {
                    fieldsDict[field.Name] = field.Value;
                }
            }

            var fieldList = new List<NamedValue>();

            foreach (var field in fieldsDict)
            {
                fieldList.Add(new NamedValue(field.Key, field.Value));
            }

            var record = RecordValue.NewRecordFromFields(fieldList);

            return Task.FromResult<FormulaValue>(record);
        }

        public override RequiredDataSourcePermissions FunctionPermission => RequiredDataSourcePermissions.Create | RequiredDataSourcePermissions.Update;
    }

    internal class PatchFunction : PatchAndValidateRecordFunctionBase, IAsyncTexlFunction
    {
        public override bool IsSelfContained => false;

        public PatchFunction()
            : base("Patch", TexlStrings.AboutPatch, FunctionCategories.Table | FunctionCategories.Behavior, DType.EmptyRecord, 0, 3, int.MaxValue, DType.EmptyTable, DType.EmptyRecord, DType.EmptyRecord)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.AboutPatch, TexlStrings.AboutPatch, TexlStrings.AboutPatch };
            yield return new[] { TexlStrings.AboutPatch, TexlStrings.AboutPatch, TexlStrings.AboutPatch, TexlStrings.AboutPatch };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 3)
            {
                return GetGenericSignatures(arity, TexlStrings.AboutPatch, TexlStrings.AboutPatch, TexlStrings.AboutPatch);
            }

            return base.GetSignatures(arity);
        }

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel)
        {
            var argFields = new Dictionary<string, FormulaValue>();
            var fieldsDict = new Dictionary<string, FormulaValue>();

            var arg0 = args[0] as TableValue;
            var arg1 = args[1] as RecordValue;

            // Starts from the 3rd argument (new values)
            for (var i = 2; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg is BlankValue)
                {
                    continue;
                }
                else if (arg is ErrorValue)
                {
                    // @@@JYL How to handle error value arguments?
                    return arg;
                }
                else if (arg is RecordValue record)
                {
                    foreach (var field in record.Fields)
                    {
                        argFields[field.Name] = field.Value;
                    }
                }
            }

            var fieldList = new List<NamedValue>();

            // Creates final record 
            foreach (var field in arg1.Fields)
            {
                var namedValue = argFields.ContainsKey(field.Name) ? argFields[field.Name] : field.Value;

                fieldList.Add(new NamedValue(field.Name, namedValue));
            }

            var newRecord = FormulaValue.NewRecordFromFields(fieldList);

            // @@@JYL Whats a good way to replace the old record with the new one and "update" the TableValue object?

            var deleted = await arg0.RemoveAsync(arg1);

            if (deleted.Value.Value)
            {
                await arg0.AppendAsync(newRecord);
            }
            else
            {
                // @@@JYL Return ErrorValue or throw exception?
                throw new Exception("The old record could not be deleted.");
            }

            return await Task.FromResult<FormulaValue>(newRecord);
        }
    }
}
