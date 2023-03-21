// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    public static class SymbolExtensions
    {
        /// <summary>
        /// Adds a UserInfo object schema.
        /// Actual object is added in Runtime config service provider.
        /// </summary>
        public static void AddUserInfoObject(this SymbolTable symbolTable)
        {
            var userInfoType = RecordType.Empty()
                .Add("FullName", FormulaType.String)
                .Add("Email", FormulaType.String);

            symbolTable.AddHostObject("UserInfo", userInfoType, GetUserInfoObject);
        }

        private static FormulaValue GetUserInfoObject(IServiceProvider serviceProvider)
        {
            var userInfo = (IUserInfo)serviceProvider.GetService(typeof(IUserInfo)) ?? throw new InvalidOperationException("UserInfo object was not added to service");

            RecordValue userRecord = FormulaValue.NewRecordFromFields(
                new NamedValue("FullName", FormulaValue.New(userInfo.FullName ?? string.Empty)),
                new NamedValue("Email", FormulaValue.New(userInfo.Email ?? string.Empty)));

            return userRecord;
        }
    }
}
