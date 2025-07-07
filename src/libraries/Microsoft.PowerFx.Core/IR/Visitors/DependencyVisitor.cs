// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Core.IR
{
    // IR has already:
    // - resolved everything to logical names.
    // - resolved implicit ThisRecord
    internal sealed class DependencyVisitor : IRNodeVisitor<DependencyVisitor.RetVal, DependencyVisitor.DependencyContext>
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

        public override RetVal Visit(UnitsLiteralNode node, DependencyContext context)
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
            // Visit all field values in case there are CallNodes. Field keys should be handled by the function caller.
            foreach (var kv in node.Fields)
            {
                kv.Value.Accept(this, context);
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
                    if (type is TableType tableType && tableType.TryGetFieldType(sym.Name.Value, out _))
                    {
                        AddDependency(tableType.TableSymbolName, sym.Name.Value);

                        return null;
                    }
                }
            }
           
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
                    AddDependency(tableLogicalName, fieldLogicalName);
                }
            }

            return null;
        }

        public override RetVal Visit(ResolvedObjectNode node, DependencyContext context)
        {
            if (node.IRContext.ResultType is AggregateType aggregateType)
            {
                AddDependency(aggregateType.TableSymbolName, null);
            }

            CheckResolvedObjectNodeValue(node, context);

            return null;
        }

        public void CheckResolvedObjectNodeValue(ResolvedObjectNode node, DependencyContext context)
        {
            if (node.Value is NameSymbol sym)
            {
                if (sym.Owner is SymbolTableOverRecordType symTable)
                {
                    RecordType type = symTable.Type;
                    var tableLogicalName = type.TableSymbolName;

                    if (symTable.IsThisRecord(sym))
                    {
                        // "ThisRecord". Whole entity
                        AddDependency(type.TableSymbolName, null);
                        return;
                    }

                    // on current table
                    var fieldLogicalName = sym.Name;

                    AddDependency(type.TableSymbolName, fieldLogicalName);
                }
            }
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
            public DependencyContext()
            {
            }
        }

        // if fieldLogicalName, then we're taking a dependency on entire record.
        public void AddDependency(string tableLogicalName, string fieldLogicalName)
        {
            if (tableLogicalName == null)
            {
                return;
            }

            if (!Info.Dependencies.ContainsKey(tableLogicalName))
            {
                Info.Dependencies[tableLogicalName] = new HashSet<string>();
            }

            if (fieldLogicalName != null)
            {
                Info.Dependencies[tableLogicalName].Add(fieldLogicalName);
            }
        }
    }

    /// <summary>
    /// Capture Dataverse field-level reads and writes within a formula.
    /// </summary>
    public class DependencyInfo
    {
        /// <summary>
        /// A dictionary of field logical names on related records, indexed by the related entity logical name.
        /// </summary>
        /// <example><![CDATA[
        /// On account, the formula "Name & 'Primary Contact'.'Full Name'" would return
        ///    "contact" => { "fullname" }
        /// The formula "Name & 'Primary Contact'.'Full Name' & Sum(Contacts, 'Number Of Childeren')" would return
        ///    "contact" => { "fullname", "numberofchildren" }.
        /// ]]></example>
        public Dictionary<string, HashSet<string>> Dependencies { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyInfo"/> class.
        /// </summary>
        public DependencyInfo()
        {
            Dependencies = new Dictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// Returns a string representation of the dependency information.
        /// </summary>
        /// <returns>A string describing the dependencies.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            DumpHelper(sb, Dependencies);

            return sb.ToString();
        }

        private static void DumpHelper(StringBuilder sb, Dictionary<string, HashSet<string>> dict)
        {
            if (dict != null)
            {
                foreach (var kv in dict)
                {
                    sb.Append("Entity");
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
