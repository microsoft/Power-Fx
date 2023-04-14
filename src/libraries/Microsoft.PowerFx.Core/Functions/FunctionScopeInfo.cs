// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Functions
{
    /// <summary>
    /// Class holding behavior informaion for all functions "with scope",
    /// i.e. that take lambda parameters. For example: Filter, First,
    /// Sort, etc.
    /// </summary>
    internal class FunctionScopeInfo
    {
        protected TexlFunction _function;

        /// <summary>
        /// True if the function uses potentially all the fields in each row to produce
        /// the final result, or false otherwise.
        /// For example, Filter uses all the fields, and produces a value that depends
        /// on all the fields. So does AddColumns, DropColumns, etc.
        /// However, Sum/Min/Max/Average/etc use only the fields specified in predicates.
        /// </summary>
        public bool UsesAllFieldsInScope { get; }

        /// <summary>
        /// True if the function supports async lambdas, or false otherwise.
        /// </summary>
        public bool SupportsAsyncLambdas { get; }

        /// <summary>
        /// If false, the author will be warned when inputting predicates that
        /// do not reference the input table.
        /// </summary>
        public bool AcceptsLiteralPredicates { get; }

        /// <summary>
        /// True indicates that the function performs some sort of iteration over
        /// the scope data source, applying the lambda. This is used to determine what
        /// default behavior to block (such as refusing lambdas that modify the scope).
        /// </summary>
        public bool IteratesOverScope { get; }

        /// <summary>
        /// Null if this is a row scope, but if it's a constant row scope this will
        /// be the constant scope of the function.
        /// </summary>
        public DType ScopeType { get; }

        public Func<int, bool> AppliesToArgument { get; }

        // True indicates that this function cannot guarantee that it will iterate over the datasource in order.
        // This means it should not allow lambdas that operate on the same data multiple times, as this will
        // cause nondeterministic behavior.
        public bool HasNondeterministicOperationOrder => IteratesOverScope && SupportsAsyncLambdas;

        public FunctionScopeInfo(TexlFunction function, bool usesAllFieldsInScope = true, bool supportsAsyncLambdas = true, bool acceptsLiteralPredicates = true, bool iteratesOverScope = true, DType scopeType = null, Func<int, bool> appliesToArgument = null)
        {
            UsesAllFieldsInScope = usesAllFieldsInScope;
            SupportsAsyncLambdas = supportsAsyncLambdas;
            AcceptsLiteralPredicates = acceptsLiteralPredicates;
            IteratesOverScope = iteratesOverScope;
            ScopeType = scopeType;
            _function = function;
            AppliesToArgument = appliesToArgument ?? (i => i > 0);
        }

        // Typecheck an input for this function, and get the cursor type for an invocation with that input.
        // arg0 and arg0Type correspond to the input and its type.
        // The cursor type for aggregate functions is generally the type of a row in the input schema (table),
        // for example Table in an invocation Average(Table, valueFunction).
        // Returns true on success, false if the input or its type are invalid with respect to this function's declaration
        // (and populate the error container accordingly).
        public virtual bool CheckInput(TexlNode inputNode, DType inputSchema, IErrorContainer errors, out DType typeScope)
        {
            Contracts.AssertValue(inputNode);
            Contracts.Assert(inputSchema.IsValid);
            Contracts.AssertValue(errors);

            var callNode = inputNode.Parent.CastList().Parent.CastCall();
            Contracts.AssertValue(callNode);

            typeScope = inputSchema;

            var fArgsValid = true;

            if (_function.ParamTypes.Length == 0)
            {
                switch (typeScope.Kind)
                {
                    case DKind.Record:
                        break;
                    case DKind.Error:
                        fArgsValid = false;
                        errors.EnsureError(inputNode, TexlStrings.ErrBadType);
                        break;
                    default:
                        fArgsValid = false;
                        errors.Error(callNode, TexlStrings.ErrBadType);
                        break;
                }
            }
            else if (_function.ParamTypes[0].IsTable)
            {
                if (!typeScope.IsTable)
                {
                    errors.Error(callNode, TexlStrings.ErrNeedTable_Func, _function.Name);
                    fArgsValid = false;
                }

                // This assumes that the lambdas operate on the individual records
                // of the table, not the entire table.
                var fError = false;
                typeScope = typeScope.ToRecord(ref fError);
                fArgsValid &= !fError;
            }
            else if (_function.ParamTypes[0].IsUntypedObject)
            {
                typeScope = DType.UntypedObject;
            }
            else
            {
                Contracts.Assert(_function.ParamTypes[0].IsRecord);
                if (!typeScope.IsRecord)
                {
                    errors.Error(callNode, TexlStrings.ErrNeedRecord_Func, _function.Name);
                    var fError = false;
                    typeScope = typeScope.ToRecord(ref fError);
                    fArgsValid = false;
                }
            }

            return fArgsValid;
        }

        // Same as the virtual overload, however all typechecks are done quietly, without posting document errors.
        public virtual bool CheckInput(TexlNode inputNode, DType inputSchema, out DType typeScope)
        {
            return CheckInput(inputNode, inputSchema, TexlFunction.DefaultErrorContainer, out typeScope);
        }

        public void CheckLiteralPredicates(TexlNode[] args, IErrorContainer errors)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(errors);

            if (!AcceptsLiteralPredicates)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    if (_function.IsLambdaParam(i))
                    {
                        if (args[i].Kind == NodeKind.BoolLit ||
                            args[i].Kind == NodeKind.NumLit ||
                            args[i].Kind == NodeKind.DecLit ||
                            args[i].Kind == NodeKind.StrLit)
                        {
                            errors.EnsureError(DocumentErrorSeverity.Warning, args[i], TexlStrings.WarnLiteralPredicate);
                        }
                    }
                }
            }
        }
    }
}
