// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    public interface IYamlFunction
    {
        internal bool GetIsDeprecated();

        internal bool GetIsInternal();

        internal bool GetIsPageable();

        internal bool GetIsSupported();

        internal bool HasDetailedProperties();

        internal int GetArityMax();

        internal int GetArityMin();

        internal string GetName();

        internal string GetNotSupportedReason();

        internal string GetOptionalParameterTypes();

        internal string GetParameterNames();

        internal string GetRequiredParameterTypes();

        internal string GetReturnType();

        internal string GetWarnings();
    }
}
