// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using IRCallNode = Microsoft.PowerFx.Core.IR.Nodes.CallNode;

namespace Microsoft.PowerFx.Core.IR
{
    // IR has already:
    // - resolved everything to logical names.
    // - resolved implicit ThisRecord
    internal class DependencyVisitor : IRNodeVisitor<DependencyVisitor.RetVal, DependencyVisitor.DependencyContext>
    {
        // Track reults.
        public DependencyInfo Info { get; private set; } = new DependencyInfo();

        public DependencyVisitor()
        {
        }

        public override RetVal Visit(TextLiteralNode node, DependencyContext context)
        {
            return null;
        }

        public override RetVal Visit(NumberLiteralNode node, DependencyContext context)
        {
            return null;
        }

        public override RetVal Visit(BooleanLiteralNode node, DependencyContext context)
        {
            return null;
        }

        public override RetVal Visit(DecimalLiteralNode node, DependencyContext context)
        {
            return null;
        }

        public override RetVal Visit(ColorLiteralNode node, DependencyContext context)
        {
            return null;
        }

        public override RetVal Visit(RecordNode node, DependencyContext context)
        {
            // Read all the fields. The context will determine if the record is referencing a data source
            foreach (var kv in node.Fields)
            {
                AddField(context, context.TableType?.TableSymbolName, kv.Key.Value);
            }

            return null;
        }

        public override RetVal Visit(ErrorNode node, DependencyContext context)
        {
            return null;
        }

        public override RetVal Visit(LazyEvalNode node, DependencyContext context)
        {
            return node.Child.Accept(this, context);
        }

        private readonly Dictionary<int, FormulaType> _scopeTypes = new Dictionary<int, FormulaType>();

        public override RetVal Visit(CallNode node, DependencyContext context)
        {
            if (node.Scope != null)
            {
                // Functions with more complex scoping will be handled by the function itself.
                var arg0 = node.Args[0];
                _scopeTypes[node.Scope.Id] = arg0.IRContext.ResultType;
            }

            node.Function.ComposeDependencyInfo(node, this, context);

            return null;
        }

        public override RetVal Visit(BinaryOpNode node, DependencyContext context)
        {
            node.Left.Accept(this, context);
            node.Right.Accept(this, context);
            return null;
        }

        public override RetVal Visit(UnaryOpNode node, DependencyContext context)
        {
            return node.Child.Accept(this, context);
        }

        public override RetVal Visit(ScopeAccessNode node, DependencyContext context)
        {
            // Could be a symbol from RowScope.
            // Price in "LookUp(t1,Price=255)"
            if (node.Value is ScopeAccessSymbol sym)
            {
                if (_scopeTypes.TryGetValue(sym.Parent.Id, out var type))
                {
                    // Ignore ThisRecord scopeaccess node. e.g. Summarize(table, f1, Sum(ThisGroup, f2)) where ThisGroup should be ignored.
                    if (type is TableType tableType && node.IRContext.ResultType is not AggregateType)
                    {
                        var tableLogicalName = tableType.TableSymbolName;
                        var fieldLogicalName = sym.Name.Value;

                        AddField(context, tableLogicalName, fieldLogicalName);

                        return null;
                    }
                }
            }

            // Any symbol access here is some temporary local, and not a field.
            return null;
        }

        // field              // IR will implicity recognize as ThisRecod.field
        // ThisRecord.field   // IR will get type of ThisRecord
        // First(Remote).Data // IR will get type on left of dot.
        public override RetVal Visit(RecordFieldAccessNode node, DependencyContext context)
        {
            node.From.Accept(this, context);

            var ltype = node.From.IRContext.ResultType;
            if (ltype is RecordType ltypeRecord)
            {
                // Logical name of the table on left side.
                // This will be null for non-dataverse records
                var tableLogicalName = ltypeRecord.TableSymbolName;
                if (tableLogicalName != null)
                {
                    var fieldLogicalName = node.Field.Value;
                    AddField(context, tableLogicalName, fieldLogicalName);
                }
            }

            return null;
        }

        public override RetVal Visit(ResolvedObjectNode node, DependencyContext context)
        {
            if (node.IRContext.ResultType is AggregateType aggregateType)
            {
                var tableLogicalName = aggregateType.TableSymbolName;
                if (context.WriteState)
                {
                    tableLogicalName = context.TableType?.TableSymbolName;
                }

                if (tableLogicalName != null)
                {
                    AddField(context, tableLogicalName, null);
                }
            }

            // Check if identifer is a field access on a table in row scope
            var obj = node.Value;
            if (obj is NameSymbol sym)
            {
                if (sym.Owner is SymbolTableOverRecordType symTable)
                {
                    RecordType type = symTable.Type;
                    var tableLogicalName = type.TableSymbolName;

                    if (symTable.IsThisRecord(sym))
                    {
                        // "ThisRecord". Whole entity
                        AddField(context, tableLogicalName, null);
                        return null;
                    }

                    // on current table
                    var fieldLogicalName = sym.Name;

                    AddField(context, tableLogicalName, fieldLogicalName);
                }
            }

            return null;
        }

        public override RetVal Visit(SingleColumnTableAccessNode node, DependencyContext context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(ChainingNode node, DependencyContext context)
        {
            foreach (var child in node.Nodes)
            {
                child.Accept(this, context);
            }

            return null;
        }

        public override RetVal Visit(AggregateCoercionNode node, DependencyContext context)
        {
            foreach (var kv in node.FieldCoercions)
            {
                kv.Value.Accept(this, context);
            }

            return null;
        }

        public class RetVal
        {
        }

        public class DependencyContext
        {
            public bool WriteState { get; set; }

            public TableType TableType { get; set; }

            public DependencyContext()
            {
            }
        }

        // Translate relationship names to actual field references.
        public string Translate(string tableLogicalName, string fieldLogicalName)
        {
            return fieldLogicalName;
        }

        // if fieldLogicalName, then we're taking a dependency on entire record.
        private void AddField(Dictionary<string, HashSet<string>> list, string tableLogicalName, string fieldLogicalName)
        {
            if (tableLogicalName == null)
            {
                return;
            }

            if (!list.TryGetValue(tableLogicalName, out var fieldReads))
            {
                fieldReads = new HashSet<string>();
                list[tableLogicalName] = fieldReads;
            }
            
            if (fieldLogicalName != null)
            {
                var name = Translate(tableLogicalName, fieldLogicalName);
                fieldReads.Add(fieldLogicalName);
            }
        }

        public void AddFieldRead(string tableLogicalName, string fieldLogicalName)
        {
            if (Info.FieldReads == null)
            {
                Info.FieldReads = new Dictionary<string, HashSet<string>>();
            }

            AddField(Info.FieldReads, tableLogicalName, fieldLogicalName);
        }

        public void AddFieldWrite(string tableLogicalName, string fieldLogicalName)
        {
            if (Info.FieldWrites == null)
            {
                Info.FieldWrites = new Dictionary<string, HashSet<string>>();
            }

            AddField(Info.FieldWrites, tableLogicalName, fieldLogicalName);
        }

        public void AddField(DependencyContext context, string tableLogicalName, string fieldLogicalName)
        {
            if (context.WriteState)
            {
                AddFieldWrite(tableLogicalName, fieldLogicalName);
            }
            else
            {
                AddFieldRead(tableLogicalName, fieldLogicalName);
            }
        }
    }

    /// <summary>
    /// Capture Dataverse field-level reads and writes within a formula.
    /// </summary>
    public class DependencyInfo
    {
#pragma warning disable CS1570 // XML comment has badly formed XML
        /// <summary>
        /// A dictionary of field logical names on related records, indexed by the related entity logical name.
        /// </summary>
        /// <example>
        /// On account, the formula "Name & 'Primary Contact'.'Full Name'" would return
        ///    "contact" => { "fullname" }
        /// The formula "Name & 'Primary Contact'.'Full Name' & Sum(Contacts, 'Number Of Childeren')" would return
        ///    "contact" => { "fullname", "numberofchildren" }.
        /// </example>
        public Dictionary<string, HashSet<string>> FieldReads { get; set; }
#pragma warning restore CS1570 // XML comment has badly formed XML

        public Dictionary<string, HashSet<string>> FieldWrites { get; set; }

        public bool HasWrites => FieldWrites != null && FieldWrites.Count > 0;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            DumpHelper(sb, "Read", FieldReads);
            DumpHelper(sb, "Write", FieldWrites);

            return sb.ToString();
        }

        private static void DumpHelper(StringBuilder sb, string kind, Dictionary<string, HashSet<string>> dict)
        {
            if (dict != null)
            {
                foreach (var kv in dict)
                {
                    sb.Append(kind);
                    sb.Append(" ");
                    sb.Append(kv.Key);
                    sb.Append(": ");

                    bool first = true;
                    foreach (var x in kv.Value)
                    {
                        if (!first)
                        {
                            sb.Append(", ");
                        }

                        first = false;
                        sb.Append(x);
                    }

                    sb.AppendLine("; ");
                }
            }
        }
    }
}
