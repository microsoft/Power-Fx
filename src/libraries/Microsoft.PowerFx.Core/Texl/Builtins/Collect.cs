// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

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

        protected virtual bool IsScalar => false;

        public override bool CanSuggestInputColumns => true;

        public override bool MutatesArg0 => true;

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

        public virtual DType GetCollectedType(PowerFx.Features features, DType argType)
        {
            Contracts.Assert(argType.IsValid);

            return argType;
        }

        // Attempt to get the unified schema of the items being collected by an invocation.
        private bool TryGetUnifiedCollectedType(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType collectedType)
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
                DType argType = GetCollectedType(context.Features, argTypes[i]);

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

                // Checks if all record names exist against table type and if its possible to coerce.
                bool checkAggregateNames = argType.CheckAggregateNames(datasourceType, args[i], errors, context.Features, SupportsParamCoercion);
                fValid = fValid && checkAggregateNames;

                if (!itemType.IsValid)
                {
                    itemType = argType;
                }
                else
                {
                    var fUnionError = false;
                    itemType = DType.Union(ref fUnionError, itemType, argType, useLegacyDateTimeAccepts: true, context.Features);
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

            // Get the unified collected type on the RHS. This will generate appropriate
            // document errors for invalid arguments such as unsupported aggregate types.
            fValid &= TryGetUnifiedCollectedType(context, args, argTypes, errors, out DType collectedType);
            Contracts.Assert(collectedType.IsRecord);

            if (fValid)
            {
                if (!collectedType.TryGetCoercionSubType(collectionType, out DType coercionType, out var coercionNeeded, context.Features))
                {
                    fValid = false;
                }
                else
                {
                    if (coercionNeeded)
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[1], coercionType);
                    }

                    var fError = false;

                    returnType = DType.Union(ref fError, collectionType.ToRecord(), collectedType, useLegacyDateTimeAccepts: false, context.Features, allowCoerce: true);

                    if (argTypes.Length == 2)
                    {
                        if (argTypes[1].IsTable && argTypes[1].Kind != DKind.ObjNull)
                        {
                            returnType = returnType.ToTable();
                        }
                    }
                    else
                    {
                        returnType = returnType.ToTable();
                    }

                    if (fError)
                    {
                        fValid = false;
                        if (!SetErrorForMismatchedColumns(collectionType, collectedType, args[1], errors, context.Features))
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTableDoesNotAcceptThisType);
                        }
                    }
                }
            }

            return fValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
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

            int skip = 1;

            MutationUtils.CheckForReadOnlyFields(argTypes[0], args.Skip(skip).ToArray(), argTypes.Skip(skip).ToArray(), errors);

            // !!! 
            //if ((binding.NameResolver?.CurrentEntity as ControlInfo)?.Template.IsDataComponent == true && !isConnected)
            //{
            //    // Stateful actions including using in-memory data sources are not allowed within data components.
            //    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.err);
            //}

            // !!!
            //FunctionUtils.ManipulatesCollectionsCheckSemantics(binding, this, args, argTypes, errors);
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 0;
        }

        public override bool TryGetDataSourceNodes(CallNode callNode, TexlBinding binding, out IList<FirstNameNode> dsNodes)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            dsNodes = new List<FirstNameNode>();
            if (callNode.Args.Count != 2)
            {
                return false;
            }

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

        public override bool IsAsyncInvocation(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            return Arg0RequiresAsync(callNode, binding);
        }

        public static DType GetCollectedTypeForGivenArgType(PowerFx.Features features, DType argType)
        {
            Contracts.Assert(argType.IsValid);

            if (!argType.IsPrimitive)
            {
                return argType;
            }

            // Passed a scalar; make a record out of it, using a name that depends on the type.
            string fieldName = Contracts.VerifyValue(CreateInvariantFieldName(features, argType.Kind));
            return DType.CreateRecord(new TypedName[] { new TypedName(argType, new DName(fieldName)) });
        }

        protected static string CreateInvariantFieldName(PowerFx.Features features, DKind dKind)
        {
            Contracts.Assert(dKind >= DKind._Min && dKind < DKind._Lim);

            return GetScalarSingleColumnNameForType(features, dKind);
        }

        private static string GetScalarSingleColumnNameForType(Features features, DKind kind)
        {
            return kind switch
            {
                DKind.Image or
                DKind.Hyperlink or
                DKind.Media or
                DKind.Blob or
                DKind.PenImage => features.ConsistentOneColumnTableResult ? TableValue.ValueName : "Url",

                _ => TableValue.ValueName
            };
        }

        // !!!
        //        public static void PushCustomJsArgs(TexlFunction func, JsTranslator translator, TexlBinding binding, CallNode node, List<Fragment> argFragments)
        //        {
        //            var collectFunc = (CollectFunction)func;

        //            // Only scalar collection functions require the scalar field name.
        //            if (!collectFunc.IsScalar)
        //            {
        //                return;
        //            }

        //            // CollectS needs to also inject the field name for these scalars; e.g. Collect(x,"a","b") -> Collect(x,"a","b","Value").
        //            // Note that a single name is sufficient. Since the scalars being pushed are of the same type, they will be collected
        //            // into the exact same column, whose name is needed (and will be pushed) here.
        //            var args = node.Args.Children;
        //            string fieldName = Contracts.VerifyValue(CollectScalarFunction.GetInvariantNameForRecord(binding.Features, binding.GetType(args[1]).Kind));
        //#if DEBUG
        //            for (int i = 1; i < argFragments.Count; i++)
        //            {
        //                Contracts.Assert(fieldName == Contracts.VerifyValue(CollectScalarFunction.GetInvariantNameForRecord(binding.Features, binding.GetType(args[i]).Kind)));
        //            }
        //#endif

        //            var builder = new PAStringBuilder(fieldName.Length + 2);
        //            builder.AppendAsPlainText(fieldName);

        //            argFragments.Add(translator.CreateFragment(builder));
        //        }
    }

    // Collect(collection:*[...], item1, ...)
    internal sealed class CollectScalarFunction : CollectFunction
    {
        protected override bool IsScalar => true;

        public override bool SupportsParamCoercion => false;

        public override DType GetCollectedType(PowerFx.Features features, DType argType)
        {
            return GetCollectedTypeForGivenArgType(features, argType);
        }
    }
}
