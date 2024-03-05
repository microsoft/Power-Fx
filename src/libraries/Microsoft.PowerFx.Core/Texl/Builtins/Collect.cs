// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.DLP;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using RecordNode = Microsoft.PowerFx.Core.IR.Nodes.RecordNode;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Collect(collection:*[...], item1:![...]|*[...], ...)
    internal class CollectFunction : BuiltinFunction
    {
        public override bool AffectsCollectionSchemas => true;

        public bool CanSuggestThisItem => true;

        public override bool SupportsParamCoercion => false;

        public override bool ManipulatesCollections => true;

        public override bool ModifiesValues => true;

        public override bool IsSelfContained => false;

        public override bool RequiresDataSourceScope => true;

        public virtual bool IsScalar => false;

        public override bool CanSuggestInputColumns => true;

        public override bool MutatesArg0 => true;

        public override RequiredDataSourcePermissions FunctionPermission => RequiredDataSourcePermissions.Create;

        /// <summary>
        /// Since Arg1 and Arg2 depends on type of Arg1 return false for them.
        /// </summary>
        public override bool TryGetTypeForArgSuggestionAt(int argIndex, out DType type)
        {
            if (argIndex == 1 || argIndex == 2)
            {
                type = default;
                return false;
            }

            return base.TryGetTypeForArgSuggestionAt(argIndex, out type);
        }

        public override bool ArgMatchesDatasourceType(int argNum)
        {
            return argNum >= 1;
        }

        public override bool IsLazyEvalParam(int index, Features features)
        {
            // First argument to mutation functions is Lazy for datasources that are copy-on-write.
            // If there are any side effects in the arguments, we want those to have taken place before we make the copy.
            return index == 0;
        }

        public CollectFunction()
            : this("Collect", TexlStrings.AboutCollect)
        {
        }

        protected CollectFunction(string name, TexlStrings.StringGetter description)
            : base(name, description, FunctionCategories.Behavior, DType.EmptyRecord, 0, 2, int.MaxValue, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // !!! TODO
            //yield return new[] { CanvasStringResources.CollectArg1, CanvasStringResources.CollectArg2 };
            //yield return new[] { CanvasStringResources.CollectArg1, CanvasStringResources.CollectArg2, CanvasStringResources.CollectArg2 };
            //yield return new[] { CanvasStringResources.CollectArg1, CanvasStringResources.CollectArg2, CanvasStringResources.CollectArg2, CanvasStringResources.CollectArg2 };

            yield return new[] { TexlStrings.CollectDataSourceArg, TexlStrings.CollectItemArg };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.CollectDataSourceArg, TexlStrings.CollectItemArg);
            }

            return base.GetSignatures(arity);
        }

        public virtual DType GetCollectedType(Features features, DType argType)
        {
            Contracts.Assert(argType.IsValid);

            return argType;
        }

        public bool TryGetUnifiedCollectedTypeCanvas(TexlNode[] args, DType[] argTypes, IErrorContainer errors, Features features, out DType collectedType)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            bool fValid = true;
            DType itemType = DType.Invalid;

            var argc = args.Length;

            for (int i = 1; i < argc; i++)
            {
                DType argType = GetCollectedType(features, argTypes[i]);

                // The subsequent args should all be aggregates.
                if (!argType.IsAggregate)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrBadType_Type, argType.GetKindString());
                    fValid = false;
                    continue;
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
                    bool fUnionError = false;
                    itemType = DType.Union(ref fUnionError, itemType, argType, useLegacyDateTimeAccepts: true, features);
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

            Contracts.Assert(!itemType.IsValid || itemType.IsTable);
            collectedType = itemType.IsValid ? itemType : DType.EmptyTable;
            return fValid;
        }

        // Attempt to get the unified schema of the items being collected by an invocation.
        private bool TryGetUnifiedCollectedTypeV1(TexlNode[] args, DType[] argTypes, IErrorContainer errors, Features features, out DType collectedType)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = true;

            DType itemType = DType.Invalid;
            DType datasourceType = argTypes[0];

            var argc = args.Length;

            for (var i = 1; i < argc; i++)
            {
                DType argType = GetCollectedType(features, argTypes[i]);

                // !!! How is it possible for an argtype to be a primitive and an aggregate at the same time?
                //if (argType.DisplayNameProvider == null && argType.Kind == DKind.ObjNull)
                //{
                //    argType.DisplayNameProvider = datasourceType.DisplayNameProvider;
                //}

                // The subsequent args should all be aggregates.
                if (!argType.IsAggregate)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrBadType_Type, argType.GetKindString());
                    fValid = false;
                    continue;
                }

                // Promote the arg type to a table to facilitate unioning.
                if (!argType.IsTable)
                {
                    argType = argType.ToTable();
                }

                // Checks if all record names exist against table type and if its possible to coerce.
                bool checkAggregateNames = argType.CheckAggregateNames(datasourceType, args[i], errors, features, SupportsParamCoercion);
                fValid = fValid && checkAggregateNames;

                if (!itemType.IsValid)
                {
                    itemType = argType;
                }
                else
                {
                    var fUnionError = false;
                    itemType = DType.Union(ref fUnionError, itemType, argType, useLegacyDateTimeAccepts: true, features);
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

            Contracts.Assert(!itemType.IsValid || itemType.IsTable);
            collectedType = itemType.IsValid ? itemType : DType.EmptyTable;
            return fValid;
        }

        // Typecheck an invocation of Collect.
        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            // Need a collection for the 1st arg
            DType collectionType = argTypes[0];
            if (!collectionType.IsTable)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrInvalidArgs_Func, Name);
                fValid = false;
            }

            DType collectedType = null;

            // Get the unified collected type on the RHS. This will generate appropriate
            // document errors for invalid arguments such as unsupported aggregate types.
            if (context.Features.PowerFxV1CompatibilityRules)
            {
                fValid &= TryGetUnifiedCollectedTypeV1(args, argTypes, errors, context.Features, out collectedType);
            }
            else
            {
                fValid &= TryGetUnifiedCollectedTypeCanvas(args, argTypes, errors, context.Features, out collectedType);
            }

            Contracts.Assert(collectedType.IsTable);

            bool fError = false;
            returnType = DType.Union(ref fError, collectionType, collectedType, useLegacyDateTimeAccepts: true, context.Features);
            if (fError)
            {
                fValid = false;
                if (!SetErrorForMismatchedColumns(collectionType, collectedType, args[1], errors, context.Features))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTableDoesNotAcceptThisType);
                }
            }
            
            if (fValid)
            {
                if (context.Features.PowerFxV1CompatibilityRules && argTypes.Length == 2 && (argTypes[1].IsRecord || argTypes[1].IsPrimitive))
                {
                    returnType = returnType.ToRecord();
                }
                else
                {
                    returnType = returnType.ToTable();
                }
            }

            return fValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            // !!! TODO
            DType dataSourceType = argTypes[0];
            bool isConnected = binding.EntityScope != null
                && binding.EntityScope.TryGetDataSource(args[0], out IExternalDataSource dataSourceInfo)
                && (dataSourceInfo.Kind == DataSourceKind.Connected || dataSourceInfo.Kind == DataSourceKind.CdsNative);

            if (isConnected)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    DType curType = argTypes[i];
                    foreach (var typedName in curType.GetNames(DPath.Root))
                    {
                        DName name = typedName.Name;
                        if (!dataSourceType.TryGetType(name, out DType dsNameType))
                        {
                            dataSourceType.ReportNonExistingName(FieldNameKind.Display, errors, name, args[i], DocumentErrorSeverity.Warning);
                        }
                    }
                }
            }

            base.CheckSemantics(binding, args, argTypes, errors);
            base.ValidateArgumentIsMutable(binding, args[0], errors);            

            MutationUtils.CheckSemantics(binding, this, args, argTypes, errors);

            if (binding.Features.PowerFxV1CompatibilityRules)
            {
                MutationUtils.CheckForReadOnlyFields(argTypes[0], args.Skip(1).ToArray(), argTypes.Skip(1).ToArray(), errors);
            }
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 0;
        }

        public override bool TryGetDataSourceNodes(PowerFx.Syntax.CallNode callNode, TexlBinding binding, out IList<FirstNameNode> dsNodes)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);
            
            if (callNode.Args.Count != 2)
            {
                dsNodes = null;
                return false;
            }

            dsNodes = new List<FirstNameNode>();

            var args = Contracts.VerifyValue(callNode.Args.Children);
            var arg1 = Contracts.VerifyValue(args[1]);

            // Only the second arg can contribute to the output for the purpose of delegation
            return ArgValidators.DataSourceArgNodeValidator.TryGetValidValue(arg1, binding, out dsNodes);
        }

        public override IEnumerable<Identifier> GetIdentifierOfModifiedValue(TexlNode[] args, out TexlNode identifierNode)
        {
            Contracts.AssertValue(args);

            identifierNode = null;
            if (args.Length == 0)
            {
                return null;
            }

            var firstNameNode = args[0]?.AsFirstName();
            identifierNode = firstNameNode;

            if (firstNameNode == null)
            {
                return null;
            }

            var identifiers = new List<Identifier>
            {
                firstNameNode.Ident
            };
            return identifiers;
        }

        public override bool IsAsyncInvocation(PowerFx.Syntax.CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            return Arg0RequiresAsync(callNode, binding);
        }

        public static DType GetCollectedTypeForGivenArgType(Features features, DType argType)
        {
            Contracts.Assert(argType.IsValid);

            if (!argType.IsPrimitive)
            {
                return argType;
            }

            // Passed a scalar; make a record out of it, using a name that depends on the type.
            var fieldName = Contracts.VerifyValue(CreateInvariantFieldName(features, argType.Kind));
            return DType.CreateRecord(new TypedName[] { new TypedName(argType, new DName(fieldName)) });
        }

        protected static string CreateInvariantFieldName(PowerFx.Features features, DKind dKind)
        {
            Contracts.Assert(dKind >= DKind._Min && dKind < DKind._Lim);

            return MutationUtils.GetScalarSingleColumnNameForType(features, dKind);
        }
    }

    // Collect(collection:*[...], item1, ...)
    internal class CollectScalarFunction : CollectFunction
    {
        public override bool IsScalar => true;

        public override bool SupportsParamCoercion => false;

        // This method returns the name of the record to be used when a scalar is passed to Collect and converted.
        // It is critical that these are *always* the invariant names.
        public static string GetInvariantNameForRecord(PowerFx.Features features, DKind dKind)
        {
            return CreateInvariantFieldName(features, dKind);
        }

        public override DType GetCollectedType(Features features, DType argType)
        {
            return GetCollectedTypeForGivenArgType(features, argType);
        }

        internal override IntermediateNode CreateIRCallNode(PowerFx.Syntax.CallNode node, IRTranslator.IRTranslatorContext context, List<IntermediateNode> args, ScopeSymbol scope)
        {
            var newArgs = new List<IntermediateNode>() { args[0] };

            foreach (var arg in args.Skip(1))
            {
                if (arg.IRContext.ResultType._type.IsPrimitive)
                {
                    newArgs.Add(
                        new RecordNode(
                            new IRContext(arg.IRContext.SourceContext, RecordType.Empty().Add(TableValue.ValueName, arg.IRContext.ResultType)), 
                            new Dictionary<DName, IntermediateNode>
                            {
                                { TableValue.ValueDName, arg }
                            }));
                }
                else
                {
                    newArgs.Add(arg);
                }
            }

            return base.CreateIRCallNode(node, context, newArgs, scope);
        }
    }
}
