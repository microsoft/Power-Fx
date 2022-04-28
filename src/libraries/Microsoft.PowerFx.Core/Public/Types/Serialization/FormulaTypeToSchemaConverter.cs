// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core
{
    public static class FormulaTypeToSchemaConverter
    {
        public static FormulaTypeSchema Convert(FormulaType type)
        {
            var visitor = new FormulaTypeToSchemaVisitor();
            type.Visit(visitor);
            return visitor.Result;
        }

        private class FormulaTypeToSchemaVisitor : ITypeVistor
        {
            public FormulaTypeSchema Result;

#region Primitive Types
            public void Visit(BlankType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.Blank };
            }

            public void Visit(BooleanType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.Boolean };
            }

            public void Visit(NumberType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.Number };
            }

            public void Visit(StringType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.String };
            }
                        
            public void Visit(HyperlinkType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.Hyperlink };
            }

            public void Visit(GuidType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.Guid };
            }

            public void Visit(ColorType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.Color };
            }

            public void Visit(DateType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.Date };
            }

            public void Visit(DateTimeType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.DateTime };
            }

            public void Visit(DateTimeNoTimeZoneType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.DateTimeNoTimeZone };
            }

            public void Visit(TimeType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.Time };
            }

            public void Visit(UntypedObjectType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.UntypedObject };
            }

            public void Visit(UnknownType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.Unknown };
            }

            public void Visit(BindingErrorType type)
            {
                Result = new FormulaTypeSchema() { Type = FormulaTypeSchema.ParamType.Error };
            }

            #endregion
            #region Complex Types

            public void Visit(OptionSetValueType type)
            {
                Result = new FormulaTypeSchema()
                {
                    Type = FormulaTypeSchema.ParamType.OptionSetValue,
                    OptionSetName = type.OptionSetName
                };
            }

            public void Visit(RecordType type)
            {
                Result = new FormulaTypeSchema()
                {
                    Type = FormulaTypeSchema.ParamType.Record,
                    Fields = GetChildren(type)
                };
            }

            public void Visit(TableType type)
            {                
                Result = new FormulaTypeSchema()
                {
                    Type = FormulaTypeSchema.ParamType.Table,
                    Fields = GetChildren(type)
                };
            }

            private Dictionary<string, FormulaTypeSchema> GetChildren(AggregateType type)
            {
                var fields = new Dictionary<string, FormulaTypeSchema>(StringComparer.Ordinal);
                foreach (var child in type.GetNames())
                {
                    child.Type.Visit(this);
                    fields.Add(child.Name, Result);
                }

                return fields;
            }
#endregion
        }
    }
}
