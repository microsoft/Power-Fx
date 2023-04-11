// Copyright (c) Microsoft Corporation.
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

        public TexlNode UdfBody { get; }

        public override bool IsSelfContained => !_isImperative;

        public UserDefinedFunction(string name, IdentToken returnType, TexlNode body, bool isImperative, ISet<UDFArg> args)
        : base(DPath.Root, name, name, SG("Custom func " + name), FunctionCategories.UserDefined, returnType.GetFormulaType()._type, 0, args.Count, args.Count, args.Select(a => a.VarType.GetFormulaType()._type).ToArray())
        {
            this._returnTypeToken = returnType;
            this._args = args;
        }

        public TexlBinding Bind(INameResolver nameResolver, IBinderGlue documentBinderGlue, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler = null)
        {
            if (nameResolver is null)
            {
                throw new ArgumentNullException(nameof(nameResolver));
            }

            if (documentBinderGlue is null)
            {
                throw new ArgumentNullException(nameof(documentBinderGlue));
            }

            var parametersRecordType = CheckParameters(out var errors);
            var bindingConfig = new BindingConfig(this._isImperative);
            var binding = TexlBinding.Run(documentBinderGlue, UdfBody, UserDefinitionsNameResolver.Merge(nameResolver, parametersRecordType), bindingConfig);

            CheckTypesOnDeclaration(binding.CheckTypesContext, _args, _returnTypeToken, binding.ResultType, binding.ErrorContainer);
            userDefinitionSemanticsHandler?.CheckSemanticsOnDeclaration(binding, _args, binding.ErrorContainer);
            binding.ErrorContainer.MergeErrors(errors);

            return binding;
        }

        private RecordType CheckParameters(out List<TexlError> errors)
        {
            var record = RecordType.Empty();
            var argsAlreadySeen = new HashSet<string>();
            errors = new List<TexlError>();

            foreach (var arg in _args)
            {
                if (!argsAlreadySeen.Add(arg.VarIdent.Name))
                {
                    errors.Add(new TexlError(arg.VarIdent, DocumentErrorSeverity.Severe, TexlStrings.ErrUDF_DuplicateParameter, arg.VarIdent.Name));
                }
                else
                {
                    argsAlreadySeen.Add(arg.VarIdent.Name);
                    record = record.Add(new NamedFormulaType(arg.VarIdent.ToString(), arg.VarType.GetFormulaType()));
                }
            }

            return record;
        }

        /// <summary>
        /// Perform sub-expression type checking and produce a return type for the function declaration, this is only applicable for UDFs.
        /// </summary>
        public static void CheckTypesOnDeclaration(CheckTypesContext context, IEnumerable<UDFArg> uDFArgs, IdentToken returnType, DType actualBodyReturnType, IErrorContainer errorContainer)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(uDFArgs);
            Contracts.AssertValue(returnType);
            Contracts.AssertValue(actualBodyReturnType);
            Contracts.AssertValue(errorContainer);

            CheckParameters(uDFArgs, errorContainer);
            CheckReturnType(returnType, actualBodyReturnType, errorContainer);
        }

        private static void CheckReturnType(IdentToken returnType, DType actualBodyReturnType, IErrorContainer errorContainer)
        {
            var returnTypeFormulaType = returnType.GetFormulaType()._type;

            if (returnTypeFormulaType.Kind.Equals(DType.Unknown.Kind))
            {
                errorContainer.EnsureError(DocumentErrorSeverity.Severe, returnType, TexlStrings.ErrUDF_UnknownType, returnType.Name);

                // Anyhow the next error will be ignored, since it is added on the same token
                return; 
            }

            if (!returnTypeFormulaType.Kind.Equals(actualBodyReturnType.Kind) || !returnTypeFormulaType.CoercesTo(returnTypeFormulaType))
            {
                errorContainer.EnsureError(DocumentErrorSeverity.Severe, returnType, TexlStrings.ErrUDF_ReturnTypeDoesNotMatch);
            }
        }

        private static void CheckParameters(IEnumerable<UDFArg> args, IErrorContainer errorContainer)
        {
            foreach (var arg in args)
            {
                var argNameToken = arg.VarIdent;
                var argTypeToken = arg.VarType;

                if (argTypeToken.GetFormulaType()._type.Kind.Equals(DType.Unknown.Kind))
                {
                    errorContainer.EnsureError(DocumentErrorSeverity.Severe, argTypeToken, TexlStrings.ErrUDF_UnknownType, argTypeToken.Name);
                }
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
        private class UserDefinitionsNameResolver : INameResolver
        {
            private readonly INameResolver _globalNameResolver;
            private readonly INameResolver _parameterNamedResolver;

            public static INameResolver Merge(INameResolver globalNameResolver, RecordType parametersRecordType)
            {
                return new UserDefinitionsNameResolver(globalNameResolver, ReadOnlySymbolTable.NewFromRecord(parametersRecordType));
            }

            private UserDefinitionsNameResolver(INameResolver globalNameResolver, INameResolver parameterNamedResolver)
            {
                this._globalNameResolver = globalNameResolver;
                this._parameterNamedResolver = parameterNamedResolver;
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
                return _parameterNamedResolver.Lookup(name, out nameInfo, preferences) || _globalNameResolver.Lookup(name, out nameInfo, preferences);
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
        }
    }
}
