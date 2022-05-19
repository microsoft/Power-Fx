// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// Various IntelliSense-like operations.
    /// </summary>
    public class IntellisenseOperations
    {
        private readonly CheckResult _checkResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntellisenseOperations"/> class.
        /// </summary>
        /// <param name="result"></param>
        public IntellisenseOperations(CheckResult result)
        {
            _checkResult = result;
        }

        /// <summary>
        /// Checks whether a call to a function with name <paramref name="functionName" /> is valid with argument list
        /// <paramref name="args" />. Additionally returns (as an out parameter) the return type of this invocation.
        /// 
        /// Note: all arguments must belong to the formula that belongs to this <see cref="CheckResult" />.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="args"></param>
        /// <param name="retType"></param>
        /// <returns></returns>
        public bool ValidateInvocation(string functionName, IReadOnlyList<TexlNode> args, out FormulaType retType)
        {
            retType = null;

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            foreach ((var arg, var index) in args.Select((value, index) => (value, index)))
            {
                if (arg == null)
                {
                    throw new ArgumentNullException(nameof(args), $"Argument {index} is null");
                }

                if (!_checkResult._binding.IsNodeValid(arg))
                {
                    throw new ArgumentException($"Argument {index} does not belong to this result");
                }
            }

            var types = args.Select(node => _checkResult._binding.GetType(node)).ToArray();

            if (!TryParseFunctionNameWithNamespace(functionName, out var fncIdent))
            {
                return false;
            }

            // Note: there could be multiple functions (e.g., overloads) with the same name and arity,
            //  hence loop through candidates and check whether one of them matches.
            var fncs = _checkResult._binding.NameResolver.Functions
                                   .Where(fnc => fnc.Name == fncIdent.Name && fnc.Namespace == fncIdent.Namespace
                                                    && args.Count >= fnc.MinArity && args.Count <= fnc.MaxArity);
            foreach (var fnc in fncs)
            {
                var result =
                    fnc.CheckInvocation(
                        _checkResult._binding,
                        args.ToArray(),
                        types,
                        _checkResult._binding.ErrorContainer,
                        out var retDType,
                        out _);

                if (result)
                {
                    retType = FormulaType.Build(retDType);
                    return true;
                }
            }

            return false;
        }

        internal static bool TryParseFunctionNameWithNamespace(string functionName, out Identifier ident)
        {
            ident = null;
            var parseResult = TexlParser.ParseScript($"{functionName}()");
            if (!parseResult.IsSuccess)
            {
                return false;
            }

            ident = parseResult.Root.AsCall()?.Head;
            return ident != null;
        }
    }
}
