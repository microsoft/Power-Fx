// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Error([arg1: any])
    internal sealed class ErrorFunction : BuiltinFunction
    {
        public override bool HasPreciseErrors => true;
        public override bool RequiresErrorContext => true;
        public override bool CanSuggestInputColumns => true;
        public override bool IsSelfContained => true;
        public override bool SupportsParamCoercion => true;

        public ErrorFunction()
            : base("Error", TexlStrings.AboutError, FunctionCategories.Logical, DType.ObjNull, 0, 1, 1)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ErrorArg1 };
        }

        public override bool CheckInvocation(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length == 1);
            Contracts.AssertValue(errors);

            var reifiedError = ErrorType.ReifiedError();
            var acceptedFields = reifiedError.GetNames(DPath.Root);
            var requiredKindField = acceptedFields.Where(tn => tn.Name == "Kind").First();
            Contracts.Assert(requiredKindField.Type.IsEnum || requiredKindField.Type.Kind == DKind.Number);
            var optionalFields = acceptedFields.Where(tn => tn.Name != "Kind");

            returnType = DType.ObjNull;
            nodeToCoercedTypeMap = null;

            var argument = args[0];
            var argumentType = argTypes[0];

            if (argumentType.Kind != DKind.Record && argumentType.Kind != DKind.Table)
            {
                errors.EnsureError(argument, TexlStrings.ErrBadType);
                return false;
            }

            // We cache the whole name list regardless of path.
            var names = argumentType.GetNames(DPath.Root).ToArray();

            // First handle required fields (currently only 'Kind')
            if (!names.Any(field => field.Name == requiredKindField.Name))
            {
                // Kind is required, point it out to the maker, and specify the enumeration type.
                errors.EnsureError(
                    argument,
                    TexlStrings.ErrBadSchema_ExpectedType,
                    reifiedError.GetKindString());
                errors.Error(
                    argument,
                    TexlStrings.ErrColumnMissing_ColName_ExpectedType,
                    requiredKindField.Name.Value,
                    "ErrorKind");
                return false;
            }

            var argumentKindType = names.First(tn => tn.Name == requiredKindField.Name).Type;
            if (!argumentKindType.CoercesTo(requiredKindField.Type))
            {
                errors.EnsureError(
                    argument,
                    TexlStrings.ErrBadSchema_ExpectedType,
                    reifiedError.GetKindString());
                errors.Error(
                    argument,
                    TexlStrings.ErrBadRecordFieldType_FieldName_ExpectedType,
                    requiredKindField.Name.Value,
                    "ErrorKind");
                return false;
            }

            bool valid = true;

            var record = argument.AsRecord();
            foreach (var name in names)
            {
                if (!acceptedFields.Any(field => field.Name == name.Name))
                {
                    // If they have a record literal, we can position the errors for rejected fields.
                    if (record != null)
                        errors.EnsureError(record.Children.Where((_, i) => record.Ids[i].Name == name.Name).FirstOrDefault() ?? record, TexlStrings.ErrErrorIrrelevantField);
                    else
                        errors.EnsureError(argument, TexlStrings.ErrErrorIrrelevantField);
                    valid = false;
                }
            }

            bool matchedWithCoercion;
            bool typeValid;
            if (argumentType.Kind == DKind.Record)
            {
                // A record with the proper types for the fields that are specified.
                var expectedOptionalFieldsRecord = DType.CreateRecord(
                    acceptedFields.Where(field =>
                        // Kind has already been handled before
                        field.Name != "Kind" && names.Any(name => name.Name == field.Name)));

                typeValid = CheckType(argument, argumentType, expectedOptionalFieldsRecord, errors, true, out matchedWithCoercion);
            }
            else
            {
                // A table with the proper types for the fields that are specified.
                var expectedOptionalFieldsTable = DType.CreateTable(
                    acceptedFields.Where(field =>
                        // Kind has already been handled before
                        field.Name != "Kind" && names.Any(name => name.Name == field.Name)));
                typeValid = CheckType(argument, argumentType, expectedOptionalFieldsTable, errors, true, out matchedWithCoercion);
            }

            if (!typeValid)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, argument, TexlStrings.ErrTypeError);
                valid = false;
            }
            else if (matchedWithCoercion && valid)
            {
                var recordOrTableSchema = acceptedFields.Where(field => names.Any(name => name.Name == field.Name));
                var expectedRecordOrTable = argumentType.Kind == DKind.Record ?
                    DType.CreateRecord(recordOrTableSchema) :
                    DType.CreateTable(recordOrTableSchema);
                CollectionUtils.Add(ref nodeToCoercedTypeMap, argument, expectedRecordOrTable);
            }

            return valid;
        }
    }
}
