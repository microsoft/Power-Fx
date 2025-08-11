// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Linq;
using System.Numerics;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    // Extensions, extracted from Utils 
    internal static partial class Utils2
    {
        public static bool TestBit(this BigInteger value, int bitIndex)
        {
            Contracts.Assert(bitIndex >= 0);

            return !(value & (BigInteger.One << bitIndex)).IsZero;
        }

        public static string GetLocalizedName(this FunctionCategories category, CultureInfo culture)
        {            
            return StringResources.Get(category.ToString(), culture.Name);
        }

        public static void FunctionSupportColumnNamesAsIdentifiersDependencyUtil(this CallNode node, DependencyVisitor visitor)
        {
            var aggregateType0 = node.Args[0].IRContext.ResultType as AggregateType;

            foreach (TextLiteralNode arg in node.Args.Skip(1).Where(a => a is TextLiteralNode))
            {
                if (aggregateType0.TryGetFieldType(arg.LiteralValue, out _))
                {
                    visitor.AddDependency(aggregateType0.TableSymbolName, arg.LiteralValue);
                }
            }
        }
    }
}
