// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Derived Identities should always override Equals/GetHashCode/ToString and be immutable.
    /// This is unenforceable in C#, but will result in unexpected behavior of the PowerFx
    /// Compiler if not done. 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1040:Avoid empty interfaces", 
        Justification = "Intentional. Used as a marker for identity of user implemented aggregate types")]
    [ThreadSafeImmutable] 
    public interface ITypeIdentity
    {
        bool Equals(object other);

        int GetHashCode();

        string ToString();
    }
}
