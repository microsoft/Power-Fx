// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Types
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1040:Avoid empty interfaces", 
        Justification = "Intentional. Used as a marker for identity of user implemented aggregate types")]
    public interface IAggregateTypeIdentity
    {
    }
}
