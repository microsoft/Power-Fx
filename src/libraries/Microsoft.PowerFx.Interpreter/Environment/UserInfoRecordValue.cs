// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    // $$$ Can this be done via TypeMarshaller instead?
    internal class UserInfoRecordValue : RecordValue
    {
        private readonly UserInfo _userInfo;

        public UserInfoRecordValue(UserInfo userInfo, RecordType type)
            : base(type)
        {
            _userInfo = userInfo;
        }

        public static async Task<FormulaValue> GetUserInfoObject(RecordType userInfoType, IServiceProvider serviceProvider)
        {
            var userInfo = serviceProvider.GetService<UserInfo>() ?? throw new InvalidOperationException("UserInfo object was not added to service");

            RecordValue userRecord = new UserInfoRecordValue(userInfo, userInfoType);

            return userRecord;
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            // Should have called TryGetFieldAsync instead.
            throw new NotImplementedException();
        }

        protected override async Task<(bool Result, FormulaValue Value)> TryGetFieldAsync(FormulaType fieldType, string fieldName, CancellationToken cancellationToken)
        {
            try
            {
                // Binder will prevent calls to properties that aren't supported.
                FormulaValue value;
                switch (fieldName)
                {
                    case nameof(UserInfo.Email):
                        value = FormulaValue.New(await _userInfo.Email(cancellationToken).ConfigureAwait(false));
                        break;
                    case nameof(UserInfo.FullName):
                        value = FormulaValue.New(await _userInfo.FullName(cancellationToken).ConfigureAwait(false));
                        break;
                    case nameof(UserInfo.DataverseUserId):
                        value = FormulaValue.New(await _userInfo.DataverseUserId(cancellationToken).ConfigureAwait(false));
                        break;
                    case nameof(UserInfo.TeamsMemberId):
                        value = FormulaValue.New(await _userInfo.TeamsMemberId(cancellationToken).ConfigureAwait(false));
                        break;

                    default:
                        // Never should get here in an expression - binder should have blocked it.
                        return (false, null);
                }

                return (true, value);
            }
            catch (CustomFunctionErrorException ex)
            {
                return (true, FormulaValue.NewError(ex.ExpressionError, fieldType));
            }
        }
    }
}
