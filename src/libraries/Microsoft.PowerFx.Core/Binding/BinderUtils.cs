// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding
{
    internal class BinderUtils
    {
        internal static bool TryConvertNodeToDPath(TexlBinding binding, DottedNameNode node, out DPath path)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(node);

            if (node.Left is DottedNameNode && TryConvertNodeToDPath(binding, node.Left as DottedNameNode, out path))
            {
                var rightNodeName = node.Right.Name;
                if (binding.TryGetReplacedIdentName(node.Right, out var possibleRename))
                {
                    rightNodeName = new DName(possibleRename);
                }

                path = path.Append(rightNodeName);
                return true;
            }
            else if (node.Left is FirstNameNode firstName)
            {
                if (binding.GetInfo(firstName).Kind == BindKind.LambdaFullRecord)
                {
                    var rightNodeName = node.Right.Name;
                    if (binding.TryGetReplacedIdentName(node.Right, out var rename))
                    {
                        rightNodeName = new DName(rename);
                    }

                    path = DPath.Root.Append(rightNodeName);
                    return true;
                }

                // Check if the access was renamed:
                var leftNodeName = firstName.Ident.Name;
                if (binding.TryGetReplacedIdentName(firstName.Ident, out var possibleRename))
                {
                    leftNodeName = new DName(possibleRename);
                }

                path = DPath.Root.Append(leftNodeName).Append(node.Right.Name);
                return true;
            }

            path = DPath.Root;
            return false;
        }

        public static void LogTelemetryForFunction(TexlFunction function, CallNode node, TexlBinding texlBinding, bool isServerDelegatable)
        {
            Contracts.AssertValue(function);
            Contracts.AssertValue(node);
            Contracts.AssertValue(texlBinding);

            // We only want to log about successful delegation status here. Any failures should have been logged by this time.
            if (isServerDelegatable)
            {
                TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.DelegationSuccessful, node, texlBinding, function);
                return;
            }
        }

        /// <summary>
        /// Tries to get the best suited overload for <paramref name="node"/> according to <paramref name="txb"/> and
        /// returns true if it is found.
        /// </summary>
        /// <param name="txb">
        /// Binding that will help select the best overload.
        /// </param>
        /// <param name="node">
        /// CallNode for which the best overload will be determined.
        /// </param>
        /// <param name="argTypes">
        /// List of argument types for <paramref name="node.Args"/>.
        /// </param>
        /// <param name="overloads">
        /// All overloads for <paramref name="node"/>. An element of this list will be returned.
        /// </param>
        /// <param name="bestOverload">
        /// Set to the best overload when this method completes.
        /// </param>
        /// <param name="nodeToCoercedTypeMap">
        /// Set to the types to which <paramref name="node.Args"/> must be coerced in order for
        /// <paramref name="bestOverload"/> to be valid.
        /// </param>
        /// <param name="returnType">
        /// The return type for <paramref name="bestOverload"/>.
        /// </param>
        /// <returns>
        /// True if a valid overload was found, false if not.
        /// </returns>
        internal static bool TryGetBestOverload(TexlBinding txb, CallNode node, DType[] argTypes, TexlFunction[] overloads, out TexlFunction bestOverload, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap, out DType returnType)
        {
            Contracts.AssertValue(node, nameof(node));
            Contracts.AssertValue(overloads, nameof(overloads));

            var args = node.Args.Children;
            var carg = args.Length;
            returnType = DType.Unknown;

            TexlFunction matchingFuncWithCoercion = null;
            var matchingFuncWithCoercionReturnType = DType.Invalid;
            nodeToCoercedTypeMap = null;
            Dictionary<TexlNode, DType> matchingFuncWithCoercionNodeToCoercedTypeMap = null;

            foreach (var maybeFunc in overloads)
            {
                Contracts.Assert(!maybeFunc.HasLambdas);

                nodeToCoercedTypeMap = null;

                if (carg < maybeFunc.MinArity || carg > maybeFunc.MaxArity)
                {
                    continue;
                }

                var typeCheckSucceeded = false;

                IErrorContainer warnings = new LimitedSeverityErrorContainer(txb.ErrorContainer, DocumentErrorSeverity.Warning);

                // Typecheck the invocation and infer the return type.
                typeCheckSucceeded = maybeFunc.CheckInvocation(txb.BindingConfig, args, argTypes, warnings, out returnType, out nodeToCoercedTypeMap);

                if (typeCheckSucceeded)
                {
                    if (nodeToCoercedTypeMap == null)
                    {
                        // We found an overload that matches without type coercion.  The correct return type
                        // and, trivially, the nodeToCoercedTypeMap are properly set at this point.
                        bestOverload = maybeFunc;
                        return true;
                    }

                    // We found an overload that matches but with type coercion. Keep going
                    // until we find another overload that matches without type coercion.
                    // If we cannot find one, we will use this overload only if there is no other
                    // overload that involves fewer coercions.
                    if (matchingFuncWithCoercion == null || nodeToCoercedTypeMap.Count < matchingFuncWithCoercionNodeToCoercedTypeMap.VerifyValue().Count)
                    {
                        matchingFuncWithCoercionNodeToCoercedTypeMap = nodeToCoercedTypeMap;
                        matchingFuncWithCoercion = maybeFunc;
                        matchingFuncWithCoercionReturnType = returnType;
                    }
                }
            }

            // We've matched, but with coercion required.
            if (matchingFuncWithCoercionNodeToCoercedTypeMap != null)
            {
                bestOverload = matchingFuncWithCoercion;
                nodeToCoercedTypeMap = matchingFuncWithCoercionNodeToCoercedTypeMap;
                returnType = matchingFuncWithCoercionReturnType;
                return true;
            }

            // There are no good overloads
            bestOverload = null;
            nodeToCoercedTypeMap = null;
            returnType = null;
            return false;
        }

        /// <summary>
        /// Helper for Lt/leq/geq/gt type checking. Restricts type to be one of the provided set, without coercion (except for primary output props).
        /// </summary>
        /// <param name="errorContainer">Errors will be reported here.</param>
        /// <param name="node">Node for which we are checking the type.</param>
        /// <param name="type">The type for node.</param>
        /// <param name="alternateTypes">List of acceptable types for this operation, in order of suitability.</param>
        /// <returns></returns>
        internal static BinderCheckTypeResult CheckComparisonTypeOneOfCore(IErrorContainer errorContainer, TexlNode node, DType type, params DType[] alternateTypes)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(alternateTypes);
            Contracts.Assert(alternateTypes.Any());

            var coercions = new List<BinderCoercionResult>();

            foreach (var altType in alternateTypes)
            {
                if (!altType.Accepts(type))
                {
                    continue;
                }

                return new BinderCheckTypeResult();
            }

            // If the node is a control, we may be able to coerce its primary output property
            // to the desired type, and in the process support simplified syntax such as: slider2 <= slider4
            IExternalControlProperty primaryOutProp;
            if (type is IExternalControlType controlType && node.AsFirstName() != null && (primaryOutProp = controlType.ControlTemplate.PrimaryOutputProperty) != null)
            {
                var outType = primaryOutProp.GetOpaqueType();
                var acceptedType = alternateTypes.FirstOrDefault(alt => alt.Accepts(outType));
                if (acceptedType != default)
                {
                    // We'll coerce the control to the desired type, by pulling from the control's
                    // primary output property. See codegen for details.
                    coercions.Add(new BinderCoercionResult() { Node = node, CoercedType = acceptedType });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }
            }

            errorContainer.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrBadType_ExpectedTypesCSV, string.Join(", ", alternateTypes.Select(t => t.GetKindString())));
            return new BinderCheckTypeResult();
        }

        // Returns whether the node was of the type wanted, and reports appropriate errors.
        // A list of allowed alternate types specifies what other types of values can be coerced to the wanted type.
        private static BinderCheckTypeResult CheckTypeCore(IErrorContainer errorContainer, TexlNode node, DType nodeType, DType typeWant, params DType[] alternateTypes)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(typeWant.IsValid);
            Contracts.Assert(!typeWant.IsError);
            Contracts.AssertValue(alternateTypes);

            var coercions = new List<BinderCoercionResult>();

            if (typeWant.Accepts(nodeType))
            {
                if (nodeType.RequiresExplicitCast(typeWant))
                {
                    coercions.Add(new BinderCoercionResult() { Node = node, CoercedType = typeWant });
                }

                return new BinderCheckTypeResult() { Coercions = coercions };
            }

            // Normal (non-control) coercion
            foreach (var altType in alternateTypes)
            {
                if (!altType.Accepts(nodeType))
                {
                    continue;
                }

                // Ensure that booleans only match bool valued option sets
                if (typeWant.Kind == DKind.Boolean && altType.Kind == DKind.OptionSetValue && !(nodeType.OptionSetInfo?.IsBooleanValued ?? false))
                {
                    continue;
                }

                // We found an alternate type that is accepted and will be coerced.
                coercions.Add(new BinderCoercionResult() { Node = node, CoercedType = typeWant });
                return new BinderCheckTypeResult() { Coercions = coercions };
            }

            // If the node is a control, we may be able to coerce its primary output property
            // to the desired type, and in the process support simplified syntax such as: label1 + slider4
            IExternalControlProperty primaryOutProp;
            if (nodeType is IExternalControlType controlType && node.AsFirstName() != null && (primaryOutProp = controlType.ControlTemplate.PrimaryOutputProperty) != null)
            {
                var outType = primaryOutProp.GetOpaqueType();
                if (typeWant.Accepts(outType) || alternateTypes.Any(alt => alt.Accepts(outType)))
                {
                    // We'll "coerce" the control to the desired type, by pulling from the control's
                    // primary output property. See codegen for details.
                    coercions.Add(new BinderCoercionResult() { Node = node, CoercedType = typeWant });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }
            }

            var messageKey = alternateTypes.Length == 0 ? TexlStrings.ErrBadType_ExpectedType : TexlStrings.ErrBadType_ExpectedTypesCSV;
            var messageArg = alternateTypes.Length == 0 ? typeWant.GetKindString() : string.Join(", ", new[] { typeWant }.Concat(alternateTypes).Select(t => t.GetKindString()));

            errorContainer.EnsureError(DocumentErrorSeverity.Severe, node, messageKey, messageArg);
            return new BinderCheckTypeResult() { Coercions = coercions };
        }

        // Performs type checking for the arguments passed to the membership "in"/"exactin" operators.
        private static BinderCheckTypeResult CheckInArgTypesCore(IErrorContainer errorContainer, TexlNode left, TexlNode right, DType typeLeft, DType typeRight, bool isEnhancedDelegationEnabled)
        {
            Contracts.AssertValue(left);
            Contracts.AssertValue(right);

            var coercions = new List<BinderCoercionResult>();

            if (!typeLeft.IsValid || typeLeft.IsUnknown || typeLeft.IsError)
            {
                errorContainer.EnsureError(DocumentErrorSeverity.Severe, left, TexlStrings.ErrTypeError);
                return new BinderCheckTypeResult() { Coercions = coercions };
            }

            if (!typeRight.IsValid || typeRight.IsUnknown || typeRight.IsError)
            {
                errorContainer.EnsureError(DocumentErrorSeverity.Severe, right, TexlStrings.ErrTypeError);
                return new BinderCheckTypeResult() { Coercions = coercions };
            }

            Contracts.Assert(!typeLeft.IsAggregate || typeLeft.IsTable || typeLeft.IsRecord);
            Contracts.Assert(!typeRight.IsAggregate || typeRight.IsTable || typeRight.IsRecord);

            if (!typeLeft.IsAggregate)
            {
                // scalar in scalar: RHS must be a string (or coercible to string when LHS type is string). We'll allow coercion of LHS.
                // This case deals with substring matches, e.g. 'FirstName in "Aldous Huxley"' or "123" in 123.
                if (!typeRight.IsAggregate)
                {
                    if (!DType.String.Accepts(typeRight))
                    {
                        if (typeRight.CoercesTo(DType.String) && DType.String.Accepts(typeLeft))
                        {
                            // Coerce RHS to a string type.
                            coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.String });
                        }
                        else
                        {
                            errorContainer.EnsureError(DocumentErrorSeverity.Severe, right, TexlStrings.ErrStringExpected);
                            return new BinderCheckTypeResult() { Coercions = coercions };
                        }
                    }

                    if (DType.String.Accepts(typeLeft))
                    {
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    if (!typeLeft.CoercesTo(DType.String))
                    {
                        errorContainer.EnsureError(DocumentErrorSeverity.Severe, left, TexlStrings.ErrCannotCoerce_SourceType_TargetType, typeLeft.GetKindString(), DType.String.GetKindString());
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    // Coerce LHS to a string type, to facilitate subsequent substring checks.
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.String });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }

                // scalar in table: RHS must be a one column table. We'll allow coercion.
                if (typeRight.IsTable)
                {
                    var names = typeRight.GetNames(DPath.Root);
                    if (names.Count() != 1)
                    {
                        errorContainer.EnsureError(DocumentErrorSeverity.Severe, right, TexlStrings.ErrInvalidSchemaNeedCol);
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    var typedName = names.Single();
                    if (typedName.Type.Accepts(typeLeft) || typeLeft.Accepts(typedName.Type))
                    {
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    if (!typeLeft.CoercesTo(typedName.Type))
                    {
                        errorContainer.EnsureError(DocumentErrorSeverity.Severe, left, TexlStrings.ErrCannotCoerce_SourceType_TargetType, typeLeft.GetKindString(), typedName.Type.GetKindString());
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    // Coerce LHS to the table column type, to facilitate subsequent comparison.
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = typedName.Type });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }

                // scalar in record or multiSelectOptionSet table: not supported. Flag an error on the RHS.
                Contracts.Assert(typeRight.IsRecord);
                errorContainer.EnsureError(DocumentErrorSeverity.Severe, right, TexlStrings.ErrBadType_Type, typeRight.GetKindString());
                return new BinderCheckTypeResult() { Coercions = coercions };
            }

            if (typeLeft.IsRecord)
            {
                // record in scalar: not supported
                if (!typeRight.IsAggregate)
                {
                    errorContainer.EnsureError(DocumentErrorSeverity.Severe, right, TexlStrings.ErrBadType_Type, typeRight.GetKindString());
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }

                // record in table: RHS must be a table with a compatible schema. No coercion is allowed.
                if (typeRight.IsTable)
                {
                    var typeLeftAsTable = typeLeft.ToTable();

                    if (typeLeftAsTable.Accepts(typeRight, out var typeRightDifferingSchema, out var typeRightDifferingSchemaType) ||
                        typeRight.Accepts(typeLeftAsTable, out var typeLeftDifferingSchema, out var typeLeftDifferingSchemaType))
                    {
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    errorContainer.Errors(left, typeLeft, typeLeftDifferingSchema, typeLeftDifferingSchemaType);
                    errorContainer.Errors(right, typeRight, typeRightDifferingSchema, typeRightDifferingSchemaType);

                    return new BinderCheckTypeResult() { Coercions = coercions };
                }

                // record in record: not supported. Flag an error on the RHS.
                Contracts.Assert(typeRight.IsRecord);
                errorContainer.EnsureError(DocumentErrorSeverity.Severe, right, TexlStrings.ErrBadType_Type, typeRight.GetKindString());
                return new BinderCheckTypeResult() { Coercions = coercions };
            }

            if (isEnhancedDelegationEnabled && typeLeft.IsTable)
            {
                // Table in table: RHS must be a single column table with a compatible schema. No coercion is allowed.
                if (typeRight.IsTable)
                {
                    var names = typeRight.GetNames(DPath.Root);
                    if (names.Count() != 1)
                    {
                        errorContainer.EnsureError(DocumentErrorSeverity.Severe, right, TexlStrings.ErrInvalidSchemaNeedCol);
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    var typedName = names.Single();

                    // Ensure we error when RHS node of table type cannot be coerced to a multiselectOptionset table node.  
                    if (!typeRight.CoercesTo(typeLeft))
                    {
                        errorContainer.EnsureError(DocumentErrorSeverity.Severe, right, TexlStrings.ErrCannotCoerce_SourceType_TargetType, typeLeft.GetKindString(), typedName.Type.GetKindString());
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    // Check if multiselectoptionset column type accepts RHS node of type table. 
                    if (typeLeft.Accepts(typedName.Type))
                    {
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }
                }
            }

            // Table in scalar or Table in Record or Table in unsupported table: not supported
            errorContainer.EnsureError(DocumentErrorSeverity.Severe, left, TexlStrings.ErrBadType_Type, typeLeft.GetKindString());
            return new BinderCheckTypeResult() { Coercions = coercions };
        }

        private static BinderCheckTypeResult PostVisitBinaryOpNodeAdditionCore(IErrorContainer errorContainer, BinaryOpNode node, DType leftType, DType rightType)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(node.Op == BinaryOp.Add);

            var leftKind = leftType.Kind;
            var rightKind = rightType.Kind;

            BinderCheckTypeResult ReportInvalidOperation()
            {
                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    node,
                    TexlStrings.ErrBadOperatorTypes,
                    leftType.GetKindString(),
                    rightType.GetKindString());
                return new BinderCheckTypeResult() { Node = node, NodeType = DType.Error };
            }

            UnaryOpNode unary;

            switch (leftKind)
            {
                case DKind.DateTime:
                    switch (rightKind)
                    {
                        case DKind.DateTime:
                        case DKind.Date:
                            unary = node.Right.AsUnaryOpLit();
                            if (unary != null && unary.Op == UnaryOp.Minus)
                            {
                                // DateTime - DateTime = Number
                                // DateTime - Date = Number
                                return new BinderCheckTypeResult() { Node = node, NodeType = DType.Number };
                            }
                            else
                            {
                                // DateTime + DateTime in any other arrangement is an error
                                // DateTime + Date in any other arrangement is an error
                                return ReportInvalidOperation();
                            }

                        case DKind.Time:
                            return new BinderCheckTypeResult { Node = node, NodeType = DType.DateTime };

                        default:
                            // DateTime + number = DateTime
                            var resRight = CheckTypeCore(errorContainer, node.Right, rightType, DType.Number, /* coerced: */ DType.String, DType.Boolean);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.DateTime, Coercions = resRight.Coercions };
                    }

                case DKind.Date:
                    switch (rightKind)
                    {
                        case DKind.Date:
                            // Date + Date = number but ONLY if its really subtraction Date + '-Date'
                            unary = node.Right.AsUnaryOpLit();
                            if (unary != null && unary.Op == UnaryOp.Minus)
                            {
                                // Date - Date = Number
                                return new BinderCheckTypeResult() { Node = node, NodeType = DType.Number };
                            }
                            else
                            {
                                // Date + Date in any other arrangement is an error
                                return ReportInvalidOperation();
                            }

                        case DKind.Time:
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.DateTime };

                        case DKind.DateTime:
                            // Date + DateTime = number but ONLY if its really subtraction Date + '-DateTime'
                            unary = node.Right.AsUnaryOpLit();
                            if (unary != null && unary.Op == UnaryOp.Minus)
                            {
                                // Date - DateTime = Number
                                return new BinderCheckTypeResult() { Node = node, NodeType = DType.Number };
                            }
                            else
                            {
                                // Date + DateTime in any other arrangement is an error
                                return ReportInvalidOperation();
                            }

                        default:
                            // Date + number = Date
                            var resRight = CheckTypeCore(errorContainer, node.Right, rightType, DType.Number, /* coerced: */ DType.String, DType.Boolean);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.DateTime, Coercions = resRight.Coercions };
                    }

                case DKind.Time:
                    switch (rightKind)
                    {
                        case DKind.Time:
                            // Time + Time = number but ONLY if its really subtraction Time + '-Time'
                            unary = node.Right.AsUnaryOpLit();
                            if (unary != null && unary.Op == UnaryOp.Minus)
                            {
                                // Time - Time = Number
                                return new BinderCheckTypeResult() { Node = node, NodeType = DType.Number };
                            }
                            else
                            {
                                // Time + Time = Time
                                return new BinderCheckTypeResult() { Node = node, NodeType = DType.Time };
                            }

                        case DKind.Date:
                        case DKind.DateTime:
                            unary = node.Right.AsUnaryOpLit();
                            if (unary != null && unary.Op == UnaryOp.Minus)
                            {
                                // Time - Date[Time] is an error
                                return ReportInvalidOperation();
                            }
                            else
                            {
                                // Time + Date = DateTime
                                return new BinderCheckTypeResult() { Node = node, NodeType = DType.DateTime };
                            }

                        default:
                            // Time + number = Time
                            var resRight = CheckTypeCore(errorContainer, node.Right, rightType, DType.Number, /* coerced: */ DType.String, DType.Boolean);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Time, Coercions = resRight.Coercions };
                    }

                default:
                    switch (rightKind)
                    {
                        case DKind.DateTime:
                            // number + DateTime = DateTime
                            var leftResDateTime = CheckTypeCore(errorContainer, node.Left, leftType, DType.Number, /* coerced: */ DType.String, DType.Boolean);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.DateTime, Coercions = leftResDateTime.Coercions };
                        case DKind.Date:
                            // number + Date = Date
                            var leftResDate = CheckTypeCore(errorContainer, node.Left, leftType, DType.Number, /* coerced: */ DType.String, DType.Boolean);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.DateTime, Coercions = leftResDate.Coercions };
                        case DKind.Time:
                            // number + Time = Time
                            var leftResTime = CheckTypeCore(errorContainer, node.Left, leftType, DType.Number, /* coerced: */ DType.String, DType.Boolean);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Time, Coercions = leftResTime.Coercions };
                        default:
                            // Regular Addition
                            var leftResAdd = CheckTypeCore(errorContainer, node.Left, leftType, DType.Number, /* coerced: */ DType.String, DType.Boolean);
                            var rightResAdd = CheckTypeCore(errorContainer, node.Right, rightType, DType.Number, /* coerced: */ DType.String, DType.Boolean);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Number, Coercions = leftResAdd.Coercions.Concat(rightResAdd.Coercions).ToList() };
                    }
            }
        }

        private static BinderCheckTypeResult CheckComparisonArgTypesCore(IErrorContainer errorContainer, TexlNode left, TexlNode right, DType typeLeft, DType typeRight)
        {
            // Excel's type coercion for inequality operators is inconsistent / borderline wrong, so we can't
            // use it as a reference. For example, in Excel '2 < TRUE' produces TRUE, but so does '2 < FALSE'.
            // Sticking to a restricted set of numeric-like types for now until evidence arises to support the need for coercion.
            var resLeft = CheckComparisonTypeOneOfCore(errorContainer, left, typeLeft, DType.Number, DType.Date, DType.Time, DType.DateTime);
            var resRight = CheckComparisonTypeOneOfCore(errorContainer, right, typeRight, DType.Number, DType.Date, DType.Time, DType.DateTime);

            var coercions = new List<BinderCoercionResult>();
            coercions.AddRange(resLeft.Coercions);
            coercions.AddRange(resRight.Coercions);

            if (!typeLeft.Accepts(typeRight) && !typeRight.Accepts(typeLeft))
            {
                // Handle DateTime <=> Number comparison by coercing one side to Number
                if (DType.Number.Accepts(typeLeft) && DType.DateTime.Accepts(typeRight))
                {
                    coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Number });
                }
                else if (DType.Number.Accepts(typeRight) && DType.DateTime.Accepts(typeLeft))
                {
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Number });
                }
                else if (DType.DateTime.Accepts(typeLeft) && DType.DateTime.Accepts(typeRight))
                {
                    // Handle Date <=> Time comparison by coercing both to DateTime
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.DateTime });
                    coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.DateTime });
                }
            }

            return new BinderCheckTypeResult() { Coercions = coercions };
        }

        private static BinderCheckTypeResult CheckEqualArgTypesCore(IErrorContainer errorContainer, TexlNode left, TexlNode right, DType typeLeft, DType typeRight)
        {
            Contracts.AssertValue(left);
            Contracts.AssertValue(right);
            Contracts.AssertValue(left.Parent);
            Contracts.Assert(ReferenceEquals(left.Parent, right.Parent));

            // EqualOp is only allowed on primitive types, polymorphic lookups, and control types.
            if (!(typeLeft.IsPrimitive && typeRight.IsPrimitive) && !(typeLeft.IsPolymorphic && typeRight.IsPolymorphic) && !(typeLeft.IsControl && typeRight.IsControl)
                && !(typeLeft.IsPolymorphic && typeRight.IsRecord) && !(typeLeft.IsRecord && typeRight.IsPolymorphic))
            {
                var leftTypeDisambiguation = typeLeft.IsOptionSet && typeLeft.OptionSetInfo != null ? $"({typeLeft.OptionSetInfo.EntityName})" : string.Empty;
                var rightTypeDisambiguation = typeRight.IsOptionSet && typeRight.OptionSetInfo != null ? $"({typeRight.OptionSetInfo.EntityName})" : string.Empty;

                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    left.Parent,
                    TexlStrings.ErrIncompatibleTypesForEquality_Left_Right,
                    typeLeft.GetKindString() + leftTypeDisambiguation,
                    typeRight.GetKindString() + rightTypeDisambiguation);
                return new BinderCheckTypeResult();
            }

            // Special case for guid, it should produce an error on being compared to non-guid types
            if ((typeLeft.Equals(DType.Guid) && !typeRight.Equals(DType.Guid)) ||
                (typeRight.Equals(DType.Guid) && !typeLeft.Equals(DType.Guid)))
            {
                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    left.Parent,
                    TexlStrings.ErrGuidStrictComparison);
                return new BinderCheckTypeResult();
            }

            // Special case for option set values, it should produce an error when the base option sets are different
            if (typeLeft.Kind == DKind.OptionSetValue && !typeLeft.Accepts(typeRight))
            {
                var leftTypeDisambiguation = typeLeft.IsOptionSet && typeLeft.OptionSetInfo != null ? $"({typeLeft.OptionSetInfo.EntityName})" : string.Empty;
                var rightTypeDisambiguation = typeRight.IsOptionSet && typeRight.OptionSetInfo != null ? $"({typeRight.OptionSetInfo.EntityName})" : string.Empty;

                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    left.Parent,
                    TexlStrings.ErrIncompatibleTypesForEquality_Left_Right,
                    typeLeft.GetKindString() + leftTypeDisambiguation,
                    typeRight.GetKindString() + rightTypeDisambiguation);

                return new BinderCheckTypeResult();
            }

            // Special case for view values, it should produce an error when the base views are different
            if (typeLeft.Kind == DKind.ViewValue && !typeLeft.Accepts(typeRight))
            {
                var leftTypeDisambiguation = typeLeft.IsView && typeLeft.ViewInfo != null ? $"({typeLeft.ViewInfo.Name})" : string.Empty;
                var rightTypeDisambiguation = typeRight.IsView && typeRight.ViewInfo != null ? $"({typeRight.ViewInfo.Name})" : string.Empty;

                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    left.Parent,
                    TexlStrings.ErrIncompatibleTypesForEquality_Left_Right,
                    typeLeft.GetKindString() + leftTypeDisambiguation,
                    typeRight.GetKindString() + rightTypeDisambiguation);
                return new BinderCheckTypeResult();
            }

            var coercions = new List<BinderCoercionResult>();

            if (!typeLeft.Accepts(typeRight) && !typeRight.Accepts(typeLeft))
            {
                // Handle DateTime <=> Number comparison
                if (DType.Number.Accepts(typeLeft) && DType.DateTime.Accepts(typeRight))
                {
                    coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Number });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }
                else if (DType.Number.Accepts(typeRight) && DType.DateTime.Accepts(typeLeft))
                {
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Number });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }

                errorContainer.EnsureError(
                    DocumentErrorSeverity.Warning,
                    left.Parent,
                    TexlStrings.ErrIncompatibleTypesForEquality_Left_Right,
                    typeLeft.GetKindString(),
                    typeRight.GetKindString());
            }

            return new BinderCheckTypeResult();
        }

        internal static BinderCheckTypeResult CheckUnaryOpCore(IErrorContainer errorContainer, UnaryOpNode node, DType childType)
        {
            Contracts.AssertValue(node);

            switch (node.Op)
            {
                case UnaryOp.Not:
                    var resNot = CheckTypeCore(errorContainer, node.Child, childType, DType.Boolean, /* coerced: */ DType.Number, DType.String, DType.OptionSetValue);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Boolean, Coercions = resNot.Coercions };
                case UnaryOp.Minus:
                    switch (childType.Kind)
                    {
                        case DKind.Date:
                            // Important to keep the type of minus-date as date, to allow D-D/d-D to be detected
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Date };
                        case DKind.Time:
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Time };
                        case DKind.DateTime:
                            // Important to keep the type of minus-datetime as datetime, to allow d-d/D-d to be detected
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.DateTime };
                        default:
                            var resDefault = CheckTypeCore(errorContainer, node.Child, childType, DType.Number, /* coerced: */ DType.String, DType.Boolean);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Number, Coercions = resDefault.Coercions };
                    }

                case UnaryOp.Percent:
                    var resPercent = CheckTypeCore(errorContainer, node.Child, childType, DType.Number, /* coerced: */ DType.String, DType.Boolean, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Number, Coercions = resPercent.Coercions };
                default:
                    Contracts.Assert(false);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Error };
            }
        }

        // REVIEW ragru: Introduce a TexlOperator abstract base plus various subclasses
        // for handling operators and their overloads. That will offload the burden of dealing with
        // operator special cases to the various operator classes.
        public static BinderCheckTypeResult CheckBinaryOpCore(IErrorContainer errorContainer, BinaryOpNode node, DType leftType, DType rightType, bool isEnhancedDelegationEnabled)
        {
            Contracts.AssertValue(node);

            var leftNode = node.Left;
            var rightNode = node.Right;

            switch (node.Op)
            {
                case BinaryOp.Add:
                    return PostVisitBinaryOpNodeAdditionCore(errorContainer, node, leftType, rightType);
                case BinaryOp.Power:
                case BinaryOp.Mul:
                case BinaryOp.Div:
                    var resLeftDiv = CheckTypeCore(errorContainer, leftNode, leftType, DType.Number, /* coerced: */ DType.String, DType.Boolean, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime);
                    var resRightDiv = CheckTypeCore(errorContainer, rightNode, rightType, DType.Number, /* coerced: */ DType.String, DType.Boolean, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Number, Coercions = resLeftDiv.Coercions.Concat(resRightDiv.Coercions).ToList() };

                case BinaryOp.Or:
                case BinaryOp.And:
                    var resLeftAnd = CheckTypeCore(errorContainer, leftNode, leftType, DType.Boolean, /* coerced: */ DType.Number, DType.String, DType.OptionSetValue);
                    var resRightAnd = CheckTypeCore(errorContainer, rightNode, rightType, DType.Boolean, /* coerced: */ DType.Number, DType.String, DType.OptionSetValue);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Boolean, Coercions = resLeftAnd.Coercions.Concat(resRightAnd.Coercions).ToList() };

                case BinaryOp.Concat:
                    var resLeftConcat = CheckTypeCore(errorContainer, leftNode, leftType, DType.String, /* coerced: */ DType.Number, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime, DType.Boolean, DType.OptionSetValue, DType.ViewValue);
                    var resRightConcat = CheckTypeCore(errorContainer, rightNode, rightType, DType.String, /* coerced: */ DType.Number, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime, DType.Boolean, DType.OptionSetValue, DType.ViewValue);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.String, Coercions = resLeftConcat.Coercions.Concat(resRightConcat.Coercions).ToList() };

                case BinaryOp.Error:
                    errorContainer.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrOperatorExpected);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Error };

                case BinaryOp.Equal:
                case BinaryOp.NotEqual:
                    var resEq = CheckEqualArgTypesCore(errorContainer, leftNode, rightNode, leftType, rightType);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Boolean, Coercions = resEq.Coercions };

                case BinaryOp.Less:
                case BinaryOp.LessEqual:
                case BinaryOp.Greater:
                case BinaryOp.GreaterEqual:
                    // Excel's type coercion for inequality operators is inconsistent / borderline wrong, so we can't
                    // use it as a reference. For example, in Excel '2 < TRUE' produces TRUE, but so does '2 < FALSE'.
                    // Sticking to a restricted set of numeric-like types for now until evidence arises to support the need for coercion.
                    var resOrder = CheckComparisonArgTypesCore(errorContainer, leftNode, rightNode, leftType, rightType);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Boolean, Coercions = resOrder.Coercions };

                case BinaryOp.In:
                case BinaryOp.Exactin:
                    var resIn = CheckInArgTypesCore(errorContainer, leftNode, rightNode, leftType, rightType, isEnhancedDelegationEnabled);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Boolean, Coercions = resIn.Coercions };

                default:
                    Contracts.Assert(false);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Error };
            }
        }
    }
}
