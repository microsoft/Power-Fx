// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    public abstract class YamlComparer<TFunction>
          where TFunction : YamlReaderWriter, IYamlFunction
    {
        internal string Category;

        internal string ReferenceRoot;

        internal string CurrentRoot;

        internal List<ConnectorStat> ConnectorStats;

        internal List<FunctionStat> FunctionStats;

        internal Action<string> Log;

        internal abstract string FilePattern { get; }

        internal abstract string CategorySuffix { get; }

        [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "These lists are accumulating results")]
        public YamlComparer(string category, string referenceRoot, string currentRoot, List<ConnectorStat> connectorStats, List<FunctionStat> functionStats, Action<string> log)
        {
            Category = category;
            ReferenceRoot = referenceRoot;
            CurrentRoot = currentRoot;
            ConnectorStats = connectorStats;
            FunctionStats = functionStats;
            Log = log;
        }

        internal TFunction[] GetFunctions(string path)
        {
            return Directory.EnumerateFiles(path, FilePattern, SearchOption.TopDirectoryOnly)
                            .Select(file => YamlReaderWriter.ReadYaml<TFunction>(File.ReadAllText(file)))
                            .ToArray();
        }

        public void CompareYamlFiles()
        {
            string referencePath = Path.Combine(ReferenceRoot, Category);
            string currentPath = Path.Combine(CurrentRoot, Category);
            string uniqueCategory = $"{Category}_{CategorySuffix}";

            Log($"Comparing {Category} files between {referencePath} and {currentPath}...");

            if (!Directory.Exists(referencePath))
            {
                throw new DirectoryNotFoundException(referencePath);
            }

            if (!Directory.Exists(currentPath))
            {
                throw new DirectoryNotFoundException(currentPath);
            }

            IOrderedEnumerable<string> refConnectors = Directory.EnumerateDirectories(referencePath).OrderBy(x => x, StringComparer.Ordinal);
            IOrderedEnumerable<string> curConnectors = Directory.EnumerateDirectories(currentPath).OrderBy(x => x, StringComparer.Ordinal);

            if (!refConnectors.Any())
            {
                throw new Exception("No reference connector");
            }

            if (!curConnectors.Any())
            {
                throw new Exception("No current connector");
            }

            if (refConnectors.Count() != curConnectors.Count())
            {
                Log($"Difference in connector count: {refConnectors.Count()} vs. {curConnectors.Count()}");
            }

            (List<string> onlyLeft, List<string> common, List<string> onlyRight) = YamlComparer.CompareLists(refConnectors, curConnectors, x => Path.GetFileName(x));

            // Missing connectors
            foreach (string connectorName in onlyLeft)
            {
                Log($"Only in reference: {connectorName}");
                TFunction[] funcs = GetFunctions(Path.Combine(referencePath, connectorName));

                string openApiErrorsFileName = Path.Combine(referencePath, connectorName, "OpenApiErrors.txt");
                ConnectorStats.Add(new ConnectorStat(uniqueCategory, connectorName, funcs, openApiErrorsFileName, new List<string>() { "Missing connector" }));

                foreach (TFunction func in funcs)
                {
                    FunctionStats.Add(new FunctionStat(uniqueCategory, connectorName, func, new List<string>() { "Missing connector", "Missing function" }));
                }
            }

            // Connectors in common
            foreach (string connectorName in common)
            {
                TFunction[] referenceFunctions = GetFunctions(Path.Combine(referencePath, connectorName));
                TFunction[] currentFunctions = GetFunctions(Path.Combine(currentPath, connectorName));

                (List<string> rfOnly, List<string> cmnf, List<string> cfOnly) = YamlComparer.CompareLists(referenceFunctions.Select(txlf => txlf.GetName()), currentFunctions.Select(txlf => txlf.GetName()), x => x);

                List<FunctionStat> missingFunctions = new List<FunctionStat>();
                List<FunctionStat> commonFunctions = new List<FunctionStat>();
                List<FunctionStat> newFunctions = new List<FunctionStat>();

                // Missing functions
                foreach (string refOnly in rfOnly)
                {
                    missingFunctions.Add(new FunctionStat(uniqueCategory, connectorName, referenceFunctions.First(txlf => txlf.GetName() == refOnly), new List<string>() { "Missing function" }));
                }

                // Common functions
                foreach (string cmnfunc in cmnf)
                {
                    TFunction refFunc = referenceFunctions.First(txlf => txlf.GetName() == cmnfunc);
                    TFunction curFunc = currentFunctions.First(txlf => txlf.GetName() == cmnfunc);

                    IReadOnlyList<string> diff = YamlComparer.Compare(refFunc, curFunc, "Reference", "Current");
                    commonFunctions.Add(new FunctionStat(uniqueCategory, connectorName, curFunc, diff));
                }

                // New functions
                foreach (string currentOnly in cfOnly)
                {
                    newFunctions.Add(new FunctionStat(uniqueCategory, connectorName, currentFunctions.First(txlf => txlf.GetName() == currentOnly), new List<string>() { "New function" }));
                }

                List<string> connectorDiff = new List<string>();

                if (missingFunctions.Count != 0)
                {
                    connectorDiff.Add($"{missingFunctions.Count} missing functions: {string.Join(", ", missingFunctions.Select(fs => fs.FunctionName))}");
                }

                IEnumerable<FunctionStat> commonFunctionsWithDifferences = commonFunctions.Where(fs => fs.DifferFromBaseline);
                if (commonFunctionsWithDifferences.Any())
                {
                    connectorDiff.Add($"{commonFunctionsWithDifferences.Count()} functions with differences: {string.Join(", ", commonFunctionsWithDifferences.Select(fs => $"{fs.FunctionName} ({fs.Differences.Count})"))}");
                }

                if (newFunctions.Count != 0)
                {
                    connectorDiff.Add($"{newFunctions.Count} new functions: {string.Join(", ", newFunctions.Select(fs => fs.FunctionName))}");
                }

                FunctionStats.AddRange(missingFunctions);
                FunctionStats.AddRange(commonFunctions);
                FunctionStats.AddRange(newFunctions);

                string openApiErrorsFileName = Path.Combine(currentPath, connectorName, "OpenApiErrors.txt");
                ConnectorStats.Add(new ConnectorStat(uniqueCategory, connectorName, currentFunctions, openApiErrorsFileName, connectorDiff));
            }

            // New connectors
            foreach (string str in onlyRight)
            {
                Log($"Only current: {str}");
                TFunction[] funcs = GetFunctions(Path.Combine(currentPath, str));

                string openApiErrorsFileName = Path.Combine(currentPath, str, "OpenApiErrors.txt");
                ConnectorStats.Add(new ConnectorStat(uniqueCategory, str, funcs, openApiErrorsFileName, new List<string>() { "New connector" }));

                foreach (TFunction func in funcs)
                {
                    FunctionStats.Add(new FunctionStat(uniqueCategory, str, func, new List<string>() { "New connector", "New function" }));
                }
            }
        }
    }

    public static class YamlComparer
    {
        internal static Dictionary<Type, (PropertyInfo[], FieldInfo[])> Cache = new ();

        public static IReadOnlyList<string> Compare<T>(T leftObject, T rightObject, string leftDescription, string rightDescription)
        {
            return Compare(typeof(T), leftObject, rightObject, leftDescription, rightDescription);
        }

        public static IReadOnlyList<string> Compare(Type type, object leftObject, object rightObject, string leftDescription, string rightDescription)
        {
            List<string> diff = new List<string>();

            // No need to lock/protect this cache as we are single threaded here.
            if (!Cache.TryGetValue(type, out (PropertyInfo[], FieldInfo[]) properties))
            {
                properties = (type.GetProperties(BindingFlags.Instance | BindingFlags.Public), type.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(f => f.Name).ToArray());
                Cache.Add(type, properties);
            }

            (PropertyInfo[] propInfos, FieldInfo[] fieldInfos) = properties;

            if (propInfos.Length != 0)
            {
                throw new Exception("Properties not supported by this object comparer");
            }

            if (leftObject == null && rightObject == null)
            {
                return diff;
            }

            if (leftObject == null)
            {
                diff.Add($"Object is null on {leftDescription} but isn't null on {rightDescription}");
                return diff;
            }

            if (rightObject == null)
            {
                diff.Add($"Object is null on {rightDescription} but isn't null on {leftDescription}");
                return diff;
            }

            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                object leftValue = fieldInfo.GetValue(leftObject);
                object rightValue = fieldInfo.GetValue(rightObject);

                bool? comparison = null;

                switch (leftValue)
                {
                    case string str:
                        comparison = str == (string)rightValue;
                        break;

                    case bool b:
                        comparison = b == (bool)rightValue;
                        break;

                    case int i:
                        comparison = i == (int)rightValue;
                        break;

                    case object obj when fieldInfo.FieldType.IsArray:
                        object[] leftArray = (object[])leftValue;
                        object[] rightArray = (object[])rightValue;

                        if (leftArray == null && rightArray == null)
                        {
                            continue;
                        }
                        else if (leftArray == null)
                        {
                            diff.Add($"{fieldInfo.Name} array is null on {leftDescription} but has {rightArray.Length} elements on {rightDescription}");
                            continue;
                        }
                        else if (rightArray == null)
                        {
                            diff.Add($"{fieldInfo.Name} array is null on {rightDescription} but has {leftArray.Length} elements on {leftDescription}");
                            continue;
                        }
                        else if (rightArray.Length != leftArray.Length)
                        {
                            diff.Add($"{fieldInfo.Name} array has different number of elements: {leftArray.Length} on {leftDescription} and {rightArray.Length} on {rightDescription}");
                            continue;
                        }

                        for (int j = 0; j < leftArray.Length; j++)
                        {
                            diff.AddRange(Compare(fieldInfo.FieldType.GetElementType(), leftArray[j], rightArray[j], $"{leftDescription}.{fieldInfo.Name}[{j}]", $"{rightDescription}.{fieldInfo.Name}[{j}]"));
                        }

                        break;

                    case object obj when fieldInfo.FieldType.GetInterfaces().Contains(typeof(IDictionary)):
                        IDictionary leftDictionary = (IDictionary)leftValue;
                        IDictionary rightDictionary = (IDictionary)rightValue;

                        if (fieldInfo.FieldType.GenericTypeArguments[0] != typeof(string))
                        {
                            throw new Exception("Only Dictionary<string, T> are supported");
                        }

                        if (leftDictionary == null && rightDictionary == null)
                        {
                            continue;
                        }
                        else if (leftDictionary == null)
                        {
                            diff.Add($"{fieldInfo.Name} dictionary is null on {leftDescription} but has {rightDictionary.Count} elements on {rightDescription}");
                            continue;
                        }
                        else if (rightDictionary == null)
                        {
                            diff.Add($"{fieldInfo.Name} dictionary is null on {rightDescription} but has {leftDictionary.Count} elements on {leftDescription}");
                            continue;
                        }
                        else if (rightDictionary.Count != leftDictionary.Count)
                        {
                            diff.Add($"{fieldInfo.Name} dictionary has different number of elements: {leftDictionary.Count} on {leftDescription} and {rightDictionary.Count} on {rightDescription}");
                            continue;
                        }

                        List<string> leftKeys = new List<string>();
                        List<object> leftValues = new List<object>();
                        List<string> rightKeys = new List<string>();
                        List<object> rightValues = new List<object>();

                        foreach (DictionaryEntry keyValuePair in leftDictionary)
                        {
                            leftKeys.Add((string)keyValuePair.Key);
                            leftValues.Add(keyValuePair.Value);
                        }

                        foreach (DictionaryEntry keyValuePair in rightDictionary)
                        {
                            rightKeys.Add((string)keyValuePair.Key);
                            rightValues.Add(keyValuePair.Value);
                        }

                        (List<string> leftOnly, List<string> common, List<string> rightOnly) = CompareLists(leftKeys, rightKeys, x => x);

                        if (leftOnly.Count != 0)
                        {
                            diff.Add($"{fieldInfo.Name} dictionary has {leftDescription} Keys ({string.Join(", ", leftOnly)}) not present in {rightDescription}");
                        }

                        if (rightOnly.Count != 0)
                        {
                            diff.Add($"{fieldInfo.Name} dictionary has {rightDescription} Keys ({string.Join(", ", rightOnly)}) not present in {leftDescription}");
                        }

                        foreach (string commonKey in common)
                        {
                            object leftDictionaryValue = leftValues[leftKeys.IndexOf(commonKey)];
                            object rightDictionaryValue = rightValues[rightKeys.IndexOf(commonKey)];

                            diff.AddRange(Compare(fieldInfo.FieldType.GenericTypeArguments[1], leftDictionaryValue, rightDictionaryValue, $@"{leftDescription}.{fieldInfo.Name}[""{commonKey}""]", $@"{rightDescription}.{fieldInfo.Name}[""{commonKey}""]"));
                        }

                        break;

                    case object obj when fieldInfo.FieldType.Assembly == type.Assembly:
                        diff.AddRange(Compare(fieldInfo.FieldType, leftValue, rightValue, $"{leftDescription}.{fieldInfo.Name}", $"{rightDescription}.{fieldInfo.Name}"));
                        break;

                    case null:
                        comparison = rightValue == null;
                        break;

                    default:
                        throw new Exception($"Unknown type for field {fieldInfo.Name}");
                }

                if (comparison == false)
                {
                    diff.Add($"{fieldInfo.Name} has different values between {leftDescription} ({leftValue ?? "null"}) and {rightDescription} ({rightValue ?? "null"})");
                }
            }

            return diff;
        }

        internal static (List<string> leftOnly, List<string> common, List<string> rightOnly) CompareLists(IEnumerable<string> leftList, IEnumerable<string> rightList, Func<string, string> keyGenerator)
        {
            List<string> leftOnly = new List<string>();
            List<string> common = new List<string>();
            List<string> rightOnly = new List<string>();

            IOrderedEnumerable<string> leftOrderedList = leftList.Select(s => keyGenerator(s)).OrderBy(x => x, StringComparer.Ordinal);
            IOrderedEnumerable<string> rightOrderedList = rightList.Select(s => keyGenerator(s)).OrderBy(x => x, StringComparer.Ordinal);

            IEnumerator<string> leftEnumerator = leftOrderedList.GetEnumerator();
            IEnumerator<string> rightEnumerator = rightOrderedList.GetEnumerator();

            bool leftElementExists = leftEnumerator.MoveNext();
            bool rightElementExists = rightEnumerator.MoveNext();

            while (true)
            {
                if (!leftElementExists && !rightElementExists)
                {
                    break;
                }

                if (!leftElementExists)
                {
                    while (rightElementExists)
                    {
                        rightOnly.Add(rightEnumerator.Current);
                        rightElementExists = rightEnumerator.MoveNext();
                    }

                    break;
                }

                if (!rightElementExists)
                {
                    while (leftElementExists)
                    {
                        leftOnly.Add(leftEnumerator.Current);
                        leftElementExists = leftEnumerator.MoveNext();
                    }

                    break;
                }

                int i = string.Compare(leftEnumerator.Current, rightEnumerator.Current, StringComparison.Ordinal);

                if (i < 0)
                {
                    leftOnly.Add(leftEnumerator.Current);
                    leftElementExists = leftEnumerator.MoveNext();
                }
                else if (i > 0)
                {
                    rightOnly.Add(rightEnumerator.Current);
                    rightElementExists = rightEnumerator.MoveNext();
                }
                else
                {
                    common.Add(leftEnumerator.Current);
                    leftElementExists = leftEnumerator.MoveNext();
                    rightElementExists = rightEnumerator.MoveNext();
                }
            }

            return (leftOnly, common, rightOnly);
        }
    }
}
