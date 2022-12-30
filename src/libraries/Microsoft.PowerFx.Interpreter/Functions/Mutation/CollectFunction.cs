// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectFunction"/> class.
        /// To be consumed by ClearCollect function.
        /// </summary>
        protected CollectFunction(string name, TexlStrings.StringGetter description)
            : base(name, description, FunctionCategories.Behavior, DType.EmptyRecord, 0, 2, 2, DType.EmptyTable, DType.EmptyRecord)
        {
        }

        public CollectFunction()
        : base(
              "Collect",
              TexlStrings.AboutCollect,
              FunctionCategories.Behavior,
              DType.EmptyRecord,
              0,
              2,
              2, // Not handling multiple arguments for now
              DType.EmptyTable,
              DType.EmptyRecord) 
        {
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.CollectDataSourceArg, TexlStrings.CollectRecordArg };
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

                // Promote the arg type to a table to facilitate unioning.
                if (!argType.IsRecord)
                {
                    argType = argType.ToRecord();
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
            }

            Contracts.Assert(!itemType.IsValid || itemType.IsRecord);
            collectedType = itemType.IsValid ? itemType : DType.EmptyRecord;
            return fValid;
        }

        // Typecheck an invocation of Collect.
        public override bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            // Need a collection for the 1st arg
            DType collectionType = argTypes[0];
            if (!collectionType.IsTable)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrInvalidArgs_Func, Name);
                fValid = false;
            }

            // Get the unified collected type on the RHS. This will generate appropriate
            // document errors for invalid arguments such as unsupported aggregate types.
            fValid &= TryGetUnifiedCollectedType(args, argTypes, errors, out DType collectedType);
            Contracts.Assert(collectedType.IsRecord);

            if (fValid)
            {
                // The item type must be compatible with the collection schema.
                var fError = false;
                returnType = DType.Union(ref fError, collectionType.ToRecord(), collectedType, useLegacyDateTimeAccepts: true);
                if (fError)
                {
                    fValid = false;
                    if (!SetErrorForMismatchedColumns(collectionType, collectedType, args[1], errors))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedValidVariableName_Arg, Name);
                    }
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

        public virtual async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            var arg0 = args[0];
            var arg1 = args[1];

            // PA returns arg0.
            // PFx returns arg1 for now except when arg0 is anything but TableValue, return arg0 or RuntimeTypeMismatch error.
            if (arg0 is BlankValue)
            {
                return arg0;
            }
            else if (arg0 is ErrorValue)
            {
                return arg0;
            }

            if (arg0 is not TableValue)
            {
                return CommonErrors.RuntimeTypeMismatch(IRContext.NotInSource(arg0.Type));
            }

            // If arg0 is valid, then return arg1.
            if (arg1 is BlankValue)
            {
                return arg1;
            }
            else if (arg1 is ErrorValue)
            {
                return arg1;
            }

            if (arg1 is not RecordValue)
            {
                return CommonErrors.RuntimeTypeMismatch(IRContext.NotInSource(arg1.Type));
            }

            var tableValue = arg0 as TableValue;
            var recordValue = arg1 as RecordValue;

            cancellationToken.ThrowIfCancellationRequested();
            var result = await tableValue.AppendAsync(recordValue, cancellationToken);

            return result.ToFormulaValue();
        }
    }
}
