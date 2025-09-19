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
    /// <summary>
    /// Represents the result of parsing formulas, including named formulas and errors.
    /// </summary>
    public class ParseFormulasResult
    {
        /// <summary>
        /// Gets the collection of named formulas parsed from the script.
        /// </summary>
        public IEnumerable<KeyValuePair<IdentToken, TexlNode>> NamedFormulas { get; }

        internal IEnumerable<TexlError> Errors { get; }

        /// <summary>
        /// Gets the collection of expression errors encountered during parsing.
        /// </summary>
        public IEnumerable<ExpressionError> ExpressionErrors => ExpressionError.New(this.Errors, null);

        /// <summary>
        /// Gets a value indicating whether any errors were encountered during parsing.
        /// </summary>
        public bool HasError { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseFormulasResult"/> class with the specified named formulas and errors.
        /// </summary>
        /// <param name="namedFormulas">The collection of named formulas parsed from the script.</param>
        /// <param name="errors">The list of errors encountered during parsing.</param>
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

        /// <summary>
        /// Parses a formulas script and returns the result. (Obsolete: Use unified UDF parser.)
        /// </summary>
        /// <param name="script">The formulas script to parse.</param>
        /// <param name="loc">The culture info to use for parsing. Optional.</param> 
        /// <returns>The result of parsing the formulas script.</returns>
        [Obsolete("Use unified UDF parser")]
        public static ParseFormulasResult ParseFormulasScript(string script, CultureInfo loc = null)
        {
            return TexlParser.ParseFormulasScript(script, loc);
        }

        /// <summary>
        /// Parses a formulas script and returns the result. (Obsolete: Use unified UDF parser.)
        /// </summary>
        /// <param name="script">The formulas script to parse.</param>
        /// <param name="parserOptions">Parser options, including culture, to use for parsing.</param>
        /// <returns>The result of parsing the formulas script.</returns>
        [Obsolete("Use unified UDF parser")]
        public static ParseFormulasResult ParseFormulasScript(string script, ParserOptions parserOptions)
        {
            return TexlParser.ParseFormulasScript(script, parserOptions);
        }
    }

    internal class ParseUDFsResult
    {
        /// <summary>
        /// Gets the collection of UDFs parsed from the script.
        /// </summary>
        internal IEnumerable<UDF> UDFs { get; }

        /// <summary>
        /// Gets a value indicating whether any errors were encountered during parsing.
        /// </summary>
        internal bool HasError { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseUDFsResult"/> class with the specified UDFs and errors.
        /// </summary>
        /// <param name="uDFs">The collection of UDFs parsed from the script.</param>
        /// <param name="errors">The list of errors encountered during parsing.</param>
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

        /// <summary>
        /// Gets the collection of errors encountered during parsing.
        /// </summary>
        internal IEnumerable<TexlError> Errors;

        /// <summary>
        /// Gets the collection of expression errors encountered during parsing.
        /// </summary>
        public IEnumerable<ExpressionError> ExpErrors => ExpressionError.New(Errors, CultureInfo.InvariantCulture);
    }

    internal class UDF
    {
        /// <summary>
        /// Gets the identifier token of the UDF.
        /// </summary>
        internal IdentToken Ident { get; }

        /// <summary>
        /// Represents ':' token before return type in a UDF.
        /// </summary>
        internal Token ReturnTypeColonToken { get; }

        /// <summary>
        /// Gets the return type identifier token of the UDF.
        /// </summary>
        internal IdentToken ReturnType { get; }

        /// <summary>
        /// Gets the body of the UDF.
        /// </summary>
        internal TexlNode Body { get; }

        /// <summary>
        /// Gets the set of arguments of the UDF.
        /// </summary>
        internal ISet<UDFArg> Args { get; }

        /// <summary>
        /// Gets a value indicating whether the UDF is imperative.
        /// </summary>
        internal bool IsImperative { get; }

        /// <summary>
        /// Gets a value indicating whether numbers are treated as floats in the UDF.
        /// </summary>
        internal bool NumberIsFloat { get; }

        /// <summary>
        /// Gets a value indicating whether the UDF is parse valid.
        /// </summary>
        internal bool IsParseValid { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UDF"/> class with the specified properties.
        /// </summary>
        /// <param name="ident">The identifier token of the UDF.</param>
        /// <param name="colonToken">The ':' token before the return type.</param>
        /// <param name="returnType">The return type identifier token of the UDF.</param>
        /// <param name="args">The set of arguments of the UDF.</param>
        /// <param name="body">The body of the UDF.</param>
        /// <param name="isImperative">A value indicating whether the UDF is imperative.</param>
        /// <param name="numberIsFloat">A value indicating whether numbers are treated as floats in the UDF.</param>
        /// <param name="isValid">A value indicating whether the UDF is parse valid.</param>
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
        /// <summary>
        /// Gets the identifier token of the defined type.
        /// </summary>
        internal IdentToken Ident { get; }

        /// <summary>
        /// Gets the type literal node of the defined type.
        /// </summary>
        internal TypeLiteralNode Type { get; }

        /// <summary>
        /// Gets a value indicating whether the defined type is parse valid.
        /// </summary>
        internal bool IsParseValid { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefinedType"/> class with the specified properties.
        /// </summary>
        /// <param name="ident">The identifier token of the defined type.</param>
        /// <param name="type">The type literal node of the defined type.</param>
        /// <param name="isParseValid">A value indicating whether the defined type is parse valid.</param>
        public DefinedType(IdentToken ident, TypeLiteralNode type, bool isParseValid)
        {
            Ident = ident;
            Type = type;
            IsParseValid = isParseValid;
        }
    }

    internal class UDFArg
    {
        /// <summary>
        /// Gets the name identifier token of the UDF argument.
        /// </summary>
        internal IdentToken NameIdent;

        /// <summary>
        /// Gets the type identifier token of the UDF argument.
        /// </summary>
        internal IdentToken TypeIdent;

        /// <summary>
        /// Represents ':' token before param type in a UDF.
        /// </summary>
        internal Token ColonToken { get; }

        /// <summary>
        /// Gets the index of the UDF argument.
        /// </summary>
        internal int ArgIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="UDFArg"/> class with the specified properties.
        /// </summary>
        /// <param name="nameIdent">The name identifier token of the UDF argument.</param>
        /// <param name="typeIdent">The type identifier token of the UDF argument.</param>
        /// <param name="colonToken">The ':' token before the parameter type.</param>
        /// <param name="argIndex">The index of the UDF argument.</param>
        public UDFArg(IdentToken nameIdent, IdentToken typeIdent, Token colonToken, int argIndex)
        {
            NameIdent = nameIdent;
            TypeIdent = typeIdent;
            ArgIndex = argIndex;
            ColonToken = colonToken;
        }
    }
}
