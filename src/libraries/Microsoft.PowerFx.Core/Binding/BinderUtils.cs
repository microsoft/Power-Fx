﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

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
            else if (node.Left is CallNode call)
            {
                var leftNodeName = call.Head.Name;
                if (binding.TryGetReplacedIdentName(call.Head, out var possibleRename))
                {
                    leftNodeName = new DName(possibleRename);
                }

                var rightNodeName = node.Right.Name;
                if (binding.TryGetReplacedIdentName(node.Right, out var rename))
                {
                    rightNodeName = new DName(rename);
                }

                path = DPath.Root.Append(leftNodeName).Append(rightNodeName);
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
        /// Tries to get the best suited overload for <paramref name="node"/> according to <paramref name="context"/> and
        /// returns true if it is found.
        /// </summary>
        /// <param name="context">
        /// CheckTypesContext used for calls to CheckTypes.
        /// </param>
        /// <param name="errors">
        /// An IErrorContainer to collect errors.
        /// </param>
        /// <param name="node">
        /// CallNode for which the best overload will be determined.
        /// </param>
        /// <param name="args">
        /// List of argument nodes for <paramref name="node"/>.
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
        internal static bool TryGetBestOverload(CheckTypesContext context, IErrorContainer errors, CallNode node, TexlNode[] args, DType[] argTypes, TexlFunction[] overloads, out TexlFunction bestOverload, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap, out DType returnType)
        {
            Contracts.AssertValue(node, nameof(node));
            Contracts.AssertValue(overloads, nameof(overloads));

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

                var localWarnings = new LimitedSeverityErrorContainer(errors, DocumentErrorSeverity.Warning);

                // Typecheck the invocation and infer the return type.
                typeCheckSucceeded = maybeFunc.CheckTypes(context, args, argTypes, localWarnings, out returnType, out nodeToCoercedTypeMap);

                (typeCheckSucceeded, returnType) = CheckDeferredType(argTypes, returnType, typeCheckSucceeded);

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
        /// if <paramref name="typeCheckSucceeded"/> failed and DeferredType arg is present, discard all the error and succeed the type check.
        /// Keep <paramref name="errorContainer"/> and <paramref name="checkErrorContainer"/> null if no need to merge errors.
        /// </summary>
        /// <param name="argTypes"> types of all the args.</param>
        /// <param name="returnType"> return type determined by function's check type.</param>
        /// <param name="typeCheckSucceeded"> function's check type succeeded or not.</param>
        /// <param name="checkErrorContainer"> Temp error container used for type checking only.</param>
        /// <param name="errorContainer"> Binder's error container.</param>
        internal static (bool typeCheckSucceeded, DType returnType) CheckDeferredType(DType[] argTypes, DType returnType, bool typeCheckSucceeded, ErrorContainer checkErrorContainer = null, ErrorContainer errorContainer = null)
        {
            var isDeferredArgPresent = argTypes.Any(type => type.IsDeferred);

            if (!typeCheckSucceeded && isDeferredArgPresent)
            {
                typeCheckSucceeded = true;

                // If one of the arg was deferred and
                // return type could not be calculated and was error, we assign it to deferred as safeguard.
                // returnType was EmptyTable, we assign it to deferred as safeguard e.g. Table(Deferred) => deferred,
                // this is because we don't want to embed deferred type inside of any aggregate type.
                if (returnType.IsError || returnType.Equals(DType.EmptyTable))
                {
                    returnType = DType.Deferred;
                }
            }
            else if (checkErrorContainer != null && errorContainer != null)
            {
                errorContainer.MergeErrors(checkErrorContainer.GetErrors());
            }

            return (typeCheckSucceeded, returnType);
        }

        /// <summary>
        /// Returns best overload in case there are no matches based on first argument and order.
        /// </summary>
        internal static TexlFunction FindBestErrorOverload(TexlFunction[] overloads, DType[] argTypes, int cArg, bool usePowerFxV1CompatibilityRules)
        {
            var candidates = overloads.Where(overload => overload.MinArity <= cArg && cArg <= overload.MaxArity);

            if (cArg == 0)
            {
                return candidates.FirstOrDefault();
            }

            // Consider overloads that have DType.Error parameter the last
            candidates = candidates.OrderBy(candidate => candidate.ParamTypes.Length > 0 && candidate.ParamTypes[0] == DType.Error).ToArray();
            foreach (var candidate in candidates)
            {
                if (candidate.ParamTypes.Length > 0 && candidate.ParamTypes[0].Accepts(argTypes[0], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                {
                    return candidate;
                }
            }

            return candidates.FirstOrDefault();
        }

        /// <summary>
        /// Helper for Lt/leq/geq/gt type checking. Restricts type to be one of the provided set, without coercion (except for primary output props).
        /// </summary>
        /// <param name="errorContainer">Errors will be reported here.</param>
        /// <param name="node">Node for which we are checking the type.</param>
        /// <param name="features">Controls set of enabled/disabled features.</param>
        /// <param name="type">The type for node.</param>
        /// <param name="alternateTypes">List of acceptable types for this operation, in order of suitability.</param>
        /// <returns></returns>
        private static BinderCheckTypeResult CheckComparisonTypeOneOfCore(IErrorContainer errorContainer, TexlNode node, Features features, DType type, params DType[] alternateTypes)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(alternateTypes);
            Contracts.Assert(alternateTypes.Any());

            var usePowerFxV1CompatibilityRules = features.PowerFxV1CompatibilityRules;
            var coercions = new List<BinderCoercionResult>();

            foreach (var altType in alternateTypes)
            {
                if (!altType.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                {
                    continue;
                }

                return new BinderCheckTypeResult();
            }

            if (!features.PrimaryOutputPropertyCoercionDeprecated)
            {
                // If the node is a control, we may be able to coerce its primary output property
                // to the desired type, and in the process support simplified syntax such as: slider2 <= slider4
                IExternalControlProperty primaryOutProp;
                if (type is IExternalControlType controlType && node.AsFirstName() != null && (primaryOutProp = controlType.ControlTemplate.PrimaryOutputProperty) != null)
                {
                    var outType = primaryOutProp.GetOpaqueType();
                    var acceptedType = alternateTypes.FirstOrDefault(alt => alt.Accepts(outType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                    if (acceptedType != default)
                    {
                        // We'll coerce the control to the desired type, by pulling from the control's
                        // primary output property. See codegen for details.
                        coercions.Add(new BinderCoercionResult() { Node = node, CoercedType = acceptedType });
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }
                }
            }

            errorContainer.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrBadType_ExpectedTypesCSV, string.Join(", ", alternateTypes.Select(t => t.GetKindString())));
            return new BinderCheckTypeResult();
        }

        // Returns whether the node was of the type wanted, and reports appropriate errors.
        // A list of allowed alternate types specifies what other types of values can be coerced to the wanted type.
        private static BinderCheckTypeResult CheckTypeCore(IErrorContainer errorContainer, TexlNode node, Features features, DType nodeType, DType typeWant, params DType[] alternateTypes)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(typeWant.IsValid || typeWant == DType.Deferred);
            Contracts.Assert(!typeWant.IsError);
            Contracts.AssertValue(alternateTypes);

            var usePowerFxV1CompatibilityRules = features.PowerFxV1CompatibilityRules;

            var coercions = new List<BinderCoercionResult>();

            if (typeWant.Accepts(nodeType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
            {
                if (nodeType.RequiresExplicitCast(typeWant, usePowerFxV1CompatibilityRules))
                {
                    coercions.Add(new BinderCoercionResult() { Node = node, CoercedType = typeWant });
                }

                return new BinderCheckTypeResult() { Coercions = coercions };
            }

            // Option sets are checked against typeWant for a match of the backing kind
            //   Pre-V1: Ensure that Booleans only match bool valued option sets
            //   V1: BackingKind matches (and Decimal can work with Number) and CanCoerceToBackingKind is true
            //
            // Checking against typeWant and not all alternateTypes (below) prevents, for example, Boolean option sets from being used in + operations,
            // as it would require a coercion to number and then another to Boolean.  Limiting option set usage in this manner is a good thing.
            // If the maker wants to allow Boolean + to work, they can use the Value function on the option set first.
            //
            // Option sets need not (and shouldn't be) listed in calls to CheckTypeCore.  For consistency,
            // we always check option sets here against the backing types.
            //
            // Option sets can always coerce to string, V1 and pre-V1.
            var nodeBackingKind = nodeType.OptionSetInfo?.BackingKind;
            if (nodeBackingKind != null &&
                (typeWant.Kind == DKind.String ||
                ((!usePowerFxV1CompatibilityRules || nodeType.OptionSetInfo.CanCoerceToBackingKind) &&
                   ((typeWant.Kind == DKind.Boolean && nodeBackingKind == DKind.Boolean) ||
                    ((typeWant.Kind == DKind.Number || typeWant.Kind == DKind.Decimal) && nodeBackingKind == DKind.Number) ||
                    (typeWant.Kind == DKind.Color && nodeBackingKind == DKind.Color)))))
            {
                coercions.Add(new BinderCoercionResult() { Node = node, CoercedType = typeWant });
                return new BinderCheckTypeResult() { Coercions = coercions };
            }

            // Normal (non-control) coercion
            foreach (var altType in alternateTypes)
            {
                if (altType.Accepts(nodeType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                {
                    // We found an alternate type that is accepted and will be coerced.
                    coercions.Add(new BinderCoercionResult() { Node = node, CoercedType = typeWant });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }
            }

            if (!features.PrimaryOutputPropertyCoercionDeprecated)
            {
                // If the node is a control, we may be able to coerce its primary output property
                // to the desired type, and in the process support simplified syntax such as: label1 + slider4
                IExternalControlProperty primaryOutProp;
                if (nodeType is IExternalControlType controlType && node.AsFirstName() != null && (primaryOutProp = controlType.ControlTemplate.PrimaryOutputProperty) != null)
                {
                    var outType = primaryOutProp.GetOpaqueType();
                    if (typeWant.Accepts(outType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || alternateTypes.Any(alt => alt.Accepts(outType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules)))
                    {
                        // We'll "coerce" the control to the desired type, by pulling from the control's
                        // primary output property. See codegen for details.
                        coercions.Add(new BinderCoercionResult() { Node = node, CoercedType = typeWant });
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }
                }
            }

            var messageKey = alternateTypes.Length == 0 ? TexlStrings.ErrBadType_ExpectedType : TexlStrings.ErrBadType_ExpectedTypesCSV;
            var messageArg = alternateTypes.Length == 0 ? typeWant.GetKindString() : string.Join(", ", new[] { typeWant }.Concat(alternateTypes).Select(t => t.GetKindString()));

            errorContainer.EnsureError(DocumentErrorSeverity.Severe, node, messageKey, messageArg);
            return new BinderCheckTypeResult() { Coercions = coercions };
        }

        // Performs type checking for the arguments passed to the membership "in"/"exactin" operators.
        private static BinderCheckTypeResult CheckInArgTypesCore(IErrorContainer errorContainer, TexlNode left, TexlNode right, DType typeLeft, DType typeRight, Features features)
        {
            Contracts.AssertValue(left);
            Contracts.AssertValue(right);

            var coercions = new List<BinderCoercionResult>();
            var usePowerFxV1CompatibilityRules = features.PowerFxV1CompatibilityRules;

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

            if (!typeLeft.IsAggregate || (usePowerFxV1CompatibilityRules && typeLeft.Kind == DKind.ObjNull))
            {
                // scalar in scalar: RHS must be a string (or coercible to string when LHS type is string). We'll allow coercion of LHS.
                // This case deals with substring matches, e.g. 'FirstName in "Aldous Huxley"' or "123" in 123.
                if (!typeRight.IsAggregate || (usePowerFxV1CompatibilityRules && typeRight.Kind == DKind.ObjNull))
                {
                    if (!DType.String.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        if (typeRight.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, features) && DType.String.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
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

                    if (DType.String.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    if (!typeLeft.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, features))
                    {
                        errorContainer.EnsureError(DocumentErrorSeverity.Severe, left, TexlStrings.ErrCannotCoerce_SourceType_TargetType, typeLeft.GetKindString(), DType.String.GetKindString());
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    // Coerce LHS to a string type, to facilitate subsequent substring checks.
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.String });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }

                // scalar in table: RHS must be a one column table. We'll allow coercion.
                if (typeRight.IsTableNonObjNull)
                {
                    var names = typeRight.GetNames(DPath.Root);
                    if (names.Count() != 1)
                    {
                        errorContainer.EnsureError(DocumentErrorSeverity.Severe, right, TexlStrings.ErrInvalidSchemaNeedCol);
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    var typedName = names.Single();
                    if (typedName.Type.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        typeLeft.Accepts(typedName.Type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    if (!typeLeft.CoercesTo(typedName.Type, aggregateCoercion: true, isTopLevelCoercion: false, features))
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

                    if (typeLeftAsTable.Accepts(typeRight, out var typeRightDifferingSchema, out var typeRightDifferingSchemaType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        typeRight.Accepts(typeLeftAsTable, out var typeLeftDifferingSchema, out var typeLeftDifferingSchemaType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
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

            if (typeLeft.IsTable)
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
                    if (!typeRight.CoercesTo(typeLeft, aggregateCoercion: true, isTopLevelCoercion: false, features))
                    {
                        errorContainer.EnsureError(DocumentErrorSeverity.Severe, right, TexlStrings.ErrCannotCoerce_SourceType_TargetType, typeLeft.GetKindString(), typedName.Type.GetKindString());
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }

                    // Check if multiselectoptionset column type accepts RHS node of type table. 
                    if (typeLeft.Accepts(typedName.Type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return new BinderCheckTypeResult() { Coercions = coercions };
                    }
                }
            }

            // Table in scalar or Table in Record or Table in unsupported table: not supported
            errorContainer.EnsureError(DocumentErrorSeverity.Severe, left, TexlStrings.ErrBadType_Type, typeLeft.GetKindString());
            return new BinderCheckTypeResult() { Coercions = coercions };
        }

        // Determine the type of a numeric binary op when it could be either Decimal or Number (+, -, *, /)
        // For binary ops that always return a Number, like ^ (power), this calculation is not needed and Deferred will never be returned
        //
        // Result types:
        // * Type codes in DTypeSpecParser.cs
        // * Date/DateTime/Time exceptions for addition are are handled in PostVisitBinaryOpNodeAdditionCore.
        // * Minus is handled through addition of a negative unary op
        // * Tests in OpMatrix_{op}_{NumberIsFloatMode}.txt files
        //
        // Non NumberIsFloat (no flag)                     NumberIsFloat
        //    +   | n  s  b  N  D  d  T  w  O  (right)        +   | n  s  b  N  D  d  T  w  O  (right)
        // =======|====================================    =======|====================================
        //      n | n  n  n  n  D  d  T  n  n                   n | n  n  n  n  D  d  T  n  n 
        //      s | n  w  w  w  D  d  T  w  w                   s | n  n  n  n  D  d  T  n  n 
        //      b | n  w  w  w  D  d  T  w  w                   b | n  n  n  n  D  d  T  n  n 
        //      N | n  w  w  w  D  d  T  w  w                   N | n  n  n  n  D  d  T  n  n 
        //      D | D  D  D  D  e  e  d  D  D                   D | D  D  D  D  e  e  d  D  D 
        //      d | d  d  d  d  e  e  d  d  d                   d | d  d  d  d  e  e  d  d  d 
        //      T | T  T  T  T  d  d  T  T  T                   T | T  T  T  T  d  d  T  T  T 
        //      w | n  w  w  w  D  d  T  w  w                   w | n  n  n  n  D  d  T  w  n 
        //      O | n  w  w  w  D  d  T  w  w                   O | n  n  n  n  D  d  T  n  n 
        // (left) |                                        (left) |
        //
        // Non NumberIsFloat (no flag)                     NumberIsFloat
        //    -   | n  s  b  N  D  d  T  w  O  (right)        -   | n  s  b  N  D  d  T  w  O  (right)
        // =======|====================================    =======|====================================
        //      n | n  n  n  n  e  e  e  n  n                   n | n  n  n  n  e  e  e  n  n
        //      s | n  w  w  w  e  e  e  w  w                   s | n  n  n  n  e  e  e  n  n
        //      b | n  w  w  w  e  e  e  w  w                   b | n  n  n  n  e  e  e  n  n
        //      N | n  w  w  w  e  e  e  w  w                   N | n  n  n  n  e  e  e  n  n
        //      D | D  D  D  D  w  w  d  D  D                   D | D  D  D  D  n  n  d  D  D
        //      d | d  d  d  d  w  w  d  d  d                   d | d  d  d  d  n  n  d  d  d
        //      T | T  T  T  T  e  e  w  T  T                   T | T  T  T  T  e  e  n  T  T
        //      w | n  w  w  w  e  e  e  w  w                   w | n  n  n  n  e  e  e  w  n
        //      O | n  w  w  w  e  e  e  w  w                   O | n  n  n  n  e  e  e  n  n
        // (left) |                                        (left) |
        //
        // Non NumberIsFloat (no flag)                     NumberIsFloat (note one w at w*w)
        //  *, /  | n  s  b  N  D  d  T  w  O  (right)       *, / | n  s  b  N  D  d  T  w  O  (right)
        // =======|====================================    =======|====================================
        //      n | n  n  n  n  n  n  n  n  n                   n | n  n  n  n  n  n  n  n  n 
        //      s | n  w  w  w  w  w  w  w  w                   s | n  n  n  n  n  n  n  n  n 
        //      b | n  w  w  w  w  w  w  w  w                   b | n  n  n  n  n  n  n  n  n 
        //      N | n  w  w  w  w  w  w  w  w                   N | n  n  n  n  n  n  n  n  n 
        //      D | n  w  w  w  w  w  w  w  w                   D | n  n  n  n  n  n  n  n  n  
        //      d | n  w  w  w  w  w  w  w  w                   d | n  n  n  n  n  n  n  n  n  
        //      T | n  w  w  w  w  w  w  w  w                   T | n  n  n  n  n  n  n  n  n  
        //      w | n  w  w  w  w  w  w  w  w                   w | n  n  n  n  n  n  n  w  n 
        //      O | n  w  w  w  w  w  w  w  w                   O | n  n  n  n  n  n  n  n  n 
        // (left) |                                        (left) |

        private static BinderCheckTypeResult CheckDecimalBinaryOp(IErrorContainer errorContainer, BinaryOpNode node, Features features, DType leftType, DType rightType, bool numberIsFloat)
        {
            var leftKind = leftType.Kind;
            var rightKind = rightType.Kind;

            // when numberIsFloat, favor Number, return type is always Number except when both operands are Decimal
            // when !numberIsFloat, favor Decimal, return type is only Number if one of the operands is Number
            var returnType = (numberIsFloat && (leftType != DType.Decimal || rightType != DType.Decimal)) ||
                             (!numberIsFloat && (leftType == DType.Number || rightType == DType.Number))
                             ? DType.Number : DType.Decimal;

            // type of the other variety of number, to be coerced to returnType
            var otherType = returnType == DType.Number ? DType.Decimal : DType.Number;

            var resLeft = CheckTypeCore(errorContainer, node.Left, features, leftType, returnType, /* coerced: */ otherType, DType.String, DType.Boolean, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime, DType.UntypedObject);
            var resRight = CheckTypeCore(errorContainer, node.Right, features, rightType, returnType, /* coerced: */ otherType, DType.String, DType.Boolean, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime, DType.UntypedObject);

            // Deferred op decimal/number or decimal/number op Deferred results in Deferred
            if (leftKind == DKind.Deferred || rightKind == DKind.Deferred)
            {
                return new BinderCheckTypeResult() { Node = node, NodeType = DType.Deferred, Coercions = resLeft.Coercions.Concat(resRight.Coercions).ToList() };
            }

            return new BinderCheckTypeResult() { Node = node, NodeType = returnType, Coercions = resLeft.Coercions.Concat(resRight.Coercions).ToList() };
        }

        private static BinderCheckTypeResult PostVisitBinaryOpNodeAdditionCore(IErrorContainer errorContainer, BinaryOpNode node, Features features, DType leftType, DType rightType, bool numberIsFloat)
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
                                return new BinderCheckTypeResult() { Node = node, NodeType = numberIsFloat ? DType.Number : DType.Decimal };
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
                            var resRight = CheckTypeCore(errorContainer, node.Right, features, rightType, DType.Number, /* coerced: */ DType.Decimal, DType.String, DType.Boolean, DType.UntypedObject);
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
                                return new BinderCheckTypeResult() { Node = node, NodeType = numberIsFloat ? DType.Number : DType.Decimal };
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
                                return new BinderCheckTypeResult() { Node = node, NodeType = numberIsFloat ? DType.Number : DType.Decimal };
                            }
                            else
                            {
                                // Date + DateTime in any other arrangement is an error
                                return ReportInvalidOperation();
                            }

                        default:
                            // Date + number = Date
                            var resRight = CheckTypeCore(errorContainer, node.Right, features, rightType, DType.Number, /* coerced: */ DType.Decimal, DType.String, DType.Boolean, DType.UntypedObject);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Date, Coercions = resRight.Coercions };
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
                                return new BinderCheckTypeResult() { Node = node, NodeType = numberIsFloat ? DType.Number : DType.Decimal };
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
                            var resRight = CheckTypeCore(errorContainer, node.Right, features, rightType, DType.Number, /* coerced: */ DType.Decimal, DType.String, DType.Boolean, DType.UntypedObject);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Time, Coercions = resRight.Coercions };
                    }

                default: // number and decimal
                    switch (rightKind)
                    {
                        case DKind.DateTime:
                            // number/decimal + DateTime = DateTime
                            var leftResDateTime = CheckTypeCore(errorContainer, node.Left, features, leftType, DType.Number, /* coerced: */ DType.Decimal, DType.String, DType.Boolean, DType.UntypedObject);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.DateTime, Coercions = leftResDateTime.Coercions };
                        case DKind.Date:
                            // number/decimal + Date = Date
                            var leftResDate = CheckTypeCore(errorContainer, node.Left, features, leftType, DType.Number, /* coerced: */ DType.Decimal, DType.String, DType.Boolean, DType.UntypedObject);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Date, Coercions = leftResDate.Coercions };
                        case DKind.Time:
                            // number/decimal + Time = Time
                            var leftResTime = CheckTypeCore(errorContainer, node.Left, features, leftType, DType.Number, /* coerced: */ DType.Decimal, DType.String, DType.Boolean, DType.UntypedObject);
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Time, Coercions = leftResTime.Coercions };
                        default:
                            // Regular Addition
                            return CheckDecimalBinaryOp(errorContainer, node, features, leftType, rightType, numberIsFloat);
                    }
            }
        }

        private static bool IsAcceptedByDateOrTime(DType type, bool usePowerFxV1CompatibilityRules)
        {
            return DType.DateTime.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                    DType.Date.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                    DType.Time.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
        }

        private static BinderCheckTypeResult CheckComparisonArgTypesCore(IErrorContainer errorContainer, TexlNode left, TexlNode right, Features features, DType typeLeft, DType typeRight, bool numberIsFloat)
        {
            var coercions = new List<BinderCoercionResult>();
            var usePowerFxV1CompatibilityRules = features.PowerFxV1CompatibilityRules;

            // Special cases for comparing option set values
            // Without StronglyTypedBuiltinEnums, option sets are convereted to their backing kind before getting here in Visitor.Visit(FirstNameNode)
            if (typeLeft.Kind == DKind.OptionSetValue || typeRight.Kind == DKind.OptionSetValue)
            {
                // Comparing values from two different option set values is not supported
                if (typeLeft.Kind == DKind.OptionSetValue && typeRight.Kind == DKind.OptionSetValue
                    && typeLeft.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                {
                    if (typeLeft.OptionSetInfo.CanCompareNumeric)
                    {
                        return new BinderCheckTypeResult
                        {
                            Coercions = new[]
                            {
                                new BinderCoercionResult { Node = left, CoercedType = DType.Number },
                                new BinderCoercionResult { Node = right, CoercedType = DType.Number }
                            }
                        };
                    }
                    else
                    {
                        errorContainer.EnsureError(
                            DocumentErrorSeverity.Severe,
                            left.Parent,
                            TexlStrings.ErrUnOrderedTypeForComparison_Type,
                            typeLeft.GetKindString());
                        return new BinderCheckTypeResult();
                    }
                }

                // Comparing to backing type is permitted under a few circumstances
                else if (typeLeft.Kind == DKind.OptionSetValue && typeLeft.OptionSetInfo.CanCompareNumeric &&
                        (!usePowerFxV1CompatibilityRules || typeLeft.OptionSetInfo.CanCoerceFromBackingKind || typeLeft.OptionSetInfo.CanCoerceToBackingKind) &&
                        (typeRight.Kind == DKind.Number || typeRight.Kind == DKind.Decimal))
                {
                    return new BinderCheckTypeResult
                    {
                        Coercions = new[]
                        {
                            new BinderCoercionResult { Node = left, CoercedType = DType.Number },
                            new BinderCoercionResult { Node = right, CoercedType = DType.Number }
                        }
                    };
                }
                else if (typeRight.Kind == DKind.OptionSetValue && typeRight.OptionSetInfo.CanCompareNumeric &&
                        (!usePowerFxV1CompatibilityRules || typeRight.OptionSetInfo.CanCoerceFromBackingKind || typeRight.OptionSetInfo.CanCoerceToBackingKind) &&
                        (typeLeft.Kind == DKind.Number || typeLeft.Kind == DKind.Decimal))
                {
                    return new BinderCheckTypeResult
                    {
                        Coercions = new[]
                        {
                            new BinderCoercionResult { Node = left, CoercedType = DType.Number },
                            new BinderCoercionResult { Node = right, CoercedType = DType.Number }
                        }
                    };
                }

                // otherwise, we have an illegal option set comparison
                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    left.Parent,
                    TexlStrings.ErrIncompatibleTypesForEquality_Left_Right,
                    typeLeft.GetKindString(),
                    typeRight.GetKindString());
                return new BinderCheckTypeResult();
            }

            // Excel's type coercion for inequality operators is inconsistent / borderline wrong, so we can't
            // use it as a reference. For example, in Excel '2 < TRUE' produces TRUE, but so does '2 < FALSE'.
            // Sticking to a restricted set of numeric-like types for now until evidence arises to support the need for coercion.
            var resLeft = CheckComparisonTypeOneOfCore(errorContainer, left, features, typeLeft, DType.Number, DType.Decimal, DType.Date, DType.Time, DType.DateTime, DType.UntypedObject);
            var resRight = CheckComparisonTypeOneOfCore(errorContainer, right, features, typeRight, DType.Number, DType.Decimal, DType.Date, DType.Time, DType.DateTime, DType.UntypedObject);

            coercions.AddRange(resLeft.Coercions);
            coercions.AddRange(resRight.Coercions);

            if (typeLeft.IsUntypedObject && typeRight.IsUntypedObject)
            {
                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    left.Parent,
                    TexlStrings.ErrBadOperatorTypes,
                    typeLeft.GetKindString(),
                    typeRight.GetKindString());
                return new BinderCheckTypeResult() { Node = left.Parent, NodeType = DType.Error };
            }

            if (!typeLeft.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                !typeRight.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
            {
                // Handle Date/Time <=> Number comparison by coercing DateTime side to Number
                // In all situations, mixing anything with Number results in Number
                if (DType.Number.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                    IsAcceptedByDateOrTime(typeRight, usePowerFxV1CompatibilityRules))
                {
                    coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Number });
                }
                else if (DType.Number.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                    IsAcceptedByDateOrTime(typeLeft, usePowerFxV1CompatibilityRules))
                {
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Number });
                }

                // Handle Decimal <=> Number comparison by coercing Decimal side to Number
                // In all situations, mixing anything with Number results in Number
                if (DType.Number.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                    DType.Decimal.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                {
                    coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Number });
                }
                else if (DType.Number.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                    DType.Decimal.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                {
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Number });
                }

                // Handle Decimal <=> DateTime comparison by coercing both sides to Number
                // Process in Number or Decimal depending on numberIsFloat for consistency with +/- operations
                if (IsAcceptedByDateOrTime(typeLeft, usePowerFxV1CompatibilityRules) &&
                    DType.Decimal.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                {
                    if (numberIsFloat)
                    {
                        coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Number });
                        coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Number });
                    }
                    else
                    {
                        // right is already decimal
                        coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Decimal });
                    }
                }
                else if (IsAcceptedByDateOrTime(typeRight, usePowerFxV1CompatibilityRules) &&
                    DType.Decimal.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                {
                    if (numberIsFloat)
                    {
                        coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Number });
                        coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Number });
                    }
                    else
                    {
                        // left is already decimal
                        coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Decimal });
                    }
                }

                if (IsAcceptedByDateOrTime(typeLeft, usePowerFxV1CompatibilityRules) && IsAcceptedByDateOrTime(typeRight, usePowerFxV1CompatibilityRules))
                {
                    // Handle Date <=> Time comparison by coercing both to DateTime
                    if (typeLeft.Kind != DType.DateTime.Kind)
                    {
                        coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.DateTime });
                    }

                    if (typeRight.Kind != DType.DateTime.Kind)
                    {
                        coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.DateTime });
                    }
                }

                if (typeLeft.IsUntypedObject && CoercionMatrix.GetCoercionKind(typeLeft, typeRight, usePowerFxV1CompatibilityRules) != CoercionKind.None)
                {
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = typeRight });
                }

                if (typeRight.IsUntypedObject && CoercionMatrix.GetCoercionKind(typeRight, typeLeft, usePowerFxV1CompatibilityRules) != CoercionKind.None)
                {
                    coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = typeLeft });
                }
            }

            if (typeLeft.IsUntypedObject && typeRight.Kind == DKind.ObjNull)
            {
                coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = numberIsFloat ? DType.Number : DType.Decimal });
            }

            if (typeRight.IsUntypedObject && typeLeft.Kind == DKind.ObjNull)
            {
                coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = numberIsFloat ? DType.Number : DType.Decimal });
            }

            return new BinderCheckTypeResult() { Coercions = coercions };
        }

        private static BinderCheckTypeResult CheckEqualArgTypesCore(IErrorContainer errorContainer, TexlNode left, TexlNode right, bool usePowerFxV1CompatibilityRules, DType typeLeft, DType typeRight, bool numberIsFloat)
        {
            Contracts.AssertValue(left);
            Contracts.AssertValue(right);
            Contracts.AssertValue(left.Parent);
            Contracts.Assert(ReferenceEquals(left.Parent, right.Parent));

            // EqualOp is only allowed on primitive types, polymorphic lookups, untyped objects, and control types.
            if (!(typeLeft.IsPrimitive && typeRight.IsPrimitive) && !(typeLeft.IsPolymorphic && typeRight.IsPolymorphic) && !(typeLeft.IsControl && typeRight.IsControl)
                && !(typeLeft.IsPolymorphic && typeRight.IsRecord) && !(typeLeft.IsRecord && typeRight.IsPolymorphic) && !(typeLeft.IsDeferred || typeRight.IsDeferred) && !(typeLeft.IsUntypedObject || typeRight.IsUntypedObject))
            {
                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    left.Parent,
                    TexlStrings.ErrIncompatibleTypesForEquality_Left_Right,
                    typeLeft.GetKindString(),
                    typeRight.GetKindString());
                return new BinderCheckTypeResult();
            }

            // Special case for guid, it should produce an error on being compared to non-guid types
            if ((typeLeft.Equals(DType.Guid) && !(typeRight.Equals(DType.Guid) || typeRight.IsDeferred)) ||
                (typeRight.Equals(DType.Guid) && !(typeLeft.Equals(DType.Guid) || typeLeft.IsDeferred)))
            {
                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    left.Parent,
                    TexlStrings.ErrGuidStrictComparison);
                return new BinderCheckTypeResult();
            }

            // Special case for comparing option set values
            if (typeLeft.Kind == DKind.OptionSetValue || typeRight.Kind == DKind.OptionSetValue)
            {
                // Comparing values from two different option set values is not supported
                if (typeLeft.Kind == DKind.OptionSetValue && typeRight.Kind == DKind.OptionSetValue)
                {
                    if (typeLeft.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return new BinderCheckTypeResult();
                    }
                }

                // Comparing to backing type is permitted under a few circumstances
                else if (typeLeft.Kind == DKind.OptionSetValue &&
                        (!usePowerFxV1CompatibilityRules || typeLeft.OptionSetInfo.CanCoerceFromBackingKind || typeLeft.OptionSetInfo.CanCoerceToBackingKind))
                {
                    if (typeLeft.OptionSetInfo.BackingKind == typeRight.Kind ||
                        (typeLeft.IsOptionSetBackedByNumber && typeRight.Kind == DKind.Decimal))
                    {
                        return new BinderCheckTypeResult
                        {
                            Coercions = new[]
                            {
                                new BinderCoercionResult { Node = left, CoercedType = new DType(typeLeft.OptionSetInfo.BackingKind) },
                                new BinderCoercionResult { Node = right, CoercedType = new DType(typeLeft.OptionSetInfo.BackingKind) }
                            }
                        };
                    }
                }
                else if (typeRight.Kind == DKind.OptionSetValue &&
                        (!usePowerFxV1CompatibilityRules || typeRight.OptionSetInfo.CanCoerceFromBackingKind || typeRight.OptionSetInfo.CanCoerceToBackingKind))
                {
                    if (typeRight.OptionSetInfo.BackingKind == typeLeft.Kind ||
                        (typeRight.IsOptionSetBackedByNumber && typeLeft.Kind == DKind.Decimal))
                    {
                        return new BinderCheckTypeResult
                        {
                            Coercions = new[]
                            {
                                new BinderCoercionResult { Node = left, CoercedType = new DType(typeRight.OptionSetInfo.BackingKind) },
                                new BinderCoercionResult { Node = right, CoercedType = new DType(typeRight.OptionSetInfo.BackingKind) }
                            }
                        };
                    }
                }
                
                // otherwise, we have an illegal option set comparison
                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    left.Parent,
                    TexlStrings.ErrIncompatibleTypesForEquality_Left_Right,
                    typeLeft.GetKindString(),
                    typeRight.GetKindString());
                return new BinderCheckTypeResult();
            }

            // Special case for view values, it should produce an error when the base views are different
            if (typeLeft.Kind == DKind.ViewValue && !typeLeft.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
            {
                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    left.Parent,
                    TexlStrings.ErrIncompatibleTypesForEquality_Left_Right,
                    typeLeft.GetKindString(),
                    typeRight.GetKindString());
                return new BinderCheckTypeResult();
            }

            if (typeLeft.IsUntypedObject && typeRight.IsUntypedObject)
            {
                errorContainer.EnsureError(
                    DocumentErrorSeverity.Severe,
                    left.Parent,
                    TexlStrings.ErrIncompatibleTypesForEquality_Left_Right,
                    typeLeft.GetKindString(),
                    typeRight.GetKindString());
                return new BinderCheckTypeResult() { Node = left.Parent, NodeType = DType.Error };
            }

            var coercions = new List<BinderCoercionResult>();

            if (!typeLeft.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                !typeRight.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
            {
                // Handle DateTime <=> Number comparison
                // In all situations, mixing anything with Number results in Number
                if (DType.Number.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                    IsAcceptedByDateOrTime(typeRight, usePowerFxV1CompatibilityRules))
                {
                    coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Number });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }
                else if (DType.Number.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                    IsAcceptedByDateOrTime(typeLeft, usePowerFxV1CompatibilityRules))
                {
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Number });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }

                // Handle Date/Time <=> DateTime comparison
                // Promote all sides to DateTime
                if (IsAcceptedByDateOrTime(typeLeft, usePowerFxV1CompatibilityRules) &&
                    IsAcceptedByDateOrTime(typeRight, usePowerFxV1CompatibilityRules))
                {
                    if (typeLeft.Kind != DKind.DateTime)
                    {
                        coercions.Add(new BinderCoercionResult { Node = left, CoercedType = DType.DateTime });
                    }

                    if (typeRight.Kind != DKind.DateTime)
                    {
                        coercions.Add(new BinderCoercionResult { Node = right, CoercedType = DType.DateTime });
                    }

                    return new BinderCheckTypeResult { Coercions = coercions };
                }

                // Handle UntypedObject comparisons
                if (typeLeft.IsUntypedObject && CoercionMatrix.GetCoercionKind(typeLeft, typeRight, usePowerFxV1CompatibilityRules) != CoercionKind.None)
                {
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = typeRight });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }
                else if (typeRight.IsUntypedObject && CoercionMatrix.GetCoercionKind(typeRight, typeLeft, usePowerFxV1CompatibilityRules) != CoercionKind.None)
                {
                    coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = typeLeft });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }

                // Handle Decimal <=> Number comparison
                // In all situations, mixing anything with Number results in Number
                if (DType.Number.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                    DType.Decimal.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                {
                    coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Number });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }
                else if (DType.Number.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                    DType.Decimal.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                {
                    coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Number });
                    return new BinderCheckTypeResult() { Coercions = coercions };
                }

                // Handle DateTime <=> Decimal comparison
                if (DType.Decimal.Accepts(typeRight, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                    IsAcceptedByDateOrTime(typeLeft, usePowerFxV1CompatibilityRules))
                {
                    if (numberIsFloat)
                    {
                        coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Number });
                        coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Number });
                    }
                    else
                    {
                        // right is already decimal
                        coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Decimal });
                    }

                    return new BinderCheckTypeResult() { Coercions = coercions };
                }
                else if (DType.Decimal.Accepts(typeLeft, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                    IsAcceptedByDateOrTime(typeRight, usePowerFxV1CompatibilityRules))
                {
                    if (numberIsFloat)
                    {
                        coercions.Add(new BinderCoercionResult() { Node = left, CoercedType = DType.Number });
                        coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Number });
                    }
                    else
                    {
                        // left is already decimal
                        coercions.Add(new BinderCoercionResult() { Node = right, CoercedType = DType.Decimal });
                    }

                    return new BinderCheckTypeResult() { Coercions = coercions };
                }

                errorContainer.EnsureError(
                    usePowerFxV1CompatibilityRules ? DocumentErrorSeverity.Severe : DocumentErrorSeverity.Warning,
                    left.Parent,
                    TexlStrings.ErrIncompatibleTypesForEquality_Left_Right,
                    typeLeft.GetKindString(),
                    typeRight.GetKindString());
            }

            return new BinderCheckTypeResult();
        }

        // Return types from CheckUnaryOpCore:
        //
        // Non NumberIsFloat (no flag)                     NumberIsFloat
        //   op   | n  s  b  N  D  d  T  w  O                 op  | n  s  b  N  D  d  T  w  O
        // =======|====================================    =======|====================================
        //      ! | b  b  b  b  e  e  e  b  b                   ! | b  b  b  b  e  e  e  b  b
        //      - | n  w  w  w  w  w  w  w  w                   - | n  n  n  n  n  n  n  w  n 
        //      % | n  w  w  w  w  w  w  w  w                   % | n  n  n  n  n  n  n  w  n 

        internal static BinderCheckTypeResult CheckUnaryOpCore(IErrorContainer errorContainer, UnaryOpNode node, Features features, DType childType, bool numberIsFloat)
        {
            Contracts.AssertValue(node);

            switch (node.Op)
            {
                case UnaryOp.Not:
                    var resNot = CheckTypeCore(errorContainer, node.Child, features, childType, DType.Boolean, /* coerced: */ DType.Number, DType.Decimal, DType.String, DType.UntypedObject);
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
                        case DKind.DateTimeNoTimeZone:
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.DateTimeNoTimeZone };
                        case DKind.Decimal:
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Decimal };
                        case DKind.Number:
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Number };
                        default:
                            var resultType = numberIsFloat ? DType.Number : DType.Decimal;
                            var resDefault = CheckTypeCore(errorContainer, node.Child, features, childType, resultType, /* coerced: */ DType.String, DType.Boolean, DType.UntypedObject);
                            return new BinderCheckTypeResult() { Node = node, NodeType = resultType, Coercions = resDefault.Coercions };
                    }

                case UnaryOp.Percent:
                    switch (childType.Kind)
                    {
                        case DKind.Decimal:
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Decimal };
                        case DKind.Number:
                            return new BinderCheckTypeResult() { Node = node, NodeType = DType.Number };
                        default:
                            var resultType = numberIsFloat ? DType.Number : DType.Decimal;
                            var resPercent = CheckTypeCore(errorContainer, node.Child, features, childType, resultType, /* coerced: */ DType.Date, DType.DateTime, DType.DateTimeNoTimeZone, DType.Time, DType.String, DType.Boolean, DType.UntypedObject);
                            return new BinderCheckTypeResult() { Node = node, NodeType = resultType, Coercions = resPercent.Coercions };
                    }

                default:
                    Contracts.Assert(false);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Error };
            }
        }

        // REVIEW ragru: Introduce a TexlOperator abstract base plus various subclasses
        // for handling operators and their overloads. That will offload the burden of dealing with
        // operator special cases to the various operator classes.
        public static BinderCheckTypeResult CheckBinaryOpCore(IErrorContainer errorContainer, BinaryOpNode node, Features features, DType leftType, DType rightType, bool numberIsFloat)
        {
            Contracts.AssertValue(node);

            var leftNode = node.Left;
            var rightNode = node.Right;

            switch (node.Op)
            {
                case BinaryOp.Add:
                    return PostVisitBinaryOpNodeAdditionCore(errorContainer, node, features, leftType, rightType, numberIsFloat);

                case BinaryOp.Power:
                    var resLeftPow = CheckTypeCore(errorContainer, leftNode, features, leftType, DType.Number, /* coerced: */ DType.Decimal, DType.String, DType.Boolean, DType.UntypedObject);
                    var resRightPow = CheckTypeCore(errorContainer, rightNode, features, rightType, DType.Number, /* coerced: */ DType.Decimal, DType.String, DType.Boolean, DType.UntypedObject);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Number, Coercions = resLeftPow.Coercions.Concat(resRightPow.Coercions).ToList() };

                case BinaryOp.Mul:
                case BinaryOp.Div:
                    return CheckDecimalBinaryOp(errorContainer, node, features, leftType, rightType, numberIsFloat);

                case BinaryOp.Or:
                case BinaryOp.And:
                    var resLeftAnd = CheckTypeCore(errorContainer, leftNode, features, leftType, DType.Boolean, /* coerced: */ DType.Number, DType.Decimal, DType.String, DType.UntypedObject);
                    var resRightAnd = CheckTypeCore(errorContainer, rightNode, features, rightType, DType.Boolean, /* coerced: */ DType.Number, DType.Decimal, DType.String, DType.UntypedObject);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Boolean, Coercions = resLeftAnd.Coercions.Concat(resRightAnd.Coercions).ToList() };

                case BinaryOp.Concat:
                    BinderCheckTypeResult resLeftConcat;
                    BinderCheckTypeResult resRightConcat;

                    if (features.StronglyTypedBuiltinEnums)
                    {
                        if (leftType == DType.OptionSetValue && rightType == DType.OptionSetValue)
                        {
                            if (rightType.OptionSetInfo.EntityName == leftType.OptionSetInfo.EntityName &&
                                rightType.OptionSetInfo.CanConcatenateStronglyTyped)
                            {
                                return new BinderCheckTypeResult() { Node = node, NodeType = leftType };
                            }
                        }
                        else if (leftType == DType.OptionSetValue &&
                                 leftType.OptionSetInfo.CanCoerceFromBackingKind && leftType.OptionSetInfo.CanConcatenateStronglyTyped)
                        {
                            resRightConcat = CheckTypeCore(errorContainer, rightNode, features, rightType, DType.String, /* coerced: */ DType.Guid, DType.Number, DType.Decimal, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime, DType.Boolean, DType.ViewValue, DType.UntypedObject);
                            return new BinderCheckTypeResult() { Node = node, NodeType = leftType, Coercions = resRightConcat.Coercions };
                        }
                        else if (rightType == DType.OptionSetValue &&
                                 rightType.OptionSetInfo.CanCoerceFromBackingKind && rightType.OptionSetInfo.CanConcatenateStronglyTyped)
                        {
                            resLeftConcat = CheckTypeCore(errorContainer, leftNode, features, leftType, DType.String, /* coerced: */ DType.Guid, DType.Number, DType.Decimal, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime, DType.Boolean, DType.ViewValue, DType.UntypedObject);
                            return new BinderCheckTypeResult() { Node = node, NodeType = rightType, Coercions = resLeftConcat.Coercions };
                        }
                    }

                    if (features.PowerFxV1CompatibilityRules)
                    {
                        resLeftConcat = CheckTypeCore(errorContainer, leftNode, features, leftType, DType.String, /* coerced: */ DType.Guid, DType.Number, DType.Decimal, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime, DType.Boolean, DType.ViewValue, DType.UntypedObject);
                        resRightConcat = CheckTypeCore(errorContainer, rightNode, features, rightType, DType.String, /* coerced: */ DType.Guid, DType.Number, DType.Decimal, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime, DType.Boolean, DType.ViewValue, DType.UntypedObject);
                    }
                    else
                    {
                        resLeftConcat = CheckTypeCore(errorContainer, leftNode, features, leftType, DType.String, /* coerced: */ DType.Number, DType.Decimal, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime, DType.Boolean, DType.ViewValue, DType.UntypedObject);
                        resRightConcat = CheckTypeCore(errorContainer, rightNode, features, rightType, DType.String, /* coerced: */ DType.Number, DType.Decimal, DType.Date, DType.Time, DType.DateTimeNoTimeZone, DType.DateTime, DType.Boolean, DType.ViewValue, DType.UntypedObject);
                    }

                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.String, Coercions = resLeftConcat.Coercions.Concat(resRightConcat.Coercions).ToList() };

                case BinaryOp.Error:
                    errorContainer.EnsureError(DocumentErrorSeverity.Severe, node, TexlStrings.ErrOperatorExpected);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Error };

                case BinaryOp.Equal:
                case BinaryOp.NotEqual:
                    var resEq = CheckEqualArgTypesCore(errorContainer, leftNode, rightNode, features.PowerFxV1CompatibilityRules, leftType, rightType, numberIsFloat);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Boolean, Coercions = resEq.Coercions };

                case BinaryOp.Less:
                case BinaryOp.LessEqual:
                case BinaryOp.Greater:
                case BinaryOp.GreaterEqual:
                    // Excel's type coercion for inequality operators is inconsistent / borderline wrong, so we can't
                    // use it as a reference. For example, in Excel '2 < TRUE' produces TRUE, but so does '2 < FALSE'.
                    // Sticking to a restricted set of numeric-like types for now until evidence arises to support the need for coercion.
                    var resOrder = CheckComparisonArgTypesCore(errorContainer, leftNode, rightNode, features, leftType, rightType, numberIsFloat);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Boolean, Coercions = resOrder.Coercions };

                case BinaryOp.In:
                case BinaryOp.Exactin:
                    var resIn = CheckInArgTypesCore(errorContainer, leftNode, rightNode, leftType, rightType, features);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Boolean, Coercions = resIn.Coercions };

                default:
                    Contracts.Assert(false);
                    return new BinderCheckTypeResult() { Node = node, NodeType = DType.Error };
            }
        }

        public static bool TryGetConstantValue(CheckTypesContext context, TexlNode node, out string nodeValue)
        {
            Contracts.AssertValue(node);
            nodeValue = null;
            switch (node.Kind)
            {
                case NodeKind.StrLit:
                    nodeValue = node.AsStrLit().Value;
                    return true;
                case NodeKind.BinaryOp:
                    var binaryOpNode = node.AsBinaryOp();
                    if (binaryOpNode.Op == BinaryOp.Concat)
                    {
                        if (TryGetConstantValue(context, binaryOpNode.Left, out var left) && TryGetConstantValue(context, binaryOpNode.Right, out var right))
                        {
                            nodeValue = string.Concat(left, right);
                            return true;
                        }
                    }

                    break;
                case NodeKind.Call:
                    var callNode = node.AsCall();
                    if (callNode.Head.Name.Value == BuiltinFunctionsCore.Concatenate.Name)
                    {
                        var parameters = new List<string>();
                        foreach (var argNode in callNode.Args.Children)
                        {
                            if (TryGetConstantValue(context, argNode, out var argValue))
                            {
                                parameters.Add(argValue);
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (parameters.Count == callNode.Args.Count)
                        {
                            nodeValue = string.Join(string.Empty, parameters);
                            return true;
                        }
                    }

                    break;
                case NodeKind.FirstName:
                    // Possibly a non-qualified enum value
                    var firstNameNode = node.AsFirstName();
                    if (context.NameResolver.Lookup(firstNameNode.Ident.Name, out var firstNameInfo, NameLookupPreferences.None))
                    {
                        if (firstNameInfo.Kind == BindKind.Enum)
                        {
                            if (firstNameInfo.Data is string enumValue)
                            {
                                nodeValue = enumValue;
                                return true;
                            }
                        }
                    }

                    break;
                case NodeKind.DottedName:
                    // Possibly an enumeration
                    var dottedNameNode = node.AsDottedName();
                    if (dottedNameNode.Left.Kind == NodeKind.FirstName)
                    {
                        // Strongly-typed enums
                        if (context.NameResolver.Lookup(dottedNameNode.Left.AsFirstName().Ident.Name, out NameLookupInfo nameInfo) && nameInfo.Kind == BindKind.Enum)
                        {
                            if (nameInfo.Data is EnumSymbol enumSymbol && enumSymbol.TryGetValue(dottedNameNode.Right.Name, out OptionSetValue osv))
                            {
                                nodeValue = osv.ToObject().ToString();
                                return true;
                            }
                        }

                        // With strongly-typed enums disabled
                        DType enumType = DType.Invalid;
                        if (context.NameResolver.EntityScope?.TryGetNamedEnum(dottedNameNode.Left.AsFirstName().Ident.Name, out enumType) ?? false)
                        {
                            if (enumType.TryGetEnumValue(dottedNameNode.Right.Name, out var enumValue))
                            {
                                if (enumValue is string strValue)
                                {
                                    nodeValue = strValue;
                                    return true;
                                }
                            }
                        }
                    }

                    break;
            }

            return false;
        }
    }
}
