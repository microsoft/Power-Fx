// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Intellisense;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class O365UserTests : BaseConnectorTest
    {
        public O365UserTests(ITestOutputHelper output)
            : base(output, @"Swagger\Office_365_Users.json")
        {
        }

        public override string GetNamespace() => "Office365Users";

        public override string GetEnvironment() => "b29c41cf-173b-e469-830b-4f00163d296b";

        public override string GetEndpoint() => "tip1002-002.azure-apihub.net";

        public override string GetConnectionId() => "1870991d56b04959a52f6704949eccad";

        public override string GetJWTToken() => "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsI...";

        [Fact]
        public void Office365Users_EnumFuncs()
        {
            EnumerateFunctions();
        }

        // Live execution might fail as we depend on the order of execution
        // By default AddMemberToGroup will fail as the user is already a member of the group
        // As a result RemoveMemberFromGroup should succeed with no result
        // If we rerun those tests, AddMemberToGroup will work & RemoveMemberFromGroup fail...
        [Theory]

        /*
            DirectReports (Deprecated)
            DirectReportsV2
            HttpRequest
            Manager (Deprecated)
            ManagerV2
            MyProfile (Deprecated)
            MyProfileV2
            MyTrendingDocuments
            RelevantPeople
            SearchUser (Deprecated)
            SearchUserV2
            TestConnection (Internal)
            TrendingDocuments
            UpdateMyPhoto
            UpdateMyProfile
            UserPhoto (Deprecated)
            UserPhotoMetadata
            UserPhotoV2
            UserProfile (Deprecated)
            UserProfileV2         
         */

        [InlineData(
            /* expression     */ @"First(Office365Users.SearchUser()).UserPrincipalName",
            /* result         */ "A_CAPEQ_01@capintegration01.onmicrosoft.com",
            /* APIM call      */ "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users?top=0",
            /* APIM body      */ "",
            /* response files */ "Response_O365Users_SearchUser.json")]

        [InlineData(
            @"First(Office365Users.SearchUser({ top: 5, searchTerm: ""sales""})).UserPrincipalName",
            "powerappsuci_sales_01@capintegration01.onmicrosoft.com",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users?searchTerm=sales&top=5",
            "",
            "Response_O365Users_SearchUser2.json")]

        // searchTerm is a "mandatory" parameter when isSearchTermRequired isn't set to false
        // When not provided or empty, this function returns no result.
        [InlineData(
            @"First(Office365Users.SearchUserV2({searchTerm: ""power""}).value).UserPrincipalName",
            "powerappsguestuser01@capintegration01.onmicrosoft.com",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/v2/users?searchTerm=power&isSearchTermRequired=True",
            "",
            "Response_O365Users_SearchUserV2.json")]

        // Hidden function
        [InlineData(
            @"Office365Users.TestConnection()",
            null,
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/testconnection",
            "",
            "200:")]

        [InlineData(
            @"First(Office365Users.MyTrendingDocuments().value).id",
            "TF_YeomWxegok2ermX9rNQjL8ysMK1jeXRLodBeZLeAw5A7NFNFXOMURYfTEUPCA8fspkPZrk648UqEd63IrjbypA",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/codeless/beta/me/insights/trending",
            "",
            "Response_O365Users_MyTrendingDocuments.json")]

        [InlineData(
            @"First(Office365Users.TrendingDocuments(""32ba11bd-ff15-46f7-bd33-6b481e5f5c7e"").value).id",
            "TF_YeomWxegok2ermX9rNQjL8ysMK1jeXRLodBeZLeAw5A7NFNFXOMURYfTEUPCA8fspkPZrk648UqEd63IrjbypA",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/codeless/beta/users/32ba11bd-ff15-46f7-bd33-6b481e5f5c7e/insights/trending",
            "",
            "Response_O365Users_TrendingDocuments.json")]

        [InlineData(
            @"Office365Users.MyProfile().MailNickname",
            "aurorauser09",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/me",
            "",
            "Response_O365Users_MyProfile.json")]

        [InlineData(
            @"Office365Users.MyProfileV2().id",
            "32ba11bd-ff15-46f7-bd33-6b481e5f5c7e",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/codeless/v1.0/me",
            "",
            "Response_O365Users_MyProfileV2.json")]

        [InlineData(
            @"Office365Users.MyProfileV2({'$select': ""id, mySite, mail, mailNickName, userPrincipalName""}).mail",
            "aurorauser09@capintegration01.onmicrosoft.com",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/codeless/v1.0/me?$select=id%2c+mySite%2c+mail%2c+mailNickName%2c+userPrincipalName",
            "",
            "Response_O365Users_MyProfileV2A.json")]

        // This function should return a FormulaValue with DType.Image type and isn't currently supported
        //[InlineData(
        //    @"Office365Users.UserPhoto(""a6d41679-60f7-4623-8d38-0538c02e59f9"")",
        //    "RAW", // JPG file - Later we'll have to use "IMAGE" and verify we get an Fx ImageValue
        //    "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/photo/value?userId=a6d41679-60f7-4623-8d38-0538c02e59f9",
        //    "",
        //    "Response_O365Users_UserPhoto.jpeg")]

        // This function should return a FormulaValue with DType.Image type and isn't currently supported
        //[InlineData(
        //    @"Office365Users.UserPhotoV2(""a6d41679-60f7-4623-8d38-0538c02e59f9"")",
        //    "RAW", // JPG file - Later we'll have to use "IMAGE" and verify we get an Fx ImageValue
        //    "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/codeless/v1.0/users/a6d41679-60f7-4623-8d38-0538c02e59f9/photo/$value",
        //    "",
        //    "Response_O365Users_UserPhoto.jpeg")]

        [InlineData(
            @"Office365Users.UserPhotoMetadata(""a6d41679-60f7-4623-8d38-0538c02e59f9"").ContentType",
            "image/jpeg",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/photo?userId=a6d41679-60f7-4623-8d38-0538c02e59f9",
            "",
            "Response_O365Users_UserPhotoMetadata.json")]

        // No support for UpdateMyPhoto function

        [InlineData(
            @"Office365Users.Manager(""a6d41679-60f7-4623-8d38-0538c02e59f9"").UserPrincipalName",
            "ERR:Office365Users.Manager failed: The server returned an HTTP error with code 404|No manager found for the specified user.",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/a6d41679-60f7-4623-8d38-0538c02e59f9/manager",
            "",
            "404:Response_O365Users_Manager404.json")]

        [InlineData(
            @"Office365Users.Manager(""32ba11bd-ff15-46f7-bd33-6b481e5f5c7e"").UserPrincipalName",
            "A_CAPEQ_01@capintegration01.onmicrosoft.com",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/32ba11bd-ff15-46f7-bd33-6b481e5f5c7e/manager",
            "",
            "Response_O365Users_Manager.json")]

        [InlineData(
            @"Office365Users.ManagerV2(""32ba11bd-ff15-46f7-bd33-6b481e5f5c7e"").userPrincipalName",
            "A_CAPEQ_01@capintegration01.onmicrosoft.com",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/codeless/v1.0/users/32ba11bd-ff15-46f7-bd33-6b481e5f5c7e/manager",
            "",
            "Response_O365Users_ManagerV2.json")]        

        [InlineData(
            @"Office365Users.UserProfile(""dae277a8-343d-42ff-bd17-16dd634092c9"").UserPrincipalName",
            "accountmanager@capintegration01.onmicrosoft.com",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/dae277a8-343d-42ff-bd17-16dd634092c9",
            "",
            "Response_O365Users_UserProfile.json")]

        [InlineData(
            @"Office365Users.UserProfileV2(""dae277a8-343d-42ff-bd17-16dd634092c9"").userPrincipalName",
            "accountmanager@capintegration01.onmicrosoft.com",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/codeless/v1.0/users/dae277a8-343d-42ff-bd17-16dd634092c9",
            "",
            "Response_O365Users_UserProfileV2.json")]

        [InlineData(
            @"Office365Users.UserProfileV2(""dae277a8-343d-42ff-bd17-16dd634092c9"", {'$select': ""displayName, UserPrincipalName""}).userPrincipalName",
            "accountmanager@capintegration01.onmicrosoft.com",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/codeless/v1.0/users/dae277a8-343d-42ff-bd17-16dd634092c9?$select=displayName%2c+UserPrincipalName",
            "",
            "Response_O365Users_UserProfileV2A.json")]        

        [InlineData(
            @"Office365Users.UpdateMyProfile({schools: [ ""MIT"" ] })",
            null,
            "PATCH:/apim/office365users/1870991d56b04959a52f6704949eccad/codeless/v1.0/me",
            @"{""schools"":[""MIT""]}",
            "204:")]        

        [InlineData(
            @"Last(FirstN(Office365Users.DirectReports(Office365Users.Manager(Office365Users.MyProfile().Id).Id), 2)).UserPrincipalName",
            "eallen@company.com",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/me|GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/1508713b-8fcb-4951-9add-e11bbbd602ff/manager|GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/df050022-0271-4106-8ad9-ca51ff7ee2fe/directReports",
            "",
            "Response_O365Users_DirectReports_01.json",  // MyProfile
            "Response_O365Users_DirectReports_02.json",  // Manager
            "Response_O365Users_DirectReports_03.json")] // DirectReports        

        [InlineData(
            @"Last(FirstN(Office365Users.DirectReportsV2(Office365Users.Manager(Office365Users.MyProfile().Id).Id).value, 2)).userPrincipalName",
            "eallen@company.com",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/me|GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/1508713b-8fcb-4951-9add-e11bbbd602ff/manager|GET:/apim/office365users/1870991d56b04959a52f6704949eccad/codeless/v1.0/users/df050022-0271-4106-8ad9-ca51ff7ee2fe/directReports",
            "",
            "Response_O365Users_DirectReports_01.json",    // MyProfile
            "Response_O365Users_DirectReports_02.json",    // Manager
            "Response_O365Users_DirectReportsV2_03.json")] // DirectReports       

        [InlineData(
            @"First(First(Office365Users.RelevantPeople(""32ba11bd-ff15-46f7-bd33-6b481e5f5c7e"").value).scoredEmailAddresses).address",
            "cavi4762@gmail.com",
            "GET:/apim/office365users/1870991d56b04959a52f6704949eccad/users/32ba11bd-ff15-46f7-bd33-6b481e5f5c7e/relevantpeople",
            "",
            "Response_O365Users_RelevantPeople.json")]

        // In PA, a 3rd param is required for Body even though it is "required":false in swagger file
        [InlineData(
            @"Office365Users.HttpRequest(""https://graph.microsoft.com/v1.0/me"", ""GET"")",
            "RECORD",
            "POST:/apim/office365users/1870991d56b04959a52f6704949eccad/codeless/httprequest",
            "",
            "Response_O365Users_HttpRequest.json")]

        public async Task Office365Users_Functions(string expr, string expectedResult, string xUrls, string xBodies, params string[] expectedFiles)
        {
            await RunConnectorTestAsync(live: false, expr, expectedResult, xUrls, xBodies, expectedFiles, true).ConfigureAwait(false);
        }
    }
}
