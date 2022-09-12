// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Interpreter
{
    // The CollectFunction class was copied from PowerApss.
    // Implementation of a Set function which just chains to 
    // RecalcEngine.UpdateVariable().
    // Set has no return value. 
    // Whereas PowerApps' Set() will implicitly define arg0,
    //  this Set() requires arg0 was already defined and has a type.
    //
    // Called as:
    //   Set(var,newValue)
    internal class CollectFunction : BuiltinFunction, IAsyncTexlFunction
    {
        public override bool ManipulatesCollections => true;

        public override bool ModifiesValues => true;

        public override bool IsSelfContained => false;

        public override bool RequiresDataSourceScope => true;

        protected virtual bool IsScalar => false;

        public override bool SupportsParamCoercion => false;

        public override bool ArgMatchesDatasourceType(int argNum)
        {
            return argNum >= 1;
        }

        public CollectFunction()
        : base(
              DPath.Root,
              "Collect",
              "Collect",
              TexlStrings.AboutSet,
              FunctionCategories.Behavior,
              DType.EmptyTable,
              0, // no lambdas
              2,
              2) // Not handling multiple arguments for now
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // PR REVIEWERS: These are wrong signature texts.
            yield return new[] { TexlStrings.WithArg1, TexlStrings.WithArg2 };
            yield return new[] { TexlStrings.WithArg1, TexlStrings.WithArg2, TexlStrings.WithArg2 };
            yield return new[] { TexlStrings.WithArg1, TexlStrings.WithArg2, TexlStrings.WithArg2, TexlStrings.WithArg2 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.WithArg1, TexlStrings.WithArg2);
            }

            return base.GetSignatures(arity);
        }

        public virtual DType GetCollectedType(DType argType)
        {
            Contracts.Assert(argType.IsValid);

            return argType;
        }

        // Attempt to get the unified schema of the items being collected by an invocation.
        public bool TryGetUnifiedCollectedType(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType collectedType)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = true;
            DType itemType = DType.Invalid;

            DType dataSourceType = argTypes[0];
            var tableType = (TableType)FormulaType.Build(dataSourceType);

            var argc = args.Length;

            for (var i = 1; i < argc; i++)
            {
                DType argType = GetCollectedType(argTypes[i]);

                // The subsequent args should all be aggregates.
                if (!argType.IsAggregate)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrBadType_Type, argType.GetKindString());
                    fValid = false;
                    continue;
                }

                foreach (var typedName in argType.GetNames(DPath.Root))
                {
                    if (!tableType.HasField(typedName.Name))
                    {
                        dataSourceType.ReportNonExistingName(FieldNameKind.Display, errors, typedName.Name, args[i]);
                        fValid = false;
                        continue;
                    }
                }

                // Promote the arg type to a table to facilitate unioning.
                if (!argType.IsTable)
                {
                    argType = argType.ToTable();
                }

                if (!itemType.IsValid)
                {
                    itemType = argType;
                }
                else
                {
                    var fUnionError = false;
                    itemType = DType.Union(ref fUnionError, itemType, argType, useLegacyDateTimeAccepts: true);
                    if (fUnionError)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrIncompatibleTypes);
                        fValid = false;
                    }
                }

                // We only support accessing entities in collections if the collection has only 1 argument that contributes to it's type
                if (argc != 2 && itemType.ContainsDataEntityType(DPath.Root))
                {
                    fValid &= DropAllOfKindNested(ref itemType, errors, args[i], DKind.DataEntity);
                }

                fValid &= DropAttachmentsIfExists(ref itemType, errors, args[i]);
            }

            Contracts.Assert(!itemType.IsValid || itemType.IsTable);
            collectedType = itemType.IsValid ? itemType : DType.EmptyTable;
            return fValid;
        }

        // Typecheck an invocation of Collect.
        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckInvocation(binding, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            DType dataSourceType = argTypes[0];

            // Need a collection for the 1st arg
            DType collectionType = argTypes[0];
            if (!collectionType.IsTable)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedValidVariableName_Arg, Name);
                fValid = false;
            }

            // Get the unified collected type on the RHS. This will generate appropriate
            // document errors for invalid arguments such as unsupported aggregate types.
            fValid &= TryGetUnifiedCollectedType(args, argTypes, errors, out DType collectedType);
            Contracts.Assert(collectedType.IsTable);

            // The item type must be compatible with the collection schema.
            var fError = false;
            returnType = DType.Union(ref fError, collectionType, collectedType, useLegacyDateTimeAccepts: true);
            if (fError)
            {
                fValid = false;
                if (!SetErrorForMismatchedColumns(collectionType, collectedType, args[1], errors))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedValidVariableName_Arg);
                }
            }

            return fValid;
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 0;
        }

        public override bool IsAsyncInvocation(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            return Arg0RequiresAsync(callNode, binding);
        }

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel)
        {
            var arg0 = (TableValue)args[0];
            var arg1 = args[1];

            if (arg1 is BlankValue)
            {
                return FormulaValue.NewBlank();
            }
            else if (arg1 is ErrorValue)
            {
                return arg1;
            }

            var record = (RecordValue)arg1;

            var result = await arg0.AppendAsync(record);

            return result.ToFormulaValue();
        }
    }
}
