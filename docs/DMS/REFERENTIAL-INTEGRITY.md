# DMS Feature: Referential Integrity

The Data Management Service guarantees referential integrity for all references
and descriptors in a given document. Thus, for example, an API client cannot
create a `StudentSchoolAssociation` for a student that does not exist. While
Ed-Fi Descriptors are conceptually different from references, in practice they
are treated like references. Therefore, the `EntryGradeLevelDescriptor` on the
`StudentSchoolAssociation` must also exist in order to successfully save or
update a `StudentSchoolAssociation`. Finally, the _referenced_ items (`Student`,
`School`, `EntryGradeLevelDescriptor`, etc.) cannot be deleted while there are
other documents that refer back to them.

## Example Document

As described in [Primary Data Storage](./PRIMARY-DATA-STORAGE/), the Data
Management Service handles reference validation on POST, PUT, and DELETE
requests by carefully utilizing the relationships between the three tables,
`dms.document`, `dms.alias`, and `dms.reference`. To illustrate the process, let
us consider a `CourseOffering` document. The following document has the minimal required
fields for creating a `School` in Data Standard 5:

```json
{
    "localCourseCode": "abc",
    "courseReference": {
        "courseCode": "abc_123",
        "educationOrganizationId": 123
    },
    "schoolReference": {
        "schoolId": 123
    },
    "sessionReference": {
        "schoolId": 123,
        "schoolYear": 2025,
        "sessionName": "Session1"
    },
    "offeredGradeLevels": [
        {
            "gradeLevelDescriptor": "uri://ed-fi.org/GradeLevelDescriptor#12th Grade"
        }
    ]
}

```

## Processing a POST Request

When processing a valid `POST` request with this payload, the DMS will:

1. Calculate the
   [deterministic](https://github.com/Informatievlaanderen/deterministic-guid-generator)
   `referentialId` (UUID) value for the document and each of the references or
   descriptors (_descriptors are treated just like references in the DMS_).
   * Composed from a string containing: project name ("ed-fi"), resource name
     ("school"), and the concatenated natural key values ("255901044").
2. Open a database transaction.
   1. Insert the document itself into `dms.document`, using a random UUID as the
      `documentuuid`, and returning the auto-generated `documentId` (int64).

      | id  | documentpartitionkey | documentuuid                         | resourcename |
      | --- | -------------------- | ------------------------------------ | ------------ |
      | 147 | 3                    | 409b5ef7-8e28-47fe-89ae-1d94bcaf8265 | courseOffering      |

   2. Insert the newly assigned `documentId` and calculated `referentialId` into
      `dms.alias`.

      | referentialid                        | documentid | documentpartitionkey |
      | ------------------------------------ | ---------- | -------------------- |
      | d2098883-257f-5a1a-acc0-9d360f691c0c | 147        | 3                    |

   3. If the resource is a sub-classed resource (ex: `School` is an
      `EducationOrganization`), insert a second record into `dms.alias` using a
      `referentialId` calculated from the parent class resource name.
      `CourseOffering` is not a sub-class, therefore there will not be a second
      `Alias` record.
   4. Insert each Reference into `dms.reference` using the stored procedure
      `dms.InsertReferences`, which queries `dms.alias` by `referentiaId` and
      `referentialpartitionkey` to find the `referenceddocumentid` and
      `referenceddocumentpartitionkey`. There will be four records, all with
      `parentdocumentid = 147 and parentdocumentpartitionkey = 3`. These four
      records will have the (`referenceddocumentid`,
      `referenceddocumentpartitionkey`, `referentialId`) for the references and descriptors:
      1. `courseReference`
      2. `schoolReference`
      3. `sessionReference`
      4. `offeredGradeLevels.[0].gradeLevelDescriptor`
   5. If the `referentialId` does not exist in `dms.alias`, then step (4) will
      fail and the transaction will abort. Thus is referential integrity
      guaranteed for this `School`.

## Processing a PUT Request

With respect to a document's references and descriptors, there validation
process is essentially the same as described in the POST section.

But what about updates to a natural key in the document, when the document is
referenced by other documents? By default, these updates are only allowed on a
small set of resources, and the changes need to be cascaded into other
documents. For example, `Session` is one of the Resources that allows key value
updates. If a `PUT` request modifies the `sessionName`, then all other documents
referring to that `Session` must also be updated.

When the key value changes, then the `referentialId` - which is calculated
directly from the natural key - also changes. The DMS performs the following
operations:

1. Lookup the `documentid` (which never changes) and old `referentialId` for the
   document that is being modified, querying by the `documentuuid` (the `id`
   value from the JSON payload, which is also present in the URL).
2. Calculate the new `referentialId`.
3. Update `dms.alias` by (old) `referentialId`, setting `referentialId` to the
   new value.
   1. This update should cascade into `dms.reference`, so that its
      `referentialId` values are auto-updated.

> [!WARNING]
> TODO: add this cascading update to foreign key. The limitation on which
> resources allow cascades will be in C#, rather than hard-coded into the
> database structure.

### Updating the JSON Document

So much for the referential integrity. However, there is a problem: _the
referencing documents still contain the old natural key value(s)_. The system
now need to update all of the JSON documents that still contain the old value.
For example, on changing a `sessionName`, the referencing `Sections` need to
have their JSON documents updated to match the new `sessionName`. And anything
that references that `Section` will also need to change... for example,
`StudentSectionAssociation` and `StudentSectionAttendanceEvent` must be updated.

This update can be performed with a single SQL statement, where the
`@documentId` placeholder is the `dms.document.id` value for the `Session` being
modified, and the `@newSessionName` is the new value to set:

```sql
update dms.document
set edfidoc = jsonb_set(edfidoc, '{sessionReference, sessionName}', '"@newSessionName"')
from (
	select parentdocumentid as id from dms.reference where referenceddocumentid = @documentId
) as sub
where document.id = sub.id;
```

> [!TIP]
> In MSSQL, it appears that
> [`JSON_MODIFY`](https://learn.microsoft.com/en-us/sql/t-sql/functions/json-modify-transact-sql?view=sql-server-ver16)
> is the equivalent to PostgreSQL's `jsonb_set`.

If the school is storing positive attendance for a section, and the
`sessionName` changes deep into the school year, then this could be a large
number of records to modify. In the Glendale sample database:

* The `Session` with the most `Sections` is "Traditional-Spring Semester" with
  16,340 `Sections`.
* There are 174,205 `StudentSectionAssociation` records for that `Session`.
* There are 93 instructional days in that `Session`
* With positive attendance, there would be 93 x 174,205 = 16,201,065
  `StudentSectionAttendanceEvent` records.
* For a total of 16,391,610 JSON documents to update (plus any other table that
  refers to `Session`).

How long will it take to update those 16 million records? Even in the ODS/API
this would take a long time. As an initial experiment, in Glendale, the
`Session` mentioned above was modified. Nearly 900,000 records were affected,
and it took about 5.5 minutes in SQL Server on a developer workstation. It may
be valuable to provide an offline update function that will allow a quick
response to the API client, followed by an eventually-consistent process to
update the JSON documents.

> [!NOTE]
> The idea of an offline update has been deferred unless and until further
> optimization is necessary. If needed, then look to the Project Meadowlark
> [Design for Offline Cascading
> Updates](https://github.com/Ed-Fi-Exchange-OSS/Meadowlark/tree/main/docs/design/offline-cascading-updates)

## Processing a DELETE Request

This is the simplest situation to handle: the SQL database engine will reject
a `DELETE` statement that would violate a foreign key constraint.

1. Try to delete the `gradeLevelDescriptor` used in the `POST` section above.
2. That delete would violate the foreign key reference from `dms.reference` back
   to `dms.document`.
3. Therefore the operation fails.
