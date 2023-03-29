// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Parser
{
    public class ParseFormulasResult
    {
        public IEnumerable<KeyValuePair<IdentToken, TexlNode>> NamedFormulas { get; }

        internal IEnumerable<TexlError> Errors { get; }

        // Expose errors publicly
        public IEnumerable<ExpressionError> ExpressionErrors => ExpressionError.New(this.Errors, null);

        public bool HasError { get; }

        internal ParseFormulasResult(IEnumerable<KeyValuePair<IdentToken, TexlNode>> namedFormulas, List<TexlError> errors)
        {
            Contracts.AssertValue(namedFormulas);

            if (errors?.Any() ?? false)
            {
                Errors = errors;
                HasError = true;
            }

            NamedFormulas = namedFormulas;
        }

        [Obsolete("Use unified UDF parser")]
        public static ParseFormulasResult ParseFormulasScript(string script, CultureInfo loc = null)
        {
            return TexlParser.ParseFormulasScript(script, loc);
        }
    }

    internal class ParseUDFsResult
    {
        internal IEnumerable<UDF> UDFs { get; }

        internal bool HasError { get; }

        public ParseUDFsResult(List<UDF> uDFs, List<TexlError> errors)
        {
            Contracts.AssertValue(uDFs);

            if (errors?.Any() ?? false)
            {
                Errors = errors;
                HasError = true;
            }

            UDFs = uDFs;
        }

        internal IEnumerable<TexlError> Errors;

        public IEnumerable<ExpressionError> ExpErrors => ExpressionError.New(Errors, CultureInfo.InvariantCulture);
    }

    internal class UDF
    {
        internal IdentToken Ident { get; }

        internal IdentToken ReturnType { get; }
        
        internal TexlNode Body { get; }

        internal ISet<UDFArg> Args { get; }

        internal bool IsImperative { get; }

        public UDF(IdentToken ident, IdentToken returnType, HashSet<UDFArg> args, TexlNode body, bool isImperative)
        {
            Ident = ident;
            ReturnType = returnType;
            Args = args;
            Body = body;
            IsImperative = isImperative;
        }
    }

    internal class UDFArg
    {
        internal IdentToken VarIdent;
        internal IdentToken VarType;

        public UDFArg(IdentToken varIdent, IdentToken varType)
        {
            VarIdent = varIdent;
            VarType = varType;
        } 
    }
}
