// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    public interface IYamlFunction
    {
        internal string GetName();

        internal bool HasDetailedProperties();

        internal bool GetIsSupported();

        internal bool GetIsDeprecated();

        internal bool GetIsInternal();

        internal bool GetIsPageable();

        internal string GetNotSupportedReason();

        internal int GetArityMin();

        internal int GetArityMax();

        internal string GetRequiredParameterTypes();

        internal string GetOptionalParameterTypes();

        internal string GetReturnType();

        internal string GetParameterNames();
    }
}
