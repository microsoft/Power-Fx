// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;
using CallNode = Microsoft.PowerFx.Syntax.CallNode;

namespace Microsoft.PowerFx.Core.Functions
{
    /// <summary>
    /// Represents a user defined function (UDF) - which is created from parsing other Power Fx.     
    /// This includings the binding (and hence IR for evaluation) - 
    /// This is conceptually immutable after initialization - if the body or signature changes, you need to create a new instance.
    /// </summary>
    internal class UserDefinedFunction : TexlFunction, IExternalPageableSymbol
    {
        private readonly bool _isImperative;
        private readonly IEnumerable<UDFArg> _args;
        private TexlBinding _binding;

        public override bool IsAsync => _binding.IsAsync(UdfBody);

        public bool IsPageable => _binding.IsPageable(_binding.Top);

        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);
            Contracts.Assert(binding.GetInfo(callNode).Function is UserDefinedFunction udf && udf.Binding != null);

            if (base.IsServerDelegatable(callNode, binding) || _binding.IsDelegatable(_binding.Top))
            {
                return true;
            }

            if (_binding.Top.Kind == NodeKind.FirstName && ArgValidators.DelegatableDataSourceInfoValidator.TryGetValidValue(_binding.Top, _binding, out _))
            {
                return true;
            }

            return false;
        }

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            return _binding.IsPageable(_binding.Top);
        }

        public override bool SupportsParamCoercion => true;

        public override bool HasPreciseErrors => true;

        private const int MaxParameterCount = 30;

        public TexlNode UdfBody { get; }

        public override bool IsSelfContained => !_isImperative;

        public TexlBinding Binding => _binding;

        public bool TryGetExternalDataSource(out IExternalDataSource dataSource)
        {
            return ArgValidators.DelegatableDataSourceInfoValidator.TryGetValidValue(_binding.Top, _binding, out dataSource);
        }

        public override bool TryGetDataSource(CallNode callNode, TexlBinding binding, out IExternalDataSource dsInfo)
        {
            return TryGetExternalDataSource(out dsInfo);
        }

        public bool HasDelegationWarning => _binding?.ErrorContainer.GetErrors().Any(error => error.MessageKey.Contains("SuggestRemoteExecutionHint")) ?? false;

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            if (!base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap))
            {
                return false;
            }

            for (int i = 0; i < argTypes.Length; i++)
            {
                if ((argTypes[i].IsTableNonObjNull || argTypes[i].IsRecordNonObjNull) &&
                    !ParamTypes[i].Accepts(argTypes[i], out var schemaDiff, out _, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules, restrictiveAggregateTypes: true) &&
                    !argTypes[i].CoercesTo(ParamTypes[i], aggregateCoercion: true, isTopLevelCoercion: false, features: context.Features, restrictiveAggregateTypes: true))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrBadSchema_AdditionalField, ParamTypes[i].GetKindString(), schemaDiff.Key);

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDefinedFunction"/> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="returnType">Return type of the function.</param>
        /// <param name="body">TexlNode for user defined function body.</param>
        /// <param name="isImperative"></param>
        /// <param name="args"></param>
        /// <param name="argTypes">Array of argTypes in order.</param>
        public UserDefinedFunction(string functionName, DType returnType, TexlNode body, bool isImperative, ISet<UDFArg> args, DType[] argTypes)
        : base(DPath.Root, functionName, functionName, SG(functionName), FunctionCategories.UserDefined, returnType, 0, args.Count, args.Count, argTypes)
        {
            this._args = args;
            this._isImperative = isImperative;

            this.UdfBody = body;
        }

        /// <summary>
        /// Gets argument index for a given argument.
        /// </summary>
        /// <param name="argName">Name of the argument.</param>
        /// <param name="argIndex">Index of the given argument.</param>
        /// <returns>True if argument is found.</returns>
        public bool TryGetArgIndex(string argName, out int argIndex)
        {
            argIndex = -1;

            if (string.IsNullOrEmpty(argName))
            {
                return false;
            }

            foreach (var arg in _args)
            {
                if (arg.NameIdent.Name.Value == argName)
                {
                    argIndex = arg.ArgIndex;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Binds user defned function body. Calling this function will change the state of IsAsync.
        /// </summary>
        /// <param name="nameResolver">Global name resolver to resolve names including other UDFs used inside the body.</param>
        /// <param name="documentBinderGlue">Document binder glue.</param>
        /// <param name="bindingConfig">Configuration for an invocation of the binder.</param>
        /// <param name="features">PowerFx features.</param>
        /// <param name="rule"></param>
        /// <param name="updateDisplayNames">If true, the binder will update the display names of the nodes in the parse tree.</param>
        /// <returns>Returns binding for the function body.</returns>
        public TexlBinding BindBody(INameResolver nameResolver, IBinderGlue documentBinderGlue, BindingConfig bindingConfig = null, Features features = null, IExternalRule rule = null, bool updateDisplayNames = false)
        {
            if (nameResolver is null)
            {
                throw new ArgumentNullException(nameof(nameResolver));
            }

            if (documentBinderGlue is null)
            {
                throw new ArgumentNullException(nameof(documentBinderGlue));
            }

            if (_binding != null)
            {
                throw new InvalidOperationException($"Body should only get bound once: {this.Name}");
            }

            bindingConfig = bindingConfig ?? new BindingConfig(this._isImperative, userDefinitionsMode: true);
            _binding = TexlBinding.Run(documentBinderGlue, UdfBody, UserDefinitionsNameResolver.Create(nameResolver, _args, ParamTypes), bindingConfig, features: features, rule: rule, updateDisplayNames: updateDisplayNames);

            CheckTypesOnDeclaration(_binding.CheckTypesContext, _binding.ResultType, _binding);

            return _binding;
        }

        /// <summary>
        /// Perform sub-expression type checking and produce a return type for the function declaration, this is only applicable for UDFs.
        /// </summary>
        private void CheckTypesOnDeclaration(CheckTypesContext context, DType actualBodyReturnType, TexlBinding binding)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(actualBodyReturnType);
            Contracts.AssertValue(binding);

            if (!ReturnType.Accepts(
                actualBodyReturnType, 
                out var schemaDiff,
                out var diffType,
                exact: true, 
                useLegacyDateTimeAccepts: false,
                usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules,
                restrictiveAggregateTypes: true))
            {
                if (actualBodyReturnType.CoercesTo(ReturnType, true, false, context.Features, restrictiveAggregateTypes: true))
                {
                    _binding.SetCoercedType(binding.Top, ReturnType);
                }
                else
                {
                    var node = UdfBody is VariadicOpNode variadicOpNode ? variadicOpNode.Children.Last() : UdfBody;

                    if ((ReturnType.IsTable && actualBodyReturnType.IsTable) || (ReturnType.IsRecord && actualBodyReturnType.IsRecord))
                    {
                        AddAggregateTypeErrors(binding.ErrorContainer, node, ReturnType, schemaDiff, diffType);
                        return;
                    }

                    binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrUDF_ReturnTypeDoesNotMatch, ReturnType.GetKindString(), actualBodyReturnType.GetKindString());
                }
            }
        }

        private void AddAggregateTypeErrors(IErrorContainer errors, TexlNode node, DType nodeType, KeyValuePair<string, DType> schemaDifference, DType schemaDifferenceType)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValid(nodeType);

            if (schemaDifferenceType.IsValid)
            {
                errors.EnsureError(
                    DocumentErrorSeverity.Severe,
                    node,
                    TexlStrings.ErrUDF_ReturnTypeSchemaIncompatible,
                    schemaDifference.Key,
                    schemaDifference.Value.GetKindString(),
                    schemaDifferenceType.GetKindString());
            }
            else
            {
                errors.EnsureError(
                    DocumentErrorSeverity.Severe,
                    node,
                    TexlStrings.ErrUDF_ReturnTypeSchemaAdditionalFields,
                    nodeType.GetKindString(),
                    schemaDifference.Key);
            }
        }

        public static StringGetter SG(string text)
        {
            return (string locale) => text;
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            return new[] { _args.Select<UDFArg, TexlStrings.StringGetter>(key => _ => key.NameIdent.Name.Value).ToArray() };
        }

        public (IntermediateNode topNode, ScopeSymbol ruleScopeSymbol) GetIRTranslator()
        {
            return IRTranslator.Translate(_binding);
        }

        /// <summary>
        /// Clones and binds a user defined function.
        /// </summary>
        /// <returns>Returns a new functions.</returns>
        public UserDefinedFunction WithBinding(INameResolver nameResolver, IBinderGlue binderGlue, out TexlBinding binding, BindingConfig bindingConfig = null, Features features = null, IExternalRule rule = null, bool updateDisplayNames = false)
        {
            if (nameResolver is null)
            {
                throw new ArgumentNullException(nameof(nameResolver));
            }

            if (binderGlue is null)
            {
                throw new ArgumentNullException(nameof(binderGlue));
            }

            var func = new UserDefinedFunction(Name, ReturnType, UdfBody, _isImperative, new HashSet<UDFArg>(_args), ParamTypes);
            binding = func.BindBody(nameResolver, binderGlue, bindingConfig, features, rule, updateDisplayNames);

            return func;
        }

        // Adding a restricted UDF name is a breaking change, this test will need to be updated and a conversion will be needed for existing scenarios
        private static readonly ISet<string> _restrictedUDFNames = new HashSet<string>
        {
            "Type", "IsType", "AsType", "Set", "Collect",
            "ClearCollect", "UpdateContext", "Navigate",
        };

        /// <summary>
        /// Helper to create IR UserDefinedFunctions.
        /// </summary>
        /// <param name="uDFs">Valid Parsed UDFs to be converted into UserDefinedFunction.</param>
        /// <param name="nameResolver">NameResolver to resolve type names.</param>
        /// <param name="errors">Errors when creating functions.</param>
        /// <returns>IEnumerable of UserDefinedFunction.</returns>
        public static IEnumerable<UserDefinedFunction> CreateFunctions(IEnumerable<UDF> uDFs, INameResolver nameResolver, out List<TexlError> errors)
        {
            Contracts.AssertValue(uDFs);
            Contracts.AssertAllValues(uDFs);

            var userDefinedFunctions = new List<UserDefinedFunction>();
            var texlFunctionSet = new TexlFunctionSet();
            errors = new List<TexlError>();

            foreach (var udf in uDFs)
            {
                Contracts.Assert(udf.IsParseValid);

                var udfName = udf.Ident.Name;
                if (texlFunctionSet.AnyWithName(udfName))
                {
                    errors.Add(new TexlError(udf.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_FunctionAlreadyDefined, udfName));
                    continue;
                }
                else if (_restrictedUDFNames.Contains(udfName) ||
                    nameResolver.Functions.WithName(udfName).Any(func => func.IsRestrictedUDFName))
                {
                    errors.Add(new TexlError(udf.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_FunctionNameRestricted, udfName));
                    continue;
                }

                if (udf.Args.Count > MaxParameterCount)
                {
                    errors.Add(new TexlError(udf.Ident, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_TooManyParameters, udfName, MaxParameterCount));
                    continue;
                }

                var parametersOk = CheckParameters(udf.Args, errors, nameResolver, out var parameterTypes);
                var returnTypeOk = CheckReturnType(udf.ReturnType, errors, nameResolver, out var returnType);
                if (!parametersOk || !returnTypeOk)
                {
                    continue;
                }

                if (nameResolver.Functions.WithName(udfName).Any())
                {
                    errors.Add(new TexlError(udf.Ident, DocumentErrorSeverity.Warning, TexlStrings.WrnUDF_ShadowingBuiltInFunction, udfName));
                }

                var func = new UserDefinedFunction(udfName.Value, returnType, udf.Body, udf.IsImperative, udf.Args, parameterTypes);

                texlFunctionSet.Add(func);
                userDefinedFunctions.Add(func);
            }

            return userDefinedFunctions;
        }

        private static bool CheckParameters(ISet<UDFArg> args, List<TexlError> errors, INameResolver nameResolver, out DType[] parameterTypes)
        {
            if (args.Count == 0)
            {
                parameterTypes = Array.Empty<DType>();
                return true;
            }

            var isParamCheckSuccessful = true;
            var argsAlreadySeen = new HashSet<string>();
            parameterTypes = new DType[args.Count];

            foreach (var arg in args)
            {
                if (argsAlreadySeen.Contains(arg.NameIdent.Name))
                {
                    errors.Add(new TexlError(arg.NameIdent, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_DuplicateParameter, arg.NameIdent.Name));
                    isParamCheckSuccessful = false;
                }
                else
                {
                    argsAlreadySeen.Add(arg.NameIdent.Name);

                    if (!nameResolver.LookupType(arg.TypeIdent.Name, out var parameterType))
                    {
                        errors.Add(new TexlError(arg.TypeIdent, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_UnknownType, arg.TypeIdent.Name));
                        isParamCheckSuccessful = false;
                    }
                    else if (IsRestrictedType(parameterType, UserDefinitions.RestrictedParameterTypes))
                    {
                        errors.Add(new TexlError(arg.TypeIdent, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_InvalidParamType, arg.TypeIdent.Name));
                        isParamCheckSuccessful = false;
                    }
                    else
                    {
                        Contracts.Assert(arg.ArgIndex >= 0);
                        Contracts.Assert(arg.ArgIndex < args.Count);
                        parameterTypes[arg.ArgIndex] = parameterType._type;
                    }
                }
            }

            return isParamCheckSuccessful;
        }

        private static bool CheckReturnType(IdentToken returnTypeToken, List<TexlError> errors, INameResolver nameResolver, out DType returnType)
        {
            if (!nameResolver.LookupType(returnTypeToken.Name, out var returnTypeFormulaType))
            {
                errors.Add(new TexlError(returnTypeToken, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_UnknownType, returnTypeToken.Name));
                returnType = DType.Invalid;
                return false;
            }
            
            if (IsRestrictedType(returnTypeFormulaType, UserDefinitions.RestrictedTypes))
            {
                errors.Add(new TexlError(returnTypeToken, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_InvalidReturnType, returnTypeToken.Name));
                returnType = DType.Invalid;
                return false;
            }
            
            returnType = returnTypeFormulaType._type;
            return true; 
        }

        // To prevent aggregate types from containing restricted types
        internal static bool IsRestrictedType(FormulaType ft, ISet<DType> restrictedTypes)
        {
            Contracts.AssertValue(ft);

            // Datasource types may contain fields that may expand to other datasource types or refernce themselves.
            // We can avoid calling this method on these types containing expand info.
            if (!ft._type.HasExpandInfo && ft is AggregateType aggType)
            {
                if (aggType.GetFieldTypes().Any(ct => IsRestrictedType(ct.Type, restrictedTypes)))
                {
                    return true;
                }
            }

            if (restrictedTypes.Contains(ft._type))
            {
                return true;
            }

            return false;
        }

        // Checks if the current UDF has the same definition as the target UDF.
        public bool HasSameDefintion(string definitionsScript, UserDefinedFunction targetUDF, string targetUDFbody)
        {
            Contracts.AssertValue(targetUDF);

            if (Name != targetUDF.Name ||
                UdfBody.GetCompleteSpan().GetFragment(definitionsScript) != targetUDFbody || 
                _args.Count() != targetUDF._args.Count() ||
                ReturnType.AssociatedDataSources.SetEquals(targetUDF.ReturnType.AssociatedDataSources) == false ||
                ReturnType != targetUDF.ReturnType ||
                _isImperative != targetUDF._isImperative)
            {
                return false;
            }

            // Argument indices should match - change in index would affect caller
            var argLookup = _args.ToDictionary(arg => arg.ArgIndex, arg => (arg.NameIdent.Name, ParamTypes[arg.ArgIndex]));

            foreach (var arg in targetUDF._args)
            {
                if (!argLookup.TryGetValue(arg.ArgIndex, out var argInfo) || 
                    argInfo.Name != arg.NameIdent.Name || 
                    argInfo.Item2.AssociatedDataSources.SetEquals(targetUDF.ParamTypes[arg.ArgIndex].AssociatedDataSources) == false ||
                    argInfo.Item2 != targetUDF.ParamTypes[arg.ArgIndex])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// NameResolver that combines global named resolver and params for user defined function.
        /// </summary>
        private partial class UserDefinitionsNameResolver : INameResolver
        {
            private readonly INameResolver _globalNameResolver;
            private readonly IReadOnlyDictionary<string, UDFArg> _args;
            private readonly DType[] _argTypes;

            public static INameResolver Create(INameResolver globalNameResolver, IEnumerable<UDFArg> args, DType[] argTypes)
            {
                return new UserDefinitionsNameResolver(globalNameResolver, args, argTypes);
            }

            private UserDefinitionsNameResolver(INameResolver globalNameResolver, IEnumerable<UDFArg> args, DType[] argTypes)
            {
                Contracts.AssertValue(args);
                Contracts.AssertValue(argTypes);
                Contracts.AssertValue(globalNameResolver);
                Contracts.Assert(args.Count() == argTypes.Length);

                this._globalNameResolver = globalNameResolver;
                this._args = args.ToDictionary(arg => arg.NameIdent.Name.Value, arg => arg);
                this._argTypes = argTypes;
            }

            public IExternalDocument Document => _globalNameResolver.Document;

            public IExternalEntityScope EntityScope => _globalNameResolver.EntityScope;

            public IExternalEntity CurrentEntity => _globalNameResolver.CurrentEntity;

            public DName CurrentProperty => _globalNameResolver.CurrentProperty;

            public DPath CurrentEntityPath => _globalNameResolver.CurrentEntityPath;

            public TexlFunctionSet Functions => _globalNameResolver.Functions;

            public IEnumerable<KeyValuePair<DName, FormulaType>> NamedTypes => _globalNameResolver.NamedTypes;

            public bool SuggestUnqualifiedEnums => _globalNameResolver.SuggestUnqualifiedEnums;

            public bool Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences = NameLookupPreferences.None)
            {
                // lookup in the local scope i.e., function params & body and then look in global scope.
                if (_args.TryGetValue(name, out var value))
                {
                    var type = _argTypes[value.ArgIndex];
                    nameInfo = new NameLookupInfo(BindKind.PowerFxResolvedObject, type, DPath.Root, 0, new UDFParameterInfo(type, value.ArgIndex, value.NameIdent.Name));

                    return true;
                }

                return _globalNameResolver.Lookup(name, out nameInfo, preferences);
            }

            public bool LookupDataControl(DName name, out NameLookupInfo lookupInfo, out DName dataControlName)
            {
                // params will not have any data controls, hence looking in just _globalNameResolver
                return _globalNameResolver.LookupDataControl(name, out lookupInfo, out dataControlName);
            }

            public IEnumerable<TexlFunction> LookupFunctions(DPath theNamespace, string name, bool localeInvariant = false)
            {
               return _globalNameResolver.LookupFunctions(theNamespace, name, localeInvariant);
            }

            public IEnumerable<TexlFunction> LookupFunctionsInNamespace(DPath nameSpace)
            {
                return _globalNameResolver.LookupFunctionsInNamespace(nameSpace);
            }

            public bool LookupType(DName name, out FormulaType fType)
            {
                return _globalNameResolver.LookupType(name, out fType);
            }

            public bool LookupGlobalEntity(DName name, out NameLookupInfo lookupInfo)
            {
                return _globalNameResolver.LookupGlobalEntity(name, out lookupInfo);
            }

            public bool LookupParent(out NameLookupInfo lookupInfo)
            {
                return _globalNameResolver.LookupParent(out lookupInfo);
            }

            public bool LookupSelf(out NameLookupInfo lookupInfo)
            {
                return _globalNameResolver.LookupSelf(out lookupInfo);
            }

            public bool TryGetInnermostThisItemScope(out NameLookupInfo nameInfo)
            {
                return _globalNameResolver.TryGetInnermostThisItemScope(out nameInfo);
            }

            public bool TryLookupEnum(DName name, out NameLookupInfo lookupInfo)
            {
                return _globalNameResolver.TryLookupEnum(name, out lookupInfo);
            }

            public bool LookupExpandedControlType(IExternalControl control, out DType controlType)
            {
                return _globalNameResolver.LookupExpandedControlType(control, out controlType);
            }
        }
    }
}
