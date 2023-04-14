// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class UserInfoTests : PowerFxTest
    {
        [Theory]
        [InlineData("FirstName LastName", "a@b.com", "12")]
        [InlineData("c203b79b-b985-42f0-b523-c10eb64387c6", null, "Id1")]
        [InlineData(null, null, null)]
        [InlineData("", "", "")]
        public async Task UserInfoObjectTest(string fullName, string email, string id)
        {            
            IUserInfo userInfo = new UserInfo
            {
                FullName = fullName,
                Email = email,
                Id = id
            };

            SymbolTable symbol = new SymbolTable();
            symbol.AddUserInfoObject();

            var engine = new RecalcEngine(new PowerFxConfig() { SymbolTable = symbol });
            var rc = new RuntimeConfig();
            rc.SetUserInfo(userInfo);

            var checkName = engine.Check("User.FullName");
            Assert.True(checkName.IsSuccess);
            var nameResult = await checkName.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);
            Assert.Equal(userInfo.FullName ?? string.Empty, nameResult.ToObject());

            var checkEmail = engine.Check("User.Email");
            Assert.True(checkEmail.IsSuccess);
            var emailResult = await checkEmail.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);
            Assert.Equal(userInfo.Email ?? string.Empty, emailResult.ToObject());

            var checkId = engine.Check("User.Id");
            Assert.True(checkId.IsSuccess);
            var idResult = await checkId.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);
            Assert.Equal(userInfo.Id ?? string.Empty, idResult.ToObject());
        }

        [Fact]
        public async Task UserInfoNoSymbolTableSetupTest()
        {
            var engine = new RecalcEngine();
            var check = engine.Check("User");
            Assert.False(check.IsSuccess);
        }

        [Fact]
        public async Task UserInfoNoServiceSetupTest()
        {
            SymbolTable symbol = new SymbolTable();
            symbol.AddUserInfoObject();

            var engine = new RecalcEngine(new PowerFxConfig() { SymbolTable = symbol });
            var rc = new RuntimeConfig();

            var check = engine.Check("User.FullName");
            Assert.True(check.IsSuccess);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await check.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // Verify disambiguation 
        [Theory]
        [InlineData("[@User].FullName", "Full")]
        [InlineData("User", "rowscope")]
        [InlineData("Field", "field")]
        public void DisambiguationTest(string expr, object expected)
        {
            var userInfo = new UserInfo
            {
                FullName = "Full", 
                Email = "Email",
                Id = "Id"
            };

            var rowScope = ReadOnlySymbolValues.NewFromRecord(RecordValue.NewRecordFromFields(
                new NamedValue("User", FormulaValue.New("rowscope")),
                new NamedValue("Field", FormulaValue.New("field"))));

            var config = new PowerFxConfig();
            config.SymbolTable.AddUserInfoObject();
            var engine = new RecalcEngine(config);
            
            var rc = new RuntimeConfig();
            rc.SetUserInfo(userInfo);
            rc.Values = rowScope;

            var result = engine.EvalAsync(expr, CancellationToken.None, runtimeConfig: rc).Result;

            Assert.Equal(expected, result.ToObject());
        }
    }
}
