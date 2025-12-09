using System.Text.Json.Serialization;

namespace ApiPublisher
{
    public class KafkaMessage
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("documentpartitionkey")]
        public int DocumentPartitionKey { get; set; }

        [JsonPropertyName("documentuuid")]
        public string DocumentUuid { get; set; } = string.Empty;

        [JsonPropertyName("resourcename")]
        public string ResourceName { get; set; } = string.Empty;

        [JsonPropertyName("resourceversion")]
        public string ResourceVersion { get; set; } = string.Empty;

        [JsonPropertyName("isdescriptor")]
        public bool IsDescriptor { get; set; }

        [JsonPropertyName("projectname")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("edfidoc")]
        public System.Text.Json.JsonElement EdfiDoc { get; set; }

        [JsonPropertyName("securityelements")]
        public System.Text.Json.JsonElement SecurityElements { get; set; }

        [JsonPropertyName("studentschoolauthorizationedorgids")]
        public object? StudentSchoolAuthorizationEdOrgIds { get; set; }

        [JsonPropertyName("studentedorgresponsibilityauthorizationids")]
        public object? StudentEdOrgResponsibilityAuthorizationIds { get; set; }

        [JsonPropertyName("contactstudentschoolauthorizationedorgids")]
        public object? ContactStudentSchoolAuthorizationEdOrgIds { get; set; }

        [JsonPropertyName("staffeducationorganizationauthorizationedorgids")]
        public object? StaffEducationOrganizationAuthorizationEdOrgIds { get; set; }

        [JsonPropertyName("createdat")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("lastmodifiedat")]
        public long LastModifiedAt { get; set; }

        [JsonPropertyName("lastmodifiedtraceid")]
        public string LastModifiedTraceId { get; set; } = string.Empty;

        [JsonPropertyName("__deleted")]
        public string Deleted { get; set; } = string.Empty;
    }

    public class DiscoveryApiResponse
    {
        [JsonPropertyName("urls")]
        public UrlsInfo Urls { get; set; } = new UrlsInfo();
    }

    public class UrlsInfo
    {
        [JsonPropertyName("oauth")]
        public string OAuth { get; set; } = string.Empty;

        [JsonPropertyName("dataManagementApi")]
        public string DataManagementApi { get; set; } = string.Empty;
    }

    public class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
