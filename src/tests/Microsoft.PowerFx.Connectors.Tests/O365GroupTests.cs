// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class O365GroupTests : BaseConnectorTest
    {
        public O365GroupTests(ITestOutputHelper output)
            : base(output, @"Swagger\Office_365_Groups.json")
        {
        }

        public override string GetNamespace() => "Office365Groups";

        public override string GetEnvironment() => "b29c41cf-173b-e469-830b-4f00163d296b";

        public override string GetEndpoint() => "tip1002-002.azure-apihub.net";

        public override string GetConnectionId() => "380cef7ddacd49d2bdb5b747184c7d8a";

        public override string GetJWTToken() => "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsI...";

        [Fact]
        public void Office365Groups_EnumFuncs()
        {
            EnumerateFunctions();
        }

        // Live execution might fail as we depend on the order of execution
        // By default AddMemberToGroup will fail as the user is already a member of the group
        // As a result RemoveMemberFromGroup should succeed with no result
        // If we rerun those tests, AddMemberToGroup will work & RemoveMemberFromGroup fail...
        [Theory]

        [InlineData(
            /* expression     */ @"First(Filter(Office365Groups.ListGroups().value, ThisRecord.id = ""202a2963-7e7d-4dc6-8aca-a58a2f3a9d53"")).description",
            /* result         */ "TestProject9Aug",
            /* APIM call      */ "GET:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/groups|GET:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/groups?$skiptoken=RFNwdAIAAQAAACpHcm91cF9iZWIxNDE4NS00ZDNmLTQ0MTYtYTc0Mi04OWRmMGI3OWY4NmEqR3JvdXBfYmViMTQxODUtNGQzZi00NDE2LWE3NDItODlkZjBiNzlmODZhAAAAAAAAAAAAAAA",
            /* APIM body      */ "",
            /* response files */ "Response_O365Groups_ListGroups_01.json",
                                 "Response_O365Groups_ListGroups_02.json")]
        
        [InlineData(
            @"First(Office365Groups.ListGroups({ '$filter': ""id eq '202a2963-7e7d-4dc6-8aca-a58a2f3a9d53'"" }).value).description",
            "TestProject9Aug",
            "GET:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/groups?$filter=id+eq+%27202a2963-7e7d-4dc6-8aca-a58a2f3a9d53%27",
            "",
            "Response_O365Groups_ListGroupsWithFilter.json")]
       
        [InlineData(
            @"First(Office365Groups.ListGroupMembers(GUID(""202a2963-7e7d-4dc6-8aca-a58a2f3a9d53"")).value).displayName",
            "aurorauser09",
            "GET:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/groups/202a2963-7e7d-4dc6-8aca-a58a2f3a9d53/members",
            "",
            "Response_O365Groups_ListGroupMembers.json")]
       
        [InlineData(
            @"First(Office365Groups.ListOwnedGroups().value).displayName",
            "11111",
            "GET:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/me/memberOf/$/microsoft.graph.group",
            "",
            "Response_O365Groups_ListOwnedGroups.json")]
        
        [InlineData(
            @"First(Office365Groups.ListOwnedGroupsV2().value).mailNickname",
            "11111",
            "GET:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/me/ownedObjects/$/microsoft.graph.group",
            "",
            "Response_O365Groups_ListOwnedGroupsV2.json")]
        
        [InlineData(
            @"First(Office365Groups.ListOwnedGroupsV3().value).mail",
            "11111@capintegration01.onmicrosoft.com",
            "GET:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v2/v1.0/me/memberOf/$/microsoft.graph.group",
            "",
            "Response_O365Groups_ListOwnedGroupsV3.json")]
        
        [InlineData(
            @"Office365Groups.AddMemberToGroup(GUID(""202a2963-7e7d-4dc6-8aca-a58a2f3a9d53""), ""aurorauser09@capintegration01.onmicrosoft.com"")",
            "ERR:Office365Groups.AddMemberToGroup failed: The server returned an HTTP error with code 400|One or more added object references already exist for the following modified properties: 'members'.",
            "POST:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/groups/202a2963-7e7d-4dc6-8aca-a58a2f3a9d53/members/$ref?userUpn=aurorauser09%40capintegration01.onmicrosoft.com",
            "",
            "400:Response_O365Groups_AddMemberToGroup.json")]

        // In start and end, timeZone is NOT mandatory (even if PA requires it)
        [InlineData( 
            @"Office365Groups.CreateCalendarEvent(GUID(""202a2963-7e7d-4dc6-8aca-a58a2f3a9d53""), ""Event1"", { dateTime: Now() }, { dateTime: Now(), timeZone: ""UTC"" }).subject",
            "Event1",
            "POST:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/groups/202a2963-7e7d-4dc6-8aca-a58a2f3a9d53/events",
            @"{""subject"":""Event1"",""start"":{""dateTime"":""2023-06-01T20:15:07.0000000"",""timeZone"":""UTC""},""end"":{""dateTime"":""2023-06-01T20:15:07.0000000"",""timeZone"":""UTC""}}",
            "201:Response_O365Groups_CreateCalendarEvent.json")]

        [InlineData(
            @"Office365Groups.CreateCalendarEventV2(GUID(""202a2963-7e7d-4dc6-8aca-a58a2f3a9d53""), ""Event1"", { dateTime: Now() }, { dateTime: Now(), timeZone: ""UTC"" }).subject",
            "Event1",
            "POST:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v2/v1.0/groups/202a2963-7e7d-4dc6-8aca-a58a2f3a9d53/events",
            @"{""subject"":""Event1"",""start"":{""dateTime"":""2023-06-01T20:15:07.0000000"",""timeZone"":""UTC""},""end"":{""dateTime"":""2023-06-01T20:15:07.0000000"",""timeZone"":""UTC""},""body"":{""contentType"":""Html""}}",
            "201:Response_O365Groups_CreateCalendarEventV2.json")]

        [InlineData(
            @"Office365Groups.CalendarDeleteItemV2(GUID(""202a2963-7e7d-4dc6-8aca-a58a2f3a9d53""), Office365Groups.CreateCalendarEvent(GUID(""202a2963-7e7d-4dc6-8aca-a58a2f3a9d53""), ""Event2"", { dateTime: Now() }, { dateTime: Now() }).id)",
            null,
            "POST:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/groups/202a2963-7e7d-4dc6-8aca-a58a2f3a9d53/events|DELETE:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/groups/202a2963-7e7d-4dc6-8aca-a58a2f3a9d53/events/AAMkADNlODZjNGVhLWQ2ZTQtNDE5Yy1iMmY3LWI4NzQ3ZWQ0OGU0NwBGAAAAAACGvU-nnljTQqIpP7Z0zqSVBwCuUfhUWTrQTaZb23ackPWXAAAAAAENAACuUfhUWTrQTaZb23ackPWXAABSONxfAAA%253d",
            @"{""subject"":""Event2"",""start"":{""dateTime"":""2023-06-01T20:15:07.0000000"",""timeZone"":""UTC""},""end"":{""dateTime"":""2023-06-01T20:15:07.0000000"",""timeZone"":""UTC""}}",
            "201:Response_O365Groups_CreateCalendarEvent_ToDelete.json",
            "204:")]

        [InlineData(
            @"Office365Groups.UpdateCalendarEvent(GUID(""202a2963-7e7d-4dc6-8aca-a58a2f3a9d53""), Office365Groups.CreateCalendarEvent(GUID(""202a2963-7e7d-4dc6-8aca-a58a2f3a9d53""), ""Event3"", { dateTime: Now(), timeZone: ""UTC"" }, { dateTime: Now(), timeZone: ""UTC"" }).id, ""Event4"", { dateTime: Now(), timeZone: ""UTC"" }, { dateTime: Now(), timeZone: ""UTC"" }).subject",
            "Event4",
            "POST:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/groups/202a2963-7e7d-4dc6-8aca-a58a2f3a9d53/events|PATCH:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/groups/202a2963-7e7d-4dc6-8aca-a58a2f3a9d53/events/AAMkADNlODZjNGVhLWQ2ZTQtNDE5Yy1iMmY3LWI4NzQ3ZWQ0OGU0NwBGAAAAAACGvU-nnljTQqIpP7Z0zqSVBwCuUfhUWTrQTaZb23ackPWXAAAAAAENAACuUfhUWTrQTaZb23ackPWXAABSONxhAAA%253d",
            @"{""subject"":""Event3"",""start"":{""dateTime"":""2023-06-01T20:15:07.0000000"",""timeZone"":""UTC""},""end"":{""dateTime"":""2023-06-01T20:15:07.0000000"",""timeZone"":""UTC""}}|{""subject"":""Event4"",""start"":{""dateTime"":""2023-06-01T20:15:07.0000000"",""timeZone"":""UTC""},""end"":{""dateTime"":""2023-06-01T20:15:07.0000000"",""timeZone"":""UTC""},""body"":{""contentType"":""Html""}}",
            "201:Response_O365Groups_CreateCalendarEvent_ToUpdate.json",
            "Response_O365Groups_UpdateCalendarEvent.json")]

        [InlineData(
            @"Office365Groups.RemoveMemberFromGroup(GUID(""202a2963-7e7d-4dc6-8aca-a58a2f3a9d53""), ""aurorauser09@capintegration01.onmicrosoft.com"")",
            null,
            "DELETE:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v1.0/groups/202a2963-7e7d-4dc6-8aca-a58a2f3a9d53/members/memberId/$ref?userUpn=aurorauser09%40capintegration01.onmicrosoft.com",
            "",
            "204:")]

        // In PA, a 3rd param is required for Body even though it is "required":false in swagger file
        [InlineData(
            @"Office365Groups.HttpRequest(""https://graph.microsoft.com/v1.0/groups/202a2963-7e7d-4dc6-8aca-a58a2f3a9d53/events"", ""GET"")",
            "RECORD",
            "POST:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/httprequest",
            "",
            "Response_O365Groups_HttpRequest.json")]

        // In PA, a 3rd param is required for Body even though it is "required":false in swagger file
        [InlineData( 
            @"Office365Groups.HttpRequestV2(""https://graph.microsoft.com/v1.0/groups/202a2963-7e7d-4dc6-8aca-a58a2f3a9d53/events"", ""GET"")", 
            "RECORD",
            "POST:/apim/office365groups/380cef7ddacd49d2bdb5b747184c7d8a/v2/httprequest",
            "",
            "Response_O365Groups_HttpRequestV2.json")] 
        public async Task Office365Groups_Functions(string expr, string expectedResult, string xUrls, string xBodies, params string[] expectedFiles)
        {
            await RunConnectorTestAsync(false, expr, expectedResult, xUrls, xBodies, expectedFiles, true).ConfigureAwait(false);
        }       

        [Theory]
        [InlineData("First(Office365Groups", "Office365Groups|Office365Groups.AddMemberToGroup|Office365Groups.CalendarDeleteItemV2|Office365Groups.CreateCalendarEventV2|Office365Groups.HttpRequestV2|Office365Groups.ListGroupMembers|Office365Groups.ListGroups|Office365Groups.ListOwnedGroups|Office365Groups.ListOwnedGroupsV2|Office365Groups.ListOwnedGroupsV3|Office365Groups.RemoveMemberFromGroup|Office365Groups.UpdateCalendarEvent")]
        [InlineData("First(Office365Groups.", "AddMemberToGroup|CalendarDeleteItemV2|CreateCalendarEventV2|HttpRequestV2|ListGroupMembers|ListGroups|ListOwnedGroups|ListOwnedGroupsV2|ListOwnedGroupsV3|RemoveMemberFromGroup|UpdateCalendarEvent")]
        [InlineData("First(Office365Groups.Lis", "ListGroupMembers|ListGroups|ListOwnedGroups|ListOwnedGroupsV2|ListOwnedGroupsV3")]
        [InlineData("First(Office365Groups.ListGroups(", "ListGroups({ $filter:String,$top:Decimal,$skiptoken:String })")]
        [InlineData("First(Office365Groups.ListGroups({", "'$filter':String|'$skiptoken':String|'$top':Decimal")]
        [InlineData("First(Office365Groups.ListGroups({'", "'$filter':String|'$skiptoken':String|'$top':Decimal")]
        [InlineData("First(Office365Groups.ListGroups({'$", "'$filter':String|'$skiptoken':String|'$top':Decimal")]
        [InlineData("First(Office365Groups.ListGroups({'$f", "'$filter':String")]
        [InlineData("First(Filter(Office365Groups.ListGroups().", "value:Table|'@odata.context':String|'@odata.nextLink':String")]
        [InlineData("First(Filter(Office365Groups.ListGroups().value,", "classification:String|createdDateTime:DateTime|description:String|displayName:String|id:String|mail:String|mailEnabled:Boolean|mailNickname:String|onPremisesLastSyncDateTime:String|onPremisesSecurityIdentifier:String|onPremisesSyncEnabled:Boolean|renewedDateTime:DateTime|securityEnabled:Boolean|ThisRecord:Record|visibility:String")]
        [InlineData("First(Filter(Office365Groups.ListGroups().value, ThisRecord.", "classification:String|createdDateTime:DateTime|description:String|displayName:String|id:String|mail:String|mailEnabled:Boolean|mailNickname:String|onPremisesLastSyncDateTime:String|onPremisesSecurityIdentifier:String|onPremisesSyncEnabled:Boolean|renewedDateTime:DateTime|securityEnabled:Boolean|visibility:String")]
        [InlineData("First(Filter(Office365Groups.ListGroups().value, ThisRecord.id = \"202", "Filter(source, logical_test, ...)|Filter(source, logical_test, logical_test, ...)|Filter(source, logical_test, logical_test, logical_test, ...)")]
        [InlineData("First(Filter(Office365Groups.ListGroups().value, This", "ThisRecord:Record")]
        [InlineData("First(Filter(Office365Groups.ListGroups().value, mail", "mail:String|mailEnabled:Boolean|mailNickname:String")]
        public async Task Office365Groups_ListGroups_Intellisense(string expr, string expectedSuggestions)
        {
            RunIntellisenseTest(expr, expectedSuggestions);
        }        
    }
}
