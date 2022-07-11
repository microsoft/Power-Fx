// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Types
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1040:Avoid empty interfaces", 
        Justification = "Intentional. Used as a marker for identity of user implemented aggregate types")]
    
    // $$$ This isn't enforced/enforcable, thoughts?
    [ThreadSafeImmutable] 
    public interface IAggregateTypeIdentity
    {
        // $$$ Maybe this should be an abstract class, since we could force implementers to override Equals/GetHashCode?
        // Seems like a bad design to force though.
    }
}
