// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;

// Use Tabs correctly
#pragma warning disable SA1027

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    /*
        CREATE TABLE [dbo].[Functions](
	        [BuildId] [int] NOT NULL,
	        [BuildNumber] [nchar](20) NOT NULL,
	        [Category] [nchar](30) NOT NULL,
	        [ConnectorName] [nvarchar](180) NOT NULL,
			[FunctionName] [nvarchar](180) NOT NULL,
			[IsSupported] [bit] NULL,
			[NotSupportedReason] [nvarchar](max) NULL,
            [Warnings] [nvarchar](max) NULL,
	        [IsDeprecated] [bit] NULL,
	        [IsInternal] [bit] NULL,
			[IsPageable] [bit] NULL,
			[ArityMin] [int] NOT NULL,
			[ArityMax] [int] NOT NULL,
	        [RequiredParameterTypes] [nvarchar](max) NULL,
			[OptionalParameterTypes] [nvarchar](max) NULL,
			[ReturnType] [nvarchar](max) NULL,
			[Parameters] [nvarchar](max) NULL,
	        [DifferFromBaseline] [bit] NOT NULL,
	        [Differences] [nvarchar](max) NULL
        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
        GO
        
        CREATE CLUSTERED INDEX [CIX_Functions] ON [dbo].[Functions]
        (
	        [BuildId] ASC,
	        [BuildNumber] ASC,
	        [Category] ASC,
	        [ConnectorName] ASC,
			[FunctionName] ASC
        ) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
        GO
    */

    public class FunctionStat
    {
        public FunctionStat(string category, string connectorName, IYamlFunction func, IReadOnlyList<string> differences = null)
        {
            Category = category;
            ConnectorName = connectorName;

            FunctionName = func.GetName();
            IsSupported = func.HasDetailedProperties() ? func.GetIsSupported() : null;
            NotSupportedReason = func.HasDetailedProperties() ? func.GetNotSupportedReason() : null;
            Warnings = func.HasDetailedProperties() ? func.GetWarnings() : null;
            IsDeprecated = func.HasDetailedProperties() ? func.GetIsDeprecated() : null;
            IsInternal = func.HasDetailedProperties() ? func.GetIsInternal() : null;
            IsPageable = func.HasDetailedProperties() ? func.GetIsPageable() : null;
            ArityMin = func.GetArityMin();
            ArityMax = func.GetArityMax();
            RequiredParameterTypes = func.GetRequiredParameterTypes();
            OptionalParameterTypes = func.GetOptionalParameterTypes();
            ReturnType = func.GetReturnType();
            Parameters = func.GetParameterNames();

            DifferFromBaseline = differences?.Any() == true;
            Differences = differences;
        }

        public override string ToString()
        {
            return $"{Category}|{ConnectorName}|{FunctionName}|{IsSupported}|{NotSupportedReason}|{Warnings}|{IsDeprecated}|{IsInternal}|{IsPageable}|{ArityMin}|{ArityMax}|{RequiredParameterTypes}|{OptionalParameterTypes}|{ReturnType}|{Parameters}|{DifferFromBaseline}|{(Differences != null ? string.Join(", ", Differences) : "None")}";
        }

        internal string Category;
        internal string ConnectorName;
        internal string FunctionName;
        internal bool? IsSupported;
        internal string NotSupportedReason;
        internal string Warnings;
        internal bool? IsDeprecated;
        internal bool? IsInternal;
        internal bool? IsPageable;
        internal int ArityMin;
        internal int ArityMax;
        internal string RequiredParameterTypes;
        internal string OptionalParameterTypes;
        internal string ReturnType;
        internal string Parameters;
        internal bool DifferFromBaseline;
        internal IReadOnlyList<string> Differences;
    }
}
