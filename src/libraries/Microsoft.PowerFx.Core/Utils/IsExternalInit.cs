// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if NETSTANDARD2_0

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// This is a hack to enable using the `init` keyword in our .NET Standard 2.0 package
    /// without including a new nuget package dependency or moving to .NET 5.
    /// See https://github.com/dotnet/roslyn/issues/45510, https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init for more details.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}

#endif
