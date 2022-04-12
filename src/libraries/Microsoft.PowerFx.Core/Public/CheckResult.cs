// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// Result of checking an expression. 
    /// </summary>
    public class CheckResult
    {
        // Null if type can't be determined. 
        public FormulaType ReturnType { get; set; }

        /// <summary>
        /// Names of fields that this formula uses. 
        /// null if unavailable.  
        /// This is only valid if there are no errors. 
        /// </summary>
        public HashSet<string> TopLevelIdentifiers { get; set; }

        /// <summary>
        /// null on success, else contains errors. 
        /// </summary>
        public ExpressionError[] Errors { get; set; }

        /// <summary>
        /// Parsed expression, or null if IsSuccess is false.
        /// </summary>
        public IExpression Expression { get; set; }

        /// <summary>
        /// Syntax node of the parsed expression.
        /// </summary>
        public TexlNode SyntaxNode => _formula?.ParseTree;

        public virtual bool IsSuccess => Errors == null;

        internal TexlBinding _binding;

        internal Formula _formula;

        public CheckResult()
        {
        }

        internal CheckResult(IEnumerable<IDocumentError> errors, TexlBinding binding = null)
        {
            _binding = binding;
            SetErrors(errors);
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
        /// <param name="nodes"></param>
        /// <param name="types"></param>
        /// <param name="retType"></param>
        /// <returns></returns>
        public bool CheckInvocation(FunctionInfo fnc, IReadOnlyList<TexlNode> nodes, IReadOnlyList<FormulaType> types, out FormulaType retType)
        {
            var nodesArr = nodes.ToArray();
            var typesArr = types.Select(t => t._type).ToArray();
            var res = fnc._fnc.CheckInvocation(_binding, nodesArr, typesArr, _binding.ErrorContainer, out var retDType, out _);

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

        internal CheckResult SetErrors(IEnumerable<IDocumentError> errors)
        {
            Errors = errors.Select(x => new ExpressionError
            {
                Message = x.ShortMessage,
                Span = x.TextSpan,
                Severity = x.Severity,
                MessageKey = x.MessageKey
            }).ToArray();

            if (Errors.Length == 0)
            {
                Errors = null;
            }

            return this;
        }
    }
}
