// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    // This is the standard formatter, which handles tables in a tabular form.
    // This differs from the minimal formatter which uses .ToExpression().
    public class StandardFormatter : ValueFormatter
    {
        // Include hash codes for objects, useful for debugging references through mutations.
        public bool HashCodes = false;

        // Format tables in tabular rows and columns.
        // Without this, tables are formatted in Table({},{},...) notation.
        public bool FormatTable = true;

        // Use a tight representation, leaving out details.
        // Tables are returned as <Table> and records as <Record>.
        public bool Minimal = false;

        private string FormatRecordCore(RecordValue record)
        {
            string resultString = string.Empty;

            var separator = string.Empty;
            if (HashCodes)
            {
                resultString += "#" + record.GetHashCode() + "#";
            }

            resultString += "{";
            foreach (var field in record.Fields)
            {
                resultString += separator + $"{field.Name}:";
                resultString += FormatField(field);
                separator = ", ";
            }

            resultString += "}";

            return resultString;
        }

        // Avoid traversing entity references.
        private string FormatField(NamedValue field)
        { 
            return field.IsExpandEntity ? "<reference>" : this.Format(field.Value);            
        }

        private string FormatTableCore(TableValue table)
        {
            // Dispatch to appropriate table formatting. 
            if (this.TryFormatTablePrimaryKeys(table, out var resultValue))
            {
                return resultValue;
            }

            return this.FormatStandardTable(table);
        }

        // If table has PrimaryId,PrimaryName, then format with just those fields. 
        // else, return false.
        private bool TryFormatTablePrimaryKeys(TableValue table, out string resultValue)
        {
            var firstRow = table.Rows.FirstOrDefault();
            if (firstRow != null && firstRow.IsValue)
            {
                var record = firstRow.Value;
                if (record.TryGetSpecialFieldValue(SpecialFieldKind.PrimaryKey, out _) &&
                    record.TryGetSpecialFieldName(SpecialFieldKind.PrimaryName, out _))
                {
                    // Has special fields, print those. 

                    int maxN = 10;
                    var drows = table.Rows.Take(maxN);

                    StringBuilder sb = new StringBuilder();
                    foreach (var drow in drows)
                    {
                        if (drow.IsValue)
                        {
                            var row = drow.Value;

                            if (row.TryGetSpecialFieldValue(SpecialFieldKind.PrimaryKey, out var keyValue) &&
                                row.TryGetSpecialFieldValue(SpecialFieldKind.PrimaryName, out var nameValue))
                            {
                                // These should be scalars. 
                                var keyStr = this.Format(keyValue);
                                var nameStr = this.Format(nameValue);

                                sb.AppendLine($"{keyStr}: {nameStr}");
                            }
                        }
                    }

                    resultValue = sb.ToString();
                    return true;
                }
            }

            resultValue = null;
            return false;
        }

        private string FormatStandardTable(TableValue table)
        {
            string resultString = string.Empty;

            var columnCount = 0;
            foreach (var row in table.Rows)
            {
                if (row.Value != null)
                {
                    columnCount = Math.Max(columnCount, row.Value.Fields.Count());
                    break;
                }
            }

            if (columnCount == 0)
            {
                return Minimal ? string.Empty : "Table()";
            }

            var columnWidth = new int[columnCount];

            foreach (var row in table.Rows)
            {
                if (row.Value != null)
                {
                    var column = 0;
                    foreach (var field in row.Value.Fields)
                    {
                        columnWidth[column] = Math.Max(columnWidth[column], Format(field.Value).Length);
                        column++;
                    }
                }
            }

            if (HashCodes)
            {
                resultString += "#" + table.GetHashCode() + "#";
            }

            // special treatment for single column table named Value
            if (columnWidth.Length == 1 && table.Rows.First().Value != null && table.Rows.First().Value.Fields.First().Name == "Value")
            {
                var separator = string.Empty;
                resultString += "[";
                foreach (var row in table.Rows)
                {
                    resultString += separator;

                    if (HashCodes)
                    {
                        resultString += "#" + row.Value.GetHashCode() + "# ";
                    }

                    resultString += FormatField(row.Value.Fields.First());
                    separator = ", ";
                }

                resultString += "]";
            }

            // otherwise a full table treatment is needed
            else if (FormatTable)
            {
                resultString += "\n ";
                var column = 0;

                foreach (var row in table.Rows)
                {
                    if (row.Value != null)
                    {
                        column = 0;
                        foreach (var field in row.Value.Fields)
                        {
                            columnWidth[column] = Math.Max(columnWidth[column], field.Name.Length);
                            resultString += " " + field.Name.PadLeft(columnWidth[column]) + "  ";
                            column++;
                        }

                        break;
                    }
                }

                resultString += "\n ";

                foreach (var width in columnWidth)
                {
                    resultString += new string('=', width + 2) + " ";
                }

                foreach (var row in table.Rows)
                {
                    column = 0;
                    resultString += "\n ";
                    if (row.Value != null)
                    {
                        foreach (var field in row.Value.Fields)
                        {
                            resultString += " " + FormatField(field).PadLeft(columnWidth[column]) + "  ";
                            column++;
                        }
                    }
                    else
                    {
                        resultString += row.IsError ? row.Error?.Errors?[0].Message : "Blank()";
                    }
                }

                resultString += "\n";
            }
            else
            {
                // table without formatting 

                resultString = "[";
                var separator = string.Empty;
                foreach (var row in table.Rows)
                {
                    resultString += separator;

                    if (HashCodes)
                    {
                        resultString += "#" + row.Value.GetHashCode() + "# ";
                    }

                    resultString += Format(row.Value);
                    separator = ", ";
                }

                resultString += "]";
            }

            return resultString;
        }

        public override string Format(FormulaValue value)
        {
            string resultString = string.Empty;

            if (value is BlankValue)
            {
                resultString = Minimal ? string.Empty : "Blank()";
            }
            else if (value is ErrorValue errorValue)
            {
                resultString = Minimal ? "<error>" : "<Error: " + errorValue.Errors[0].Message + ">";
            }
            else if (value is UntypedObjectValue)
            {
                resultString = Minimal ? "<untyped>" : "<Untyped: Use Value, Text, Boolean, or other functions to establish the type>";
            }
            else if (value is StringValue str)
            {
                resultString = Minimal ? str.Value : str.ToExpression();
            }
            else if (value is RecordValue record)
            {
                if (Minimal)
                {
                    resultString = "<record>";
                }
                else
                {
                 resultString = this.FormatRecordCore(record);
                }
            }
            else if (value is TableValue table)
            {
                if (Minimal)
                {
                    resultString = "<table>";
                }
                else
                {
                    resultString = this.FormatTableCore(table);
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
