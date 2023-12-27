// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class O365OutlookTests : BaseConnectorTest
    {
        public O365OutlookTests(ITestOutputHelper output)
            : base(output, @"Swagger\Office_365_Outlook.json")
        {
        }

        public override string GetNamespace() => "Office365Outlook";

        public override string GetEnvironment() => "123bc930-88ea-ecde-aaae-538f922180f2";

        public override string GetEndpoint() => "tip1002-002.azure-apihub.net";

        public override string GetConnectionId() => "3ea3b1e7f28d4c54a23a4dcbcae7de69";

        [Fact]
        public void Office365Outlook_EnumFuncs()
        {
            EnumerateFunctions();
        }

        // Live execution might fail as we depend on the order of execution and might not be repeated without changing the expressions
        // Deletions will only work once.
        // Some functions aren't supported - see below comments
        [Theory]

        /*
            CalendarDeleteItem (Deprecated)
            CalendarDeleteItemV2
            CalendarGetItem (Deprecated)
            CalendarGetItems (Deprecated)
            CalendarGetTable (Hidden)
            CalendarGetTables (Deprecated)
            CalendarGetTablesV2
            CalendarPatchItem (Deprecated)
            CalendarPostItem (Deprecated)
            ContactDeleteItem (Deprecated)
            ContactDeleteItemV2
            ContactGetItem (Deprecated)
            ContactGetItems (Deprecated)
            ContactGetItemsV2
            ContactGetItemV2
            ContactGetTable (Hidden)
            ContactGetTables (Deprecated)
            ContactGetTablesV2
            ContactPatchItem (Deprecated)
            ContactPatchItemV2
            ContactPostItem (Deprecated)
            ContactPostItemV2
            DeleteApprovalMailSubscription (Hidden)
            DeleteEmail (Deprecated)
            DeleteEmailV2
            DeleteEventSubscription (Hidden)
            DeleteOnNewEmailSubscription (Hidden)
            DeleteOptionsMailSubscription (Hidden)
            ExportEmail (Deprecated)
            ExportEmailV2
            FindMeetingTimes (Deprecated)
            FindMeetingTimesV2
            Flag (Deprecated)
            FlagV2
            ForwardEmail (Deprecated)
            ForwardEmailV2
            GetAttachment (Deprecated)
            GetAttachmentV2
            GetDataSets (Hidden)
            GetDataSetsMetadata (Hidden)
            GetEmail (Deprecated)
            GetEmails (Deprecated)
            GetEmailsV2 (Deprecated)
            GetEmailsV3
            GetEmailV2
            GetEventsCalendarView (Deprecated)
            GetEventsCalendarViewV2 (Deprecated)
            GetEventsCalendarViewV3
            GetMailTips (Deprecated)
            GetMailTipsV2
            GetRoomLists (Deprecated)
            GetRoomListsV2
            GetRooms (Deprecated)
            GetRoomsInRoomList (Deprecated)
            GetRoomsInRoomListV2
            GetRoomsV2
            GetSensitivityLabels (Hidden)
            HttpRequest
            MarkAsRead (Deprecated)
            MarkAsReadV2 (Deprecated)
            MarkAsReadV3
            Move (Deprecated)
            MoveV2
            OnFilePickerBrowse (Hidden)
            OnFilePickerOpen (Hidden)
            ReceiveEventFromSubscription (Hidden)
            ReceiveEventFromSubscriptionV2 (Hidden)
            ReceiveMailFromSubscription (Hidden)
            ReceiveMailFromSubscriptionV2 (Hidden)
            ReceiveResponseGet (Hidden)
            ReceiveResponsePost (Hidden)
            RenewEventSubscription (Hidden)
            RenewOnNewEmailSubscription (Hidden)
            ReplyTo (Deprecated)
            ReplyToV2 (Deprecated)
            ReplyToV3
            RespondToEvent (Deprecated)
            RespondToEventV2
            SendEmail (Deprecated)
            SendEmailV2
            SetAutomaticRepliesSetting (Deprecated)
            SetAutomaticRepliesSettingV2
            SharedMailboxSendEmail (Deprecated)
            SharedMailboxSendEmailV2
            TestConnection (Hidden)
            UpdateMyContactPhoto
            V2CalendarGetItem (Deprecated)
            V2CalendarGetItems (Deprecated)
            V2CalendarPatchItem (Deprecated)
            V2CalendarPostItem (Deprecated)
            V3CalendarGetItem
            V3CalendarGetItems (Deprecated)
            V3CalendarPatchItem (Deprecated)
            V3CalendarPostItem (Deprecated)
            V4CalendarGetItems
            V4CalendarPatchItem
            V4CalendarPostItem
        */

        [InlineData(
            /* expression     */ @"First(Office365Outlook.GetEmails({folderPath: ""Inbox""})).Subject",
            /* result         */ "Your scheduled refresh has been paused.",
            /* APIM call      */ "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Mail?folderPath=Inbox&fetchOnlyUnread=True&includeAttachments=False&top=10&skip=0",
            /* APIM body      */ "",
            /* response files */ "Response_O365Outlook_GetEmails.json")]

        [InlineData(
            @"First(Office365Outlook.GetEmails()).Subject",
            "Your scheduled refresh has been paused.",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Mail?folderPath=Inbox&fetchOnlyUnread=True&includeAttachments=False&top=10&skip=0",
            "",
            "Response_O365Outlook_GetEmails.json")]

        [InlineData(
            @"First(Office365Outlook.GetEmailsV2({from:""no-reply-powerbi@microsoft.com""}).value).DateTimeReceived",
            "DATETIME:2023-12-19T06:43:51.0000000Z",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/v2/Mail?folderPath=Inbox&from=no-reply-powerbi%40microsoft.com&importance=Any&fetchOnlyWithAttachment=False&fetchOnlyUnread=True&fetchOnlyFlagged=False&includeAttachments=False&top=10",
            "",
            "Response_O365Outlook_GetEmailsV2.json")]

        [InlineData(
            @"First(Office365Outlook.GetEmailsV3({from:""no-reply-powerbi@microsoft.com""}).value).internetMessageId",
            "<3bffb0bf-213c-4e07-92e4-3b9eb49d4288@az.eastus.microsoft.com>",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/v3/Mail?folderPath=Inbox&from=no-reply-powerbi%40microsoft.com&importance=Any&fetchOnlyWithAttachment=False&fetchOnlyUnread=True&fetchOnlyFlagged=False&includeAttachments=False&top=10",
            "",
            "Response_O365Outlook_GetEmailsV3.json")]

        [InlineData(
            @"Office365Outlook.GetEmail(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABwN-WMAAA="").Subject",
            "Your scheduled refresh has been paused.",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Mail/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABwN-WMAAA%3d?includeAttachments=False",
            "",
            "Response_O365Outlook_GetEmail.json")]

        [InlineData(
            @"Office365Outlook.GetEmailV2(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABwN-WMAAA="").subject",
            "Your scheduled refresh has been paused.",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/v2/Mail/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABwN-WMAAA%3d?includeAttachments=False",
            "",
            "Response_O365Outlook_GetEmailV2.json")]

        [InlineData(
            @"Office365Outlook.GetAttachment(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZgAAA="", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZgAAABEgAQAAaOrCiujABLpFlM9d390HE="")",
            "RAW", // PNG file - Later we'll have to use "IMAGE" and verify we get an Fx ImageValue
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Mail/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZgAAA%3d/Attachments/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZgAAABEgAQAAaOrCiujABLpFlM9d390HE%3d",
            "",
            "Response_O365Outlook_GetAttachment.png")]

        [InlineData(
            @"Office365Outlook.GetAttachmentV2(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZgAAA="", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZgAAABEgAQAAaOrCiujABLpFlM9d390HE="").name",
            "Outlook-rwoti1ve.png",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/messages/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZgAAA%3d/attachments/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZgAAABEgAQAAaOrCiujABLpFlM9d390HE%3d",
            "",
            "Response_O365Outlook_GetAttachmentV2.json")]

        // This function is not available in Power Apps as it is hidden
        [InlineData(
            @"First(Office365Outlook.GetDataSets().value).DisplayName",
            "Calendars",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets",
            "",
            "Response_O365Outlook_GetDataSets.json")]

        // This function is not available in Power Apps as it is hidden
        [InlineData(
            @"Office365Outlook.GetDataSetsMetadata().tabular.tablePluralName",
            "Tables",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/$metadata.json/datasets",
            "",
            "Response_O365Outlook_GetDataSetsMetadata.json")]

        [InlineData(
            @"First(Office365Outlook.V2CalendarGetItems(""Calendar"").value).Id",
            "AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAABrZtyeAAA=",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v2/tables/Calendar/items",
            "",
            "Response_O365Outlook_V2CalendarGetItems.json")]

        [InlineData(
            @"First(Office365Outlook.V2CalendarGetItems(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA="").value).Location",
            "United Kingdom",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v2/tables/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA%253d/items",
            "",
            "Response_O365Outlook_V2CalendarGetItems2.json")]

        [InlineData(
            @"First(Office365Outlook.V3CalendarGetItems(""Calendar"").value).Subject",
            "564654",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v3/tables/Calendar/items",
            "",
            "Response_O365Outlook_V3CalendarGetItems.json")]

        [InlineData(
            @"First(Office365Outlook.V4CalendarGetItems(""Calendar"").value).iCalUId",
            "040000008200E00074C5B7101A82E00800000000AE18CD5B2D2DDA010000000000000000100000002ADF82E895DB074895BA50D227CBBE87",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v4/tables/Calendar/items",
            "",
            "Response_O365Outlook_V4CalendarGetItems.json")]

        [InlineData(
            @"First(Office365Outlook.CalendarGetTables().value).DisplayName",
            "Birthdays",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/tables",
            "",
            "Response_O365Outlook_CalendarGetTables.json")]

        [InlineData(
            @"First(Office365Outlook.CalendarGetTablesV2().value).owner.address",
            "aurorauser01@aurorafinanceintegration02.onmicrosoft.com",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/calendars?skip=0&top=256&orderBy=name",
            "",
            "Response_O365Outlook_CalendarGetTablesV2.json")]

        // This function is not available in Power Apps as it is hidden
        [InlineData(
            @"Office365Outlook.CalendarGetTable(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA="").title",
            "United Kingdom holidays",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/$metadata.json/datasets/calendars/tables/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA%253d",
            "",
            "Response_O365Outlook_CalendarGetTable.json")]

        [InlineData(
            @"First(Office365Outlook.CalendarGetItems(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA="").value).Subject",
            "Boxing Day Bank Holiday",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/tables/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA%253d/items",
            "",
            "Response_O365Outlook_CalendarGetItems.json")]

        [InlineData(
            @"Office365Outlook.CalendarGetItem(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA="",""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAABNUGGhAADr1A2S1MsmTIW9552ybeHbAABNUI7rAAA="").Subject",
            "Late Summer Holiday",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/tables/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA%253d/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAABNUGGhAADr1A2S1MsmTIW9552ybeHbAABNUI7rAAA%253d",
            "",
            "Response_O365Outlook_CalendarGetItem.json")]

        [InlineData(
            @"Office365Outlook.V2CalendarGetItem(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA="",""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAABNUGGhAADr1A2S1MsmTIW9552ybeHbAABNUI7rAAA="").Subject",
            "Late Summer Holiday",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v2/tables/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA%253d/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAABNUGGhAADr1A2S1MsmTIW9552ybeHbAABNUI7rAAA%253d",
            "",
            "Response_O365Outlook_V2CalendarGetItem.json")]

        [InlineData(
            @"Office365Outlook.V3CalendarGetItem(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA="",""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAABNUGGhAADr1A2S1MsmTIW9552ybeHbAABNUI7rAAA="").subject",
            "Late Summer Holiday",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v3/tables/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbIAAA%253d/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAABNUGGhAADr1A2S1MsmTIW9552ybeHbAABNUI7rAAA%253d",
            "",
            "Response_O365Outlook_V3CalendarGetItem.json")]

        [InlineData(
            @"First(Office365Outlook.GetEventsCalendarView(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbHAAA="", ""2020-01-01T08:00:00-07:00"", ""2024-01-01T08:00:00-07:00"").Values).Subject",
            "564654",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Events/CalendarView?calendarId=AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbHAAA%3d&startDateTimeOffset=2020-01-01T08%3a00%3a00-07%3a00&endDateTimeOffset=2024-01-01T08%3a00%3a00-07%3a00",
            "",
            "Response_O365Outlook_GetEventsCalendarView.json")]

        [InlineData(
            @"First(Office365Outlook.GetEventsCalendarView(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbHAAA="", ""2017-01-01T08:00:00-07:00"", ""2024-01-01T08:00:00-07:00"").Values).Subject",
            "ERR:Office365Outlook.GetEventsCalendarView failed: The server returned an HTTP error with code 400.|Your request can't be completed. The range between the start and end dates is greater than the allowed range. Maximum number of days: 1825",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Events/CalendarView?calendarId=AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbHAAA%3d&startDateTimeOffset=2017-01-01T08%3a00%3a00-07%3a00&endDateTimeOffset=2024-01-01T08%3a00%3a00-07%3a00",
            "",
            "400:Response_O365Outlook_GetEventsCalendarView_Error.json")]

        [InlineData(
            @"First(Office365Outlook.GetEventsCalendarViewV2(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbHAAA="", ""2020-01-01T08:00:00-07:00"", ""2024-01-01T08:00:00-07:00"").value).TimeZone",
            "Pacific Standard Time",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v2/tables/items/calendarview?calendarId=AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbHAAA%3d&startDateTimeOffset=2020-01-01T08%3a00%3a00-07%3a00&endDateTimeOffset=2024-01-01T08%3a00%3a00-07%3a00",
            "",
            "Response_O365Outlook_GetEventsCalendarViewV2.json")]

        [InlineData(
            @"First(Office365Outlook.GetEventsCalendarViewV3(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbHAAA="", ""2020-01-01T08:00:00-07:00"", ""2024-01-01T08:00:00-07:00"").value).webLink",
            "https://outlook.office365.com/owa/?itemid=AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAABrZtyeAAA%3D&exvsurl=1&path=/calendar/item",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v3/tables/items/calendarview?calendarId=AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEGAADr1A2S1MsmTIW9552ybeHbAABNUIbHAAA%3d&startDateTimeUtc=2020-01-01T08%3a00%3a00-07%3a00&endDateTimeUtc=2024-01-01T08%3a00%3a00-07%3a00",
            "",
            "Response_O365Outlook_GetEventsCalendarViewV3.json")]

        [InlineData(
            @"Office365Outlook.GetMailTips(""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"").MaxMessageSize",
            "DECIMAL:37748736",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/MailTips?mailboxAddress=aurorauser01%40aurorafinanceintegration02.onmicrosoft.com",
            "",
            "Response_O365Outlook_GetMailTips.json")]

        [InlineData(
            @"First(Office365Outlook.GetMailTipsV2(""externalMemberCount, mailboxFullStatus, maxMessageSize, moderationStatus"", [""aurorauser01@aurorafinanceintegration02.onmicrosoft.com""]).value).maxMessageSize",
            "DECIMAL:37748736",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/getMailTips",
            @"{""MailTipsOptions"":""externalMemberCount, mailboxFullStatus, maxMessageSize, moderationStatus"",""EmailAddresses"":[""aurorauser01@aurorafinanceintegration02.onmicrosoft.com""]}",
            "Response_O365Outlook_GetMailTipsV2.json")]

        // Not from Aurora env.
        [InlineData(
            @"First(Office365Outlook.GetRoomLists().value).Address",
            "seattle@aurorafinanceintegration02.onmicrosoft.com",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/api/beta/me/findroomlists",
            "",
            "Response_O365Outlook_GetRoomLists.json")]

        // Not from Aurora env.
        [InlineData(
            @"First(Office365Outlook.GetRoomListsV2().value).address",
            "seattle@aurorafinanceintegration02.onmicrosoft.com",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/beta/me/findRoomLists",
            "",
            "Response_O365Outlook_GetRoomListsV2.json")]

        // Not from Aurora env.
        [InlineData(
            @"First(Office365Outlook.GetRooms().value).Address",
            "seattle@aurorafinanceintegration02.onmicrosoft.com",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/api/beta/me/findrooms",
            "",
            "Response_O365Outlook_GetRooms.json")]

        // Not from Aurora env.
        [InlineData(
            @"First(Office365Outlook.GetRoomsV2().value).address",
            "seattle@aurorafinanceintegration02.onmicrosoft.com",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/beta/me/findRooms",
            "",
            "Response_O365Outlook_GetRoomsV2.json")]

        // Not from Aurora env.
        [InlineData(
            @"First(Office365Outlook.GetRoomsInRoomList(""seattle@aurorafinanceintegration02.onmicrosoft.com"").value).Address",
            "cfsea11@aurorafinanceintegration02.onmicrosoft.com",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/api/beta/me/findrooms(roomlist='seattle%40aurorafinanceintegration02.onmicrosoft.com')",
            "",
            "Response_O365Outlook_GetRoomsInRoomList.json")]

        // Not from Aurora env.
        [InlineData(
            @"First(Office365Outlook.GetRoomsInRoomListV2(""seattle@aurorafinanceintegration02.onmicrosoft.com"").value).address",
            "cfsea11@aurorafinanceintegration02.onmicrosoft.com",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/beta/me/findRooms(RoomList='seattle%40aurorafinanceintegration02.onmicrosoft.com')",
            "",
            "Response_O365Outlook_GetRoomsInRoomListV2.json")]

        // This function is not available in Power Apps as it is hidden
        // Requires an update swagger file as we were expecting a record with 'value' containing the array of sensitivity labels
        // Impacts SendEmail and SendEmailV2 functions where dynamic intellisense will not work properly for Sensitivity param
        [InlineData(
            @"First(Office365Outlook.GetSensitivityLabels()).DisplayName",
            "Public",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/SensitivityLabels",
            "",
            "Response_O365Outlook_GetSensitivityLabels.json")]

        [InlineData(
            @"First(Office365Outlook.ContactGetTables().value).DisplayName",
            "Contacts",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/contacts/tables",
            "",
            "Response_O365Outlook_ContactGetTables.json")]

        // Hidden function
        [InlineData(
            @"Office365Outlook.ContactGetTable(""Contacts"").title",
            "Contacts",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/$metadata.json/datasets/contacts/tables/Contacts",
            "",
            "Response_O365Outlook_ContactGetTable.json")]

        [InlineData(
            @"First(Office365Outlook.ContactGetTablesV2().value).displayName",
            "Contacts",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/v2/datasets/contacts/tables",
            "",
            "Response_O365Outlook_ContactGetTablesV2.json")]

        [InlineData(
            @"First(Office365Outlook.ContactGetItems(""Contacts"").value).DisplayName",
            "John Doe",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/contacts/tables/Contacts/items",
            "",
            "Response_O365Outlook_ContactGetItems.json")]

        [InlineData(
            @"First(Office365Outlook.ContactGetItemsV2(""Contacts"").value).displayName",
            "John Doe",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/contactFolders/Contacts/contacts",
            "",
            "Response_O365Outlook_ContactGetItemsV2.json")]

        [InlineData(
            @"Office365Outlook.ContactGetItem(""Contacts"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LYAAA="").DisplayName",
            "John Doe",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/contacts/tables/Contacts/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LYAAA%253d",
            "",
            "Response_O365Outlook_ContactGetItem.json")]

        [InlineData(
            @"Office365Outlook.ContactGetItemV2(""Contacts"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LYAAA="").displayName",
            "John Doe",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/contactFolders/Contacts/contacts/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LYAAA%253d",
            "",
            "Response_O365Outlook_ContactGetItemV2.json")]

        // The following functions aren't as they are unsupported, all releated to webhooks or subscriptions
        // They are also deprecated - https://devblogs.microsoft.com/microsoft365dev/subscription-lifetime-changes-for-outlook-resources/
        //
        // DeleteApprovalMailSubscription
        // DeleteEventSubscription
        // DeleteOnNewEmailSubscription
        // DeleteOptionsMailSubscription
        // ReceiveEventFromSubscription
        // ReceiveEventFromSubscriptionV2
        // ReceiveMailFromSubscription
        // ReceiveMailFromSubscriptionV2
        // ReceiveResponseGet
        // ReceiveResponsePost
        // RenewEventSubscription
        // RenewOnNewEmailSubscription
        // RespondToEvent
        // RespondToEventV2

        [InlineData(
            @"Office365Outlook.CalendarPostItem(""Calendar"", DateTime(2023, 12, 20, 17, 05, 0), DateTime(2023, 12, 20, 15, 15, 0), ""Event 17"", { ShowAs: ""Some Event"" }).Subject",
            "Event 17",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/tables/Calendar/items",
            @"{""End"":""2023-12-20T16:05:00.000Z"",""ShowAs"":""Some Event"",""Start"":""2023-12-20T14:15:00.000Z"",""Subject"":""Event 17""}",
            "201:Response_O365Outlook_CalendarPostItem.json")]

        [InlineData(
            @"Office365Outlook.CalendarPatchItem(""Calendar"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAABxF7A1AAA="", DateTime(2023, 12, 20, 17, 05, 0), DateTime(2023, 12, 20, 15, 15, 0), ""Event 18"").Subject",
            "Event 18",
            "PATCH:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/tables/Calendar/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAABxF7A1AAA%253d",
            @"{""End"":""2023-12-20T16:05:00.000Z"",""Start"":""2023-12-20T14:15:00.000Z"",""Subject"":""Event 18""}",
            "200:Response_O365Outlook_CalendarPatchItem.json")]

        [InlineData(
            @"Office365Outlook.CalendarDeleteItem(""Calendar"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAABxF7A1AAA="")",
            "ERR:Office365Outlook.CalendarDeleteItem failed: The server returned an HTTP error with code 404.|The specified object was not found in the store.",
            "DELETE:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/tables/Calendar/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAABxF7A1AAA%253d",
            "",
            "404:Response_O365Outlook_CalendarDeleteItem_NotFound.json")]

        [InlineData(
            @"Office365Outlook.CalendarDeleteItem(""Calendar"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAABxF7A4AAA="")",
            null,
            "DELETE:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/tables/Calendar/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAABxF7A4AAA%253d",
            "",
            "200:")]

        [InlineData(
            @"Office365Outlook.CalendarDeleteItemV2(""Calendar"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAABxF7A7AAA="")",
            null,
            "DELETE:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/calendars/Calendar/events/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAABxF7A7AAA%253d",
            "",
            "204:")]

        [InlineData(
            @"Office365Outlook.ContactPostItem(""Contacts"", ""John Smith"", [""+1 (555) 457-987-1174""], { DisplayName: ""John II"", EmailAddresses: [ { Name: ""email1"", Address: ""john2@nowhere.com"" }, { Name: ""email2"", Address: ""john117zz@hotmail.se"" } ], CompanyName: ""Bank of Sweden""}).DisplayName",
            "John II",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/contacts/tables/Contacts/items",
            @"{""DisplayName"":""John II"",""GivenName"":""John Smith"",""EmailAddresses"":[{""Name"":""email1"",""Address"":""john2@nowhere.com""},{""Name"":""email2"",""Address"":""john117zz@hotmail.se""}],""CompanyName"":""Bank of Sweden"",""HomePhones"":[""\u002B1 (555) 457-987-1174""]}",
            "201:Response_O365Outlook_ContactPostItem.json")]

        [InlineData(
            @"Office365Outlook.ContactPostItemV2(""Contacts"", ""John Smith 3"", [""+1 (555) 457-987-1174""], { displayName: ""John III"", emailAddresses: [ { name: ""email1"", address: ""john2@nowhere.com"" }, { name: ""email2"", address: ""john117zz@hotmail.se"" } ], companyName: ""Bank of Sweden""}).displayName",
            "John III",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/contactFolders/Contacts/contacts",
            @"{""displayName"":""John III"",""givenName"":""John Smith 3"",""emailAddresses"":[{""name"":""email1"",""address"":""john2@nowhere.com""},{""name"":""email2"",""address"":""john117zz@hotmail.se""}],""companyName"":""Bank of Sweden"",""homePhones"":[""\u002B1 (555) 457-987-1174""]}",
            "201:Response_O365Outlook_ContactPostItemV2.json")]

        [InlineData(
            @"Office365Outlook.ContactPatchItem(""Contacts"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LaAAA="", ""John Smith 2"", [""+1 (555) 457-987-1175""]).DisplayName",
            "John Smith 2",
            "PATCH:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/contacts/tables/Contacts/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LaAAA%253d",
            @"{""GivenName"":""John Smith 2"",""HomePhones"":[""\u002B1 (555) 457-987-1175""]}",
            "200:Response_O365Outlook_ContactPatchItem.json")]

        [InlineData(
            @"Office365Outlook.ContactPatchItemV2(""Contacts"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LaAAA="", ""John Smith IIIa"",[""+1 (555) 457-987-1177""]).displayName",
            "John Smith IIIa",
            "PATCH:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/contactFolders/Contacts/contacts/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LaAAA%253d",
            @"{""givenName"":""John Smith IIIa"",""homePhones"":[""\u002B1 (555) 457-987-1177""]}",
            "201:Response_O365Outlook_ContactPatchItemV2.json")]

        [InlineData(
            @"Office365Outlook.ContactDeleteItem(""Contacts"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LnAAA="")",
            null,
            "DELETE:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/contacts/tables/Contacts/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LnAAA%253d",
            "",
            "200:")]

        [InlineData(
            @"Office365Outlook.ContactDeleteItemV2(""Contacts"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LpAAA="")",
            null,
            "DELETE:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/contactFolders/Contacts/contacts/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEOAADr1A2S1MsmTIW9552ybeHbAABxF5LpAAA%253d",
            "",
            "204:")]

        [InlineData(
            @"Office365Outlook.DeleteEmail(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZiAAA="")",
            null,
            "DELETE:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Mail/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZiAAA%3d",
            "",
            "200:")]

        [InlineData(
            @"Office365Outlook.DeleteEmailV2(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZgAAA="")",
            null,
            "DELETE:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/messages/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZgAAA%3d",
            "",
            "204:")]

        [InlineData(
            @"Office365Outlook.ExportEmail(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABwN-WMAAA="")",
            "STARTSWITH:Received: from LV3P223MB0915.NAMP223.PROD.OUTLOOK.COM (2603:10b6:408:1dd::10)",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/api/beta/me/messages/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABwN-WMAAA%3d/$value",
            "",
            "Response_O365Outlook_ExportEmail.eml")]

        [InlineData(
            @"Office365Outlook.ExportEmailV2(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABwN-WMAAA="")",
            "STARTSWITH:Received: from LV3P223MB0915.NAMP223.PROD.OUTLOOK.COM (2603:10b6:408:1dd::10)",
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/beta/me/messages/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABwN-WMAAA%3d/$value",
            "",
            "Response_O365Outlook_ExportEmail.eml")]

        [InlineData(
            @"First(Office365Outlook.FindMeetingTimes().MeetingTimeSuggestions).MeetingTimeSlot.Start.DateTime",
            "2023-12-22T17:00:00.0000000",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/api/v2.0/me/findmeetingtimes",
            @"{""ActivityDomain"":""Work""}",
            "Response_O365Outlook_FindMeetingTimes.json")]

        [InlineData(
            @"First(Office365Outlook.FindMeetingTimesV2().meetingTimeSuggestions).meetingTimeSlot.start.dateTime",
            "2023-12-22T17:30:00.0000000",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/beta/me/findMeetingTimes",
            @"{""ActivityDomain"":""Work""}",
            "Response_O365Outlook_FindMeetingTimesV2.json")]

        [InlineData(
            @"Office365Outlook.Flag(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABvkNaQAAA="")",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Mail/Flag/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABvkNaQAAA%3d",
            "",
            "200:")]

        [InlineData(
            @"Office365Outlook.FlagV2(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABtn17OAAA="", { flag: { flagStatus: ""complete"" } })",
            "STARTSWITH:{\r\n  \"@odata.context\": \"https://graph.microsoft.com/v1.0/$metadata#users('d8e90db6-d09c-4a90-bc1e-86512a797fd3')/messages/$entity\",\r\n  \"@odata.etag\": \"W/\\\"CQAAABYAAADr",
            "PATCH:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/messages/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABtn17OAAA%3d/flag",
            @"{""flag"":{""flagStatus"":""complete""}}",
            "Response_O365Outlook_FlagV2.json")]

        [InlineData(
            @"Office365Outlook.ForwardEmail(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABs3xsZAAA="", ""testuser4@aurorafinanceintegration02.onmicrosoft.com"", {Comment: ""FYI""})",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/api/v2.0/me/messages/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABs3xsZAAA%3d/forward",
            @"{""Comment"":""FYI"",""ToRecipients"":""testuser4@aurorafinanceintegration02.onmicrosoft.com""}",
            "202:")]

        [InlineData(
            @"Office365Outlook.ForwardEmailV2(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABs3xsZAAA="", ""testuser4@aurorafinanceintegration02.onmicrosoft.com"", {Comment: ""FYI""})",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/messages/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABs3xsZAAA%3d/forward",
            @"{""Comment"":""FYI"",""ToRecipients"":""testuser4@aurorafinanceintegration02.onmicrosoft.com""}",
            "202:")]

        [InlineData(
            @"Office365Outlook.HttpRequest(""https://graph.microsoft.com/v1.0/me/calendar"", ""GET"")",
            "RECORD",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/httprequest",
            @"",
            "Response_O365Outlook_HttpRequest.json",
            "Method: GET|Uri: https://graph.microsoft.com/v1.0/me/calendar")] // Parameters are stored in 2 headers: Method & Uri

        [InlineData(
            @"Office365Outlook.MarkAsRead(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABs3xsZAAA="")",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Mail/MarkAsRead/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABs3xsZAAA%3d",
            "",
            "200:")]

        [InlineData(
            @"Office365Outlook.MarkAsReadV2(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABs3xsZAAA="", { isRead: false })",
            "STARTSWITH:{\r\n  \"@odata.context\": \"https://graph.microsoft.com/v1.0/$metadata#users('d8e90db6-d09c-4a90-bc1e-86512a797fd3')/messages/$entity\",\r\n  \"@odata.etag\": \"W/\\\"CQAAABYAAADr1A2S1MsmT",
            "PATCH:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/messages/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABs3xsZAAA%3d/markAsRead",
            @"{""isRead"":false}",
            "Response_O365Outlook_MarkAsReadV2.json")]

        [InlineData(
            @"Office365Outlook.MarkAsReadV3(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABs3xsZAAA="", true)",
            "STARTSWITH:{\r\n  \"@odata.context\": \"https://graph.microsoft.com/v1.0/$metadata#users('d8e90db6-d09c-4a90-bc1e-86512a797fd3')/messages/$entity\",\r\n  \"@odata.etag\": \"W/\\\"CQAAABYAAADr1A2S1MsmT",
            "PATCH:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v3/v1.0/me/messages/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABs3xsZAAA%3d/markAsRead",
            @"{""isRead"":true}",
            "Response_O365Outlook_MarkAsReadV3.json")]

        [InlineData(
            @"Office365Outlook.Move(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZnAAA="", ""Archive"").Id",
            "AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAETAADr1A2S1MsmTIW9552ybeHbAABxF_ZdAAA=",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Mail/Move/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZnAAA%3d?folderPath=Archive",
            "",
            "Response_O365Outlook_Move.json")]

        [InlineData(
            @"Office365Outlook.MoveV2(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAETAADr1A2S1MsmTIW9552ybeHbAABxF_ZdAAA="", ""Inbox"").id",
            "AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZpAAA=",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/v2/Mail/Move/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAETAADr1A2S1MsmTIW9552ybeHbAABxF_ZdAAA%3d?folderPath=Inbox",
            "",
            "Response_O365Outlook_MoveV2.json")]

        /*  File picker functions are not supported yet
                      
            OnFilePickerBrowse
            OnFilePickerOpen
        */

        [InlineData(
            @"Office365Outlook.ReplyTo(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZqAAA="", ""This is my reply."", { replyAll: false })",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Mail/ReplyTo/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZqAAA%3d?comment=This+is+my+reply.&replyAll=False",
            "",
            "200:")]

        [InlineData(
            @"Office365Outlook.ReplyToV2(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZqAAA="", {Body: ""Hello!""})",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/v2/Mail/ReplyTo/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZqAAA%3d",
            @"{""Body"":""Hello!""}",
            "200:")]

        [InlineData(
            @"Office365Outlook.ReplyToV3(""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZqAAA="", {Body: ""Hello2!""})",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/v3/Mail/ReplyTo/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAEMAADr1A2S1MsmTIW9552ybeHbAABxFrZqAAA%3d",
            @"{""Body"":""Hello2!""}",
            "200:")]

        [InlineData(
            @"Office365Outlook.SendEmail(""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"", ""Test email 7"", ""This is a test email."", {IsHtml: false})",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/Mail",
            @"{""To"":""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"",""Subject"":""Test email 7"",""Body"":""This is a test email."",""Importance"":""Normal"",""IsHtml"":false}",
            "200:")]

        [InlineData(
            @"Office365Outlook.SendEmailV2(""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"", ""Test email 8"", ""This is a test email."")",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/v2/Mail",
            @"{""To"":""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"",""Subject"":""Test email 8"",""Body"":""This is a test email."",""Importance"":""Normal""}",
            "200:")]

        [InlineData(
            @"Office365Outlook.SetAutomaticRepliesSetting(""AlwaysEnabled"", ""All"", {InternalReplyMessage: ""Internal reply"", ExternalReplyMessage: ""External reply""})",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/AutomaticRepliesSetting",
            @"{""Status"":""AlwaysEnabled"",""ExternalAudience"":""All"",""InternalReplyMessage"":""Internal reply"",""ExternalReplyMessage"":""External reply""}",
            "200:")]

        [InlineData(
            @"Office365Outlook.SetAutomaticRepliesSettingV2({ automaticRepliesSetting: { status: ""alwaysEnabled"", externalAudience: ""all"", internalReplyMessage: ""internal message"", externalReplyMessage: ""external message""}}).automaticRepliesSetting.status",
            "alwaysEnabled",
            "PATCH:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/codeless/v1.0/me/mailboxSettings",
            @"{""automaticRepliesSetting"":{""status"":""alwaysEnabled"",""externalAudience"":""all"",""internalReplyMessage"":""internal message"",""externalReplyMessage"":""external message""}}",
            "Response_O365Outlook_SetAutomaticRepliesSettingV2.json")]

        [InlineData(
            @"Office365Outlook.SharedMailboxSendEmail(""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"", ""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"", ""Shared email"", ""Some body."")",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/SharedMailbox/Mail",
            @"{""MailboxAddress"":""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"",""To"":""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"",""Subject"":""Shared email"",""Body"":""Some body."",""Importance"":""Normal""}",
            "200:")]

        [InlineData(
            @"Office365Outlook.SharedMailboxSendEmailV2(""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"", ""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"", ""Shared email"", ""Some body."")",
            null,
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/v2/SharedMailbox/Mail",
            @"{""MailboxAddress"":""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"",""To"":""aurorauser01@aurorafinanceintegration02.onmicrosoft.com"",""Subject"":""Shared email"",""Body"":""Some body."",""Importance"":""Normal""}",
            "200:")]

        // Hidden function
        [InlineData(
            @"Office365Outlook.TestConnection()",
            null,
            "GET:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/testconnection",
            "",
            "200:")]

        // No support for UpdateMyContactPhoto function - needs Image support

        [InlineData(
            @"Office365Outlook.V2CalendarPostItem(""Calendar"", ""Event 30"", DateTime(2023, 12, 27, 15, 10, 59, 117.594), DateTime(2023, 12, 27, 16, 22, 3, 902.111)).Subject",
            "Event 30",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v2/tables/Calendar/items",
            @"{""Subject"":""Event 30"",""Start"":""2023-12-27T14:10:59.117Z"",""End"":""2023-12-27T15:22:03.902Z""}",
            "201:Response_O365Outlook_V2CalendarPostItem.json")]

        [InlineData(
            @"Office365Outlook.V3CalendarPostItem(""Calendar"", ""Event 31"", DateTime(2023, 12, 27, 15, 10, 59, 117.594), DateTime(2023, 12, 27, 16, 22, 3, 902.111)).Subject",
            "Event 31",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v3/tables/Calendar/items",
            @"{""Subject"":""Event 31"",""Start"":""2023-12-27T14:10:59.117Z"",""End"":""2023-12-27T15:22:03.902Z""}",
            "201:Response_O365Outlook_V3CalendarPostItem.json")]

        [InlineData(
            @"Office365Outlook.V4CalendarPostItem(""Calendar"", ""Event 32"", DateTime(2023, 12, 27, 15, 10, 59, 117.594), DateTime(2023, 12, 27, 16, 22, 3, 902.111),""(UTC+09:30) Darwin"").startWithTimeZone",
            "DATETIME:2023-12-27T06:40:59.117Z",
            "POST:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v4/tables/Calendar/items",
            @"{""subject"":""Event 32"",""start"":""2023-12-27T15:10:59.117"",""end"":""2023-12-27T16:22:03.902"",""timeZone"":""(UTC\u002B09:30) Darwin""}",
            "201:Response_O365Outlook_V4CalendarPostItem.json")]

        [InlineData(
            @"Office365Outlook.V2CalendarPatchItem(""Calendar"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAAB1rhBHAAA="", ""Event 30a"", DateTime(2023, 12, 27, 15, 10, 59, 117.594), DateTime(2023, 12, 27, 16, 22, 3, 902.111)).Subject",
            "Event 30a",
            "PATCH:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v2/tables/Calendar/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAAB1rhBHAAA%253d",
            @"{""Subject"":""Event 30a"",""Start"":""2023-12-27T14:10:59.117Z"",""End"":""2023-12-27T15:22:03.902Z""}",
            "Response_O365Outlook_V2CalendarPatchItem.json")]

        [InlineData(
            @"Office365Outlook.V3CalendarPatchItem(""Calendar"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAAB1rhBGAAA="", ""Event 31a"", DateTime(2023, 12, 27, 15, 10, 59, 117.594), DateTime(2023, 12, 27, 16, 22, 3, 902.111)).Subject",
            "Event 31a",
            "PATCH:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v3/tables/Calendar/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAAB1rhBGAAA%253d",
            @"{""Subject"":""Event 31a"",""Start"":""2023-12-27T14:10:59.117Z"",""End"":""2023-12-27T15:22:03.902Z""}",
            "Response_O365Outlook_V3CalendarPatchItem.json")]

        [InlineData(
            @"Office365Outlook.V4CalendarPatchItem(""Calendar"", ""AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAAB1rhBMAAA="", ""Event 32a"", DateTime(2023, 12, 27, 15, 10, 59, 117.594), DateTime(2023, 12, 27, 16, 22, 3, 902.111),""(UTC+09:30) Darwin"").subject",
            "Event 32a",
            "PATCH:/apim/office365/3ea3b1e7f28d4c54a23a4dcbcae7de69/datasets/calendars/v4/tables/Calendar/items/AAMkADZiMmZiZGEwLTIyZDYtNDA3ZC1hZjJkLTljYjgxNjQ5YjFkNwBGAAAAAAC1gTSkmbm5QLpPwj9qarJqBwDr1A2S1MsmTIW9552ybeHbAAAAAAENAADr1A2S1MsmTIW9552ybeHbAAB1rhBMAAA%253d",
            @"{""subject"":""Event 32a"",""start"":""2023-12-27T15:10:59.117"",""end"":""2023-12-27T16:22:03.902"",""timeZone"":""(UTC\u002B09:30) Darwin""}",
            "Response_O365Outlook_V4CalendarPatchItem.json")]
        public async Task Office365Outlook_Functions(string expr, string expectedResult, string xUrls, string xBodies, string expectedFiles, string extra = null)
        {
            await RunConnectorTestAsync(false, expr, expectedResult, xUrls, xBodies, expectedFiles.Split("|").ToArray(), true, extra).ConfigureAwait(false);
        }

        // This function has 2 "id" parameters, one required and one optional
        // We verify that the optional parameter is properly renamed "id_1"
        [Fact]
        public void Office365Outlook_ContactPatchItemV2Parameters()
        {
            (LoggingTestServer testConnector, OpenApiDocument apiDoc, PowerFxConfig config, HttpClient httpClient, PowerPlatformConnectorClient client, ConnectorSettings connectorSettings, RuntimeConfig runtimeConfig) = GetElements(false);
            IReadOnlyList<ConnectorFunction> funcs = config.AddActionConnector(connectorSettings, apiDoc, new ConsoleLogger(_output));
            ConnectorFunction func = funcs.First(f => f.Name == "ContactPatchItemV2");

            string required = string.Join(", ", func.RequiredParameters.Select(rp => rp.Name));
            string optional = string.Join(", ", func.OptionalParameters.Select(rp => rp.Name));
            string hiddenRequired = string.Join(", ", func.HiddenRequiredParameters.Select(rp => rp.Name));

            Assert.Equal("folder, id, givenName, homePhones", required);
            Assert.Equal("id_1, parentFolderId, birthday, fileAs, displayName, initials, middleName, nickName, surname, title, generation, emailAddresses, imAddresses, jobTitle, companyName, department, officeLocation, profession, businessHomePage, assistantName, manager, businessPhones, mobilePhone, homeAddress, businessAddress, otherAddress, yomiCompanyName, yomiGivenName, yomiSurname, categories, changeKey, createdDateTime, lastModifiedDateTime", optional);
            Assert.Equal(string.Empty, hiddenRequired);
        }
    }
}
