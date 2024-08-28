// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Readers.Exceptions;
using Microsoft.OpenApi.Validations;

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    public static class SwaggerFileIdentification
    {
        public const string UNKNOWN_SWAGGER = "UNKNOWN_SWAGGER";

        /// <summary>
        /// Recursively identify swagger files located in a set of folders and uniquely identify (connector name, file) elements.
        /// </summary>
        /// <param name="folders">Set of folders to scan.</param>
        /// <returns>Dictionary of unique (connector name, file location) couples.</returns>
        public static Dictionary<string, string> LocateSwaggerFiles(string[] folders, string pattern, SwaggerLocatorSettings locatorSettings = null)
        {
            return LocateSwaggerFilesWithDocuments(folders, pattern, locatorSettings).ToDictionary(r => r.Key, r => r.Value.location);
        }

        /// <summary>
        /// Recursively identify swagger files located in a set of folders and uniquely identify (connector name, file) elements.
        /// If errors were identified during OpenApi reading they are also returned (and ignored).
        /// If the OpenApi errors do not allow identifying the document name (title), the dictionary key will be "UNKNOWN_SWAGGER_xxx" (where xxx is a unique number).
        /// </summary>
        /// <param name="folders">Set of folders to scan.</param>
        /// <returns>Dictionary of unique (connector name, file location) couples.</returns>
        public static Dictionary<string, (string folder, string location, OpenApiDocument document, List<string> errors)> LocateSwaggerFilesWithDocuments(string[] folders, string pattern, SwaggerLocatorSettings locatorSettings = null)
        {
            SwaggerLocatorSettings settings = locatorSettings ?? new SwaggerLocatorSettings();

            IEnumerable<(string folder, string file)> files = folders.SelectMany(folder => Directory.EnumerateFiles(folder, pattern, new EnumerationOptions() { RecurseSubdirectories = true }).Select(f => (folder, file: f)))
                                                                     .Where(f => settings.FoldersToExclude.All(fte => f.file.IndexOf(fte, 0, StringComparison.OrdinalIgnoreCase) < 0));                                                                     

            // items: <connector title, (source folder, swagger location, OpenApiDocument)>
            Dictionary<string, List<(string folder, string location, OpenApiDocument document, List<string> errors)>> list = new (StringComparer.OrdinalIgnoreCase);
            Dictionary<string, (string folder, string location, OpenApiDocument document, List<string> errors)> list2 = new (StringComparer.OrdinalIgnoreCase);

            // parse all files we have
            foreach ((string folder, string file) in files)
            {
                ParseSwagger(folder, file, list);
            }

            // if we have multiple files with the same title, let's identify the ones we want to keep
            foreach (KeyValuePair<string, List<(string folder, string location, OpenApiDocument document, List<string> errors)>> swagger in list)
            {
                if (swagger.Key == UNKNOWN_SWAGGER)
                { 
                    continue;
                }

                List<(string folder, string location, OpenApiDocument document, List<string> errors)> docs = swagger.Value;
                
                // determine vMax and only keep those versions
                Version vMax = docs.Max(d => d.document.GetVersion());
                docs = swagger.Value.Where(d => d.document.GetVersion() == vMax).ToList();

                // remove files in "preview" folders
                if (docs.Count > 1)
                {
                    var nonPreviewDocs = docs.Where(d => !d.location.Contains("preview", StringComparison.OrdinalIgnoreCase)).ToList();
                    if (nonPreviewDocs.Count > 0)
                    {
                        docs = nonPreviewDocs;
                    }
                }

                int distinctDocCount = docs.Select(t => t.document.HashCode).Distinct().Count();

                // if single document or all documents with same hashcode
                if (distinctDocCount == 1)
                {
                    list2.Add(swagger.Key, docs.First());
                }

                // if 2 docs have same description + with one in aapt & one in ppc folders, take the one in 1st folder which aapt version (internal version)
                else if (distinctDocCount == 2)
                {
                    var first = docs.First();
                    var second = docs.Last();

                    // if one in each folder, let's take aapt version
                    if (docs.Select(v => v.location.StartsWith(folders.First(), StringComparison.OrdinalIgnoreCase) ? 1 : 2).Sum() == 3)
                    {
                        list2.Add(swagger.Key, docs.First(v => v.location.StartsWith(folders.First(), StringComparison.OrdinalIgnoreCase)));
                    }
                    else
                    {
                        // when in same folder, take the file with highest number of operations
                        int firstOpCount = first.document.Paths.SelectMany(p => p.Value.Operations).Count();
                        int secondOpCount = second.document.Paths.SelectMany(p => p.Value.Operations).Count();

                        if (firstOpCount > secondOpCount)
                        {
                            list2.Add(swagger.Key, first);
                        }
                        else if (secondOpCount > firstOpCount)
                        {
                            list2.Add(swagger.Key, second);
                        }
                        else
                        {
                            throw new InvalidDataException($"Two documents with same number of operations, can't determine which one to select {new KeyValuePair<string, List<(string folder, string location, OpenApiDocument document, List<string> errors)>>(swagger.Key, docs).GetString()}");
                        }
                    }
                }
                else
                {
                    throw new InvalidDataException($"More than 2 documents are found, can't determine which one to select {new KeyValuePair<string, List<(string folder, string location, OpenApiDocument document, List<string> errors)>>(swagger.Key, docs).GetString()}");
                }
            }

            if (list.ContainsKey(UNKNOWN_SWAGGER))
            {
                list2 = list2.Union(list[UNKNOWN_SWAGGER].Select(((string, string, OpenApiDocument, List<string>) f, int i) => new KeyValuePair<string, (string, string, OpenApiDocument, List<string>)>($"{UNKNOWN_SWAGGER}_{i}", f))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            return list2;
        }

        private static void ParseSwagger(string folder, string swaggerFile, Dictionary<string, List<(string folder, string location, OpenApiDocument document, List<string> errors)>> list)
        {
            (OpenApiDocument doc, List<string> errors) = ReadSwagger(swaggerFile);
            string title = null;

            if (doc == null)
            {
                errors.Add("No OpenApiDocument");
                title = UNKNOWN_SWAGGER;
            }
            else if (doc.Info == null)
            {
                errors.Add("No OpenApiDocument Info");
                title = UNKNOWN_SWAGGER;
            }
            else if (doc.Info.Title == "Default title")
            {
                title = swaggerFile.Substring(folder.Length).Split('\\', StringSplitOptions.RemoveEmptyEntries)[1];
            }
            else 
            {
                title = doc.Info.Title;
            }

            (string, string, OpenApiDocument, List<string>) item = (folder, swaggerFile, doc, errors);

            if (list.ContainsKey(title))
            {
                list[title].Add(item);
            }
            else
            {
                list.Add(title, new List<(string folder, string location, OpenApiDocument document, List<string> errors)>() { item });
            }
        }

        public static (OpenApiDocument document, List<string> errors) ReadSwagger(string name)
        {
            List<string> errors = new List<string>();
            OpenApiDocument doc = null;

            try
            {
                using FileStream stream = File.OpenRead(name);
                doc = new OpenApiStreamReader(new OpenApiReaderSettings() { RuleSet = DefaultValidationRuleSet }).Read(stream, out OpenApiDiagnostic diag);

                if (diag != null && diag.Errors != null)
                {
                    foreach (OpenApiError error in diag.Errors)
                    {
                        if (error is OpenApiValidatorError vError)
                        {
                            errors.Add($"{vError.RuleName} {vError.Pointer} {vError.Message}");
                        }
                        else
                        {
                            // Could be OpenApiError or OpenApiReferenceError
                            errors.Add($"{error.Pointer} {error.Message}");
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is OpenApiUnsupportedSpecVersionException || ex is OpenApiReaderException)
            {
                errors.Add("Cannot read swagger file");
                errors.Add($"Exception: {ex.Message}");
            }

            return (doc, errors);
        }

        internal static ValidationRuleSet DefaultValidationRuleSet
        {
            get
            {
                IList<ValidationRule> rules = ValidationRuleSet.GetDefaultRuleSet().Rules;

                // OpenApiComponentsRules.KeyMustBeRegularExpression is the only rule with this type
                var keyMustBeRegularExpression = rules.First(r => r.GetType() == typeof(ValidationRule<OpenApiComponents>));
                rules.Remove(keyMustBeRegularExpression);

                return new ValidationRuleSet(rules);
            }
        }
    }

    public static class Ext
    {
        internal static string GetString(this KeyValuePair<string, List<(string folder, string location, OpenApiDocument document, List<string> errors)>> s) => $"{s.Key}, {string.Join("\r\n  ", s.Value.Select(v => v.GetString()))}";

        internal static string GetString(this (string folder, string location, OpenApiDocument document, List<string> errors) v) => $"{v.location}, VER: {v.document.Info.Version}, DESC: {v.document.Info.Description}, OP_COUNT: {v.document.Paths.SelectMany(p => p.Value.Operations).Count()}, ERRORS: {(v.errors.Any() ? "None" : string.Join(",", v.errors))}";

        internal static Version GetVersion(this OpenApiDocument d)
        {
            string v = d.Info.Version.ToLowerInvariant().Replace("v", string.Empty, StringComparison.OrdinalIgnoreCase);

            if (Version.TryParse(v, out Version ver))
            {
                return ver;
            }

            if (int.TryParse(v, out int vi))
            {
                return new Version(vi, 0);
            }

            if (v.Equals("latest", StringComparison.OrdinalIgnoreCase))
            {
                return new Version(int.MaxValue, int.MaxValue);
            }

            return new Version(0, 0);
        }
    }
}
