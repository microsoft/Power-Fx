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
    // The CollectFunction class was inspired (copied) by Set function.
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
        public override bool SuppressIntellisenseForComponent => true;

        public override bool ManipulatesCollections => true;

        public override bool ModifiesValues => true;

        public override bool AffectsCollectionSchemas => true;

        public override bool IsSelfContained => true;

        public override bool CanSuggestThisItem => true;

        public override bool RequiresDataSourceScope => true;

        protected virtual bool IsScalar => false;

        public override bool SupportsParamCoercion => false;

        public override bool DisableForCommanding => true;

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
              int.MaxValue)
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

            var argc = args.Length;

            for (var i = 1; i < argc; i++)
            {
                DType argType = GetCollectedType(argTypes[i]);

                // The subsequent args should all be aggregates.
                if (!argType.IsAggregate)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrNeedValidVariableName_Arg);
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

            // Not connected for now
            //bool isConnected = binding.EntityScope != null &&
            //    FunctionUtils.TryGetDataSource((EntityScope)binding.EntityScope, args[0], out DataSourceInfo dataSourceInfo) &&
            //    (dataSourceInfo.Kind == DataSourceKind.Connected || dataSourceInfo.Kind == DataSourceKind.CdsNative);

            //if (isConnected)
            //{
            //    for (int i = 1; i < args.Length; i++)
            //    {
            //        DType curType = argTypes[i];
            //        foreach (var typedName in curType.GetNames(DPath.Root))
            //        {
            //            DName name = typedName.Name;
            //            if (!dataSourceType.TryGetType(name, out DType dsNameType))
            //                dataSourceType.ReportNonExistingName(FieldNameKind.Display, errors, name, args[i], DocumentErrorSeverity.Warning);
            //        }
            //    }
            //}

            // TASK: 75145: SPEC: what if the types align for arg0, but arg0 is not a name node? For example:
            //      Collect( Filter(T,A<2), {A:10} )
            // The current behavior is that Collect has no side effects for transient tables/collections.

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

            //if ((binding.NameResolver?.CurrentEntity as ControlInfo)?.Template.IsDataComponent == true && !isConnected)
            //{
            //    // Stateful actions including using in-memory data sources are not allowed within data components.
            //    fValid = false;
            //    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], CanvasStringResources.DataComponent_ErrCollectionInDataComponent);
            //}

            return fValid;
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

        public static DType GetCollectedTypeForGivenArgType(DType argType)
        {
            Contracts.Assert(argType.IsValid);

            if (!argType.IsPrimitive)
            {
                return argType;
            }

            // Passed a scalar; make a record out of it, using a name that depends on the type.
            var fieldName = Contracts.VerifyValue(CreateInvariantFieldName(argType.Kind));
            return DType.CreateRecord(new TypedName[] { new TypedName(argType, new DName(fieldName)) });
        }

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
        //            TexlNode[] args = node.Args.Children;
        //            string fieldName = Contracts.VerifyValue(CollectScalarFunction.GetInvariantNameForRecord(binding.GetType(args[1]).Kind));
        //#if DEBUG
        //            for (int i = 1; i < argFragments.Count; i++)
        //                Contracts.Assert(fieldName == Contracts.VerifyValue(CollectScalarFunction.GetInvariantNameForRecord(binding.GetType(args[i]).Kind)));
        //#endif

        //            var builder = new PAStringBuilder(fieldName.Length + 2);
        //            builder.AppendAsPlainText(fieldName);

        //            argFragments.Add(translator.CreateFragment(builder));
        //        }

        protected static string CreateInvariantFieldName(DKind dKind)
        {
            Contracts.Assert(dKind >= DKind._Min && dKind < DKind._Lim);

            switch (dKind)
            {
                case DKind.Image:
                case DKind.Hyperlink:
                case DKind.Media:
                case DKind.Blob:
                case DKind.PenImage:
                    return "Url";
                default:
                    return "Value";
            }
        }

        public Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel)
        {
            var arg0 = (RecordsOnlyTableValue)args[0];
            var arg1 = (RecordValue)args[1];

            var mytype0 = arg0.GetType();
            var mytype1 = arg1.GetType();

            arg0.Append(arg1);

            return Task.FromResult<FormulaValue>(FormulaValue.New(true));
        }
    }
}
