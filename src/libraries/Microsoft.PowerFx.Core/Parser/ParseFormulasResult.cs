// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Parser
{
    internal class ParseFormulasResult
    {
        internal IEnumerable<KeyValuePair<IdentToken, TexlNode>> NamedFormulas { get; }

        internal IEnumerable<TexlError> Errors { get; }

        internal bool HasError { get; }

        public ParseFormulasResult(IEnumerable<KeyValuePair<IdentToken, TexlNode>> namedFormulas, List<TexlError> errors)
        {
            Contracts.AssertValue(namedFormulas);

            if (errors?.Any() ?? false)
            {
                Errors = errors;
                HasError = true;
            }

            NamedFormulas = namedFormulas;
        }
    }

    internal class ParseUDFsResult
    {
        internal IEnumerable<UDF> UDFs { get; }

        internal List<TexlError> Errors { get; }

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

        public IEnumerable<ExpressionError> ExpErrors => ExpressionError.New(Errors);
    }

    internal class UDF
    {
        internal IdentToken Ident { get; }

        internal IdentToken ReturnType { get; }
        
        internal TexlNode Body { get; }

        internal ISet<Arg> Args { get; }

        public UDF(IdentToken ident, IdentToken returnType, HashSet<Arg> args, TexlNode body)
        {
            Ident = ident;
            ReturnType = returnType;
            Args = args;
            Body = body;
        }

        public override string ToString()
        {
            var str = $"ident: {Ident}, body: {Body}, returnType: {ReturnType}, args: [";
            foreach (var arg in Args)
            {
                str += $"{arg},";
            }

            return str += "]";
        }
    }

    internal class Arg
    {
        internal IdentToken _varIdent;
        internal IdentToken _varType;

        public Arg(IdentToken varIdent, IdentToken varType)
        {
            _varIdent = varIdent;
            _varType = varType;
        }

        public override string ToString()
        {
            return $"ident: {_varIdent}, type: {_varType}";
        }
    }
}
