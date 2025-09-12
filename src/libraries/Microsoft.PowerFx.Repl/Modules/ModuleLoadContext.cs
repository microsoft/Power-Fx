// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;
using YamlDotNet.Serialization;

namespace Microsoft.PowerFx.Repl
{
    internal static class ModuleLoadContextExtentions
    {   
        // Load a module from disk. 
        // This may throw on hard error (missing file), or return null with compile errors. 
        public static async Task<Module> LoadFromFileAsync(this ModuleLoadContext context, string path, List<ExpressionError> errors)
        {
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);

            var loader = new FileLoader(dir);

            return await context.LoadAsync(name, loader, errors).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Context for loading a single module. 
    /// This can ensure the module dependencies don't have cycles. 
    /// </summary>
    internal class ModuleLoadContext
    {
        // Core symbols we resolve against. 
        // This is needed for all the builtins. 
        private readonly ReadOnlySymbolTable _commonIncomingSymbols;

        // Set of modules we visited - used to detect cycles. 
        // Key String is the module's full path.
        // Value is null if module is in-progress of being loaded (this detects cycles). 
        // We still allow diamond dependencies. 
        private readonly Dictionary<ModuleIdentity, Module> _alreadyLoaded = new Dictionary<ModuleIdentity, Module>();

        public ModuleLoadContext(ReadOnlySymbolTable commonIncomingSymbols)
        {
            _commonIncomingSymbols = commonIncomingSymbols;
        }

        /// <summary>
        /// Load and return a resolved module.  May throw on errors. 
        /// </summary>
        /// <param name="name">name to resolve via loader parameter.</param>
        /// <param name="loader">file loader to resolve the name and any further imports.</param>
        /// <param name="errors">error collection to populate if there are errors in parsing, binding, etc. </param>
        /// <returns>A module. Or possible null if there are errors. Or may throw. </returns>        
        public async Task<Module> LoadAsync(string name, IFileLoader loader, List<ExpressionError> errors)
        {
            (var poco, var loader2) = await loader.LoadAsync(name).ConfigureAwait(false);

            ModuleIdentity fullPathIdentity = poco.GetIdentity();
            if (_alreadyLoaded.TryGetValue(fullPathIdentity, out var existing))
            {
                if (existing != null)
                {
                    // If we fully loaded the module, then it's not circular. 
                    return existing;
                }                
                
                // Circular reference. 
                throw new InvalidOperationException($"Circular reference: {name}");                
            }

            _alreadyLoaded.Add(fullPathIdentity, null); // In-progress

            // Load all imports first. 

            var symbolList = new List<ReadOnlySymbolTable>
            {
                _commonIncomingSymbols
            };
            var unique = new ConflictTracker();

            if (poco.Imports != null)
            {
                foreach (var import in poco.Imports)
                {
                    string file = import.File;
                    if (file != null)
                    {
                        var m2 = await LoadAsync(file, loader2, errors).ConfigureAwait(false);
                        if (m2 == null)
                        {
                            return null;
                        }

                        unique.VerifyUnique(m2);
                        symbolList.Add(m2.Symbols);
                    }
                }
            }

            var incomingSymbols = ReadOnlySymbolTable.Compose(symbolList.ToArray());

            var moduleExports = new SymbolTable { DebugName = name };

            bool ok = ResolveBody(poco, moduleExports, incomingSymbols, errors);

            if (!ok)
            {
                // On errors
                return null;
            }

            var module = new Module(fullPathIdentity, moduleExports)
            {
                FullPath = poco.Src_Filename
            };

            _alreadyLoaded[fullPathIdentity] = module; // done loading. 

            return module;
        }

        // poco - the yaml file contents. 
        // incomingSymbols - what body can bind to. Builtins, etc. 
        //   any module dependencies should already be resolved and included here. 
        // moduleExports - symbols to add to. 
        // Return true on successful load, false on failure
        private bool ResolveBody(ModulePoco poco, SymbolTable moduleExports, ReadOnlySymbolTable incomingSymbols, List<ExpressionError> errors)
        {
            var str = poco.Formulas.Value;

            bool allowSideEffects = true;
            var options = new ParserOptions
            {
                AllowsSideEffects = true
            };

            var parseResult = UserDefinitions.Parse(str, options, Features.PowerFxV1);

            var fragmentLocation = poco.Formulas.Location;

            // Scan for errors.
            errors.AddRange(ExpressionError.NewFragment(parseResult.Errors, str, fragmentLocation));

            if (errors.Any(x => !x.IsWarning))
            {
                // Abort on errors. 
                return false;
            }

            {
                var errors2 = parseResult.UDFs.Where(udf => !udf.IsParseValid).FirstOrDefault();
                if (errors2 != null)
                {
                    // Should have been caught above. 
                    throw new InvalidOperationException($"Errors should have already been caught");
                }
            }

            var definedTypes = parseResult.DefinedTypes;
            if (definedTypes != null && definedTypes.Any())
            {
                var resolvedTypes = DefinedTypeResolver.ResolveTypes(definedTypes, incomingSymbols, false, out var errors4);
                errors.AddRange(ExpressionError.NewFragment(errors4, str, fragmentLocation));
                if (errors.Any(x => !x.IsWarning))
                {
                    // Abort on errors. 
                    return false;
                }

                moduleExports.AddTypes(resolvedTypes);
            }

            var s2 = ReadOnlySymbolTable.Compose(moduleExports, incomingSymbols);

            // Convert parse --> TexlFunctions
            // fail if duplicates detected within this batch. 
            // fail if any names are resverd
            // Basic type checking 
            IEnumerable<UserDefinedFunction> udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs, s2, out var errors3);

            errors.AddRange(ExpressionError.NewFragment(errors3, str, fragmentLocation));
            if (errors.Any(x => !x.IsWarning))
            {
                return false;
            }

            // Body can refer to other functions defined in this batch. So we need 2 pass.
            // First add all definitions. 
            foreach (var udf in udfs)
            {
                moduleExports.AddFunction(udf);
            }

            // Then bind all bodies.
            foreach (var udf in udfs)
            {
                var config = new BindingConfig(allowsSideEffects: allowSideEffects, useThisRecordForRuleScope: false, numberIsFloat: false);

                Features features = Features.PowerFxV1;

                var binding = udf.BindBody(s2, new Glue2DocumentBinderGlue(), config, features);

                List<TexlError> bindErrors = new List<TexlError>();

                binding.ErrorContainer.GetErrors(ref bindErrors);
                errors.AddRange(ExpressionError.NewFragment(bindErrors, str, fragmentLocation));

                if (errors.Any(x => !x.IsWarning))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
