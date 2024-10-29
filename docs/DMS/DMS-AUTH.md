# Authorization in the Data Management Service

Also see [Authentication and Authorization Requirements](../AUTH.md)

> [!NOTE]
> These initial notes are not fully developed yet.

## Claimsets

In order to keep the JWT from becoming unreasonably large, and to prevent
managing detailed permissions within the OAuth 2.0 provider, these schemes may
use a role and/or "claimset" name that can then be mapped to detailed
information.

For example, a client of the Resources API might be granted the "SIS Vendor"
claimset. This would be a claim registered in the JWT. The Data Management
Service would need to load the actual object-level permissions for that
claimset, retrieving those details from the Configuration Service.

## Custom Claims

Other claims may be necessary, for example `EducationOrganizationId` or
`GradeLevel`. That determination will be made at a later date, with strong
community input to ensure that we create a meaningful new authorization scheme.

## Authorization and Dependency Ordering

The dependencies endpoint in the Discovery API will likely be influenced by
authorization requirements. For example, in the ODS/API's _education
organization_ based authorization, the Student _create_ permission has a higher
order number for the Student _update_ permission, with the Student School
Association coming in between the two. This is because the Student cannot be
updated until there is a Student School Association with which to decide if the
update is authorized.

## Details on existing ODS API Authorization

### 1. Retrieve Api Client Identity from Token

When an API request comes in, the system extracts the identity of the API client
by decoding the token included in the request.

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
Resources](https://edfi.atlassian.net/wiki/spaces/ODSAPIS3V61/pages/18810069/API+Claim+Sets+Resources)
for more details.

## Namespace based authorization on DMS

### 1. Authenticating and Authorizing the Client on DMS

The client provides its client ID and secret to the oauth/token endpoint on DMS.
The system then authenticates these credentials using a third-party
authentication provider(`Keycloak`), which returns a token upon successful
authentication.

### 2. Token Validator and Interpreter

The provided token will be validated to ensure it includes the expected role,
such as 'dms-client.' Then, the token interpreter will decode the token to
extract essential authorization metadata, including the Client ID, ClaimSet
name, Education Organization IDs, Namespace prefixes, and other relevant
details.

### 3. Authorization Context

An authorization context is established, including the client's basic
authorization details (Client id, Claim set name, Education Organization ids,
Namespace prefixes etc..). This context data is then cached to streamline
further requests.

### 4. Security Metadata Cache

> [!NOTE]
> The initial implementation may not need to validate or account for hierarchy
> or grouping based authorization.

Caching only the claim set data may be insufficient, as parent entity details
could also be required for authorization. On DMS, the claim set
details—including resource claims, resource claim actions, and resource claim
action authorization strategies—are retrieved and cached from the DMS
Configuration Service, which stores this data in its own database.

### 5. Authorizing /data Endpoint Requests

Authorization checks for the /data endpoint requests are performed immediately
before database operations (CRUD operations).

### 6. Authorization Middleware

Within the authorization middleware, the system decides whether the request is
authorized or not.

### 7. Security Backend

A dedicated project is in place to handle all authorization processes.

### 8. Initial Implementation

The initial plugin structure (Interfaces and classes) will be established to
support the process of verifying the request against the claim set.

Handlers will be created for two authorization strategies:

* Namespace-based Authorization

* No Further Authorization

#### Resources and Descriptors authorization

**Resources:** Initially, resource operations will use the "No Further
Authorization" strategy.

**Descriptors:** Create, update, and delete operations for descriptors will
require "Namespace Based" authorization, while read operations require "No
Further Authorization".

```mermaid
---
title: Cache basic authorization details
---
flowchart TB
a1<--token-->k1
subgraph Keycloak
    k1[Token provider]
end
 subgraph DMS
    a1[Token endpoint]-->a2[Token Interpreter]
    a2-->a3[Retrieve the Client ID, Claim Set, Namespace Prefixes, and EdOrg IDs, then cache them within the API client context]
    end
 subgraph Client
    oauth/token-->a1
    end
```

```mermaid
---
title: Authorization flow
---
flowchart TD
 subgraph DMS
        a1[Authorization Middleware] --> a2[Retrieve ClaimSet Details]
        a2 --> a3[Compare Incoming Resource and Action, and Retrieve Auth Strategy]
        a3 --> a4[Direct to Appropriate Auth Strategy Handler]
        a4 --> a5{Is Request Authorized?}
        a5 --YES--> a6[Proceed with CRUD operation]
        a5 --NO--> a7[Return an appropriate error response]
    end

    subgraph Client
        data[Request to /data/ed-fi/students] --> a1
    end
```
