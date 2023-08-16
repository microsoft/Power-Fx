// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class CoreUtilExtensionTests
    {
        [Fact]
        public void GetDetailedExceptionMessage_NullException_ReturnsNoExceptionProvided()
        {
            Exception ex = null;         
            var result = ex.GetDetailedExceptionMessage();         
            Assert.Equal("Exception is null", result);
        }

        [Fact]
        public void GetDetailedExceptionMessage_DivisionByZero()
        {
            try
            {
                var x = 0;
                var y = 1 / x;
            }
            catch (Exception ex)
            {
                var result = ex.GetDetailedExceptionMessage();
                Assert.Contains(@"Exception System.DivideByZeroException: Message='Attempted to divide by zero.', HResult=0x80020012, StackTrace='   at Microsoft.PowerFx.Core.Tests.CoreUtilExtensionTests.GetDetailedExceptionMessage_DivisionByZero() in C:\Data\2\Power-Fx\src\tests\Microsoft.PowerFx.Core.Tests\CoreUtilExtensionTests.cs", result);
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void GetDetailedExceptionMessage_Inner()
        {
            Exception ex = new HttpRequestException("Some HTTP error", new Exception("Some inner exception") { HResult = unchecked((int)0x80740BC0) }) { HResult = unchecked((int)0x80190477) };
            string msg = ex.GetDetailedExceptionMessage();
            string expected = @"Exception System.Net.Http.HttpRequestException: Message='Some HTTP error', HResult=0x80190477, StackTrace=''
   ----------
   [Inner]Exception System.Exception: Message='Some inner exception', HResult=0x80740BC0, StackTrace=''";

            Assert.Equal(expected, msg);
        }

        [Fact]
        public void GetDetailedExceptionMessage_Aggregate()
        {
            AggregateException ae = new AggregateException(
                "Some Exception",
                new AggregateException(
                    "Some Exception - 2",
                    new AggregateException(
                        "Some Exception - 3",
                        new AggregateException(
                            "Some Exception - 4",
                            new StackOverflowException("Oups!"),
                            new DivideByZeroException("Not good!")),
                        new WebException(
                            "Web exception",
                            new HttpRequestException("HTTP request exception"))),
                    new InvalidOperationException("Invalid operation"),
                    new DllNotFoundException("This disk is too large, I can't find it")),
                new CookieException());

            string msg = ae.GetDetailedExceptionMessage(1);

            Assert.Equal(@"Exception System.AggregateException: Message='Some Exception (Some Exception - 2 (Some Exception - 3 (Some Exception - 4 (Oups!) (Not good!)) (Web exception)) (Invalid operation) (This disk is too large, I can't find it)) (One of the identified items was in an invalid format.)', HResult=0x80131500, StackTrace=''", msg);

            msg = ae.GetDetailedExceptionMessage();
            string expected = @"Exception System.AggregateException: Message='Some Exception (Some Exception - 2 (Some Exception - 3 (Some Exception - 4 (Oups!) (Not good!)) (Web exception)) (Invalid operation) (This disk is too large, I can't find it)) (One of the identified items was in an invalid format.)', HResult=0x80131500, StackTrace=''
   ----------
   [Inner#0]Exception System.AggregateException: Message='Some Exception - 2 (Some Exception - 3 (Some Exception - 4 (Oups!) (Not good!)) (Web exception)) (Invalid operation) (This disk is too large, I can't find it)', HResult=0x80131500, StackTrace=''
      ----------
      [Inner#0]Exception System.AggregateException: Message='Some Exception - 3 (Some Exception - 4 (Oups!) (Not good!)) (Web exception)', HResult=0x80131500, StackTrace=''
         ----------
         [Inner#0]Exception System.AggregateException: Message='Some Exception - 4 (Oups!) (Not good!)', HResult=0x80131500, StackTrace=''
            ----------
            [Inner#0]Exception System.StackOverflowException: Message='Oups!', HResult=0x800703E9, StackTrace=''
            ----------
            [Inner#1]Exception System.DivideByZeroException: Message='Not good!', HResult=0x80020012, StackTrace=''
         ----------
         [Inner#1]Exception System.Net.WebException: Message='Web exception', HResult=0x80131500, Status=16 (UnknownError), StackTrace=''
            ----------
            [Inner]Exception System.Net.Http.HttpRequestException: Message='HTTP request exception', HResult=0x80131500, StackTrace=''
      ----------
      [Inner#1]Exception System.InvalidOperationException: Message='Invalid operation', HResult=0x80131509, StackTrace=''
      ----------
      [Inner#2]Exception System.DllNotFoundException: Message='This disk is too large, I can't find it', HResult=0x80131524, StackTrace=''
   ----------
   [Inner#1]Exception System.Net.CookieException: Message='One of the identified items was in an invalid format.', HResult=0x80131537, StackTrace=''";

            Assert.Equal(expected, msg);
        }

        [Fact]
        public void GetDetailedExceptionMessage_WebRequest()
        {
            using HttpClient client = new HttpClient();
            using HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "https://0.1.2.3");

            try
            {
                HttpResponseMessage response = client.SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (HttpRequestException ex)
            {
                string msg = ex.GetDetailedExceptionMessage();

                Assert.Contains("Exception System.Net.Http.HttpRequestException: Message='A socket operation was attempted to an unreachable network.', HResult=0x80004005", msg);
                Assert.Contains("   [Inner]Exception System.Net.Sockets.SocketException: Message='A socket operation was attempted to an unreachable network.', HResult=0x80004005", msg);
                return;
            }

            Assert.True(false);
        }
    }
}
