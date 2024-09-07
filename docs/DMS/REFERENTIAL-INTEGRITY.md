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

      | id  | documentpartitionkey | documentuuid                         | resourcename   |
      | --- | -------------------- | ------------------------------------ | -------------- |
      | 147 | 3                    | 409b5ef7-8e28-47fe-89ae-1d94bcaf8265 | courseOffering |

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

With respect to a document's references and descriptors, the validation
process is essentially the same as described in the POST section.

### Cascading Referential Id Changes

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

> [!NOTE]
> The changes above preserve the referential integrity of the relationships, but
> they do nothing to update the documents that are retrieved with `GET`
> requests. For more on this topic, see [DMS Feature: Cascading Updates into JSON
> Documents](./CASCADING-JSON.md)

### Allowable Key Updates

By default, most resources do not allow modification of natural keys, and hence
do no allow cascading updates. There is a small set of domain entities in the
Ed-Fi Unified Data Model that allow natural key (aka "identity") updates. An
implementation host should be able to opt-in to allow other updates. The
database operations described above will work for _all_ resources, so a
mechanism is needed in application code to restrict to those that are allowed by
the standard and to any others that the hosting providers has opted into.

> [!TIP]
> In C#, the ApiSchema.json file contains a flag `allowIdentityUpdates` to
> indicate which entities can be updated. An appSettings key with comma
> separated list can be used to specify other entities that should allow
> identity updates.

## Processing a DELETE Request

This is the simplest situation to handle: the SQL database engine will reject
a `DELETE` statement that would violate a foreign key constraint.

1. Try to delete the `gradeLevelDescriptor` used in the `POST` section above.
2. That delete would violate the foreign key reference from `dms.reference` back
   to `dms.document`.
3. Therefore the operation fails.
