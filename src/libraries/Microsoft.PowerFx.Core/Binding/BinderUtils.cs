// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

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
    }
}
