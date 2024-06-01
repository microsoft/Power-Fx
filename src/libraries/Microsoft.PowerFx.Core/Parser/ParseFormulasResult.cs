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

        /// <summary>
        /// Represents ':' token before return type in a UDF.
        /// </summary>
        internal Token ReturnTypeColonToken { get; }

        internal IdentToken ReturnType { get; }
        
        internal TexlNode Body { get; }

        internal ISet<UDFArg> Args { get; }

        internal bool IsImperative { get; }

        internal bool NumberIsFloat { get; }

        /// <summary>
        /// False if UDF is incomplete eg: Add(a: Number, b: Number): .
        /// </summary>
        internal bool IsParseValid { get; }

        public UDF(IdentToken ident, Token colonToken, IdentToken returnType, HashSet<UDFArg> args, TexlNode body, bool isImperative, bool numberIsFloat, bool isValid)
        {
            Ident = ident;
            ReturnType = returnType;
            Args = args;
            Body = body;
            IsImperative = isImperative;
            NumberIsFloat = numberIsFloat;
            IsParseValid = isValid;
            ReturnTypeColonToken = colonToken;
        }
    }

    internal class DefinedType
    {
        internal IdentToken Ident { get; }

        internal TypeLiteralNode Type { get; }

        internal bool IsParseValid { get; }

        public DefinedType(IdentToken ident, TypeLiteralNode type, bool isParseValid)
        {
            Ident = ident;
            Type = type;
            IsParseValid = isParseValid;
        }
    }

    internal class UDFArg
    {
        internal IdentToken NameIdent;

        internal IdentToken TypeIdent;

        /// <summary>
        /// Represents ':' token before param type in a UDF.
        /// </summary>
        internal Token ColonToken { get; }

        internal int ArgIndex;

        public UDFArg(IdentToken nameIdent, IdentToken typeIdent, Token colonToken, int argIndex)
        {
            NameIdent = nameIdent;
            TypeIdent = typeIdent;
            ArgIndex = argIndex;
            ColonToken = colonToken;
        } 
    }
}
