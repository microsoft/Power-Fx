// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;

// Use Tabs correctly
#pragma warning disable SA1027 

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    public class ConnectorStat
    {
        /* 
            CREATE TABLE [dbo].[Connectors](
	            [BuildId] [int] NOT NULL,
	            [BuildNumber] [nchar](20) NOT NULL,
	            [Category] [nchar](30) NOT NULL,
	            [ConnectorName] [nvarchar](180) NOT NULL,
	            [Functions] [int] NOT NULL,
	            [Supported] [int] NULL,
	            [Deprecated] [int] NULL,
	            [Internal] [int] NULL,
	            [Pageable] [int] NULL,
	            [OpenApiErrors] [nvarchar](max) NULL,
	            [DifferFromBaseline] [bit] NOT NULL,
	            [Differences] [nvarchar](max) NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
            GO
        
            CREATE CLUSTERED INDEX [CIX_Connectors] ON [dbo].[Connectors]
            (
	            [BuildId] ASC,
	            [BuildNumber] ASC,
	            [Category] ASC,
	            [ConnectorName] ASC
            ) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
            GO

        */

        public ConnectorStat(string category, string connectorName, IYamlFunction[] funcs, string openApiErrorFileName, IReadOnlyList<string> differences = null)
        {
            bool hasDetailedProps = funcs.Length != 0 ? funcs[0].HasDetailedProperties() : false;

            Category = category;
            ConnectorName = connectorName;
            Functions = funcs.Length;
            Supported = hasDetailedProps ? funcs.Count(f => f.GetIsSupported()) : null;
            Deprecated = hasDetailedProps ? funcs.Count(f => f.GetIsDeprecated()) : null;
            Internal = hasDetailedProps ? funcs.Count(f => f.GetIsInternal()) : null;
            Pageable = hasDetailedProps ? funcs.Count(f => f.GetIsPageable()) : null;
            OpenApiErrors = File.Exists(openApiErrorFileName) ? string.Join(", ", File.ReadAllLines(openApiErrorFileName).Select(l => l.Trim())) : null;
            DifferFromBaseline = differences?.Any() == true;
            Differences = differences;
        }

        public override string ToString()
        {
            return $"{Category}|{ConnectorName}|{Functions}|{Supported}|{Deprecated}|{Internal}|{Pageable}|{OpenApiErrors}|{DifferFromBaseline}|{(Differences != null ? string.Join(", ", Differences) : "None")}";
        }

        internal string Category;
        internal string ConnectorName;
        internal int Functions;
        internal int? Supported;
        internal int? Deprecated;
        internal int? Internal;
        internal int? Pageable;
        internal string OpenApiErrors;
        internal bool DifferFromBaseline;
        internal IReadOnlyList<string> Differences;
    }
}
