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

namespace Microsoft.PowerFx
{
    internal class UserDefinedFunction : CustomTexlFunction
    {
        private readonly bool _isImperative;

        public TexlNode UdfBody { get; }

        public override bool IsSelfContained => !_isImperative;

        public UserDefinedFunction(string name, FormulaType returnType, TexlNode udfBody, bool isImperative, params FormulaType[] paramTypes)
            : base(name, returnType._type, Array.ConvertAll(paramTypes, x => x._type))
        {
            UdfBody = udfBody;
            _isImperative = isImperative;
        }

        /// <summary>
        /// Perform sub-expression type checking and produce a return type for the function declaration, this is only applicable for UDFs.
        /// </summary>
        public static void CheckTypesOnDeclaration(CheckTypesContext context, IEnumerable<UDFArg> uDFArgs, IdentToken returnType, DType actualReturnType, IErrorContainer errorContainer)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(uDFArgs);
            Contracts.AssertValue(returnType);
            Contracts.AssertValue(actualReturnType);
            Contracts.AssertValue(errorContainer);

            CheckParameters(uDFArgs, errorContainer);
            CheckReturnType(returnType, actualReturnType, errorContainer);
        }

        private static void CheckReturnType(IdentToken returnType, DType actualReturnType, IErrorContainer errorContainer)
        {
            var returnTypeFormulaType = returnType.GetFormulaType()._type;

            if (returnTypeFormulaType.Kind.Equals(DType.Unknown.Kind))
            {
                errorContainer.EnsureError(DocumentErrorSeverity.Severe, returnType, TexlStrings.ErrUDF_UnknownType, returnType.Name);

                // Anyhow the next error will be ignored, since it is added on the same token
                return; 
            }

            if (returnTypeFormulaType.Kind.Equals(actualReturnType.Kind))
            {
                errorContainer.EnsureError(DocumentErrorSeverity.Severe, returnType, TexlStrings.ErrUDF_ReturnTypeDoesNotMatch);
            }
        }

        private static void CheckParameters(IEnumerable<UDFArg> args, IErrorContainer errorContainer)
        {
            var argsAlreadySeen = new HashSet<string>();

            foreach (var arg in args)
            {
                var argNameToken = arg.VarIdent;
                var argTypeToken = arg.VarType;

                if (!argsAlreadySeen.Add(arg.VarIdent.Name))
                {
                    errorContainer.EnsureError(DocumentErrorSeverity.Severe, argNameToken, TexlStrings.ErrUDF_DuplicateParameter, argNameToken.Name);
                }

                if (argTypeToken.GetFormulaType()._type.Kind.Equals(DType.Unknown.Kind))
                {
                    errorContainer.EnsureError(DocumentErrorSeverity.Severe, argTypeToken, TexlStrings.ErrUDF_UnknownType, argTypeToken.Name);
                }
            }
        }
    }
}
