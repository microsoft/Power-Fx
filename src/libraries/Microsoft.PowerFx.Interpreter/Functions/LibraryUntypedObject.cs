// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        public static FormulaValue Index_UO(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (UntypedObjectValue)args[0];
            var arg1 = (NumberValue)args[1];

            var element = arg0.Impl;

            var len = element.GetArrayLength();
            var index = (int)arg1.Value - 1; // 1-based index

            if (index < len)
            {
                var result = element[index];

                // Map null to blank
                if (result.Type == FormulaType.Blank)
                {
                    return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
                }

                return new UntypedObjectValue(irContext, result);
            }
            else
            {
                return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
            }
        }

        public static FormulaValue Value_UO(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;
            double number;

            if (impl.Type == FormulaType.String)
            {
                if (!double.TryParse(impl.GetString(), out number))
                {
                    return CommonErrors.InvalidNumberFormatError(irContext);
                }
            }
            else if (impl.Type == FormulaType.Blank)
            {
                return new BlankValue(irContext);
            }
            else if (impl.Type == FormulaType.Boolean)
            {
                number = impl.GetBoolean() ? 1 : 0;
            }
            else
            {
                number = impl.GetDouble();
            }

            return new NumberValue(irContext, number);
        }

        public static FormulaValue Text_UO(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;
            string str;

            if (impl.Type == FormulaType.String)
            {
                str = impl.GetString();
            }
            else if (impl.Type == FormulaType.Blank)
            {
                str = string.Empty;
            }
            else if (impl.Type == FormulaType.Boolean)
            {
                str = PowerFxBooleanToString(impl.GetBoolean());
            }
            else
            {
                str = impl.GetDouble().ToString();
            }

            return new StringValue(irContext, str);
        }

        public static FormulaValue Table_UO(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, UntypedObjectValue[] args)
        {
            var tableType = (TableType)irContext.ResultType;
            var resultType = tableType.ToRecord();
            var itemType = resultType.GetFieldType(BuiltinFunction.ColumnName_ValueStr);

            var resultRows = new List<DValue<RecordValue>>();

            var len = args[0].Impl.GetArrayLength();

            for (var i = 0; i < len; i++)
            {
                var element = args[0].Impl[i];

                var namedValue = new NamedValue(BuiltinFunction.ColumnName_ValueStr, new UntypedObjectValue(IRContext.NotInSource(itemType), element));
                var record = new InMemoryRecordValue(IRContext.NotInSource(resultType), new List<NamedValue>() { namedValue });
                resultRows.Add(DValue<RecordValue>.Of(record));
            }

            return new InMemoryTableValue(irContext, resultRows);
        }

        private static FormulaValue UntypedObjectPrimitiveChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is UntypedObjectValue cov)
            {
                if (cov.Impl.Type == FormulaType.Number)
                {
                    var number = cov.Impl.GetDouble();
                    if (IsInvalidDouble(number))
                    {
                        return CommonErrors.ArgumentOutOfRange(irContext);
                    }
                }
                else if (cov.Impl.Type is ExternalType)
                {
                    return CommonErrors.RuntimeTypeMismatch(irContext);
                }
            }

            return arg;
        }

        private static FormulaValue UntypedObjectArrayChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is UntypedObjectValue cov)
            {
                if (!(cov.Impl.Type is ExternalType et && et.Kind == ExternalTypeKind.Array))
                {
                    return new ErrorValue(irContext, new ExpressionError()
                    {
                        Message = "The UntypedObject does not represent an array",
                        Span = irContext.SourceContext,
                        Kind = ErrorKind.InvalidFunctionUsage
                    });
                }
            }

            return arg;
        }
    }
}
