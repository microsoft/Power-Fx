// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Logging.Trackers
{
    internal sealed class DelegationTelemetryInfo
    {
        private DelegationTelemetryInfo(string info)
        {
            Contracts.AssertValue(info);

            Info = info;
        }

        public string Info { get; }

        public static DelegationTelemetryInfo CreateEmptyDelegationTelemetryInfo()
        {
            return new DelegationTelemetryInfo(string.Empty);
        }

        public static DelegationTelemetryInfo CreateBinaryOpNoSupportedInfoTelemetryInfo(BinaryOp op)
        {
            return new DelegationTelemetryInfo(op.ToString());
        }

        public static DelegationTelemetryInfo CreateUnaryOpNoSupportedInfoTelemetryInfo(UnaryOp op)
        {
            return new DelegationTelemetryInfo(op.ToString());
        }

        public static DelegationTelemetryInfo CreateDataSourceNotDelegatableTelemetryInfo(IExternalDataSource ds)
        {
            Contracts.AssertValue(ds);

            return new DelegationTelemetryInfo(ds.Name);
        }

        public static DelegationTelemetryInfo CreateUndelegatableFunctionTelemetryInfo(TexlFunction func)
        {
            Contracts.AssertValueOrNull(func);

            if (func == null)
            {
                return CreateEmptyDelegationTelemetryInfo();
            }

            return new DelegationTelemetryInfo(func.Name);
        }

        public static DelegationTelemetryInfo CreateNoDelSupportByColumnTelemetryInfo(FirstNameInfo info)
        {
            Contracts.AssertValue(info);

            return new DelegationTelemetryInfo(info.Name);
        }

        public static DelegationTelemetryInfo CreateNoDelSupportByColumnTelemetryInfo(string columnName)
        {
            Contracts.AssertNonEmpty(columnName);

            return new DelegationTelemetryInfo(columnName);
        }

        public static DelegationTelemetryInfo CreateImpureNodeTelemetryInfo(TexlNode node, TexlBinding binding = null)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValueOrNull(binding);

            switch (node.Kind)
            {
                case NodeKind.Call:
                    var callNode = node.AsCall();
                    var funcName = binding?.GetInfo(callNode)?.Function?.Name ?? string.Empty;
                    return new DelegationTelemetryInfo(funcName);
                default:
                    return new DelegationTelemetryInfo(StructuralPrint.Print(node, binding));
            }
        }

        public static DelegationTelemetryInfo CreateAsyncNodeTelemetryInfo(TexlNode node, TexlBinding binding = null)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValueOrNull(binding);

            switch (node.Kind)
            {
                case NodeKind.Call:
                    var callNode = node.AsCall();
                    var funcName = binding?.GetInfo(callNode)?.Function?.Name ?? string.Empty;
                    return new DelegationTelemetryInfo(funcName);
                default:
                    return new DelegationTelemetryInfo(StructuralPrint.Print(node, binding));
            }
        }

        public static DelegationTelemetryInfo CreateUnsupportArgTelmetryInfo(DType dType)
        {
            Contracts.AssertValue(dType);

            return new DelegationTelemetryInfo(dType.Kind.ToString());
        }

        public static DelegationTelemetryInfo CreateUnSupportedDistinctArgTelmetryInfo(int condition)
        {
            return new DelegationTelemetryInfo(condition.ToString());
        }
    }
}
