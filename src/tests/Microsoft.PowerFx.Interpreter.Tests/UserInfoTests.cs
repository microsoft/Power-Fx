﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using NuGet.Frameworks;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class UserInfoTests : PowerFxTest
    {
        [Fact]
        public async Task UserInfoObjectTest()
        {
            var g1 = Guid.NewGuid();
            var g2 = Guid.NewGuid();

            var userInfo = new BasicUserInfo
            {
                FullName = "fullname",
                Email = "me@contoso.com",
                DataverseUserTableId = g1,
                BotMemberId = g2,
            };

            // Use string literals for properties here to ensure they didn't change. 
            var props = new Dictionary<string, object>
            {
                { "FullName", userInfo.FullName },
                { "Email", userInfo.Email },
                { "DataverseUserTableId", userInfo.DataverseUserTableId },
                { "BotMemberId", userInfo.BotMemberId }
            };

            var allKeys = props.Keys.ToArray();
            SymbolTable symbol = new SymbolTable();

            symbol.AddUserInfoObject(allKeys);

            var engine = new RecalcEngine(new PowerFxConfig() { SymbolTable = symbol });
            var rc = new RuntimeConfig();
            rc.SetUserInfo(userInfo);
                        
            foreach (var kv in props)
            {
                var propName = kv.Key;
                var expectedValue = kv.Value;
                
                var check = engine.Check("User." + propName);                
                Assert.True(check.IsSuccess);

                var result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);

                // ToObject is type specific, will validate we have correct type (string vs guid).
                Assert.Equal(expectedValue, result.ToObject());
            }
        }

        // Get compiler-time errors when trying to access fields not provided at symbol time. 
        [Theory]
        [InlineData("FullName", "User.FullName", true)]
        [InlineData("FullName", "User.Email", false)]
        [InlineData("Email", "User.Email", true)]
        public void LimitedFields(string keys, string expr, bool success)
        {
            SymbolTable symbol = new SymbolTable();
            symbol.AddUserInfoObject(keys);

            var engine = new Engine(new PowerFxConfig() { SymbolTable = symbol });

            var check = engine.Check(expr);
            Assert.Equal(check.IsSuccess, success);
        }

        // Errors when creating a the record type for User. 
        [Theory]
        [InlineData(null, typeof(ArgumentNullException))] // null 
        [InlineData("", typeof(InvalidOperationException))] // 0-length
        [InlineData("othername", typeof(InvalidOperationException))] // not recognized filed 
        [InlineData("FullName,FullName", typeof(InvalidOperationException))] // duplicated
        public void UserTypeFail(string fields, Type exceptionType)
        {
            string[] x = fields?.Split(',');
            if (fields == string.Empty) 
            { 
                x = new string[0]; 
            }

            try
            {
                UserInfo.GetUserType(x);
                Assert.True(false); // wrong exception type
            }
            catch (Exception ex) 
            {
                if (ex.GetType() == exceptionType)
                {
                    // Ok
                    return;
                }

                Assert.True(false); // wrong exception type
            }
        }
                
        // User symbol is not available by default. Must be added. 
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
            symbol.AddUserInfoObject(
                 nameof(UserInfo.FullName));

            var engine = new RecalcEngine(new PowerFxConfig() { SymbolTable = symbol });
            var rc = new RuntimeConfig();

            var check = engine.Check("User.FullName");
            Assert.True(check.IsSuccess);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await check.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // Verify disambiguation between User property and User field in rowscope. 
        [Theory]
        [InlineData("[@User].FullName", "Full")]
        [InlineData("User", "rowscope")]
        [InlineData("Field", "field")]
        public void DisambiguationTest(string expr, object expected)
        {
            var userInfo = new BasicUserInfo
            {
                FullName = "Full",
                Email = "Email"
            };

            var rowScope = ReadOnlySymbolValues.NewFromRecord(RecordValue.NewRecordFromFields(
                new NamedValue("User", FormulaValue.New("rowscope")),
                new NamedValue("Field", FormulaValue.New("field"))));

            var config = new PowerFxConfig();
            config.SymbolTable.AddUserInfoObject(
                nameof(UserInfo.FullName), 
                nameof(UserInfo.Email));
            var engine = new RecalcEngine(config);
            
            var rc = new RuntimeConfig();
            rc.SetUserInfo(userInfo);
            rc.Values = rowScope;

            var result = engine.EvalAsync(expr, CancellationToken.None, runtimeConfig: rc).Result;

            Assert.Equal(expected, result.ToObject());
        }

        // BasicUserInfo has same properties as UserInfo
        [Fact]
        public void Consistency()
        {
            Assert.Equal("User", UserInfo.ObjectName);

            var methods = typeof(UserInfo).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                var prop = typeof(BasicUserInfo).GetProperty(method.Name, BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(prop);
            }
        }

        // Ensure that properties on the UserInfo object are not invoked until used. 
        [Fact]
        public async Task UserInfoLazy()
        {
            var userInfo = new MyUserInfo();

            SymbolTable symbol = new SymbolTable();

            var config = new PowerFxConfig();
            config.SymbolTable.AddUserInfoObject(nameof(UserInfo.FullName));
            
            var engine = new RecalcEngine(config);

            var rc = new RuntimeConfig();
            rc.SetUserInfo(userInfo);

            var check = engine.Check("User.FullName");
            Assert.True(check.IsSuccess);
            Assert.Equal(0, userInfo._counter);

            var result = check.GetEvaluator().Eval(rc);
            Assert.Equal(1, userInfo._counter);

            Assert.Equal("full name", result.ToObject());
        }

        [Fact]
        public async Task UserInfoCancel()
        {
            var userInfo = new MyUserInfo();

            SymbolTable symbol = new SymbolTable();

            var config = new PowerFxConfig();
            config.SymbolTable.AddUserInfoObject(nameof(UserInfo.Email));

            var engine = new RecalcEngine(config);

            var rc = new RuntimeConfig();
            rc.SetUserInfo(userInfo);

            var check = engine.Check("User.Email");
            Assert.True(check.IsSuccess);
            Assert.Equal(0, userInfo._counter);

            using var cancel = new CancellationTokenSource();
            var task = check.GetEvaluator().EvalAsync(cancel.Token, rc);

            cancel.CancelAfter(50);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task.ConfigureAwait(false)).ConfigureAwait(false);
        }

        // Also demonstrates we can partially implement.
        public class MyUserInfo : UserInfo
        {
            public int _counter = 0;

            public override async Task<string> FullName(CancellationToken cancel = default)
            {
                _counter++;
                return "full name";
            }

            public TaskCompletionSource<string> _tsc = new TaskCompletionSource<string>();

            public override async Task<string> Email(CancellationToken cancel = default)
            {
                var task = _tsc.Task;
                var timeout = Task.Delay(Timeout.Infinite, cancel);
                var x = await Task.WhenAny(task, timeout).ConfigureAwait(false);

                cancel.ThrowIfCancellationRequested();

                // Should never get here. Should have cancelled first. 
                throw new Exception($"Shouldn't be here.");
            }
        }
    }
}
