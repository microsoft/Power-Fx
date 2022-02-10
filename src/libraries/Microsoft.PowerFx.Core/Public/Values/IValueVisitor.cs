// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Public.Values
{
    public interface IValueVisitor
    {
        void Visit(BlankValue value);

        void Visit(NumberValue value);

        void Visit(BooleanValue value);

        void Visit(StringValue value);

        void Visit(ErrorValue value);

        void Visit(RecordValue value);

        void Visit(TableValue value);

        void Visit(TimeValue value);

        void Visit(DateValue value);

        void Visit(DateTimeValue value);

        void Visit(UntypedObjectValue value);

        void Visit(OptionSetValue value);
    }
}
