// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
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
                typeCheckSucceeded = maybeFunc.CheckInvocation(txb, args, argTypes, warnings, out returnType, out nodeToCoercedTypeMap);

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
    }
}
