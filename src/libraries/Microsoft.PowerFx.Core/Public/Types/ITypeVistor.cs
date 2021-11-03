// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Public.Types
{
    /// <summary>
    /// Visitor for walking <see cref="FormulaType"/>
    /// </summary>
    public interface ITypeVistor
    {
        void Visit(BlankType type);
        void Visit(BooleanType type);
        void Visit(NumberType type);
        void Visit(StringType type);
        void Visit(RecordType type);
        void Visit(TableType type);
        void Visit(DateType type);
        void Visit(DateTimeType type);
        void Visit(DateTimeNoTimeZoneType type);
        void Visit(TimeType type);
        void Visit(OptionSetValueType type);
    }
}