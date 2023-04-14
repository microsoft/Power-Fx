﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class UserDefinedFunction : TexlFunction
    {
        private readonly bool _isImperative;
        private readonly IdentToken _returnTypeToken;
        private readonly ISet<UDFArg> _args;
        private readonly RecordType _parametersRecordType;

        public TexlNode UdfBody { get; }

        public override bool IsSelfContained => !_isImperative;

        public UserDefinedFunction(string name, IdentToken returnTypeToken, TexlNode body, bool isImperative, ISet<UDFArg> args, RecordType parametersRecordType)
        : base(DPath.Root, name, name, SG("Custom func " + name), FunctionCategories.UserDefined, returnTypeToken.GetFormulaType()._type, 0, args.Count, args.Count, args.Select(a => a.VarType.GetFormulaType()._type).ToArray())
        {
            this._returnTypeToken = returnTypeToken;
            this._args = args;
            this._parametersRecordType = parametersRecordType;
            this._isImperative = isImperative;

            this.UdfBody = body;
        }

        public TexlBinding BindBody(INameResolver nameResolver, IBinderGlue documentBinderGlue, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler = null, INameResolver functionNameResolver = null)
        {
            if (nameResolver is null)
            {
                throw new ArgumentNullException(nameof(nameResolver));
            }

            if (documentBinderGlue is null)
            {
                throw new ArgumentNullException(nameof(documentBinderGlue));
            }

            var bindingConfig = new BindingConfig(this._isImperative);
            var binding = TexlBinding.Run(documentBinderGlue, UdfBody, UserDefinitionsNameResolver.Merge(nameResolver, _parametersRecordType, functionNameResolver), bindingConfig);

            CheckTypesOnDeclaration(binding.CheckTypesContext, binding.ResultType, binding.ErrorContainer);
            userDefinitionSemanticsHandler?.CheckSemanticsOnDeclaration(binding, _args, binding.ErrorContainer);

            return binding;
        }

        /// <summary>
        /// Perform sub-expression type checking and produce a return type for the function declaration, this is only applicable for UDFs.
        /// </summary>
        public void CheckTypesOnDeclaration(CheckTypesContext context, DType actualBodyReturnType, IErrorContainer errorContainer)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(actualBodyReturnType);
            Contracts.AssertValue(errorContainer);

            var returnTypeFormulaType = _returnTypeToken.GetFormulaType()._type;

            if (!returnTypeFormulaType.Kind.Equals(actualBodyReturnType.Kind) || !returnTypeFormulaType.CoercesTo(returnTypeFormulaType))
            {
                errorContainer.EnsureError(DocumentErrorSeverity.Severe, _returnTypeToken, TexlStrings.ErrUDF_ReturnTypeDoesNotMatch);
            }
        }

        public static StringGetter SG(string text)
        {
            return (string locale) => text;
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { SG("Arg 1") };
        }

        /// <summary>
        /// NameResolver that combines global named resolver and params for user defined function.
        /// </summary>
        private partial class UserDefinitionsNameResolver : INameResolver
        {
            private readonly INameResolver _globalNameResolver;
            private readonly INameResolver _parameterNamedResolver;
            private readonly INameResolver _functionNameResolver;

            public static INameResolver Merge(INameResolver globalNameResolver, RecordType parametersRecordType, INameResolver functionNameResolver)
            {
                return new UserDefinitionsNameResolver(globalNameResolver, ReadOnlySymbolTable.NewFromRecord(parametersRecordType), functionNameResolver);
            }

            private UserDefinitionsNameResolver(INameResolver globalNameResolver, INameResolver parameterNamedResolver, INameResolver functionNameResolver = null)
            {
                this._globalNameResolver = globalNameResolver;
                this._parameterNamedResolver = parameterNamedResolver;
                this._functionNameResolver = functionNameResolver;
            }

            public IExternalDocument Document => _globalNameResolver.Document;

            public IExternalEntityScope EntityScope => _globalNameResolver.EntityScope;

            public IExternalEntity CurrentEntity => _globalNameResolver.CurrentEntity;

            public DName CurrentProperty => _globalNameResolver.CurrentProperty;

            public DPath CurrentEntityPath => _globalNameResolver.CurrentEntityPath;

            public TexlFunctionSet Functions => _functionNameResolver?.Functions ?? _globalNameResolver.Functions;

            public bool SuggestUnqualifiedEnums => _globalNameResolver.SuggestUnqualifiedEnums;

            public bool Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences = NameLookupPreferences.None)
            {
                // lookup in the local scope i.e., function params & body and then look in global scope.
                return _parameterNamedResolver.Lookup(name, out nameInfo, preferences) || _globalNameResolver.Lookup(name, out nameInfo, preferences);
            }

            public bool LookupDataControl(DName name, out NameLookupInfo lookupInfo, out DName dataControlName)
            {
                // params will not have any data controls, hence looking in just _globalNameResolver
                return _globalNameResolver.LookupDataControl(name, out lookupInfo, out dataControlName);
            }

            public IEnumerable<TexlFunction> LookupFunctions(DPath theNamespace, string name, bool localeInvariant = false)
            {
                var functionsFromGlobalNameResolver = _globalNameResolver.LookupFunctions(theNamespace, name, localeInvariant);
                if (_functionNameResolver == null)
                {
                    return functionsFromGlobalNameResolver;
                }
                else
                {
                    var functions = _functionNameResolver.LookupFunctions(theNamespace, name, localeInvariant);
                    return functions.Any() ? functions : functionsFromGlobalNameResolver;
                }
            }

            public IEnumerable<TexlFunction> LookupFunctionsInNamespace(DPath nameSpace)
            {
                var functionsFromGlobalNameResolver = _globalNameResolver.LookupFunctionsInNamespace(nameSpace);
                if (_functionNameResolver == null) 
                {
                    return functionsFromGlobalNameResolver;
                }
                else
                {
                    var functions = _functionNameResolver.LookupFunctionsInNamespace(nameSpace);
                    return functions.Any() ? functions : functionsFromGlobalNameResolver;
                }
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
        }
    }
}
