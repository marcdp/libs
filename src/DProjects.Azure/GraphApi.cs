using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using DProjects.DataObjects;
using DProjects.DataTypes;
using DProjects.Db;
using DProjects.Fs;
using DProjects.Streams;
using DProjects.Utils;

namespace DProjects.Azure {


    public class GraphApi {


        //inner classes   
        public class Application {
            public string Id { get; set; } = "";
            public string AppId { get; set; } = "";
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string DisplayName { get; set; } = "";
        }
        public class Applications {
            public Application[] Value { get; set; } = new Application[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class ValueString {
            public string Value { get; set; } = "";
        }
        public class VerifiedDomain {
            public string Name { get; set; } = "";
            public string Capabilities { get; set; } = "";
            public bool IsDefault { get; set; } = false;
            public bool IsInitial { get; set; } = false;
            public string Type { get; set; } = "";
        }
        public class Organization {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public VerifiedDomain[] VerifiedDomains { get; set; } = new VerifiedDomain[] { };
        }
        public class Organizations {
            public Organization[] Value { get; set; } = new Organization[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class DriveQuota {
            public long Deleted { get; set; } = 0;
            public long Remaining { get; set; } = 0;
            public long Total { get; set; } = 0;
            public long Used { get; set; } = 0;
            public string State { get; set; } = "";
        }
        public class Drive {
            public string Id { get; set; } = "";
            public string DriveType { get; set; } = "";
            public string Name { get; set; } = "";
            public bool Deleted { get; set; } = false;
            public string Description { get; set; } = "";
            public DateTime CreatedDateTime { get; set; } = default;
            public DateTime LastModifiedDateTime { get; set; } = default;
            public DriveQuota? Quota { get; set; }
            public Entry ToEntry(string path) {
                return new Entry(PathUtils.Combine(path, Id), EntryType.Directory, CreatedDateTime, CreatedDateTime, 0, "", 0);
            }
        }
        public class DriveRoot {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public DateTime CreatedDateTime { get; set; } = default;
            public DateTime LastModifiedDateTime { get; set; } = default;
            public long Size { get; set; } = 0;
            public Entry ToEntry(string path) {
                return new Entry(PathUtils.Combine(path, "root"), EntryType.Directory, CreatedDateTime, CreatedDateTime, 0, "", 0);
            }
        }
        public class User {
            public string Id { get; set; } = "";
            public string Mail { get; set; } = "";
            public string MobilePhone { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string GivenName { get; set; } = "";
            public string MiddleName { get; set; } = "";
            public string Surname { get; set; } = "";
            public DateTime CreatedDateTime { get; set; } = default;
            public string UserPrincipalName { get; set; } = "";
            public string PreferredLanguage { get; set; } = "";
            public string ExternalSource { get; set; } = "";
            public string UserType { get; set; } = "";
            public string UsageLocation { get; set; } = "";
            public string Department { get; set; } = "";
            public string[] OtherMails { get; set; } = new string[] { };
            public string[] SignInNames { get; set; } = new string[] { };
            public virtual Entry ToEntry(string path) {
                return new Entry(PathUtils.Combine(path, UserPrincipalName), EntryType.Directory, CreatedDateTime, CreatedDateTime, 0, "", 0);
            }
        }
        public class Users {
            public User[] Value { get; set; } = new User[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class GroupSettingValue {
            public string Name { get; set; } = "";
            public string Value { get; set; } = "";
        }
        public class GroupSetting {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string TemplateId { get; set; } = "";
            public GroupSettingValue[] Values { get; set; } = new GroupSettingValue[] { };
        }
        public class GroupSettings {
            public GroupSetting[] Value { get; set; } = new GroupSetting[] { };
        }
        public class GroupEndpoint {
            public string Id { get; set; } = "";
            public string Capability { get; set; } = "";
            public string ProviderId { get; set; } = "";
            public string ProviderName { get; set; } = "";
            public string ProviderResourceId { get; set; } = "";
            public string Uri { get; set; } = "";
        }
        public class GroupEndpoints {
            public GroupEndpoint[] Value { get; set; } = new GroupEndpoint[] { };
        }
        public class Group {
            public string Id { get; set; } = ""; 
            public string Description { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Mail { get; set; } = "";
            public bool MailEnabled { get; set; } = false;
            public bool SecurityEnabled { get; set; } = false;
            public DateTime CreatedDateTime { get; set; } = default;
            public string[] GroupTypes { get; set; } = [];
            public string Visibility { get; set; } = "";
            public string MailNickname { get; set; } = "";
            public string PreferredLanguage { get; set; } = "";
            public string[] CreationOptions { get; set; } = [];
            public string[] ResourceBehaviorOptions { get; set; } = [];
            public string[] ResourceProvisioningOptions { get; set; } = [];
            public Entry ToEntry(string path) {
                return new Entry(PathUtils.Combine(path, Id), EntryType.Directory, CreatedDateTime, CreatedDateTime, 0, "", 0);
            }
        }
        public class GroupAdd {
            //public string Id { get; set; } = "";
            public string Description { get; set; } = "";
            public string DisplayName { get; set; } = "";
            //public string Mail { get; set; } = "";
            public bool MailEnabled { get; set; } = false;
            public bool SecurityEnabled { get; set; } = false;
            //public DateTime CreatedDateTime { get; set; } = default;
            public string[] GroupTypes { get; set; } = [];
            //public string Visibility { get; set; } = "";
            public string MailNickname { get; set; } = "";
            //public string PreferredLanguage { get; set; } = "";
            //public string[] ResourceBehaviorOptions { get; set; } = [];
            //public string[] ResourceProvisioningOptions { get; set; } = [];
            //public Entry ToEntry(string path) {
            //    return new Entry(PathUtils.Combine(path, Id), EntryType.Directory, CreatedDateTime, CreatedDateTime, 0, "", 0);
            //}
        }
        public class Groups {
            public Group[] Value { get; set; } = new Group[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class Site {
            public string Id { get; set; } = "";
            public string Description { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Name { get; set; } = "";
            public DateTime CreatedDateTime { get; set; } = default;
            public DateTime LastModifiedDateTime { get; set; } = default;
            public string WebUrl { get; set; } = "";
            public Entry ToEntry(string path) {
                return new Entry(PathUtils.Combine(path, Id), EntryType.Directory, CreatedDateTime, CreatedDateTime, 0, "", 0);
            }
        }
        public class Sites {
            public Site[] Value { get; set; } = new Site[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class DriveItemFolder {
            public long ChildCount { get; set; }
        }
        public class DriveItemFile {
            public string MimeType { get; set; } = "";
        }
        public class DriveItemRoot {
        }
        public class DriveItem {
            public string Id { get; set; } = "";
            public string ETag { get; set; } = "";
            public DateTime CreatedDateTime { get; set; } = default;
            public DateTime LastModifiedDateTime { get; set; } = default;
            public string Name { get; set; } = "";
            public long Size { get; set; }
            public DriveItemRoot? Root { get; set; }
            public DriveItemFolder? Folder { get; set; }
            public DriveItemFile? File { get; set; }
            public Entry ToEntry(string path) {
                return new Entry((Root != null ? "/" : PathUtils.Combine(path, Name)),
                    (File != null ? EntryType.File : EntryType.Directory),
                    CreatedDateTime.ToLocalTime(),
                    LastModifiedDateTime.ToLocalTime(),
                    (Root != null ? 0 : Size),
                    (File != null ? ETag : ""),
                    0
                    );
            }
        }
        public class DriveItems {
            public DriveItem[] Value { get; set; } = new DriveItem[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class Drives {
            public Drive[] Value { get; set; } = new Drive[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class Address {
            public string City { get; set; } = "";
            public string CountryOrRegion { get; set; } = "";
            public string PostalCode { get; set; } = "";
            public string State { get; set; } = "";
            public string Street { get; set; } = "";
        }
        public class EducationSchool {
            public string Id { get; set; } = "";
            public string ExternalId { get; set; } = "";
            public string ExternalPrincipalId { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Description { get; set; } = "";
            public string Status { get; set; } = "";
            public string PrincipalEmail { get; set; } = "";
            public string PrincipalName { get; set; } = "";
            public string SchoolNumber { get; set; } = "";
            public Address? Address { get; set; } = null;
            public string Phone { get; set; } = "";
            public Entry ToEntry(string path) {
                return new Entry(PathUtils.Combine(path, Id), EntryType.Directory, default, default, 0, "", 0);
            }
        }
        public class EducationSchools {
            public EducationSchool[] Value { get; set; } = new EducationSchool[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class EducationTerm {
            [System.Text.Json.Serialization.JsonPropertyName("@odata.type")]
            public string ODataType { get; set; } = "";
        }
        public class EducationClasse {
            [System.Text.Json.Serialization.JsonPropertyName("@odata.type")]
            public string ODataType { get; set; } = "#microsoft.graph.educationClass";
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Description { get; set; } = "";
            public Boolean MailEnabled { get; set; } = false;
            public string MailNickname { get; set; } = "";
            public string ClassCode { get; set; } = "";
            public string ExternalId { get; set; } = "";
            public string ExternalName { get; set; } = "";
            public string ExternalSource { get; set; } = "";
            public EducationTerm? Term { get; set; } 
            public Entry ToEntry(string path) {
                return new Entry(PathUtils.Combine(path, Id), EntryType.Directory, default, default, 0, "", 0);
            }
        }
        public class EducationClasses {
            public EducationClasse[] Value { get; set; } = new EducationClasse[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class EducationUser {
            public string Id { get; set; } = "";
            public string Mail { get; set; } = "";
            public string MobilePhone { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string GivenName { get; set; } = "";
            public string MiddleName { get; set; } = "";
            public string Surname { get; set; } = "";
            public string UsageLocation { get; set; } = "";
            public string UserType { get; set; } = "";
            public string ExternalSource { get; set; } = "";
            public string PreferredLanguage { get; set; } = "";
            public string UserPrincipalName { get; set; } = "";

            public string PrimaryRole { get; set; } = "";
            public EducationTeacher? Teacher { get; set; }
            public EducationStudent? Student { get; set; }
            public Entry ToEntry(string path) {
                return new Entry(PathUtils.Combine(path, Id), EntryType.Directory, default, default, 0, "", 0);
            }
        }
        public class EducationStudent : EducationUser {
            public string ExternalId { get; set; } = "";
            public string StudentNumber { get; set; } = "";
        }
        public class EducationTeacher : EducationUser {
            public string ExternalId { get; set; } = "";
            public string TeacherNumber { get; set; } = "";
        }
        public class EducationUsers {
            public EducationUser[] Value { get; set; } = new EducationUser[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class Invitation {
            public string Id { get; set; } = "";
            public string InviteRedeemUrl { get; set; } = "";
            public string InviteUserType { get; set; } = "";
            public string InviteUserEmailAddress { get; set; } = "";
            public string InviteRedirectUrl { get; set; } = "";
            public string Status { get; set; } = "";
            public User? InvitedUser { get; set; } = null;
        }
        public class TeamInfo {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Description { get; set; } = "";
            public string InternalId { get; set; } = "";
            public string Specialization { get; set; } = "";
            public string Visibility { get; set; } = "";
            public string WebUrl { get; set; } = "";
            public bool IsArchived { get; set; } = false;
            public bool IsMembershipLimitedToOwners { get; set; } = false;
        }
        public class TeamChannel {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Description { get; set; } = "";
            public bool? IsFavoriteByDefault { get; set; } = false;
            public string Email { get; set; } = "";
            public string WebUrl { get; set; } = "";
            public string MembershipType { get; set; } = "";
        }
        public class TeamChannels {
            public TeamChannel[] Value { get; set; } = new TeamChannel[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class TeamTabConfiguration {
            public string EntityId { get; set; } = "";
            public string ContentUrl { get; set; } = "";
            public string WebsiteUrl { get; set; } = "";
            public string RemoveUrl { get; set; } = "";
        }
        public class TeamApp {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string DistributionMethod { get; set; } = "";
        }
        public class TeamTab {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string WebUrl { get; set; } = "";
            public TeamTabConfiguration? Configuration { get; set; }
            public TeamApp? TemsApp { get; set; }
        }
        public class TeamTabs {
            public TeamTab[] Value { get; set; } = new TeamTab[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class TeamMembers {
            public TeamMember[] Value { get; set; } = new TeamMember[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class TeamMember {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string[] Roles { get; set; } = new string[] { };
            public string UserId { get; set; } = "";
            public string Email { get; set; } = "";
        }
        public class ConversationMember {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string[] Roles { get; set; } = new string[] { };
            public string UserId { get; set; } = "";
            public string Email { get; set; } = "";
        }
        public class ConversationMembers {
            public ConversationMember[] Value { get; set; } = new ConversationMember[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class ServicePLanInfo {
            public string ServicePlanId { get; set; } = "";
            public string servicePlanName { get; set; } = "";
            public string ProvisioningStatus { get; set; } = "";
            public string AppliesTo { get; set; } = "";
        }
        public class LicenseDetails {
            public string Id { get; set; } = "";
            public ServicePLanInfo[] ServicePlans { get; set; } = new ServicePLanInfo[] { };
            public string SkuId { get; set; } = "";
            public string SkuPartNumber { get; set; } = "";
        }
        public class LicenseDetailsList {
            public LicenseDetails[] Value { get; set; } = new LicenseDetails[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class UserAssignedLicense {
            public string[] DisabledPLans { get; set; } = new string[] { };
            public string SkuId { get; set; } = "";
        }
        public class UserWithLicense {
            public string Id { get; set; } = "";
            public string UserPrincipalName { get; set; } = "";
            public UserAssignedLicense[] AssignedLicenses { get; set; } = new UserAssignedLicense[] { };
        }
        public class UserWithLicenses {
            public UserWithLicense[] Value { get; set; } = new UserWithLicense[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class Subscription {
            public string Id { get; set; } = "";
            public string Resource { get; set; } = "";
            public string ApplicationId { get; set; } = "";
            public string ChangeType { get; set; } = "";
            public string ClientState { get; set; } = "";
            public string NotificationUrl { get; set; } = "";
            public string ExpirationDateTime { get; set; } = "";
            public string CreatorId { get; set; } = "";
            public string LatestSupportedTlsVersion { get; set; } = "";
            public string NotificationContentType { get; set; } = "";
        }
        public class ResourceData {
            public string Id { get; set; } = "";
            [System.Text.Json.Serialization.JsonPropertyName("@odata.id")]
            public string ODataId { get; set; } = "";
            [System.Text.Json.Serialization.JsonPropertyName("@odata.type")]
            public string ODataType { get; set; } = "";
            [System.Text.Json.Serialization.JsonPropertyName("@odata.etag")]
            public string ODataEtag { get; set; } = "";
        }
        public class EncryptedContent {
            public string Data { get; set; } = "";
            public string DataSignature { get; set; } = "";
            public string DataKey { get; set; } = "";
            public string EncryptionCertificateId { get; set; } = "";
            public string EncryptionCertificateThumbprint { get; set; } = "";
        }
        public class ChangeNotification {
            public string Id { get; set; } = "";
            public string ChangeType { get; set; } = "";
            public string ClientState { get; set; } = "";
            public string SubscriptionId { get; set; } = "";
            public string Resource { get; set; } = "";
            public string TenantId { get; set; } = "";
            public ResourceData ResourceData { get; set; } = new ResourceData();
            public EncryptedContent? EncryptedContent { get; set; } 
        }
        public class Identity {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
        }
        public class IdentitySet {
            public Identity User { get; set; } = new Identity() { };
        }
        public class CallRecord {
            public string Id { get; set; } = "";
            public string Type { get; set; } = "";
            public int Version { get; set; } = 0;
            public string[] Modalities { get; set; } = new string[]{};
            public string LastModifiedDateTime { get; set; } = "";
            public string StartDateTime { get; set; } = "";
            public string EndDateTime { get; set; } = "";
            public string JoinWebUrl { get; set; } = "";
            public IdentitySet Organizer { get; set; } = new IdentitySet();
            public IdentitySet[] Participants { get; set; } = new IdentitySet[] { };
        }
        public class EmailAddress {
            public string Address{ get; set; } = "";
            public string Name { get; set; } = "";            
        }
        public class Calendar {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public EmailAddress Owner { get; set; } = new EmailAddress();
            public string HexColor { get; set; } = "";
            public bool IsDefaultCalendar { get; set; }
            public bool IsRemovable { get; set; }
            public bool IsTallyingResponses { get; set; }
            public bool CanViewPrivateItems { get; set; }
            public bool CanShare { get; set; }
            public bool CanEdit { get; set; }
        }
        public class ItemBody {
            public string Content { get; set; } = "";
            public string ContentType { get; set; } = "";
        }
        public class Event {
            public string Id { get; set; } = "";
            public string ICalUId { get; set; } = "";            
            public string CreatedDateTime { get; set; } = "";
            public string LastModifiedDateTime { get; set; } = "";
            public string ChangeKey { get; set; } = "";
            public string Subject { get; set; } = "";
            public bool hasAttachments { get; set; }
            public bool IsOnlineMeeting { get; set; }
            public bool IsDraft { get; set; }
            public bool IsOrganizer { get; set; }
            public bool IsAllDay { get; set; }
            public bool IsCancelled { get; set; }
            public Location? Location { get; set; }
            public Location[] Locations { get; set; } = new Location[] { };
            public OnlineMeetingInfo? OnlineMeeting { get; set; }
            public Recipient Organizer { get; set; } = new Recipient();
            public Ateende[] Attendees { get; set; } = new Ateende[] { };
            public string[] Categories { get; set; } = [];
            public ItemBody Body { get; set; } = new ItemBody();
            public string BodyPreview { get; set; } = "";
            public string Sensitivity { get; set; } = "";
            public string Type { get; set; } = "";
            public string WebLink { get; set; } = "";
            public DateTimeTimeZone Start { get; set; } = new DateTimeTimeZone();
            public DateTimeTimeZone End { get; set; } = new DateTimeTimeZone();
        }
        public class Events {
            public Event[] Value { get; set; } = new Event[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class Location {
            public PhysicalAddress Address { get; set; } = new PhysicalAddress();
            public string DisplayName { get; set; } = "";
            public string LocationEmailAddress { get; set; } = "";
            public string LocationUri { get; set; } = "";
            public string LocationType { get; set; } = "";
            public string UniqueId { get; set; } = "";
            public string UniqueIdType { get; set; } = "";
        }
        public class Phone {
            public string Number { get; set; } = "";
            public string Type { get; set; } = "";
        }
        public class DateTimeTimeZone {
            public string DateTime { get; set; } = "";
            public string TimeZone { get; set; } = "";
        }
        public class PhysicalAddress {
            public string City { get; set; } = "";
            public string CountryOrRegion { get; set; } = "";
            public string PostalCode { get; set; } = "";
            public string State { get; set; } = "";
            public string Street { get; set; } = "";
        }
        public class OnlineMeetingInfo {
            public string ConferenceId { get; set; } = "";
            public string JoinUrl { get; set; } = "";
            public Phone[] Phones { get; set; } = new Phone[] { };
        }
        public class OnlineMeeting {
            public string Id { get; set; } = "";
            public string Subject { get; set; } = "";
            public string VideoTeleconferenceId { get; set; } = "";
            public string StartDateTime { get; set; } = "";
            public string EndDateTime { get; set; } = "";
        }
        public class OnlineMeetings {
            public OnlineMeeting[] Value { get; set; } = new OnlineMeeting[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class Recipient  {
            public EmailAddress EmailAddress { get; set; } = new EmailAddress();
        }
        public class AteendeStatus {
            public string Response { get; set; } = "";
            public DateTime Time { get; set; } = default;
        }
        public class Ateende {
            public string Type { get; set; } = "";
            public AteendeStatus Status { get; set; } = new AteendeStatus();
            public EmailAddress EmailAddress { get; set; } = new EmailAddress();
        }
        public class Chat {
            public string Id { get; set; } = "";
            public string Topic { get; set; } = "";
            public string CreatedDateTime { get; set; } = "";
            public string LastUpdateddDateTime { get; set; } = "";
            public string ChatType { get; set; } = "";
            public ConversationMember[] Members = new ConversationMember[] { };
        }
        public class ProfilePhoto {
            [System.Text.Json.Serialization.JsonPropertyName("@odata.mediaContentType")]
            public string MediaContentType { get; set; } = "";
            [System.Text.Json.Serialization.JsonPropertyName("@odata.mediaEtag")]
            public string MediaEtag { get; set; } = "";
            public string Id { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
        }
        public class Contact {
            public string Id { get; set; } = "";

            public string DisplayName { get; set; } = "";
            public string GivenName { get; set; } = "";
            public string Surname { get; set; } = "";
            public string MiddleName { get; set; } = "";
            public string Title { get; set; } = "";
            public string NickName { get; set; } = "";
            public string CompanyName { get; set; } = "";
            public string JobTitle { get; set; } = "";
            public string AssistantName { get; set; } = "";

            public string PersonalNotes { get; set; } = "";
            public string[] Categories { get; set; } = [];

            public string MobilePhone { get; set; } = "";
            public string[] HomePhones { get; set; } = [];
            public string[] BusinessPhones { get; set; } = [];
            public EmailAddress[] EmailAddresses { get; set; } = [];

            public string ParentFolderId { get; set; } = "";

            public string CreatedDateTime { get; set; } = "";
            public string LastModifiedDateTime { get; set; } = "";
        }
        public class Contacts {
            public Contact[] Value { get; set; } = new Contact[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public class ContactFolder {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string ParentFolderId { get; set; } = "";
            public string WellKnownName { get; set; } = "";
        }
        public class ContactFolders {
            public ContactFolder[] Value { get; set; } = new ContactFolder[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        public  class ContactFolderWithContacts {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string ParentFolderId { get; set; } = "";
            public Contact[] Contacts { get; set; } = new Contact[] { };
        }
        public class ContactFoldersWithContacts {
            public ContactFolderWithContacts[] Value { get; set; } = new ContactFolderWithContacts[] { };
            [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
            public string ODataNextLink { get; set; } = "";
        }
        


        //variables
        private AuthenticationProvider mAuthenticationProvider;
        private HttpClient mHttpClient;


        //constructor
        public GraphApi(Uri url) {
            var httpClientHandler = new HttpClientHandler();
            mHttpClient = new HttpClient(httpClientHandler);
            mHttpClient.BaseAddress = new Uri("https://graph.microsoft.com/v1.0");
            mHttpClient.Timeout = TimeSpan.FromDays(1);
            var appScopes = new string[] { "https://graph.microsoft.com/.default" };
            var clientId = UrlUtils.UrlDecode(url.UserInfo.Split(':')[0]);
            var clientSecret = UrlUtils.UrlDecode(url.UserInfo.Split(':')[1]);
            var tenantId = url.AbsolutePath.Substring(1);
            mAuthenticationProvider = new AuthenticationProvider(clientId, clientSecret, appScopes, tenantId);
        }
        public GraphApi(string clientId, string clientSecret, string tenantId) {
            var httpClientHandler = new HttpClientHandler();
            mHttpClient = new HttpClient(httpClientHandler);
            mHttpClient.BaseAddress = new Uri("https://graph.microsoft.com/v1.0");
            mHttpClient.Timeout = TimeSpan.FromDays(1);
            var appScopes = new string[] { "https://graph.microsoft.com/.default" };
            mAuthenticationProvider = new AuthenticationProvider(clientId, clientSecret, appScopes, tenantId);
        }
        public GraphApi(AuthenticationProvider authenticationProvider, HttpClient httpClient) {
            mAuthenticationProvider = authenticationProvider;
            mHttpClient = httpClient;
        }


        //properties
        public AuthenticationProvider AuthenticationProvider => mAuthenticationProvider;


        //methods
        #region "organization"
        public async Task<Organization?> GetOrganizationAsync() {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/organization", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) { 
                    throw new Exception("Unable to get organization: " + httpResponse.StatusCode + " (" + json + ")");
                }
                var organizations = new DProjects.Text.Json.JsonDeserializer().Deserialize<Organizations>(json);
                if (organizations.Value.Length == 0) return null;
                return organizations.Value[0];
            }
        }
        #endregion

        #region
        public async Task<Application[]> GetApplicationsAsync(string? pattern) {
            var nextQuery = "";
            var result = new List<Application>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/applications", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get applications: " + httpResponse.StatusCode + " (" + json + ")");
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<Applications>(json);
                    foreach (var application in list.Value) {
                        if (pattern != null && !StringUtils.Like(application.Name, pattern)) {
                            continue;
                        }
                        result.Add(application);
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        #endregion

        #region "Directory"
        public async Task<bool> ExistsDeletedDirectoryItem(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/directory/deletedItems/" + id, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return false;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get item: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return true;
            }
        }
        public async Task RestoreDeletedDirectoryItem(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/directory/deletedItems/" + id + "/restore", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to restore directory item: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }

        #endregion


        #region "Users"
        public async Task<User?> GetUserAsync(string name) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + name, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get item: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<User>(json);
            }
        }
        public async Task<Group[]> GetUserMemberOfAsync(string id) {
            var result = new List<Group>();
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + id + "/memberOf", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return [];
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get item: " + httpResponse.StatusCode + " (" + json + ")");
                }
                var listGroups = new DProjects.Text.Json.JsonDeserializer().Deserialize<Groups>(json);
                foreach (var group in listGroups.Value) {
                    result.Add(group);
                }
            }
            return result.ToArray();
        }
        public async Task<Group[]> GetUserTransitiveMemberOfAsync(string id) {
            var result = new List<Group>();
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + id + "/transitiveMemberOf", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return [];
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get item: " + httpResponse.StatusCode + " (" + json + ")");
                }
                var listGroups = new DProjects.Text.Json.JsonDeserializer().Deserialize<Groups>(json);
                foreach (var group in listGroups.Value) {
                    result.Add(group);
                }
            }
            return result.ToArray();
        }
        public async Task<User[]> GetUsersAsync(string? pattern) {
            var nextQuery = "$select=id,mail,mobilePhone,displayName,givenName,middleName,surname,userPrincipalName,preferredLanguage,externalSource,userType,usageLocation,otherMails,signInNames,department&$orderBy=UserPrincipalName";
            var result = new List<User>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get users: " + httpResponse.StatusCode + " (" + json + ")");
                    var listUsers = new DProjects.Text.Json.JsonDeserializer().Deserialize<Users>(json);
                    foreach (var user in listUsers.Value) {
                        if (pattern != null && !StringUtils.Like(user.UserPrincipalName, pattern)) {
                            continue;
                        }
                        result.Add(user);
                    }
                    var nextLink = listUsers.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        //public User[] GetUsersWithAlternateEmails() {
        //    var nextQuery = "$select=id,mail,mobilePhone,displayName,givenName,middleName,surname,userPrincipalName,preferredLanguage,externalSource,userType,usageLocation,otherMails,signInNames,department"; // &$filter=otherMails/any";
        //    var result = new List<User>();
        //    do {
        //        var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users", nextQuery);
        //        using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
        //            var json = await httpResponse.Content.ReadAsStringAsync();
        //            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get users: " + httpResponse.StatusCode + " (" + json + ")");
        //            var listUsers = new DProjects.Text.Json.JsonDeserializer().Deserialize<Users>(json);
        //            foreach (var user in listUsers.Value) {
        //                var valid = false;
        //                foreach (var mail in user.OtherMails) {
        //                    if (mail != user.Mail) {
        //                        valid = true;
        //                    }
        //                }
        //                if (valid) result.Add(user);
        //            }
        //            var nextLink = listUsers.ODataNextLink;
        //            if (nextLink == "") {
        //                nextQuery = "";
        //            } else {
        //                nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
        //            }
        //        }
        //    } while (nextQuery.Length > 0);
        //    return result.ToArray();
        //}
        public async Task<User> CreateUserAsync(string userPrincipalName, string mail, string mailNickName, string givenName, string surName, string displayName, bool accountEnabled, string password, string usageLocation) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/users", "");
            var dict = new Dictionary<string, object>();
            dict["userPrincipalName"] = userPrincipalName;
            dict["mail"] = mail;
            dict["mailNickName"] = mailNickName;
            dict["givenName"] = givenName;
            dict["surName"] = surName;
            dict["displayName"] = displayName;
            dict["usageLocation"] = usageLocation;
            dict["officeLocation"] = usageLocation;
            dict["accountEnabled"] = accountEnabled;
            var passwordProfile = new Dictionary<string, object>();
            passwordProfile["forceChangePasswordNextSignIn"] = true;
            passwordProfile["password"] = password;
            dict["passwordProfile"] = passwordProfile;
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                } else {
                    throw new Exception("Unable to create user: " + httpResponse.StatusCode + ", " + json);
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<User>(json);
            }
        }
        public async Task UpdateUserAsync(string userId, Dictionary<string,object> settings) {
            var httpRequest = await CreateHttpRequestAsync(new HttpMethod("PATCH"), "/users/" + userId, "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(settings));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else {
                    throw new Exception("Unable to update user: " + httpResponse.StatusCode + ", " + json);
                }
            }
        }
        public async Task<Invitation> CreateUserInvitationAsync(string email, string full_name, bool sendInvitationMessage, string invitedUserMessageInfoLanguage, string userType, string inviteRedirectUrl) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/invitations", "");
            var dict = new Dictionary<string, object>();
            dict["invitedUserEmailAddress"] = email;
            dict["invitedUserDisplayName"] = full_name;
            dict["inviteRedirectUrl"] = inviteRedirectUrl;
            dict["sendInvitationMessage"] = sendInvitationMessage;
            if (sendInvitationMessage) {
                var invitedUserMessageInfoDict = new Dictionary<string, object>();
                invitedUserMessageInfoDict["messageLanguage"] = invitedUserMessageInfoLanguage;
                dict["invitedUserMessageInfo"] = invitedUserMessageInfoDict;
            }
            dict["invitedUserType"] = userType;
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                } else {
                    throw new Exception("Unable to create user invitation: " + httpResponse.StatusCode + ", " + json);
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<Invitation>(json);
            }
        }
        public async Task<string[]> GetUserIdsWithLicenseAsync(string skuId) {
            var nextQuery = "$select=id,userPrincipalName,assignedLicenses";
            var result = new List<string>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get users: " + httpResponse.StatusCode + " (" + json + ")");
                    var listUsers = new DProjects.Text.Json.JsonDeserializer().Deserialize<UserWithLicenses>(json);
                    foreach (var user in listUsers.Value) {
                        var licenseFound = false;
                        foreach (var assignedLicense in user.AssignedLicenses) {
                            if (assignedLicense.SkuId == skuId) {
                                licenseFound = true;
                            }
                        }
                        if (licenseFound) result.Add(user.Id);
                    }
                    var nextLink = listUsers.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<LicenseDetails[]> GetUserLicenseDetailsAsync(string id) {
            var nextQuery = "";
            var result = new List<LicenseDetails>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + id + "/licenseDetails", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get user license details: " + httpResponse.StatusCode + " (" + json + ")");
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<LicenseDetailsList>(json);
                    foreach (var item in list.Value) {
                        result.Add(item);
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task AssignUserLicensesAsync(string userId, string[] addLicenses) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/users/" + userId  + "/assignLicense", "");
            var dict = new Dictionary<string, object>();
            var addLicensesList= new List<Dictionary<string, object>>();
            foreach (var addLicense in addLicenses) {
                var a = new Dictionary<string, object>();
                a["skuId"]= addLicense;
                addLicensesList.Add(a);
            }
            dict["addLicenses"] = addLicensesList.ToArray();
            dict["removeLicenses"] = new string[] { };
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK ) {
                } else {
                    throw new Exception("Unable to assign a license: " + httpResponse.StatusCode + ", " + json);
                }
            }
        }
        public async Task<ProfilePhoto?> GetUserProfilePhotoAsync(string id) {
            var nextQuery = "";
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + id + "/photo", nextQuery);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    //beta (fallback)
                    var httpRequestBeta = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + id + "/photo", nextQuery, true);
                    using (var httpResponseBeta = mHttpClient.SendAsync(httpRequestBeta).Result) {
                        json = httpResponseBeta.Content.ReadAsStringAsync().Result;
                        if (httpResponseBeta.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                        if (httpResponseBeta.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get user profile photo: " + httpResponseBeta.StatusCode + " (" + json + ")");
                        return new DProjects.Text.Json.JsonDeserializer().Deserialize<ProfilePhoto>(json);
                    }
                }
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get user profile photo: " + httpResponse.StatusCode + " (" + json + ")");
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<ProfilePhoto>(json);
            }
        }
        public async Task UpdateUserProfilePhotoAsync(string id, string filename) {
            var nextQuery = "";
            using (var filestream = new System.IO.FileStream(filename, FileMode.Open)) {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Put, "/users/" + id + "/photo/$value", nextQuery);
                httpRequest.Content = new StreamContent(filestream);
                httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.GetMimeType(filename));
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                        //beta (fallback)
                        using (var filestreamBeta = new System.IO.FileStream(filename, FileMode.Open)) {
                            var httpRequestBeta = await CreateHttpRequestAsync(HttpMethod.Put, "/users/" + id + "/photo/$value", nextQuery, true);
                            httpRequestBeta.Content = new StreamContent(filestreamBeta);
                            httpRequestBeta.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.GetMimeType(filename));
                            using (var httpResponseBeta = mHttpClient.SendAsync(httpRequestBeta).Result) {
                                json = httpResponseBeta.Content.ReadAsStringAsync().Result;
                                if (httpResponseBeta.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to update user profile photo: " + httpResponse.StatusCode + " (" + json + ")");
                                return;
                            }
                        }
                    }
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to update user profile photo: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task<string?> GetUserProfilePhotoStreamAsync(string id, string tempDirectory) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + id + "/photo/$value", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    //beta (fallback)
                    var httpRequestBeta = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + id + "/photo/$value", "", true);
                    using (var httpResponseBeta = mHttpClient.SendAsync(httpRequestBeta).Result) {
                        if (httpResponseBeta.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                        if (httpResponseBeta.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to download user profile photo: " + httpResponse.StatusCode);
                        var contentType = httpResponseBeta.Content.Headers.ContentType.MediaType;
                        using (var stream = httpResponseBeta.Content.ReadAsStreamAsync().Result) {
                            var tempFileName = System.IO.Path.Combine(tempDirectory, System.Guid.NewGuid().ToString() + MimeTypeUtils.GetExtensions(contentType)[0]);
                            FileUtils.WriteFile(tempFileName, stream);
                            return tempFileName;
                        }
                    }
                } else {
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to download user profile photo: " + httpResponse.StatusCode);
                    var contentType = httpResponse.Content.Headers.ContentType.MediaType;
                    using (var stream = httpResponse.Content.ReadAsStreamAsync().Result) {
                        var tempFileName = System.IO.Path.Combine(tempDirectory, System.Guid.NewGuid().ToString() + MimeTypeUtils.GetExtensions(contentType)[0]);
                        FileUtils.WriteFile(tempFileName, stream);
                        return tempFileName;
                    }
                }
            }
        }

        #endregion

        #region "Groups"
        public async Task<Group?> GetGroupAsync(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + id, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get group: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<Group>(json);
            }
        }
        public async Task<Group> CreateGroupAsync(GroupAdd groupAdd) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/groups", "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(groupAdd));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                } else {
                    throw new Exception("Unable to create group: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<Group>(json);
            }
        }
        public async Task AddGroupMemberAsync(string groupId, string userId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/groups/" + groupId + "/members/$ref", "");
            var vo = new VO();
            vo["@odata.id"] = mHttpClient.BaseAddress + "/directoryObjects/" + userId;
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(vo));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to add member to a group: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task RemoveGroupMemberAsync(string groupId, string userId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Delete, "/groups/" + groupId + "/members/" + userId + "/$ref", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to remove member from group: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task<GroupSettings> GetGroupSettingsAsync() {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groupSettings", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get tenant-wide group settings: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<GroupSettings>(json);
            }
        }
        public async Task<GroupSettings> GetGroupSettingsAsync(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + id + "/settings", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get group settings: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<GroupSettings>(json);
            }
        }
        //public async Task<GroupEndpoints> GetGroupEndPointsAsync(string id) {
        //    var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + id + "/endpoints", "", false);
        //    using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
        //        var json = await httpResponse.Content.ReadAsStringAsync();
        //        if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
        //            throw new Exception("Unable to get group endpoints: " + httpResponse.StatusCode + " (" + json + ")");
        //        }
        //        return new DProjects.Text.Json.JsonDeserializer().Deserialize<GroupEndpoints>(json);
        //    }
        //}
        public async Task<GroupSetting> CreateGroupSettingAsync(string groupId, GroupSetting groupSetting) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/groups/" + groupId + "/settings", "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(groupSetting));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                } else {
                    throw new Exception("Unable to create group setting: " + httpResponse.StatusCode + ", " + json);
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<GroupSetting>(json);
            }
        }
        public async Task<Group[]> GetGroupsAsync(string? pattern) {
            var nextQuery = "";
            //var nextQuery = "$expand=teamsApp";
            var result = new List<Group>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get groups: " + httpResponse.StatusCode + " (" + json + ")");
                    var listGroups = new DProjects.Text.Json.JsonDeserializer().Deserialize<Groups>(json);
                    foreach (var group in listGroups.Value) {
                        if (pattern != null && !StringUtils.Like(group.Id, pattern)) {
                            continue;
                        }
                        result.Add(group);
                    }
                    var nextLink = listGroups.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        //public async Task<Group[]> GetGroupsDistributionListsAsync(string? pattern) {
        //    var nextQuery = ""; // "$filter=NOT groupTypes/any(c:c eq 'Unified') and mailEnabled eq true and securityEnabled eq false&$count=true";
        //    var result = new List<Group>();
        //    do {
        //        var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups", nextQuery);
        //        using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
        //            var json = await httpResponse.Content.ReadAsStringAsync();
        //            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get groups: " + httpResponse.StatusCode + " (" + json + ")");
        //            var listGroups = new DProjects.Text.Json.JsonDeserializer().Deserialize<Groups>(json);
        //            foreach (var group in listGroups.Value) {
        //                if (pattern != null && !StringUtils.Like(group.Id, pattern)) {
        //                    continue;
        //                }
        //                result.Add(group);
        //            }
        //            var nextLink = listGroups.ODataNextLink;
        //            if (nextLink == "") {
        //                nextQuery = "";
        //            } else {
        //                nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
        //            }
        //        }
        //    } while (nextQuery.Length > 0);
        //    return result.ToArray();
        //}
        public async Task RemoveGroupAsync(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Delete, "/groups/" + id, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to remove group: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task<string?> GetGroupWebUrlAsync(string groupId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + groupId + "/drive/root/webUrl", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else {
                    throw new Exception("Unable to get group web Url: " + httpResponse.StatusCode + ", " + json);
                }
                var result = new DProjects.Text.Json.JsonDeserializer().Deserialize<ValueString>(json);
                return result.Value;
            }
        }
        public async Task<string?> GetGroupCreatedOnBehalfOfAsync(string groupId) {
            var nextQuery = "$select=displayName";
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + groupId + "/createdOnBehalfOf", nextQuery);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else {
                    throw new Exception("Unable to get group web CreatedOnBehalfOf: " + httpResponse.StatusCode + ", " + json);
                }
                var dictionary = new DProjects.Text.Json.JsonDeserializer().Deserialize<IDictionary<string, object?>>(json);
                if (dictionary.TryGetValue("displayName", out object? result)) {
                    return result!.ToString();
                }
                return null;
            }
        }
        public async Task<string?> GetGroupCreatedByAppIdAsync(string groupId) {
            var nextQuery = "$select=createdByAppId";
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + groupId, nextQuery, true);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else {
                    throw new Exception("Unable to get group web CreatedByAppId: " + httpResponse.StatusCode + ", " + json);
                }
                var dictionary = new DProjects.Text.Json.JsonDeserializer().Deserialize<IDictionary<string, object?>>(json);
                if (dictionary.TryGetValue("createdByAppId", out object? result)) {
                    if (result != null) return result!.ToString();
                }
                return null;
            }
        }
        
        public async Task<Drive[]> GetGroupDrivesAsync(string groupId) {
            var nextQuery = "";
            var result = new List<Drive>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + groupId + "/drives", "");
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                    } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                        return new Drive[] { };
                    } else { 
                        throw new Exception("Unable to get group drives: " + httpResponse.StatusCode + " (" + json + ")");
                    }
                    var listDrives = new DProjects.Text.Json.JsonDeserializer().Deserialize<Drives>(json);
                    foreach (var drive in listDrives.Value) {
                        result.Add(drive);
                    }
                    var nextLink = listDrives.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<TeamInfo?> GetGroupTeamAsync(string groupId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + groupId + "/team", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else {
                    throw new Exception("Unable to get group team: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<TeamInfo>(json);
            }
        }
        public async Task<Site?> GetGroupSitesRootAsync(string groupId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + groupId + "/sites/root", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else {
                    throw new Exception("Unable to get group site root: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<Site>(json);
            }
        }
        //public async Task<Site[]> GetGroupSitesAsync(string groupId) {
        //    var nextQuery = "";
        //    var result = new List<Site>();
        //    do {
        //        var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + groupId + "/sites", "");
        //        using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
        //            var json = await httpResponse.Content.ReadAsStringAsync();
        //            if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
        //            } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
        //                return new Site[] { };
        //            } else {
        //                throw new Exception("Unable to get group drives: " + httpResponse.StatusCode + " (" + json + ")");
        //            }
        //            var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<Sites>(json);
        //            foreach (var site in list.Value) {
        //                result.Add(site);
        //            }
        //            var nextLink = list.ODataNextLink;
        //            if (nextLink == "") {
        //                nextQuery = "";
        //            } else {
        //                nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
        //            }
        //        }
        //    } while (nextQuery.Length > 0);
        //    return result.ToArray();
        //}
        public async Task<User[]> GetGroupOwnersAsync(string groupId) {
            var nextQuery = "$select=id,mail,mobilePhone,displayName,givenName,middleName,surname,usageLocation,userType,externalSource,preferredLanguage,userPrincipalName";
            var result = new List<User>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + groupId + "/owners", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get class owners: " + httpResponse.StatusCode);
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<Users>(json);
                    foreach (var item in list.Value) {
                        result.Add(item);
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<User[]> GetGroupMembersAsync(string groupId) {
            var nextQuery = "$select=id,mail,mobilePhone,displayName,givenName,middleName,surname,usageLocation,userType,externalSource,preferredLanguage,userPrincipalName";
            var result = new List<User>();
            do { 
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + groupId + "/members", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get class members: " + httpResponse.StatusCode);
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<Users>(json);
                    foreach (var item in list.Value) {
                        result.Add(item);
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        #endregion

        #region "Sites"
        public async Task<Site?> GetSiteAsync(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/sites/" + id, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get site: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<Site>(json);
            }
        }
        public async Task<Site[]> GetSitesAsync(string? pattern) {
            var nextQuery = ""; // "$orderBy=Id";
            var result = new List<Site>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/sites", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get sites: " + httpResponse.StatusCode + " (" + json + ")");
                    var listSites = new DProjects.Text.Json.JsonDeserializer().Deserialize<Sites>(json);
                    foreach (var site in listSites.Value) {
                        if (pattern != null && !StringUtils.Like(site.Id, pattern)) {
                            continue;
                        }
                        result.Add(site);
                    }
                    var nextLink = listSites.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);            
            return result.ToArray();
        }

        #endregion

        #region "Drives"
        public async Task<Drive?> GetDriveAsync(string id, string? prefix=null) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, (prefix != null ? prefix : "") + "/drives/" + id, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get item: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<Drive>(json);
            }
        }
        public async Task<Drive[]> GetDrivesAsync(string? pattern, string? prefix = null) {
            var nextQuery = "$orderBy=Id";
            var result = new List<Drive>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, (prefix != null ? prefix : "") + "/drives", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get drives: " + httpResponse.StatusCode + " (" + json + ")");
                    var listDrives = new DProjects.Text.Json.JsonDeserializer().Deserialize<Drives>(json);
                    foreach (var drive in listDrives.Value) {
                        if (pattern != null && !StringUtils.Like(drive.Id, pattern)) {
                            continue;
                        }
                        result.Add(drive);
                    }
                    var nextLink = listDrives.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        #endregion

        #region "DriveItems"
        public async Task<DriveItem?> GetDriveItemAsync(string path, string subPath) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, path + (subPath == "/" ? "" : ":" + subPath + ""), "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get entries: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<DriveItem>(json);
            }
        }
        public async Task<DriveItem[]> GetDriveItemsAsync(string path, string subPath) {
            var result = new List<DriveItem>();
            var nextQuery = "$orderBy=Name";
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, path + (subPath == "/" ? "/children" : ":" + subPath + ":/children"), nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                        throw new Exception("Unable to get entries: " + httpResponse.StatusCode + " (" + json + ")");
                    }
                    var driveItems = new DProjects.Text.Json.JsonDeserializer().Deserialize<DriveItems>(json);
                    foreach (var driveItem in driveItems.Value) {
                        result.Add(driveItem);
                    }
                    var nextLink = driveItems.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<Stream> GetDriveItemContentAsync(string path, string subPath, long offset = 0, long length = -1) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, path + (subPath == "/" ? "/content" : ":" + subPath + ":/content"), "");
            if (offset == 0 && length == -1) {
            } else {
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                if (length == -1 || length == long.MaxValue) {
                    httpRequest.Headers.Range = new RangeHeaderValue(offset, null);
                } else {
                    httpRequest.Headers.Range = new RangeHeaderValue(offset, offset + length - 1);
                }
            }
            var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
            } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.PartialContent) {
            } else {
                httpResponse.Dispose();
                throw new Exception("Unable to load read stream: " + httpResponse.StatusCode);
            }
            var result = new DisposableStream(await httpResponse.Content.ReadAsStreamAsync(), () => {
                httpResponse.Dispose();
            }, true);
            return result;
        }
        public async Task<DriveItem> CreateDriveItemDirectoryAsync(string path, string subPath) {
            var subPathName = PathUtils.GetPathName(subPath);
            var subPathParent = PathUtils.GetPathParent(subPath);
            var content = new VO();
            content["name"] = subPathName;
            content["folder"] = new object();
            content["@microsoft.graph.conflictBehavior"] = "replace";
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, path + (subPathParent == "/" ? "/children" : ":" + subPathParent + ":/children"), "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(content));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                } else {
                    throw new Exception("Unable to create directory: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<DriveItem>(json);
            }
        }
        public async Task DeleteDriveItemAsync(string path, string subPath) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Delete, path + ":" + subPath, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.NoContent) {
                    throw new Exception("Unable to delete drite item: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task<DriveItem> UploadDriveItemContentAsync(string path, string subPath, Stream stream) {
            var uploadPartSize = 1024 * 1024 * 4; //limit direct upload 4mb
            var tempFilename = System.IO.Path.GetTempFileName();
            try {
                using (var tempStream = new FileStream(tempFilename, FileMode.Truncate, FileAccess.ReadWrite)) {
                    //consume
                    var bytesReaded = await StreamUtils.CopyAsync(new LimitedInputStream(stream, uploadPartSize, true), tempStream);
                    //decide single vs multipart upload
                    if (bytesReaded < uploadPartSize) {
                        //single upload                        
                        tempStream.Seek(0, SeekOrigin.Begin);
                        var httpRequest = await CreateHttpRequestAsync(HttpMethod.Put, path + ":" + subPath + ":/content", "");
                        httpRequest.Content = new StreamContent(tempStream);
                        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.GetMimeType(subPath));
                        var httpResponse = await mHttpClient.SendAsync(httpRequest);
                        var json = await httpResponse.Content.ReadAsStringAsync();
                        if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                        } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                        } else {
                            httpResponse.Dispose();
                            throw new Exception("Unable to load read stream: " + httpResponse.StatusCode + ", " + json);
                        }
                        return new DProjects.Text.Json.JsonDeserializer().Deserialize<DriveItem>(json);
                    } else {
                        //multipart upload
                        //read until 320K multiple*2 
                        await StreamUtils.CopyAsync(stream, tempStream);
                        tempStream.Seek(0, SeekOrigin.Begin);
                        //create upload session
                        Microsoft.Graph.Models.UploadSession? uploadSession = null;
                        var content = new VO();
                        var contentItem = new VO();
                        contentItem["@microsoft.graph.conflictBehavior"] = "replace";
                        content["item"] = contentItem;
                        var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, path + ":" + subPath + ":/createUploadSession", "");
                        httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(content));
                        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
                        using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                            var json = await httpResponse.Content.ReadAsStringAsync();
                            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                            } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                            } else {
                                throw new Exception("Unable to create directory: " + httpResponse.StatusCode + ", " + json);
                            }
                            uploadSession = new DProjects.Text.Json.JsonDeserializer().Deserialize<Microsoft.Graph.Models.UploadSession>(json);
                        }
                        //upload multipart
                        DriveItem? driveItem = null;
                        long bytesUploaded = 0;
                        while (bytesUploaded < tempStream.Length) {
                            long bytesToUpload = (320 * 1024) * 10 * 2 * 5; //must be multiple of 320Kb and less than 60Mb (microssoft limits)
                            if (bytesToUpload > tempStream.Length - bytesUploaded) bytesToUpload = tempStream.Length - bytesUploaded;
                            httpRequest = new HttpRequestMessage(HttpMethod.Put, uploadSession.UploadUrl);
                            await mAuthenticationProvider.AuthenticateRequestAsync(httpRequest);
                            httpRequest.Content = new StreamContent(new Streams.LimitedInputStream(tempStream, bytesToUpload, true));
                            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_OCTET_STREAM);
                            httpRequest.Content.Headers.ContentRange = new ContentRangeHeaderValue(bytesUploaded, bytesUploaded + bytesToUpload - 1, tempStream.Length);
                            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                                var json = await httpResponse.Content.ReadAsStringAsync();
                                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Accepted) {
                                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK || httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                                    //ok
                                    driveItem = new DProjects.Text.Json.JsonDeserializer().Deserialize<DriveItem>(json);
                                } else {
                                    throw new Exception("Unable to save file: unable to upload multipart part: " + httpResponse.StatusCode + ", " + json);
                                }
                                bytesUploaded += bytesToUpload;
                            }
                        }
                        //return
                        if (driveItem == null) throw new Exception("Unable to upload file: drive item is null");
                        return driveItem;
                    }
                }
            } finally {
                FileUtils.DeleteFile(tempFilename);
            }
        }
        public Task CopyDriveItemAsync(string path, string subPathSource, string subPathDestination, bool overwrite, bool recursive) {
            throw new NotImplementedException();
        }
        public async Task MoveDriveItemAsync(string path, string subPathSource, string subPathDestination) {
            if (PathUtils.GetPathParent(subPathSource).Equals(PathUtils.GetPathParent(subPathDestination))) {
                //rename
                var pathName = PathUtils.GetPathName(subPathSource);
                var content = new VO();
                content["name"] = PathUtils.GetPathName(subPathDestination);
                var httpRequest = await CreateHttpRequestAsync(new HttpMethod("PATCH"), path + ":" + subPathSource, "");
                httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(content));
                httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                    } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                    } else {
                        throw new Exception("Unable to move: " + httpResponse.StatusCode + " (" + json + ")");
                    }
                }
            } else {
                //move to a new parent
                var pathItem = await GetDriveItemAsync(path, PathUtils.GetPathParent(subPathDestination));
                if (pathItem == null) throw new Exception("Unable to move: path not found: " + PathUtils.GetPathParent(subPathDestination));
                var pathName = PathUtils.GetPathName(subPathSource);
                var content = new VO();
                var parentReference = new VO();
                parentReference["id"] = pathItem.Id;
                content["parentReference"] = parentReference;
                content["name"] = PathUtils.GetPathName(subPathDestination);
                content["@microsoft.graph.conflictBehavior"] = "replace";
                var httpRequest = await CreateHttpRequestAsync(new HttpMethod("PATCH"), path + ":" + subPathSource, "");
                httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(content));
                httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                    } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                    } else {
                        throw new Exception("Unable to move: " + httpResponse.StatusCode + " (" + json + ")");
                    }
                }
            }
        }
        #endregion

        #region "Education Schools"
        public async Task<EducationSchool?> GetEducationSchoolAsync(string name) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/education/schools/" + name, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get item: " + httpResponse.StatusCode);
                }
                var results = new DProjects.Text.Json.JsonDeserializer().Deserialize<EducationSchools>(json);
                if (results.Value.Length>0) return results.Value[0];
                return null;
            }
        }
        public async Task<EducationSchool[]> GetEducationSchoolsAsync(string? pattern) {
            var nextQuery = ""; //$orderBy=";
            var result = new List<EducationSchool>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/education/schools", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get users: " + httpResponse.StatusCode);
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<EducationSchools>(json);
                    foreach (var school in list.Value) {
                        if (pattern != null && !StringUtils.Like(school.Id, pattern)) {
                            continue;
                        }
                        result.Add(school);
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<EducationSchool> CreateEducationSchoolAsync(EducationSchool school) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/education/schools", "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(school));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                } else {
                    throw new Exception("Unable to create school: " + httpResponse.StatusCode + ", " + json);
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<EducationSchool>(json);
            }
        }
        #endregion 

        #region "Education Classes"
        public async Task<EducationClasse?> GetEducationClasseAsync(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/education/classes/" + id, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get classe: " + httpResponse.StatusCode + " (" + json + ")");
                }
                var result = new DProjects.Text.Json.JsonDeserializer().Deserialize<EducationClasse>(json);
                return result;
            }
        }
        public async Task<EducationClasse[]> GetEducationClassesAsync(string? pattern) {
            var nextQuery = ""; // "$orderBy=Id";
            var result = new List<EducationClasse>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/education/classes", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get classes: " + httpResponse.StatusCode);
                    var listClasses = new DProjects.Text.Json.JsonDeserializer().Deserialize<EducationClasses>(json);
                    foreach (var classe in listClasses.Value) {
                        if (pattern != null && !StringUtils.Like(classe.Id, pattern)) {
                            continue;
                        }
                        result.Add(classe);
                    }
                    var nextLink = listClasses.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<EducationClasse> CreateEducationClassAsync(EducationClasse classe) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/education/classes", "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(classe));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                } else {
                    throw new Exception("Unable to create classe: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<EducationClasse>(json);
            }
        }
        public async Task UpdateEducationClassAsync(EducationClasse classe) {
            var httpRequest = await CreateHttpRequestAsync(new HttpMethod("PATCH"), "/education/classes/" + classe.Id, "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(classe));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else {
                    throw new Exception("Unable to update classe: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task RemoveEducationClasseAsync(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Delete, "/education/classes/" + id, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent ) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to remove classe: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task<EducationUser[]> GetEducationClasseTeachersAsync(string classeId) {
            var nextQuery = "$select=id,mail,mobilePhone,displayName,givenName,middleName,surname,usageLocation,userType,externalSource,preferredLanguage,userPrincipalName,primaryRole,teacher,student"; 
            var result = new List<EducationUser>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/education/classes/" + classeId + "/teachers", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get class teachers: " + httpResponse.StatusCode);
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<EducationUsers>(json);
                    foreach (var item in list.Value) {
                        result.Add(item);
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task AddEducationClasseTeacherAsync(string classeId, string userId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/education/classes/" + classeId + "/teachers/$ref", "");
            var vo = new VO();
            vo["@odata.id"] = mHttpClient.BaseAddress + "/education/users/" + userId;
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(vo));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to add teacher: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task RemoveEducationClasseTeacherAsync(string classeId, string userId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Delete, "/education/classes/" + classeId + "/teachers/" + userId + "/$ref", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to remove teacher: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task<EducationUser[]> GetEducationClasseStudentsAsync(string classeId) {
            var nextQuery = "$select=id,mail,mobilePhone,displayName,givenName,middleName,surname,usageLocation,userType,externalSource,preferredLanguage,userPrincipalName,primaryRole,teacher,student";
            var result = new List<EducationUser>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/education/classes/" + classeId + "/members", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get class members: " + httpResponse.StatusCode);
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<EducationUsers>(json);
                    foreach (var item in list.Value) {
                        if (item.Student != null && !string.IsNullOrEmpty(item.Student.ExternalId)) {
                            result.Add(item);
                        }
                        //if (item.PrimaryRole.Equals("teacher")) {
                        //} else {
                        //    result.Add(item);
                        //}
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task AddEducationClasseStudentAsync(string classeId, string userId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/education/classes/" + classeId + "/members/$ref", "");
            var vo = new VO();
            vo["@odata.id"] = mHttpClient.BaseAddress + "/education/users/" + userId;
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(vo));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to add student: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task RemoveEducationClasseStudentAsync(string classeId, string userId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Delete, "/education/classes/" + classeId + "/members/" + userId + "/$ref", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to remove student: " + httpResponse.StatusCode + " (" + json+  ")");
                }
            }
        }
        #endregion

        #region "Education Users"
        public async Task<EducationUser?> GetEducationUserAsync(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/education/users/" + id, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get user: " + httpResponse.StatusCode);
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<EducationUser>(json);
            }
        }
        public async Task<EducationUser[]> GetEducationUsersAsync(string? pattern, string? filter = null) {
            var nextQuery = "$select=id,mail,mobilePhone,displayName,givenName,middleName,surname,usageLocation,userType,externalSource,preferredLanguage,userPrincipalName,primaryRole,teacher,student" + (filter !=null ? "&$filter=" + filter : "");
            var result = new List<EducationUser>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/education/users", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get users: " + httpResponse.StatusCode + " (" + json + ")");
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<EducationUsers>(json);
                    foreach (var item in list.Value) {
                        if (pattern != null && !StringUtils.Like(item.Id, pattern)) {
                            continue;
                        }
                        result.Add(item);
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<EducationUser[]> GetEducationUsersTeachersAsync(string? pattern) {
            return await GetEducationUsersAsync(pattern, "primaryRole eq 'teacher'");
        }
        public async Task UpdateEducationUserAsync(EducationUser educationUser) {
            var httpRequest = await CreateHttpRequestAsync(new HttpMethod("PATCH"), "/education/users/" + educationUser.Id , "");
            var dict = new Dictionary<string, object>();
            dict["displayName"] = educationUser.DisplayName;
            dict["givenName"] = educationUser.GivenName;
            dict["surName"] = educationUser.Surname;
            dict["primaryRole"] = educationUser.PrimaryRole;
            if (educationUser.Student!=null)  dict["student"] = educationUser.Student;
            if (educationUser.Teacher != null) dict["teacher"] = educationUser.Teacher ;
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else {
                    throw new Exception("Unable to update education user: " + httpResponse.StatusCode + ", " + json);
                }
            }
        }
        public async Task RemoveEducationUserAsync(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Delete, "/education/users/" + id, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to remove education user: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        #endregion

        #region "Teams"
        public async Task<Group[]> GetTeamsAsync(string? pattern) {
            var nextQuery = "";
            var result = new List<Group>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get groups: " + httpResponse.StatusCode);
                    var listGroups = new DProjects.Text.Json.JsonDeserializer().Deserialize<Groups>(json);
                    foreach (var group in listGroups.Value) {
                        if (pattern != null && !StringUtils.Like(group.Id, pattern)) {
                            continue;
                        } else if (System.Array.IndexOf(group.ResourceProvisioningOptions, "Team") == -1) {
                            continue;
                        }
                        result.Add(group);
                    }
                    var nextLink = listGroups.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<TeamInfo?> GetTeamInfoAsync(string groupId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/teams/" + groupId, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get team info " + groupId + ": " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<TeamInfo>(json);
            }
        }
        public async Task<TeamInfo> CreateTeamFromGroupAsync(string groupId, string template) {
            var dict = new Dictionary<string, object>();
            dict["template@odata.bind"] = "https://graph.microsoft.com/v1.0/teamsTemplates('" + template + "')";
            dict["group@odata.bind"] = "https://graph.microsoft.com/v1.0/groups('" + groupId + "')";

            var memberSettings = new Dictionary<string, object>();
            memberSettings["allowCreateUpdateChannels"] = true;
            memberSettings["allowDeleteChannels"] = true;
            memberSettings["allowAddRemoveApps"] = true;
            memberSettings["allowCreateUpdateRemoveTabs"] = true;
            memberSettings["allowCreateUpdateRemoveConnectors"] = true;
            dict["memberSettings"] = memberSettings;

            var messagingSettings = new Dictionary<string, object>();
            messagingSettings["allowUserEditMessages"] = true;
            messagingSettings["allowUserDeleteMessages"] = true;
            dict["messagingSettings"] = messagingSettings;

            var guestSettings = new Dictionary<string, object>();
            guestSettings["allowCreateUpdateChannels"] = false;
            guestSettings["allowDeleteChannels"] = false;
            dict["guestSettings"] = guestSettings;

            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/teams", "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Accepted) {
                } else {
                    throw new Exception("Unable to create team from group: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
            var result = await GetTeamInfoAsync(groupId);
            return result!;
        }
        //public async Task ActivateTeamAsync(string groupId) {
        //    var dict = new Dictionary<string, object>();
        //    dict["isMembershipLimitedToOwners"] = false;
        //    var httpRequest = await CreateHttpRequestAsync(new HttpMethod("PATCH"), "/teams/" + groupId, "");
        //    httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
        //    httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
        //    using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
        //        var json = await httpResponse.Content.ReadAsStringAsync();
        //        if (httpResponse.StatusCode == System.Net.HttpStatusCode.Accepted) {
        //        } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
        //        } else {
        //            throw new Exception("Unable to activate team: " + httpResponse.StatusCode + " (" + json + ")");
        //        }
        //    }
        //}
        public async Task ArchiveTeamAsync(string groupId) {
            var dict = new Dictionary<string, object>();
            dict["shouldSetSpoSiteReadOnlyForMembers"] = true;
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/teams/" + groupId + "/archive", "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Accepted) {
                } else {
                    throw new Exception("Unable to archive team: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task UnarchiveTeamAsync(string groupId) {
            var dict = new Dictionary<string, object>();
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/teams/" + groupId + "/unarchive", "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Accepted) {
                } else {
                    throw new Exception("Unable to unarchive team: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task<TeamMember[]> GetTeamMembersAsync(string teamId) {
            var nextQuery = "";
            var result = new List<TeamMember>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/teams/" + teamId + "/members", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get team members: " + httpResponse.StatusCode);
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<TeamMembers>(json);
                    foreach (var item in list.Value) {
                        result.Add(item);
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<ConversationMember> AddTeamMemberAsync(string groupId, string userId, string[]? roles) {
            var dict = new Dictionary<string, object>();
            dict["@odata.type"] = "#microsoft.graph.aadUserConversationMember";
            dict["user@odata.bind"] = "https://graph.microsoft.com/v1.0/users('" + userId + "')";
            if (roles != null) dict["roles"] = roles;
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/teams/" + groupId + "/members", "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                } else {
                    throw new Exception("Unable to add team member (" + string.Join(",", roles ?? new string[] { }) + "): " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<ConversationMember>(json);
            }
        }
        public async Task<TeamChannel[]> GetTeamChannelsAsync(string groupId) {
            var nextQuery = "";
            var result = new List<TeamChannel>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/teams/" + groupId + "/channels", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get channels (groupId:" + groupId + "): " + httpResponse.StatusCode +  " (" + json  + ")");
                    var listChannels = new DProjects.Text.Json.JsonDeserializer().Deserialize<TeamChannels>(json);
                    foreach (var channel in listChannels.Value) {
                        result.Add(channel);
                    }
                    var nextLink = listChannels.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<TeamChannel> CreateTeamChannelAsync(string groupId, string displayName, string description, string mail , bool isFavoriteByDefault, string membershipType, IList<string> ownerIds, IList<string> guestIds) {
            var dict = new Dictionary<string, object>();
            dict["@odata.type"] = "#Microsoft.Graph.channel";
            dict["displayName"] = displayName;
            dict["description"] = description;
            dict["mail"] = mail;
            dict["isFavoriteByDefault"] = isFavoriteByDefault;
            dict["membershipType"] = membershipType;
            var members = new List<Dictionary<string, object>>();
            foreach(var ownerId in ownerIds) {
                var member = new Dictionary<string, object>();
                member["@odata.type"] = "#microsoft.graph.aadUserConversationMember";
                member["user@odata.bind"] = "https://graph.microsoft.com/v1.0/users('" + ownerId  + "')";
                member["roles"] = new string[] { "owner" };
                members.Add(member);
            }
            foreach (var guestId in guestIds) {
                var member = new Dictionary<string, object>();
                member["@odata.type"] = "#Microsoft.Graph.aadUserConversationMember";
                member["user@odata.bind"] = "https://graph.microsoft.com/v1.0/users('" + guestId + "')";
                member["roles"] = new string[] { "guest" };
                members.Add(member);
            }
            dict["members"] = members.ToArray();
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/teams/" + groupId + "/channels", "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                } else {
                    throw new Exception("Unable to create team channel: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<TeamChannel>(json);
            }
        }
        public async Task<TeamChannel?> GetTeamChannelAsync(string groupId, string channelId) {
            var httpRequest = await CreateHttpRequestAsync(new HttpMethod("GET"), "/teams/" + groupId + "/channels/" + channelId, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get team channel: " + httpResponse.StatusCode + " (" + json + ")");
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<TeamChannel>(json);
            }
        }
        public async Task UpdateTeamChannelAsync(string groupId, string channelId, string displayName, string email, string description) {
            var dict = new Dictionary<string, object>();
            dict["@odata.type"] = "#Microsoft.Graph.channel";
            dict["displayName"] = displayName;
            dict["description"] = description;
            dict["email"] = email;
            var httpRequest = await CreateHttpRequestAsync(new HttpMethod("PATCH"), "/teams/" + groupId + "/channels/" + channelId, "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else {
                    throw new Exception("Unable to update channel: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task RemoveTeamChannelAsync(string groupId, string channelId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Delete, "/teams/" + groupId + "/channels/" + channelId, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to remove channel: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task<ConversationMember[]> GetTeamChannelMembersAsync(string groupId, string channelId) {
            var nextQuery = "";
            var result = new List<ConversationMember>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/teams/" + groupId + "/channels/" + channelId + "/members", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get channel members: " + httpResponse.StatusCode + " (" + json + ")");
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<ConversationMembers>(json);
                    foreach (var item in list.Value) {
                        result.Add(item);
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        

        public async Task<ConversationMember> AddTeamChannelMemberAsync(string groupId, string channelId, string userId, string[]? roles) {
            var dict = new Dictionary<string, object>();
            dict["@odata.type"] = "#microsoft.graph.aadUserConversationMember";
            dict["user@odata.bind"] = "https://graph.microsoft.com/v1.0/users('" + userId  + "')";
            if (roles!=null) dict["roles"] = roles;
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/teams/" + groupId + "/channels/" + channelId + "/members", "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                } else {
                    throw new Exception("Unable to add team channel member (" + string.Join(",", roles ?? new string[]{ }) + "): " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<ConversationMember>(json);
            }
        }

        public async Task RemoveTeamChannelMemberAsync(string groupId, string channelId, string memberId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Delete , "/teams/" + groupId + "/channels/" + channelId + "/members/" + memberId, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent ) {
                } else {
                    throw new Exception("Unable to remove team channel member: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task<TeamTab[]> GetTeamTabsAsync(string groupId, string channelId) {
            var nextQuery = "$expand=teamsApp";
            var result = new List<TeamTab>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/teams/" + groupId + "/channels/" + channelId + "/tabs", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get channels: " + httpResponse.StatusCode + " (" + json + ")");
                    var listTabs = new DProjects.Text.Json.JsonDeserializer().Deserialize<TeamTabs>(json);
                    foreach (var tab in listTabs.Value) {
                        result.Add(tab);
                    }
                    var nextLink = listTabs.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        #endregion

        #region "Subscriptions"
        public async Task<Subscription?> GetSubscriptionAsync(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/subscriptions/" + id, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get subscription: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<Subscription>(json);
            }
        }
        public async Task<Subscription?> RenewSubscriptionAsync(string id, DateTime expirationDateTime) {
            var dict = new Dictionary<string, object>();
            var httpRequest = await CreateHttpRequestAsync(new HttpMethod("PATCH"), "/subscriptions/" + id, "");
            dict["expirationDateTime"] = expirationDateTime.ToString(DateTimeUtils.DATETIME_ISO8601);
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else {
                    throw new Exception("Unable to renew subscription: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<Subscription>(json);
            }
        }
        public async Task<Subscription> CreateSubscriptionAsync(string changeType, string notificationUrl, string resource, string clientState, DateTime expirationDateTime, bool includeResourceData, string encryptionCertificate, string encryptionCertificateId) {
            var dict = new Dictionary<string, object>();
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/subscriptions", "");
            dict["changeType"] = changeType;
            dict["notificationUrl"] = notificationUrl;
            dict["resource"] = resource;
            dict["clientState"] = clientState;            
            dict["expirationDateTime"] = expirationDateTime.ToString(DateTimeUtils.DATETIME_ISO8601);
            if (includeResourceData) {
                dict["includeResourceData"] = includeResourceData;
                dict["encryptionCertificate"] = encryptionCertificate;
                dict["encryptionCertificateId"] = encryptionCertificateId;
            }
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created ) {
                } else {
                    throw new Exception("Unable to create subscription: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<Subscription>(json);                
            }
        }
        #endregion

        #region "CallRecords"
        public async Task<CallRecord?> GetCallRecordAsync(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/communications/callRecords/" + id + "?$expand=sessions($expand=segments)", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync(); 
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get callRecord: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<CallRecord>(json);
            }
        }
        #endregion

        #region "Online meetings"

        public async Task<OnlineMeeting[]> GetOnlineMeetingsAsync() {
            var nextQuery = "";
            var result = new List<OnlineMeeting>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/communications/onlineMeetings/", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get online meetings: " + httpResponse.StatusCode + " (" + json + ")");
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<OnlineMeetings>(json);
                    foreach (var item in list.Value) {
                        result.Add(item);
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        #endregion

        #region "Calendar"
        public async Task<Calendar?> GetCalendarByUserIdAsync(string id) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + id + "/calendar", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get calendar: " + httpResponse.StatusCode + " (" + json + ")");
                }
                return new DProjects.Text.Json.JsonDeserializer().Deserialize<Calendar>(json);
            }
        }

        public async Task<Event[]> GetCalendarEventsByUserAsync(string groupId, DateTime from, DateTime to) {
            var nextQuery = "?startDateTime=" + from.ToString("yyyy-MM-ddTHH:mm:ss") + "&endDateTime=" + to.ToString("yyyy-MM-ddTHH:mm:ss");
            var result = new List<Event>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + groupId + "/calendar/calendarView", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get calendar events: " + httpResponse.StatusCode + " (" + json + ")");
                    var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<Events>(json);
                    foreach (var item in list.Value) {
                        result.Add(item);
                    }
                    var nextLink = list.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        //public async Task<Event[]> GetCalendarEventsByUserAsync(string groupId) {
        //    var nextQuery = "";
        //    var result = new List<Event>();
        //    do {
        //        var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + groupId + "/calendar/events", nextQuery);
        //        using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
        //            var json = await httpResponse.Content.ReadAsStringAsync();
        //            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get calendar events: " + httpResponse.StatusCode + " (" + json + ")");
        //            var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<Events>(json);
        //            foreach (var item in list.Value) {
        //                result.Add(item);
        //            }
        //            var nextLink = list.ODataNextLink;
        //            if (nextLink == "") {
        //                nextQuery = "";
        //            } else {
        //                nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
        //            }
        //        }
        //    } while (nextQuery.Length > 0);
        //    return result.ToArray();
        //}
        //public async Task<Calendar?> GetCalendarByGroupIdAsync(string id) {
        //    var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + id + "/calendar", "");
        //    using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
        //        var json = await httpResponse.Content.ReadAsStringAsync();
        //        if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
        //            return null;
        //        } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
        //            throw new Exception("Unable to get calendar: " + httpResponse.StatusCode + " (" + json + ")");
        //        }
        //        return new DProjects.Text.Json.JsonDeserializer().Deserialize<Calendar>(json);
        //    }
        //}
        //public async Task<Event[]> GetCalendarEventsByGroupAsync(string groupId) {
        //    var nextQuery = "";
        //    var result = new List<Event>();
        //    do {
        //        var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/groups/" + groupId + "/calendar/events", nextQuery);
        //        using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
        //            var json = await httpResponse.Content.ReadAsStringAsync();
        //            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get calendar events: " + httpResponse.StatusCode + " (" + json + ")");
        //            var list = new DProjects.Text.Json.JsonDeserializer().Deserialize<Events>(json);
        //            foreach (var item in list.Value) {
        //                result.Add(item);
        //            }
        //            var nextLink = list.ODataNextLink;
        //            if (nextLink == "") {
        //                nextQuery = "";
        //            } else {
        //                nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
        //            }
        //        }
        //    } while (nextQuery.Length > 0);
        //    return result.ToArray();
        //}

        #endregion


        #region "Reports"
        public async Task<DBTable> GetReportTeamsTeamActivityDetailAsync(int days) {
            return await GetReportAsync("getTeamsTeamActivityDetail", days);
        }
        public async Task<DBTable> GetReportOffice365GroupsActivityCountsAsync(int days) {
            return await GetReportAsync("getOffice365GroupsActivityCounts", days);
        }
        public async Task<DBTable> GetReportOffice365GroupsActivityGroupCountsAsync(int days) {
            return await GetReportAsync("getOffice365GroupsActivityGroupCounts", days);
        }
        public async Task<DBTable> GetReportOffice365GroupsActivityStorageAsync(int days) {
            return await GetReportAsync("getOffice365GroupsActivityStorage", days);
        }
        public async Task<DBTable> GetReportOffice365GroupsActivityFileCountsAsync(int days) {
            return await GetReportAsync("getOffice365GroupsActivityFileCounts", days);
        }
        public async Task<DBTable> GetReportSharePointSiteUsageFileCountsAsync(int days) {
            return await GetReportAsync("getSharePointSiteUsageFileCounts", days);
        }
        public async Task<DBTable> GetReportSharePointSiteUsageStorageAsync(int days) {
            return await GetReportAsync("getSharePointSiteUsageStorage", days);
        }
        public async Task<DBTable> GetReportSharePointSiteUsageDetail(int days) {
            return await GetReportAsync("getSharePointSiteUsageDetail", days);
        }        
        public async Task<DBTable> GetReportAsync(string name, int days) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/reports/" + name + "(period='D" + days + "')", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                } else {
                    throw new Exception("Unable to get report: " + httpResponse.StatusCode);
                }
                var csv = await httpResponse.Content.ReadAsStringAsync();
                using var textReader = new StringReader(csv); 
                using var csvDBReader = new DProjects.Db.Readers.DBReaderCsv(textReader);
                return await DBTable.FromDBReaderAsync(csvDBReader);
            }
        }


        #endregion

#region "Contacts"
        public async Task<Contact[]> GetContactsByUserIdAsync(string userId, string folderId) {
            var nextQuery = "$top=1000";
            var result = new List<Contact>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + userId + (string.IsNullOrEmpty(folderId) ? "" : "/contactFolders/" + folderId) + "/contacts", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get contacts: " + userId + ": " + httpResponse.StatusCode);
                    var listContacts = new DProjects.Text.Json.JsonDeserializer().Deserialize<Contacts>(json);
                    foreach (var contact in listContacts.Value) {
                        result.Add(contact);
                    }
                    var nextLink = listContacts.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<Contact> CreateContactAsync(string userId, string folderId, Contact contact) {
            Contact? result = null;
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/users/" + userId + (string.IsNullOrEmpty(folderId) ? "" : "/contactFolders/" + folderId) + "/contacts", "");
            //var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/users/" + userId + "/contacts", "");
            var dict = new Dictionary<string, object>();
            dict["surname"] = contact.Surname;
            dict["givenName"] = contact.GivenName;
            dict["middleName"] = contact.MiddleName;
            dict["displayName"] = contact.DisplayName;
            dict["categories"] = contact.Categories;
            dict["personalNotes"] = contact.PersonalNotes;
            dict["mobilePhone"] = contact.MobilePhone;
            dict["emailAddresses"] = contact.EmailAddresses;
            var json = new DProjects.Text.Json.JsonSerializer().Serialize(dict);
            httpRequest.Content = new StringContent(json);
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created ) {
                    result = new DProjects.Text.Json.JsonDeserializer().Deserialize<Contact>(json);
                } else {
                    throw new Exception("Unable to create contact: " + userId + ": " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
            return result!;
        }
        public async Task<Contact> UpdateContactAsync(string userId, Contact contact) {
            Contact? result = null;
            var httpRequest = await CreateHttpRequestAsync(new HttpMethod("PATCH"), "/users/" + userId + "/contacts/" + contact.Id, "");
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(contact));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync(); 
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK) {
                    result = new DProjects.Text.Json.JsonDeserializer().Deserialize<Contact>(json);
                } else {
                    throw new Exception("Unable to patch contact: " + userId + ": " + httpResponse.StatusCode + " (" + json + ")");
                } 
            }
            return result!;
        }
        public async Task RemoveContactAsync(string userId, Contact contact) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Delete, "/users/" + userId + "/contactFolders/" + contact.ParentFolderId + "/contacts/" + contact.Id, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to remove contact from user: " + userId + ": " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        public async Task<ContactFolder[]> GetContactFoldersByUserIdAndParentIdAsync(string userId, string folderId) {
            var nextQuery = "";
            var result = new List<ContactFolder>();
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + userId + "/contactFolders" + (string.IsNullOrEmpty(folderId) ? "" : "/" + folderId), nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get contact folders: " + httpResponse.StatusCode);
                    var listContactFolders = new DProjects.Text.Json.JsonDeserializer().Deserialize<ContactFolders>(json);
                    foreach (var contactFolder in listContactFolders.Value) {
                        result.Add(contactFolder);
                    }
                    var nextLink = listContactFolders.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<ContactFolderWithContacts[]> GetContactsByUserIdRecursiveAsync(string userId) {
            //var nextQuery = "$expand=contacts";
            var nextQuery = "";
            var result = new List<ContactFolderWithContacts>();
            result.Add(new ContactFolderWithContacts() { 
                 Contacts = await this.GetContactsByUserIdAsync(userId, "")  
            });
            do {
                var httpRequest = await CreateHttpRequestAsync(HttpMethod.Get, "/users/" + userId + "/contactFolders", nextQuery);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get contacts recursive: " + httpResponse.StatusCode);
                    var contactFoldersWithContacts = new DProjects.Text.Json.JsonDeserializer().Deserialize<ContactFoldersWithContacts>(json);
                    foreach (var contactFolderWithContacts in contactFoldersWithContacts.Value) {
                        contactFolderWithContacts.Contacts = await GetContactsByUserIdAsync(userId, contactFolderWithContacts.Id);
                        result.Add(contactFolderWithContacts);
                    }
                    var nextLink = contactFoldersWithContacts.ODataNextLink;
                    if (nextLink == "") {
                        nextQuery = "";
                    } else {
                        nextQuery = nextLink.Substring(nextLink.IndexOf("?") + 1);
                    }
                }
            } while (nextQuery.Length > 0);
            return result.ToArray();
        }
        public async Task<ContactFolder> CreateContactFolderAsync(string userId, string? parentFolderId, string displayName) {
            ContactFolder? result = null;             
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Post, "/users/" + userId + "/contactFolders" + (string.IsNullOrEmpty(parentFolderId) ? "": "/" + parentFolderId + "/childFolders"), "");
            var dict = new Dictionary<string, object>();
            dict["displayName"] = displayName;
            httpRequest.Content = new StringContent(new DProjects.Text.Json.JsonSerializer().Serialize(dict));
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_JSON);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Created) {
                    result = new DProjects.Text.Json.JsonDeserializer().Deserialize<ContactFolder>(json);
                } else {
                    throw new Exception("Unable to create contact folder: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
            return result;
        }
        public async Task RemoveContactFolderAsync(string userId, string contactFolderId) {
            var httpRequest = await CreateHttpRequestAsync(HttpMethod.Delete, "/users/" + userId + "/contactFolders/" + contactFolderId, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to remove contact folder user: " + httpResponse.StatusCode + " (" + json + ")");
                }
            }
        }
        #endregion


        //utils
        private async Task<HttpRequestMessage> CreateHttpRequestAsync(HttpMethod method, string path, string querystring, bool useBetaEndPoint = false) {
            var aux = PathUtils.Combine(mHttpClient.BaseAddress.AbsolutePath, PathUtils.GetPathURLEncoded(path)) + (querystring.Length > 0 ? "?" + querystring : "");
            if (useBetaEndPoint) {
                aux = aux.Replace("/v1.0/", "/beta/");
            }
            Uri requestUri = new Uri(aux, UriKind.Relative);
            var request = new HttpRequestMessage(method, requestUri);
            await mAuthenticationProvider.AuthenticateRequestAsync(request);
            return request;
        }
    }

}


;
