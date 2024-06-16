---
description: Authentication and Authorization Requirements
---

# Auth

:::note

Work in progress.

:::

All HTTP requests to Ed-Fi services, except for endpoints from the Ed-Fi
Discovery API, must be authenticated using an OAuth 2.0 (bearer) access token.
That token should be a JSON Web Token (JWT), so that it can encode some basic
claim information.

Various authorization schemes will be applicable depending on the API being
accessed, and will be described in more detail in that application's
documentation. In order to keep the JWT from becoming unreasonably large, and to
prevent managing detailed permissions within the OAuth 2.0 provider, these
schemes may use a role and/or "claimset" name that can then be mapped to
detailed information.

For example, a client of the Resources API might be granted the "SIS Vendor"
claimset. This would be a claim registered in the JWT. The Data Management
Service would need to load the actual object-level permissions for that
claimset, retrieving those details from the Configuration Service.

Other claims may be necessary, for example `EducationOrganizationId` or
`GradeLevel`. That determination will be made at a later date, with strong
community input to ensure that we create a meaningful new authorization scheme.
