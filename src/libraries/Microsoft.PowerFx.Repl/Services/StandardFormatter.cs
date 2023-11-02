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

        // Append a newline to the end of a formatted table.
        // If the REPL does not include a newline for the prompt, set this to true for proper spacing.
        public bool FormatTableNewLine { get; set; } = false;

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

            if (Minimal)
            {
                return "<table>";
            }

            var fieldNames = (selectedFieldNames ?? table.Type.FieldNames).ToArray();
            if (fieldNames.Length == 0)
            {
                return Minimal ? string.Empty : "Table()";
            }

            // special treatment for empty table
            if (!table.Rows.Any())
            {
                return "\n<empty table>";
            }

            // special treatment for single column table named Value
            if (fieldNames.Length == 1 && fieldNames[0] == "Value")
            {
                var separator = string.Empty;
                resultString = new StringBuilder("[");
                foreach (var row in table.Rows)
                {
                    resultString.Append(separator);
                    resultString.Append(FormatField(row.Value.Fields.First(), false));
                    separator = ", ";
                }

                return resultString.Append("]").ToString();
            }

            if (!FormatTable)
            {
                // table without formatting 
                // REVIEW: should this honor MaxTableRows?

                resultString = new StringBuilder("[");
                var separator = string.Empty;
                foreach (var row in table.Rows)
                {
                    resultString.Append(separator);
                    resultString.Append(FormatRecordCore(row.Value));
                    separator = ", ";
                }

                return resultString.Append(']').ToString();
            }

            // otherwise a full table treatment is needed
            var colMap = new Dictionary<string, int>();
            var columnWidth = new int[fieldNames.Length];
            for (int i = 0; i < fieldNames.Length; i++)
            {
                var name = fieldNames[i];
                colMap[name] = i;
                columnWidth[i] = name.Length;
            }

            var maxRows = MaxTableRows;
            foreach (var row in table.Rows)
            {
                if (maxRows-- <= 0)
                {
                    break;
                }

                if (row.Value != null)
                {
                    foreach (var field in row.Value.Fields)
                    {
                        if (colMap.TryGetValue(field.Name, out int column))
                        {
                            int len = FormatField(field, true).Length;
                            columnWidth[column] = Math.Max(columnWidth[column], len);
                        }
                    }
                }
            }

            resultString = new StringBuilder("\n ");
            for (int i = 0; i < fieldNames.Length; i++)
            {
                var name = fieldNames[i];
                resultString.Append(' ', columnWidth[i] - name.Length + 1).Append(name).Append("  ");
            }

            resultString.Append("\n ");
            foreach (var width in columnWidth)
            {
                resultString.Append('=', width + 2).Append(" ");
            }

            if (selectedFieldNames != null)
            {
                resultString.Append("=====");
            }

            maxRows = MaxTableRows;
            foreach (var row in table.Rows)
            {
                if (maxRows-- <= 0)
                {
                    break;
                }

                resultString.Append("\n ");
                if (row.Value != null)
                {
                    int col = 0;
                    var pairs = row.Value.Fields
                        .Select(f => (f, colMap.TryGetValue(f.Name, out int c) ? c : -1))
                        .Where(p => p.Item2 >= 0)
                        .OrderBy(p => p.Item2);

                    foreach (var (field, column) in pairs)
                    {
                        while (col < column)
                        {
                            resultString.Append(' ', columnWidth[col] + 3);
                            col++;
                        }

                        resultString.Append(' ');
                        var str = FormatField(field, true);
                        int cch = columnWidth[column] - str.Length;
                        if (cch > 0)
                        {
                            resultString.Append(' ', cch);
                        }

                        resultString.Append(str);
                        resultString.Append("  ");
                        col++;
                    }
                }
                else
                {
                    resultString.Append(row.IsError ? row.Error?.Errors?[0].Message : "Blank()");
                }
            }

            if (maxRows < 0)
            {
                resultString.Append($"\n (showing first {MaxTableRows} records)");
            }

            if (FormatTableNewLine)
            {
                resultString.Append("\n ");
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
