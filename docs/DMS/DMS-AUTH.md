# Authorization in the Data Management Service

Also see [Authentication and Authorization Requirements](../AUTH.md)

> [!NOTE]
> These initial notes are not fully developed yet.

## Claimsets

In order to keep the JWT from becoming unreasonably large, and to
prevent managing detailed permissions within the OAuth 2.0 provider, these
schemes may use a role and/or "claimset" name that can then be mapped to
detailed information.

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
