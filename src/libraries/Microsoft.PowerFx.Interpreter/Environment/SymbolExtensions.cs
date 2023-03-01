// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Linq;

namespace Microsoft.PowerFx
{
    public static class SymbolExtensions
    {        
        /// <summary>
        /// Set TimeZoneInfo.
        /// </summary>
        /// <param name="symbols">SymbolValues where to set the TimeZoneInfo.</param>
        /// <param name="timezone">TimeZoneInfo to set.</param>
        /// <exception cref="ArgumentNullException">When timezone is null.</exception>
        public static void SetTimeZone(this RuntimeConfig symbols, TimeZoneInfo timezone)
        {
            symbols.AddService(timezone ?? throw new ArgumentNullException(nameof(timezone)));
        }

        /// <summary>
        /// Set CultureInfo.
        /// </summary>
        /// <param name="symbols">SymbolValues where to set the CultureInfo.</param>
        /// <param name="culture">CultureInfo to set.</param>
        /// <exception cref="ArgumentNullException">When culture is null.</exception>
        public static void SetCulture(this RuntimeConfig symbols, CultureInfo culture)
        {
            symbols.AddService(culture ?? throw new ArgumentNullException(nameof(culture)));
        }

        /// <summary>
        /// Create a set of values against this symbol table.
        /// </summary>
        /// <returns></returns>
        public static ReadOnlySymbolValues CreateValues(this ReadOnlySymbolTable symbolTable, params ReadOnlySymbolValues[] existing)
        {
            return ComposedReadOnlySymbolValues.New(true, symbolTable, existing);
        }
    }
}
