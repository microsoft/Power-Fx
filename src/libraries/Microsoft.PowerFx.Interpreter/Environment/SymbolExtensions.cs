// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

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
        /// Set clock service for use with Today(), IsToday(), Now(). 
        /// </summary>
        /// <param name="symbols">SymbolValues where to set the CultureInfo.</param>
        /// <param name="clock">service to provide current time.</param>
        /// <exception cref="ArgumentNullException">When clock is null.</exception>
        public static RuntimeConfig SetClock(this RuntimeConfig symbols, IClockService clock)
        {
            symbols.AddService<IClockService>(clock ?? throw new ArgumentNullException(nameof(clock)));
            return symbols;
        }

        /// <summary>
        /// Set random service to override generating random numbers with Rand(), RandBetween().
        /// </summary>
        /// <param name="symbols">SymbolValues where to set the CultureInfo.</param>
        /// <param name="random">servce to generate random numbers.</param>
        /// <exception cref="ArgumentNullException">When random is null.</exception>
        public static void SetRandom(this RuntimeConfig symbols, IRandomService random)
        {
            symbols.AddService<IRandomService>(random ?? throw new ArgumentNullException(nameof(random)));
        }

        /// <summary>
        /// Set UserInfo at runtime. Called in conjuction with <see cref="AddUserInfoObject"/>.
        /// </summary>
        /// <param name="symbols">SymbolValues where to set the UserInfo.</param>
        /// <param name="userInfo">UserInfo to set.</param>
        /// <exception cref="ArgumentNullException">When userInfo is null.</exception>
        public static void SetUserInfo(this RuntimeConfig symbols, UserInfo userInfo)
        {
            symbols.AddService(userInfo ?? throw new ArgumentNullException(nameof(userInfo)));
        }

        public static void SetUserInfo(this RuntimeConfig symbols, BasicUserInfo userInfo)
        {
            symbols.SetUserInfo(userInfo.UserInfo);            
        }

        /// <summary>
        /// Adds a UserInfo object schema.
        /// Actual object is added in Runtime config service provider via SetUserInfo()/>.
        /// </summary>
        /// <param name="symbolTable">Symbol table to add to.</param>
        /// <param name="fields">The fields on <see cref="UserInfo"/> that the host is implementing.
        /// These will show up in intellisense.</param>
        public static void AddUserInfoObject(this SymbolTable symbolTable, params string[] fields)
        {
            var userInfoType = UserInfo.GetUserTypeWorker(fields);

            symbolTable.AddHostObject(UserInfo.ObjectName, userInfoType, (sp) => UserInfoRecordValue.GetUserInfoObject(userInfoType, sp));
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
