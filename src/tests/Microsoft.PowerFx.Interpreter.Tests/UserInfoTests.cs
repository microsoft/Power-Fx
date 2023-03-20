// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class UserInfoTests : PowerFxTest
    {
        [Theory]
        [InlineData("FirstName LastName", "a@b.com")]
        [InlineData("c203b79b-b985-42f0-b523-c10eb64387c6", null)]
        [InlineData(null, null)]
        [InlineData("", "")]
        public async Task UserInfoObjectTest(string fullName, string email)
        {            
            IUserInfo userInfo = new UserInfo
            {
                FullName = fullName,
                Email = email
            };

            SymbolTable symbol = new SymbolTable();
            symbol.AddUserInfoObject();

            var engine = new RecalcEngine(new PowerFxConfig() { SymbolTable = symbol });
            var rc = new RuntimeConfig();
            rc.SetUserInfo(userInfo);

            var checkName = engine.Check("UserInfo.FullName");
            Assert.True(checkName.IsSuccess);
            var nameResult = await checkName.GetEvaluator().EvalAsync(CancellationToken.None, rc);
            Assert.Equal(userInfo.FullName ?? string.Empty, nameResult.ToObject());

            var checkEmail = engine.Check("UserInfo.Email");
            Assert.True(checkEmail.IsSuccess);
            var emailResult = await checkEmail.GetEvaluator().EvalAsync(CancellationToken.None, rc);
            Assert.Equal(userInfo.Email ?? string.Empty, emailResult.ToObject());
        }

        [Fact]
        public async Task UserInfoNoSymbolTableSetupTest()
        {
            var engine = new RecalcEngine();
            var check = engine.Check("UserInfo");
            Assert.False(check.IsSuccess);
        }

        [Fact]
        public async Task UserInfoNoServiceSetupTest()
        {
            SymbolTable symbol = new SymbolTable();
            symbol.AddUserInfoObject();

            var engine = new RecalcEngine(new PowerFxConfig() { SymbolTable = symbol });
            var rc = new RuntimeConfig();

            var check = engine.Check("UserInfo.FullName");
            Assert.True(check.IsSuccess);

            try
            {
                var result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);
            }
            catch (Exception ex)
            {
                Assert.Contains("UserInfo object was not added to service", ex.Message);
            }
        }
    }
}
