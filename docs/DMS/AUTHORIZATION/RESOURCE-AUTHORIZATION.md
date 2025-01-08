# DMS Resource Authorization

## Authorization Claims

The out of the box authorization strategies depend on one or more of the
following pieces of information:

* resources (e.g. the things managed by the API)
* actions (create, read, update, delete, read changes)
* (document) authorization strategies
* education organization(s)
* namespaces

Ideally, a JSON Web Token would encode all of the information needed to grant
authorization for a client access request. But an Ed-Fi API needs too much
information; storing it all in the JWT would create a very large token that
would contribute to significant network traffic.

This document is concerned primarily with _resources_ and _actions_, which
define the "resource authorization" strategy. [DMS Document
Authorization](./DOCUMENT-AUTHORIZATION.md) covers the other topics.

## Claimset as a Scope

A pre-built combination of resources, actions, and authorization strategies is
called a _claimset_ in Ed-Fi API systems. [Claimset
Management](../../CS/CLAIMSET-MGMT.md) is performed through the DMS
Configuration Service. A given deployment typically has a small number of
claimsets, with potentially many clients using the same claimset. The DMS will
expect to find the claimset's unique name as a _scope_, which will be included
in the JWT as a claim. For example, the JWT may contain `"scope": "SIS-Vendor"`
to grant access to the "SIS-Vendor" claimset.

Using the OAuth concept of _scope_ opens the future possibility of using
three-legged OAuth and allowing the client to request a claimset by using the
Scope parameter in their initial token request.

The DMS thus must lookup the details of the claimset, since those details are
not included in the token. Furthermore, the OAuth Identity Provider (IdP) will
not store this information; the DMS will need to pull this information from the
DMS Configuration Service. A client credential can only have a single claimset;
there is no need to reconcile competing claimsets at runtime.

> ![NOTE]
> Claimset names in the ODS/API Platform often have spaces in them. These spaces
> will be replaced with a short dash `-` for DMS, since space is used as a
> separator for multiple scopes in a JWT.

## Authorizing Access to a Resource

### Basic Scenario

The following JSON object is a simplified version of a claimset. Actions and
authorization strategies have been removed, making this a non-functional
example. In this contrived example, a client access token with `SIS-Vendor` in
the scopes has access to two resources only: `absenceEventCategoryDescriptors`
and `academicWeeks`. Attempts to access any other resource will result in HTTP
Status code 403, `Forbidden`.

```javascript
{
  "resourceClaims": [
    {
      "id": 1,
      "name": "absenceEventCategoryDescriptors",
      "actions": [],
      "_defaultAuthorizationStrategiesForCRUD": [],
      "authorizationStrategyOverridesForCRUD": [],
      "children": []
    },
    {
      "id": 1,
      "name": "academicWeeks",
      "actions": [],
      "_defaultAuthorizationStrategiesForCRUD": [],
      "authorizationStrategyOverridesForCRUD": [],
      "children": []
    }
  ],
  "id": 1,
  "name": "SIS-Vendor",
}
```

### Groupings and Hierarchies

> [!WARNING]
> There are grouping concepts in claimsets. These need to be investigated and
> documented here. For example: `EducationOrganization` and `ManagedDescriptors`.

## Authorizing Actions

The following simplified example adds the concept of _actions_, which correspond
to HTTP verbs. In this example, the client is allowed to issue a POST request to
create a new document on the `absenceEventCategoryDescriptors` endpoint, but it
cannot read, update, or delete the document. However, the client _is_ allowed to
perform all four actions for the `academicWeeks` endpoint.

```javascript
{
  "resourceClaims": [
    {
      "id": 1,
      "name": "absenceEventCategoryDescriptors",
      "actions": [
        {
          "name": "Create",
          "enabled": true
        },
      ],
      "_defaultAuthorizationStrategiesForCRUD": [],
      "authorizationStrategyOverridesForCRUD": [],
      "children": []
    },
    {
      "id": 1,
      "name": "academicWeeks",
      "actions": [
        {
          "name": "Create",
          "enabled": true
        },
        {
          "name": "Delete",
          "enabled": true
        },
        {
          "name": "Update",
          "enabled": true
        },
        {
          "name": "Read",
          "enabled": true
        }
      ],
      "_defaultAuthorizationStrategiesForCRUD": [],
      "authorizationStrategyOverridesForCRUD": [],
      "children": []
    }
  ],
  "id": 1,
  "name": "SIS-Vendor",
}
```
