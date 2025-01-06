# Ed-Fi ODS / API Authorization

## Process Flow

### 1. Retrieve Api Client Identity for Token

When an API request comes in, the system determines the identity of the API client
by looking up the bearer token value in the `EdFi_Admin` database.

### 2. Retrieve associated Claimset

The associated claimset for the Api client will be retrieved. This claimset
defines Resource claims, Resource claims actions, Resource claims action
authorization strategies.

### 3. Retrieve and cache Basic Claims details

The system retrieves additional basic claims details, such as the education
organization id, key, profiles, and namespace prefixes for the ApiClient. These
details are stored in a cached context.

### 4. Retrieve Claimset details from Security Database (Cached)

On the ODS API, the entire security metadata is cached. The claim set
details—including resource claims, resource claim actions, and resource claim
action authorization strategies—are retrieved from this cached security
metadata.

### 5. Check incoming request against Api Details Context

The system checks whether the incoming request complies with the permissions and
access rights defined in the API details context. Specifically, the system
verifies if the client has access to the requested educational organization or
namespace prefix.

### 6. Check incoming request against Claim Set details

The incoming resource and action will be compared to the resource claims in the
claim set. If the resource name is present in the resource claims list, the
action will be validated according to the specified authorization strategies.
Finally, the resource data will be checked to ensure compliance with the
authorization strategy rule sets. If all verification steps pass, the requested
action will proceed.

Description of different authorization strategies :

* **NoFurtherAuthorizationRequired:** Explicitly performs no additional
  authorization (beyond resource/action verification).

* **NamespaceBased:** Allows access to items based on the caller’s NamespacePrefix
  claim. NamespacePrefix values are assigned when a vendor's record is created.

* **Ownership based:** Allows access to items based on ownership tokens associated
  with the caller. Somewhat similar to the namespace-based strategy, in this
  case, the caller is granted access to the resource when token associated with
  the resource matches an ownership token associated with the caller. This
  strategy is available when “OwnershipBasedAuthorization” feature is turned on
  by the API hosts, which is necessary to capture ownership token at each
  aggregate root.

* **Relationship-based strategies:** A family of strategies that authorize access to
  student and education organization-related data through ODS relationships from
  the perspective of the education organization(s) contained in the caller’s
  claims.

Please refer [API ClaimSet and
Resources](https://docs.ed-fi.org/reference/ods-api/platform-dev-guide/security/api-claim-sets-resources)
for more details.
