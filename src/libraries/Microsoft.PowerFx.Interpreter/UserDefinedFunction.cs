// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal class UserDefinedFunction : CustomTexlFunction
    {
        public UserDefinedFunction(string name, FormulaType returnType, params FormulaType[] paramTypes) 
            : base(name, returnType._type, Array.ConvertAll(paramTypes, x => x._type))
        {
        }

        /// <summary>
        /// Perform sub-expression type checking and produce a return type for the function declaration, this is only applicable for UDFs.
        /// </summary>
        public virtual bool CheckTypesOnDeclaration(CheckTypesContext context, TexlNode[] args, DType[] argTypes, DType returnType, IErrorContainer errors, out DType actualReturnType)
        {
            actualReturnType = null;

            return false;
        }

        /// <summary>
        /// Perform expression-level semantics checks which require a binding, this is only applicable for UDFs.
        /// </summary>
        public virtual void CheckSemanticsOnDeclaration(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
        }
    }
}
