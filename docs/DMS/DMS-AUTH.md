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

## Education Organization Authorization

> [!NOTE]
> Half-baked thoughts here... needs a lot more attention.

There are at least two scenarios to deal with:

1. Client is authorized for a School.
2. Client is authorized for a Local Education Agency, which grants permission to
   all Schools in the Local Education Agency.

When retrieving a record from _any_ backend data store, we must be able to limit
the search results by the education organization in both relational queries and
queries in a search database.

These authorization scenarios apply when accessing Students, Staff, or Contacts,
and they depend on existing of a `StudentEducationOrganizationAssociation`,
`StaffEducationOrganizationAssociation`, or `StudentContactAssociation`.

> [!WARNING]
> What about other resource types? For example, is
> `StudentEducationOrganizationAssociation` itself also protected? What else is
> covered? What rules determine which resources are governed by this security?
> `ResourceClaimSets` might be the answer. Which means editable by the system
> administrators.

### Students Example

Assuming that `Students` is subject to this authorization, how could we support
this requirement in PostgreSQL? Can we take advantage of the `Reference` table
with a little modification?

```sql
SELECT
  doc.*
FROM
  dms.document as doc

-- Link from this doc to those things that point back to it
INNER JOIN
  dms.reference as ref
ON
  doc.id = ref.referenceddocumentid

-- Only look for those relationships that are Student Education Organization Associations
INNER JOIN
  dms.document as related
ON
  ref.parentId = related.id
AND
  related.resourcename = 'StudentEducationOrganizationAssociation'

-- Limit to the School ID from the JSON Web Token.
-- ... but there currently is no way to query by Education Organization ID!
WHERE
  ???
```

Problem: we don't want to query into the Document itself ot find its Id. So we
need something else. Should we use the query tables? Might be nice to use something
else, for those who are using OpenSearch.

Can we use something in the `Alias` table?

Does OpenSearch support joins?

* [Join field
  type](https://opensearch.org/docs/latest/field-types/supported-field-types/join/)
  appears to setup a specialized link _within_ an index. But we might want a
  different index for this. [A nice
  walkthrough](https://opster.com/guides/opensearch/opensearch-data-architecture/how-to-model-relationships-between-documents-in-opensearch-using-join/).
* [SQL
  plugin](https://opensearch.org/docs/latest/search-plugins/sql/sql/complex/)
  supports joins. We decided not to use the SQL Plugin because it doesn't (or
  didn't?) exist in Elasticsearch.
* Elasticsearch does [have a SQL
  plugin](https://www.elastic.co/guide/en/elasticsearch/reference/current/sql-syntax-select.html),
  which does not appear to support joins.
  * [StackOverflow
    suggestion](https://stackoverflow.com/questions/59921816/joining-two-indexes-in-elastic-search-like-a-table-join)
    also suggests the [Join
    type](https://www.elastic.co/guide/en/elasticsearch/reference/7.5/parent-join.html),
    which appears to be in common with OpenSearch.

In general, better to denormalize the information in OpenSearch. The problem is
the complexity of the potential queries and authorization patterns.

> [!WARNING]
> Is OpenSearch really feasible for complex authorization support? Might have to limit
> "complex" authorization (dynamic auth mentioned below) only to PostgreSQL. Maybe
> we can find a way to solve education organization hierarchy.

Maybe a two part query of some sort, if joins aren't feasible: look up all of
the allowed ed org Ids and include the whole list in the query? AND have a
parent/child setup with StudentEducationOrganizationAssociation to Student? _And
anything else_. Which doesn't work with dynamic ResourceClaimSet assignments.
Still too hard-coded.

## Namespace Authorization

placeholder

## Dynamic Authorization

> ![NOTE]
> Based on the new dynamic views in ODS/API 7.3. This is an are where the PostgreSQL query handler
> has a clear advantage over the OpenSearch query handler: easy to have views that pull from
> the query tables.
