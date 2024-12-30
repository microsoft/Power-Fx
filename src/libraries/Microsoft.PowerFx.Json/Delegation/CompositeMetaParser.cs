// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    internal sealed partial class DelegationMetadata : DelegationMetadataBase
    {
        private class ComposedMetadata
        {
            public List<OperationCapabilityMetadata> OperationCapabilityMetadataList;
            public TableType TableType;
        }

        public DelegationMetadata(DType schema, string delegationMetadataJson)
            : base(schema, new DelegationMetadataParser().Parse(delegationMetadataJson, schema))
        {
        }

        public DelegationMetadata(DType schema, List<OperationCapabilityMetadata> metadata)
            : base(schema, new CompositeCapabilityMetadata(schema, metadata))
        {
        }

        public DelegationMetadata(TableType tableType, IDelegationMetadata left, IDelegationMetadata right, IEnumerable<string> columns, IReadOnlyDictionary<string, string> columnMap)
            : this(GetComposedMetadata(tableType, left, right, columns, columnMap))
        {
        }

        private DelegationMetadata(ComposedMetadata composedMetadata)
            : base(composedMetadata.TableType._type, new CompositeCapabilityMetadata(composedMetadata.TableType._type, composedMetadata.OperationCapabilityMetadataList))
        {
        }

        // resulting metadata = all left metadata + right columns metadata
        // columnMap is used to align column names properly (dictionary key = new name, value = old name of the column)
        private static ComposedMetadata GetComposedMetadata(TableType tableType, IDelegationMetadata left, IDelegationMetadata right, IEnumerable<string> columns, IReadOnlyDictionary<string, string> columnMap)
        {
            void CopyElements<T>(Dictionary<DPath, T> sourceElements, Dictionary<DPath, T> destination)
            {
                if (sourceElements == null)
                {
                    return;
                }

                foreach (KeyValuePair<DPath, T> kvp in sourceElements)
                {
                    string renamed = columnMap.FirstOrDefault(k => k.Value == kvp.Key.Name).Key;

                    if (!string.IsNullOrEmpty(renamed))
                    {
                        destination.Add(DPath.Root.Append(new DName(renamed)), kvp.Value);
                    }
                    else
                    {
                        destination.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            Dictionary<DPath, DelegationCapability> filterColumnCapabilities = new Dictionary<DPath, DelegationCapability>();
            Dictionary<DPath, DelegationCapability> filterColumnRestrictions = new Dictionary<DPath, DelegationCapability>();
            Dictionary<DPath, DelegationCapability> sortColumnRestrictions = new Dictionary<DPath, DelegationCapability>();
            Dictionary<DPath, DelegationCapability> groupColumnRestrictions = new Dictionary<DPath, DelegationCapability>();
            Dictionary<DPath, DPath> oDataPathReplacementMap = new Dictionary<DPath, DPath>();

            CopyElements(left.FilterDelegationMetadata?.ColumnCapabilities, filterColumnCapabilities);
            CopyElements(left.FilterDelegationMetadata?.ColumnRestrictions2, filterColumnRestrictions);
            CopyElements(left.SortDelegationMetadata?.ColumnRestrictions2, sortColumnRestrictions);
            CopyElements(left.GroupDelegationMetadata?.ColumnRestrictions2, groupColumnRestrictions);
            CopyElements(left.ODataPathReplacementMap, oDataPathReplacementMap);

            RecordType recordType = RecordType.Empty();
            IEnumerable<IExpandInfo> expands = tableType._type.GetExpands();            

            foreach (string fieldName in tableType.FieldNames)
            {
                string renamed = columnMap.FirstOrDefault(k => k.Value == fieldName).Key;
                IExpandInfo expandInfo = expands.FirstOrDefault(ei => ei.Name == fieldName);
               
                recordType = recordType.Add(renamed ?? fieldName, expandInfo == null ? tableType.GetFieldType(fieldName) : new EntityType(expandInfo));
            }

            if (columns != null && right != null)
            {
                foreach (string column in columns)
                {
                    DPath columnPath = DPath.Root.Append(new DName(column));

                    if (right.FilterDelegationMetadata?.ColumnCapabilities?.TryGetValue(columnPath, out DelegationCapability filterColumnCapability) == true)
                    {
                        filterColumnCapabilities.Add(columnPath, filterColumnCapability);
                    }

                    if (right.FilterDelegationMetadata?.ColumnRestrictions2?.TryGetValue(columnPath, out DelegationCapability filterColumnRestriction) == true)
                    {
                        filterColumnRestrictions.Add(columnPath, filterColumnRestriction);
                    }

                    if (right.SortDelegationMetadata?.ColumnRestrictions2?.TryGetValue(columnPath, out DelegationCapability sortColumnRestriction) == true)
                    {
                        sortColumnRestrictions.Add(columnPath, sortColumnRestriction);
                    }

                    if (right.GroupDelegationMetadata?.ColumnRestrictions2?.TryGetValue(columnPath, out DelegationCapability groupColumnRestriction) == true)
                    {
                        groupColumnRestrictions.Add(columnPath, groupColumnRestriction);
                    }

                    if (right.ODataPathReplacementMap?.TryGetValue(columnPath, out DPath replacement) == true)
                    {
                        oDataPathReplacementMap.Add(columnPath, replacement);
                    }
                }
            }

            TableType newTableType = recordType.ToTable();

            return new ComposedMetadata()
            {
                TableType = newTableType,
                OperationCapabilityMetadataList = new List<OperationCapabilityMetadata>()
                {
                    new FilterOpMetadata(newTableType._type, filterColumnRestrictions, filterColumnCapabilities, left.FilterDelegationMetadata.DefaultColumnCapabilities, null),
                    new SortOpMetadata(newTableType._type, sortColumnRestrictions),
                    new GroupOpMetadata(newTableType._type, groupColumnRestrictions),
                    new ODataOpMetadata(newTableType._type, oDataPathReplacementMap)
                }
            };
        }

        // We use this class to avoid calling DType.GetExpandedEntityType in FormulaType.Build which can be expensive
        private sealed class EntityType : FormulaType
        {
            public EntityType(IExpandInfo expandInfo)
                : base(DType.CreateExpandType(expandInfo.Clone()))
            {
            }

            public override void Visit(ITypeVisitor vistor)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class CompositeMetaParser : MetaParser
        {
            private readonly List<MetaParser> _metaParsers;

            public CompositeMetaParser()
            {
                _metaParsers = new List<MetaParser>();
            }

            public override OperationCapabilityMetadata Parse(JsonElement dataServiceCapabilitiesJsonObject, DType schema)
            {
                Contracts.AssertValid(schema);

                var capabilities = new List<OperationCapabilityMetadata>();
                foreach (var parser in _metaParsers)
                {
                    var capabilityMetadata = parser.Parse(dataServiceCapabilitiesJsonObject, schema);
                    if (capabilityMetadata != null)
                    {
                        capabilities.Add(capabilityMetadata);
                    }
                }

                return new CompositeCapabilityMetadata(schema, capabilities);
            }

            public void AddMetaParser(MetaParser metaParser)
            {
                Contracts.AssertValue(metaParser);

                _metaParsers.Add(metaParser);
            }
        }        
    }
}
