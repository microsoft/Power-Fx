// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
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
        public bool CheckPredicateUsage { get; }

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

        /// <summary>
        /// Indicates whether a new scope can be created by a record,
        /// instead of just by a table, which is the typical case for functions
        /// that take a table as a first argument.
        /// </summary>
        public bool CanBeCreatedByRecord { get; }

        // True indicates that this function cannot guarantee that it will iterate over the datasource in order.
        // This means it should not allow lambdas that operate on the same data multiple times, as this will
        // cause nondeterministic behavior.
        public bool HasNondeterministicOperationOrder => IteratesOverScope && SupportsAsyncLambdas;

        public FunctionScopeInfo(
            TexlFunction function,
            bool usesAllFieldsInScope = true,
            bool supportsAsyncLambdas = true,
            bool checkPredicateUsage = false,
            bool iteratesOverScope = true,
            DType scopeType = null,
            Func<int, bool> appliesToArgument = null,
            bool canBeCreatedByRecord = false)
        {
            UsesAllFieldsInScope = usesAllFieldsInScope;
            SupportsAsyncLambdas = supportsAsyncLambdas;
            CheckPredicateUsage = checkPredicateUsage;
            IteratesOverScope = iteratesOverScope;
            ScopeType = scopeType;
            _function = function;
            AppliesToArgument = appliesToArgument ?? (i => i > 0);
            CanBeCreatedByRecord = canBeCreatedByRecord;
        }

        /// <summary>
        /// Allows to type check multiple scopes.
        /// </summary>
        /// <param name="features">Features flags.</param>
        /// <param name="callNode">Caller call node.</param>
        /// <param name="inputNodes">ArgN node.</param>
        /// <param name="typeScope">Calculated DType type.</param>
        /// <param name="errors"></param>
        /// <param name="inputSchema">List of data sources to compose the calculated type.</param>
        /// <returns></returns>
        public virtual bool CheckInput(Features features, CallNode callNode, TexlNode[] inputNodes, out DType typeScope, params DType[] inputSchema)
        {
            return CheckInput(features, callNode, inputNodes[0], inputSchema[0], out typeScope);
        }

        // Typecheck an input for this function, and get the cursor type for an invocation with that input.
        // arg0 and arg0Type correspond to the input and its type.
        // The cursor type for aggregate functions is generally the type of a row in the input schema (table),
        // for example Table in an invocation Average(Table, valueFunction).
        // Returns true on success, false if the input or its type are invalid with respect to this function's declaration
        // (and populate the error container accordingly).
        public virtual bool CheckInput(Features features, TexlNode inputNode, DType inputSchema, IErrorContainer errors, out DType typeScope)
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
                bool isBadArgumentType = false;
                if (typeScope.IsRecordNonObjNull)
                {
                    isBadArgumentType = !CanBeCreatedByRecord;
                }
                else
                {
                    isBadArgumentType =
                        features.PowerFxV1CompatibilityRules ?
                            !typeScope.IsTableNonObjNull : // Untyped blank values should not be used to define the scope
                            !typeScope.IsTable;
                }

                if (isBadArgumentType)
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
        public virtual bool CheckInput(Features features, TexlNode inputNode, DType inputSchema, out DType typeScope)
        {
            return CheckInput(features, inputNode, inputSchema, TexlFunction.DefaultErrorContainer, out typeScope);
        }

        public virtual bool CheckInput(Features features, CallNode callNode, TexlNode inputNode, DType inputSchema, out DType typeScope)
        {
            return CheckInput(features, inputNode, inputSchema, TexlFunction.DefaultErrorContainer, out typeScope);
        }

        /// <summary>
        /// Get the scope identifiers for the function based on the CallNode child args.
        /// The majority of the functions will have a single scope identifier named ThisRecord.
        /// Other functions, like Join, may create 2 or more scope identifiers.
        /// </summary>
        /// <param name="nodes">Call child nodes.</param>
        /// <param name="scopeIdents">Scope names.</param>
        /// <returns></returns>
        public virtual bool GetScopeIdent(TexlNode[] nodes, out DName[] scopeIdents)
        {
            scopeIdents = new[] { TexlBinding.ThisRecordDefaultName };
            if (nodes[0] is AsNode asNode)
            {
                scopeIdents = new[] { asNode.Right.Name };
                return true;
            }

            return false;
        }

        public virtual void CheckPredicateFields(DType fields, CallNode callNode, IEnumerable<FirstNameInfo> lambdaNames, ErrorContainer errors)
        {
            if (fields == DType.Error || fields.GetAllNames(DPath.Root).Any())
            {
                return;
            }

            GetScopeIdent(callNode.Args.ChildNodes.ToArray(), out var idents);

            if (!lambdaNames.Any(lambdaName => idents.Contains(lambdaName.Name)))
            {
                errors.EnsureError(DocumentErrorSeverity.Warning, callNode, TexlStrings.WarnCheckPredicateUsage);
            }
        }
    }

    internal class FunctionThisGroupScopeInfo : FunctionScopeInfo
    {
        public static DName ThisGroup => new DName("ThisGroup");

        public FunctionThisGroupScopeInfo(TexlFunction function)
            : base(function, appliesToArgument: (argIndex) => argIndex > 0)
        {
        }

        public override bool CheckInput(Features features, CallNode callNode, TexlNode inputNode, DType inputSchema, out DType typeScope)
        {
            var ret = base.CheckInput(features, inputNode, inputSchema, out typeScope);
            var newTypeScope = DType.EmptyRecord;

            foreach (var node in callNode.Args.ChildNodes)
            {
                string name = null;

                switch (node)
                {
                    case FirstNameNode firstNameNode:
                        name = firstNameNode.Ident.Name.Value;
                        break;

                    default:
                        continue;
                }

                if (typeScope.Contains(new DName(name)) && !newTypeScope.Contains(new DName(name)) && typeScope.TryGetType(new DName(name), out DType type))
                {
                    newTypeScope = newTypeScope.Add(new DName(name), type);
                }
            }

            typeScope = newTypeScope;

            if (!typeScope.Contains(ThisGroup))
            {
                typeScope = typeScope.Add(new TypedName(inputSchema.ToTable(), ThisGroup));
            }

            return ret;
        }
    }

    internal class FunctionJoinScopeInfo : FunctionScopeInfo
    {
        public static DName LeftRecord => new DName("LeftRecord");

        public static DName RightRecord => new DName("RightRecord");

        public FunctionJoinScopeInfo(TexlFunction function)
            : base(function, appliesToArgument: (argIndex) => argIndex > 1, checkPredicateUsage: true)
        {
        }

        public override bool CheckInput(Features features, CallNode callNode, TexlNode[] inputNodes, out DType typeScope, params DType[] inputSchema)
        {
            var ret = true;
            var argCount = Math.Min(inputNodes.Length, 2);

            typeScope = DType.EmptyRecord;

            GetScopeIdent(inputNodes, out DName[] idents);

            for (var i = 0; i < argCount; i++)
            {
                ret &= base.CheckInput(features, callNode, inputNodes[i], inputSchema[i], out var type);
                typeScope = typeScope.Add(idents[i], type);
            }

            return ret;
        }

        public override bool GetScopeIdent(TexlNode[] nodes, out DName[] scopeIdents)
        {
            scopeIdents = new DName[2];

            if (nodes.Length > 0 && nodes[0] is AsNode leftAsNode)
            {
                scopeIdents[0] = leftAsNode.Right.Name;
            }
            else
            {
                scopeIdents[0] = LeftRecord;
            }

            if (nodes.Length > 1 && nodes[1] is AsNode rightAsNode)
            {
                scopeIdents[1] = rightAsNode.Right.Name;
            }
            else
            {
                scopeIdents[1] = RightRecord;
            }

            // Returning false to indicate that the scope is not a whole scope.
            // Meaning that the scope is a record type and we are accessing the fields directly.
            return false;
        }

        public override void CheckPredicateFields(DType fields, CallNode callNode, IEnumerable<FirstNameInfo> lambdaNames, ErrorContainer errors)
        {
            // If Join call node has less than 5 records, we are possibly looking for suggestions.
            if (callNode.Args.ChildNodes.Count < 5)
            {
                return;
            }

            GetScopeIdent(callNode.Args.ChildNodes.ToArray(), out var idents);

            var foundIdents = new HashSet<DName>();
            var predicate = callNode.Args.ChildNodes[2];

            // In the Join function, arg2 and argN > 3 are lambdas nodes.
            // We need to check if scope identifiers are used arg2 (predicate).
            foreach (var lambda in lambdaNames)
            {
                var parent = lambda.Node.Parent;

                while (parent != null)
                {
                    if (parent.Id == predicate.Id)
                    {
                        foundIdents.Add(lambda.Name);
                        break;
                    }

                    parent = parent.Parent;
                }
            }

            var foundIdentsArray = foundIdents.ToArray();

            if (foundIdents.Count == 2 && 
                fields.TryGetType(foundIdentsArray[0], out _) && 
                fields.TryGetType(foundIdentsArray[1], out _))
            {                
                return;
            }

            errors.EnsureError(DocumentErrorSeverity.Warning, callNode, TexlStrings.WarnCheckPredicateUsage);
        }
    }
}
