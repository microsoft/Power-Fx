// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class ResourceValidationTests
    {
        [Fact]
        public void AllBuiltinFunctionsHaveParameterDescriptions()
        {
            var texlFunctionsLibrary = BuiltinFunctionsCore.BuiltinFunctionsLibrary;
            var functions = texlFunctionsLibrary
                .Where(x => !x.FunctionCategoriesMask.HasFlag(FunctionCategories.REST));

            foreach (var function in functions)
            {
                if (function.MaxArity == 0)
                {
                    continue;
                }

                foreach (var paramName in function.GetParamNames())
                {
                    Assert.True(
                        function.TryGetParamDescription(paramName, out var descr),
                        "Missing parameter description. Please add the following to Resources: " + "About" + function.LocaleInvariantName + "_" + paramName);
                }
            }
        }
    }
}
