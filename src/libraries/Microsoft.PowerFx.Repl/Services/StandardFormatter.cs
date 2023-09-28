// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Repl.Services
{
    // This is the standard formatter, which handles tables in a tabular form.
    // This differs from the minimal formatter which uses .ToExpression().
    public class StandardFormatter : ValueFormatter
    {
        // Include hash codes for objects, useful for debugging references through mutations.
        public bool HashCodes { get; set; } = false;

        // Format tables in tabular rows and columns.
        // Without this, tables are formatted in Table({},{},...) notation.
        public bool FormatTable { get; set; } = true;

        // Use a tight representation, leaving out details.
        // Tables are returned as <Table> and records as <Record>.
        public bool Minimal { get; set; } = false;

        // Maximum number of records to show when formatting a table.
        // Set to Int32.MaxInt to remove restriction.
        public int MaxTableRows { get; set; } = 10;

        private string FormatRecordCore(RecordValue record)
        {
            return FormatStandardRecord(record, TryGetSpecialFieldNames(record));
        }

        private string FormatStandardRecord(RecordValue record, HashSet<string> selectedFieldNames)
        {
            StringBuilder resultString = new StringBuilder();

            IEnumerable<string> fieldNames = selectedFieldNames ?? record.Type.FieldNames;

            var separator = string.Empty;
            if (HashCodes)
            {
                resultString.Append("#" + record.GetHashCode() + "#");
            }

            resultString.Append("{");
            foreach (var field in record.Fields)
            {
                if (fieldNames.Contains(field.Name))
                {
                    resultString.Append(separator);
                    resultString.Append(field.Name);
                    resultString.Append(':');
                    resultString.Append(FormatField(field));
                    separator = ", ";
                }
            }

            if (selectedFieldNames != null)
            {
                resultString.Append(separator);
                resultString.Append("...");
            }

            resultString.Append("}");

            return resultString.ToString();
        }

        // Avoid traversing entity references.
        private string FormatField(NamedValue field, bool minimal = false)
        {
            return field.IsExpandEntity ? "<reference>" : FormatValue(field.Value, minimal);
        }

        private string FormatTableCore(TableValue table)
        {
            return FormatStandardTable(table, TryGetSpecialFieldNames(table));
        }

        private HashSet<string> TryGetSpecialFieldNames(TableValue table)
        {
            var firstRow = table.Rows.FirstOrDefault();
            if (firstRow != null && firstRow.IsValue)
            {
                return TryGetSpecialFieldNames(firstRow.Value);
            }

            return null;
        }

        private HashSet<string> TryGetSpecialFieldNames(RecordValue record)
        {
            if (record.TryGetSpecialFieldName(SpecialFieldKind.PrimaryKey, out var primaryKey) &&
                record.TryGetSpecialFieldName(SpecialFieldKind.PrimaryName, out var primaryName))
            { 
                    return new HashSet<string>() { primaryName, primaryKey };
            }

            return null;
        }

        private string FormatStandardTable(TableValue table, HashSet<string> selectedFieldNames)
        {
            StringBuilder resultString;

            IEnumerable<string> fieldNames = selectedFieldNames ?? table.Type.FieldNames;

            if (Minimal)
            {
                resultString = new StringBuilder("<table>");
            }

            // special treatment for single column table named Value
            else if (table.Rows.Count() > 0 && 
                     table.Rows.First().Value?.Fields.Count() == 1 && 
                     table.Rows.First().Value?.Fields.First().Name == "Value")
            {
                var separator = string.Empty;
                resultString = new StringBuilder("[");
                foreach (var row in table.Rows)
                {
                    resultString.Append(separator);
                    resultString.Append(FormatField(row.Value.Fields.First(), false));
                    separator = ", ";
                }

                resultString.Append("]");
            }
            else
            {
                // otherwise a full table treatment is needed

                var columnCount = fieldNames.Count();

                if (columnCount == 0)
                {
                    return Minimal ? string.Empty : "Table()";
                }

                var columnWidth = new int[columnCount];

                var maxRows = MaxTableRows;
                foreach (var row in table.Rows)
                {
                    if (row.Value != null)
                    {
                        if (maxRows-- <= 0)
                        {
                            break;
                        }

                        var column = 0;

                        foreach (NamedValue field in row.Value.Fields)
                        {
                            if (fieldNames.Contains(field.Name))
                            {
                                columnWidth[column] = Math.Max(columnWidth[column], FormatField(field, true).Length);
                                column++;
                            }
                        }
                    }
                }

                if (FormatTable)
                {
                    resultString = new StringBuilder("\n ");
                    var column = 0;

                    foreach (var row in table.Rows)
                    {
                        if (row.Value != null)
                        {
                            column = 0;

                            foreach (NamedValue field in row.Value.Fields)
                            {
                                if (fieldNames.Contains(field.Name))
                                {
                                    columnWidth[column] = Math.Max(columnWidth[column], field.Name.Length);
                                    resultString.Append(' ');
                                    resultString.Append(field.Name.PadRight(columnWidth[column]));
                                    resultString.Append("  ");
                                    column++;
                                }
                            }

                            if (selectedFieldNames != null)
                            {
                                resultString.Append(" ...");
                            }

                            break;
                        }
                    }

                    resultString.Append("\n ");

                    foreach (var row in table.Rows)
                    {
                        if (row.Value != null)
                        {
                            column = 0;

                            foreach (NamedValue field in row.Value.Fields)
                            {
                                if (fieldNames.Contains(field.Name))
                                {
                                    resultString.Append(new string('=', columnWidth[column] + 2));
                                    resultString.Append(' ');
                                    column++;
                                }
                            }

                            if (selectedFieldNames != null)
                            {
                                resultString.Append("=====");
                            }

                            break;
                        }
                    }

                    var maxRows2 = MaxTableRows;
                    foreach (var row in table.Rows)
                    {
                        if (maxRows2-- <= 0)
                        {
                            break;
                        }

                        column = 0;
                        resultString.Append("\n ");
                        if (row.Value != null)
                        {
                            foreach (NamedValue field in row.Value.Fields)
                            {
                                if (fieldNames.Contains(field.Name))
                                {
                                    resultString.Append(' ');
                                    resultString.Append(FormatField(field, true).PadRight(columnWidth[column]));
                                    resultString.Append("  ");
                                    column++;
                                }
                            }
                        }
                        else
                        {
                            resultString.Append(row.IsError ? row.Error?.Errors?[0].Message : "Blank()");
                        }
                    }

                    resultString.Append("\n ");

                    if (maxRows2 < 0)
                    {
                        resultString.Append($" (showing first {MaxTableRows} records)\n ");
                    }
                }
                else
                {
                    // table without formatting 

                    resultString = new StringBuilder("[");
                    var separator = string.Empty;
                    foreach (var row in table.Rows)
                    {
                        resultString.Append(separator);
                        resultString.Append(FormatRecordCore(row.Value));
                        separator = ", ";
                    }

                    resultString.Append(']');
                }
            }

            return resultString.ToString();
        }

        public override string Format(FormulaValue value)
        {
            return this.FormatValue(value, Minimal);
        }

        public string FormatValue(FormulaValue value, bool minimalLocal)
        {
            var resultString = string.Empty;

            if (value is BlankValue)
            {
                resultString = minimalLocal ? string.Empty : "Blank()";
            }
            else if (value is ErrorValue errorValue)
            {
                resultString = minimalLocal ? "<error>" : "<Error: " + errorValue.Errors[0].Message + ">";
            }
            else if (value is UntypedObjectValue)
            {
                resultString = minimalLocal ? "<untyped>" : "<Untyped: Use Value, Text, Boolean, or other functions to establish the type>";
            }
            else if (value is StringValue str)
            {
                resultString = minimalLocal ? str.Value : str.ToExpression();
            }
            else if (value is RecordValue record)
            {
                if (minimalLocal)
                {
                    resultString = "<record>";
                }
                else
                {
                    resultString = FormatRecordCore(record);
                }
            }
            else if (value is TableValue table)
            {
                if (minimalLocal)
                {
                    resultString = "<table>";
                }
                else
                {
                    resultString = FormatTableCore(table);
                }
            }
            else
            {
                var sb = new StringBuilder();
                var settings = new FormulaValueSerializerSettings()
                {
                    UseCompactRepresentation = true,
                };
                value.ToExpression(sb, settings);

                resultString = sb.ToString();
            }

            return resultString;
        }
    }
}
