// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.OpenApi.Any;

namespace Microsoft.PowerFx.Connectors.Tabular
{
    // x-ms-relationships
    internal sealed class Relationship
    {
        public string TargetEntity;

        public Dictionary<string, string> ReferentialConstraints;

        public static Dictionary<string, Relationship> ParseRelationships(OpenApiObject relationships)
        {
            Dictionary<string, Relationship> relations = new Dictionary<string, Relationship>();

            foreach (KeyValuePair<string, IOpenApiAny> kvp in relationships)
            {
                Relationship rel = new Relationship();
                string name = kvp.Key;

                foreach (KeyValuePair<string, IOpenApiAny> kvp2 in kvp.Value as OpenApiObject)
                {
                    if (kvp2.Key == "targetEntity")
                    {
                        rel.TargetEntity = (kvp2.Value as OpenApiString).Value;
                    }
                    else if (kvp2.Key == "referentialConstraints")
                    {
                        rel.ReferentialConstraints = new Dictionary<string, string>();

                        foreach (KeyValuePair<string, IOpenApiAny> kvp3 in kvp2.Value as OpenApiObject)
                        {
                            rel.ReferentialConstraints.Add(kvp3.Key, ((kvp3.Value as OpenApiObject)["referencedProperty"] as OpenApiString).Value);
                        }
                    }
                }

                relations.Add(name, rel);
            }

            return relations;
        }
    }
}
