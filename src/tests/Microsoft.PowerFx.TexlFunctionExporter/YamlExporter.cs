// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.PowerFx.Core.Functions;

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    public static class YamlExporter
    {
        // internal function as TexlFunction is internal
        internal static void ExportTexlFunction(string folder, TexlFunction texlFunction, bool isLibrary = false)
        {            
            YamlTexlFunction txlFunction = new YamlTexlFunction(texlFunction, isLibrary);

            string funcName = isLibrary ? texlFunction.GetType().Name : texlFunction.Name;

            if (isLibrary)
            {
                if (funcName.EndsWith("Function", StringComparison.Ordinal))
                {
                    funcName = funcName.Substring(0, funcName.Length - 8);
                }

                if (funcName.EndsWith("Function_UO", StringComparison.Ordinal))
                {
                    funcName = $"{funcName.Substring(0, funcName.Length - 11)}_UO";
                }

                if (funcName.EndsWith("Function_T", StringComparison.Ordinal))
                {
                    funcName = $"{funcName.Substring(0, funcName.Length - 10)}_T";
                }

                if (funcName != texlFunction.Name)
                {
                    funcName = $"{texlFunction.Name}_{funcName}";
                }
            }

            funcName = "Texl_" + funcName;

            string functionFile = Path.Combine(folder, funcName.Replace("/", "_", StringComparison.OrdinalIgnoreCase) + ".yaml");
            Directory.CreateDirectory(folder);

            if (File.Exists(functionFile))
            {
                throw new IOException($"File {functionFile} already exists!");
            }

            File.WriteAllText(functionFile, txlFunction.GetYaml(), Encoding.UTF8);
        }
    }
}
