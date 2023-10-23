// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Core.Functions
{
    /// <summary>
    /// Represents a user defined function (UDF) - which is created from parsing other Power Fx.     
    /// This includings the binding (and hence IR for evaluation) - 
    /// This is conceptually immutable after initialization - if the body or signature changes, you need to create a new instance.
    /// </summary>
    internal class UserDefinedFunction : TexlFunction
    {
        private readonly bool _isImperative;
        private readonly IEnumerable<UDFArg> _args;
        private TexlBinding _binding;

        public override bool IsAsync => _binding?.IsAsync(UdfBody) ?? false;

        public override bool SupportsParamCoercion => true;

        public TexlNode UdfBody { get; }

        public override bool IsSelfContained => !_isImperative;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDefinedFunction"/> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="returnType">Return type of the function.</param>
        /// <param name="body">TexlNode for user defined function body.</param>
        /// <param name="isImperative"></param>
        /// <param name="args"></param>
        public UserDefinedFunction(string functionName, DType returnType, TexlNode body, bool isImperative, ISet<UDFArg> args)
            : base(DPath.Root, functionName, functionName, SG(functionName), FunctionCategories.UserDefined, returnType, 0, args.Count, args.Count, args.Select(a => a.TypeIdent.GetFormulaType()._type).ToArray())
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
        /// <returns>Returns binding for the function body.</returns>
        public TexlBinding BindBody(INameResolver nameResolver, IBinderGlue documentBinderGlue, BindingConfig bindingConfig = null, Features features = null, IExternalRule rule = null)
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

            bindingConfig = bindingConfig ?? new BindingConfig(this._isImperative);
            _binding = TexlBinding.Run(documentBinderGlue, UdfBody, UserDefinitionsNameResolver.Create(nameResolver, _args), bindingConfig, features: features, rule: rule);

            CheckTypesOnDeclaration(_binding.CheckTypesContext, _binding.ResultType, _binding);

            return _binding;
        }

        /// <summary>
        /// Perform sub-expression type checking and produce a return type for the function declaration, this is only applicable for UDFs.
        /// </summary>
        public void CheckTypesOnDeclaration(CheckTypesContext context, DType actualBodyReturnType, TexlBinding binding)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(actualBodyReturnType);
            Contracts.AssertValue(binding);

            if (!ReturnType.Accepts(actualBodyReturnType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
            {
                if (actualBodyReturnType.CoercesTo(ReturnType, true, false, context.Features.PowerFxV1CompatibilityRules))
                {
                    _binding.SetCoercedType(binding.Top, ReturnType);
                }
                else
                {
                    binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Severe, UdfBody, TexlStrings.ErrUDF_ReturnTypeDoesNotMatch);
                }
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
        /// NameResolver that combines global named resolver and params for user defined function.
        /// </summary>
        private partial class UserDefinitionsNameResolver : INameResolver
        {
            private readonly INameResolver _globalNameResolver;
            private readonly IReadOnlyDictionary<string, UDFArg> _args;

            public static INameResolver Create(INameResolver globalNameResolver, IEnumerable<UDFArg> args)
            {
                return new UserDefinitionsNameResolver(globalNameResolver, args);
            }

            private UserDefinitionsNameResolver(INameResolver globalNameResolver, IEnumerable<UDFArg> args)
            {
                this._globalNameResolver = globalNameResolver;
                this._args = args.ToDictionary(arg => arg.NameIdent.Name.Value, arg => arg);
            }

            public IExternalDocument Document => _globalNameResolver.Document;

            public IExternalEntityScope EntityScope => _globalNameResolver.EntityScope;

            public IExternalEntity CurrentEntity => _globalNameResolver.CurrentEntity;

            public DName CurrentProperty => _globalNameResolver.CurrentProperty;

            public DPath CurrentEntityPath => _globalNameResolver.CurrentEntityPath;

            public TexlFunctionSet Functions => _globalNameResolver.Functions;

            public bool SuggestUnqualifiedEnums => _globalNameResolver.SuggestUnqualifiedEnums;

            public bool Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences = NameLookupPreferences.None)
            {
                // lookup in the local scope i.e., function params & body and then look in global scope.
                if (_args.TryGetValue(name, out var value))
                {
                    var type = value.TypeIdent.GetFormulaType()._type;
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
