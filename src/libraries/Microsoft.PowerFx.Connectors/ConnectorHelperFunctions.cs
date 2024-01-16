// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal static class ConnectorHelperFunctions
    {
        internal static string Null(string param = null) => param == null ? "<null>" : $"<{param} is null>";

        internal static string LogFunction(this ConnectorFunction connectorFunction, string name)
        {
            return $"{connectorFunction.Namespace}.{connectorFunction.Name}.{name}";
        }

        internal static string LogKnownParameters(NamedValue[] knownParameters)
        {
            if (knownParameters == null || knownParameters.Length == 0)
            {
                return "no parameter";
            }

            return $"{knownParameters.Length} parameters {string.Join(" | ", knownParameters.Select(nv => $"{nv.Name}:{nv.Value.Type._type}"))}";
        }

        internal static string LogConnectorType(ConnectorType connectorType)
        {
            if (connectorType == null)
            {
                return Null(nameof(ConnectorType));
            }

            string log = $"{nameof(ConnectorType)} {connectorType?.Name ?? Null(nameof(connectorType.Name))} {connectorType?.FormulaType._type.ToString() ?? Null(nameof(ConnectorType.FormulaType))}";

            if (connectorType.HasErrors)
            {
                log += $" With errors: {string.Join(", ", connectorType.Errors)}";
            }

            return log;
        }

        internal static string LogConnectorParameter(ConnectorParameter connectorParameter)
        {
            if (connectorParameter == null)
            {
                return Null(nameof(ConnectorParameter));
            }

            return $"{nameof(ConnectorParameter)} {connectorParameter.Name ?? Null(nameof(connectorParameter.Name))} Param.{LogConnectorType(connectorParameter.ConnectorType)}";
        }

        internal static string LogArguments(FormulaValue[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                return "no argument";
            }

            return $"{arguments.Length} arguments {string.Join(" | ", arguments.Select(a => a.Type._type))}";
        }

        internal static string LogFormulaValue(FormulaValue formulaValue)
        {
            if (formulaValue == null)
            {
                return Null();
            }

            if (formulaValue is ErrorValue ev)
            {
                return $"ErrorValue {string.Join(", ", ev.Errors.Select(er => er.Message))}";
            }

            return $"{formulaValue.Type._type}";
        }

        internal static string LogConnectorEnhancedSuggestions(ConnectorEnhancedSuggestions suggestions)
        {
            if (suggestions == null)
            {
                return Null();
            }

            if (suggestions.ConnectorType == null)
            {
                return Null(nameof(ConnectorEnhancedSuggestions.ConnectorType));
            }

            if (suggestions.ConnectorSuggestions.Suggestions == null || !suggestions.ConnectorSuggestions.Suggestions.Any())
            {
                return "no suggestion";
            }

            return $"{suggestions.ConnectorSuggestions.Suggestions.Count()} suggestions";
        }

        internal static string LogConnectorSuggestions(ConnectorSuggestions suggestions)
        {
            if (suggestions == null)
            {
                return Null();
            }            

            if (suggestions.Suggestions == null || !suggestions.Suggestions.Any())
            {
                return "no suggestion";
            }

            return $"{suggestions.Suggestions.Count()} suggestions";
        }

        internal static string LogConnectorParameters(ConnectorParameters connectorParameters)
        {
            if (connectorParameters == null)
            {
                return Null();
            }

            if (connectorParameters.ParametersWithSuggestions == null || connectorParameters.ParametersWithSuggestions.Length == 0)
            {
                return $"no parameter, Completed = {connectorParameters.IsCompleted}";
            }

            return $"{connectorParameters.ParametersWithSuggestions.Count()} parameters, {string.Join(" | ", connectorParameters.ParametersWithSuggestions.Select(pws => $"{pws.Name} : {pws.Suggestions.Count} sugg."))}, Completed = {connectorParameters.IsCompleted}";
        }

        internal static string LogConnectorSettings(ConnectorSettings connectorSettings)            
        {
            if (connectorSettings == null)
            {
                return Null();
            }

            return $"{nameof(connectorSettings.Namespace)} {connectorSettings.Namespace ?? Null(nameof(connectorSettings.Namespace))}, " +
                   $"{nameof(connectorSettings.MaxRows)} {connectorSettings.MaxRows}, " +
                   $"{nameof(connectorSettings.FailOnUnknownExtension)} {connectorSettings.FailOnUnknownExtension}, " +
                   $"{nameof(connectorSettings.AllowUnsupportedFunctions)} {connectorSettings.AllowUnsupportedFunctions}, " +
                   $"{nameof(connectorSettings.IncludeInternalFunctions)} {connectorSettings.IncludeInternalFunctions}, ";
        }

        internal static string LogException(Exception ex)
        {
            return $"Exception {ex.GetType().FullName}, Message {ex.Message}, Callstack {ex.StackTrace}";
        }
    }
}
