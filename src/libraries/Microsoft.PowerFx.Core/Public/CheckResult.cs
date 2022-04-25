// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// Result of binding an expression. 
    /// </summary>
    public class CheckResult : IOperationStatus
    {
        /// <summary> 
        /// Return type of the expression. Null if type can't be determined. 
        /// </summary>
        public FormulaType ReturnType { get; set; }

        /// <summary>
        /// Names of fields that this formula uses. 
        /// null if unavailable.  
        /// This is only valid when <see cref="IsSuccess"/> is true.
        /// </summary>
        public HashSet<string> TopLevelIdentifiers { get; set; }

        /// <summary>
        /// List of errors and warnings. Check <see cref="ExpressionError.IsWarning"/>.
        /// Not null, but empty on success.
        /// </summary>
        public IEnumerable<ExpressionError> Errors { get; set; }

        private IEnumerable<ExpressionError> BindingErrors => ExpressionError.New(_binding.ErrorContainer.GetErrors());

        internal void SetErrors(IEnumerable<IDocumentError> errors)
        {
            Errors = ExpressionError.New(errors);
        }

        /// <summary>
        /// Parsed expression for evaluation. 
        /// Null on failure or if there is no evaluation. 
        /// </summary>
        public IExpression Expression { get; set; }

        /// <summary>
        /// True if no errors. 
        /// </summary>
        public bool IsSuccess => !Errors.Any(x => !x.IsWarning);

        internal TexlBinding _binding;

        /// <summary>
        /// Results from parsing. 
        /// </summary>
        public ParseResult Parse { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckResult"/> class.
        /// </summary>
        public CheckResult()
        {
        }

        internal CheckResult(ParseResult parse, TexlBinding binding = null)
        {
            Parse = parse ?? throw new ArgumentNullException(nameof(parse));
           
            _binding = binding;

            Errors = Parse.Errors.Concat(BindingErrors);
        }

        public void ThrowOnErrors()
        {
            if (!IsSuccess)
            {
                var msg = string.Join("\r\n", Errors.Select(err => err.ToString()).ToArray());
                throw new InvalidOperationException($"Errors: " + msg);
            }
        }

        /// <summary>
        /// Gets the type of a syntax node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public FormulaType GetNodeType(TexlNode node) => FormulaType.Build(_binding.GetType(node));

        /// <summary>
        /// Whether the node is a constant node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool IsNodeConstant(TexlNode node) => _binding.IsConstant(node);

        /// <summary>
        /// Check whether function invocation in type-correct.
        /// </summary>
        /// <param name="fnc"></param>
        /// <param name="args"></param>
        /// <param name="retType"></param>
        /// <returns></returns>
        public bool CheckInvocation(FunctionInfo fnc, IReadOnlyList<TexlNode> args, out FormulaType retType)
        {
            var argsArr = args.ToArray();
            var typesArr = args.Select(node => GetNodeType(node)._type).ToArray();
            var res = fnc._fnc.CheckInvocation(argsArr, typesArr, _binding.ErrorContainer, out var retDType, out _);

            if (res)
            {
                retType = FormulaType.Build(retDType);
            }
            else
            {
                retType = null;
            }

            return res;
        }

        internal IReadOnlyDictionary<string, TokenResultType> GetTokens(GetTokensFlags flags) => GetTokensUtils.GetTokens(_binding, flags);
    }

    // Internal interface to ensure that Result objects have a common contract
    // for error reporting. 
    internal interface IOperationStatus
    {
        public IEnumerable<ExpressionError> Errors { get; }

        public bool IsSuccess { get; }
    }
}
