// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
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

        public UserDefinedFunction(string name, FormulaType returnType, TexlNode udfBody, bool isImperative, params FormulaType[] paramTypes)
            : base(DPath.Root, name, name, SG("Custom func " + name), FunctionCategories.MathAndStat, returnType._type, 0, paramTypes.Length, paramTypes.Length, Array.ConvertAll(paramTypes, x => x._type))
        {
            UdfBody = udfBody;
            _isImperative = isImperative;
        }

        public UserDefinedFunction(DName udfName, IdentToken returnType, TexlNode body, bool isImperative, ISet<UDFArg> args)
            : this(udfName.Value, returnType.GetFormulaType(), body, isImperative, args.Select(a => a.VarType.GetFormulaType()).ToArray())
        {
            this._returnTypeToken = returnType;
            this._args = args;
        }

        public TexlBinding Bind(INameResolver nameResolver, IUserDefinitionSemanticsHandler userDefinitionSemanticsHandler)
        {
            var glue = new Glue2DocumentBinderGlue();
            var parametersRecordType = CheckParameters(out var errors);
            var bindingConfig = new BindingConfig(this._isImperative);

            // TODO: Create a new name resolver
            var binding = TexlBinding.Run(glue, UdfBody, ReadOnlySymbolTable.Compose(nameResolver as ReadOnlySymbolTable, ReadOnlySymbolTable.NewFromRecord(parametersRecordType)), bindingConfig);

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

            if (!returnTypeFormulaType.Kind.Equals(actualBodyReturnType.Kind))
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
    }
}
