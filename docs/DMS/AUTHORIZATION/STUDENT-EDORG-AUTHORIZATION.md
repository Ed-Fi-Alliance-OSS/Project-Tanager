# Overview

The goal of this design is to enable Student-EducationOrganization relationship-based authorization through multiple authorization strategies. Examples include StudentSchoolAssociationAuthorization and StudentProgramAssociationAuthorization. In this document we will use StudentSchoolAssociationAuthorization by itself as the strategy as our example. We will build on the existing EducationOrganizationHierarchy table to implement Student relationship-based authorization by using a concrete table for each relationship strategy.

The challenge is to denormalize just enough information to allow for efficient authorization checks, both against the primary datastore and search engine, while at the same time not be overly burdensome for Document inserts and updates.

# Primary Datastore Support

## Authorization Strategy Table - StudentSchoolAssociationAuthorization example

There will be a denormalized, non-partitioned table per authorization pathway. An example with StudentSchoolAssociationAuthorization as the pathway:

```mermaid
erDiagram
    StudentSchoolAssociationAuthorization o{--|| EducationOrganizationHierarchy : "Hierarchy for SchoolId"
    StudentSchoolAssociationAuthorization ||--|| Document : "Only for StudentSchoolAssociation Documents"
    StudentSchoolAssociationAuthorization {
        bigint Id PK "Sequential key pattern, clustered"
        bigint StudentId "Indexed for lookup by StudentId"
        bigint HierarchySchoolId FK "FK to EducationOrganizationHierarchy.EducationOrganizationId with delete cascade"
        jsonb StudentSchoolAuthorizationEdOrgIds "Denormalized array of EdOrgIds for the Student, derived from EducationOrganizationHierarchy"
        bigint StudentSchoolAssociationId FK "FK to derived-from Document StudentSchoolAssociation as Document.Id with delete cascade"
        tinyint StudentSchoolAssociationPartitionKey FK "Partition key of StudentSchoolAssociation Document"
    }
    EducationOrganizationHierarchy {
        bigint Id PK "Sequential key pattern, clustered"
        string ProjectName "Example: Ed-Fi (for DS)"
        string ResourceName "Example: School"
        bigint EducationOrganizationId "Example: 123"
        bigint ParentId "Parent-child relationship"
    }
    Document {
        bigint Id PK "Sequential key pattern, clustered"
        tinyint DocumentPartitionKey PK "Partition key for this table, derived from DocumentUuid"
        _ _ "Other Document columns"
    }
```
The EducationOrganizationHierarchy table is unchanged from its current structure.

StudentSchoolAssociationAuthorization records are created/updated/deleted along with StudentSchoolAssociation documents. The HierarchySchoolId FK will be used to access the full hierarchy of EducationOrganizations the School is a part of, via EducationOrganizationHierarchy. The StudentSchoolAuthorizationEdOrgIds column will be a JSONB column with a simple denormalized array taken from EducationOrganizationHierarchy for the School. The StudentSchoolAssociationId FK is for syncing updates and deletes of StudentSchoolAssociation Documents via their Document.Id

Updates to a StudentSchoolAssociation Document will require corresponding updates to the StudentSchoolAssociationAuthorization table. This includes updates due to a cascade. We will need a way for core to communicate to the backend on insert/update/delete of a StudentSchoolAssociation that this is the StudentSchoolAssociationAuthorization pathway, along with the extracted StudentId and SchoolId for insert/update.

Separately, we need to make sure EducationOrganizationHierarchy is indexed on EducationOrganizationId.

# Denormalization for Search Engine Support

Our search engine support will require the introduction of denormalized EducationOrganizationId arrays on the Document table (omitted from diagram above, but shown below). This information will be copied from the denormalized row provided by StudentSchoolAssociationAuthorization, with one column per authorization pathway. In this case, the Document column would be StudentSchoolAuthorizationEdOrgIds, a JSONB column containing a simple array of EducationOrganizationIds for a Student, derived from StudentSchoolAssociation. This denormalized array is what will be used for authorization filtering on search engine queries.

When a new StudentId-securable document is inserted, the backend will need to lookup the StudentId on the StudentSchoolAssociationAuthorization table and apply the StudentSchoolAuthorizationEdOrgIds to the document.

Additionally, we will need a non-partitioned table StudentIdSecurableDocument that will act as an index into the Document table for all StudentId-securable documents. This will provide efficient access to StudentId-securable Documents for synchronization when StudentSchoolAuthorizationEdOrgIds change. When a new StudentId-securable Document is inserted, the backend will add this record. The FK will be cascade deleted when a StudentId-securable document is deleted.

```mermaid
erDiagram
    StudentIdSecurableDocument ||--|| Document : "Only for Documents with a securable StudentId"

    Document {
        bigint Id PK "Sequential key pattern, clustered"
        tinyint DocumentPartitionKey PK "Partition key for this table, derived from DocumentUuid"
        _ _ "Other Document columns"
        jsonb StudentSchoolAuthorizationEdOrgIds "Nullable, array of EducationOrganizationIds through StudentSchoolAssociation"
        jsonb OtherAuthorizationPathwayEdOrgIds "Nullable, example of array of EducationOrganizationIds for another pathway"
    }
    StudentIdSecurableDocument {
        bigint Id PK "Sequential key pattern, clustered"
        string StudentId "Indexed for lookup by StudentId"
        bigint StudentIdSecurableDocumentId FK "FK to a StudentId-securable document as Document.Id with delete cascade"
        tinyint StudentIdSecurableDocumentPartitionKey FK "Partition key of StudentId-securable document"
    }
```

## Authorization Algorithm for Create/Update/Delete/Get-by-ID of a StudentId-Securable Document

Assuming this is a StudentId-securable Document, using the StudentSchoolAssociationAuthorization strategy:

* Create/Update
  1. In DMS Core, get the StudentSchoolAuthorizationEdOrgIds array for this student, via the backend. This means we need a new interface to query the backend for EducationOrganizationIds for StudentId-securable documents for specific authorization pathway(s).
  2. DMS Core compares the StudentSchoolAuthorizationEdOrgIds with client authorizations and allow or deny.

* Get-by-ID
  1. DMS core calls the backend with the request. We need the Get-By-Id interface to be told when the Document is StudentId-securable using specific authorization pathway(s). Backend returns the document along with its StudentSchoolAuthorizationEdOrgIds.
  2. DMS Core compares the StudentSchoolAuthorizationEdOrgIds with client authorizations and allow or deny.

* Delete
  1. DMS core calls the backend with the request. We need the Delete interface to be told when the Document is StudentId-securable using specific authorization pathway(s), and also pass along client authorizations. (Yeah, not thrilled about this.)
  2. Backend compares the StudentSchoolAuthorizationEdOrgIds with client authorizations and allow or deny.

## Synchronization between StudentSchoolAssociation document (Document table), StudentSchoolAssociationAuthorization, StudentIdSecurableDocument, and StudentId-Securable document (Document table)

* StudentSchoolAssociation (Document table)
  * Create
    1. Insert StudentSchoolAssociation document into Document
    2. Insert derived row into StudentSchoolAssociationAuthorization
    3. Compute EdOrgId array for Student from SchoolId and EducationOrganizationHierarchy
    4. Include EdOrgId array on StudentSchoolAssociationAuthorization row
    5. Update EdOrgId array on each StudentId-Securable Document for this Student, using indexed StudentId on StudentIdSecurableDocument

  * Update (including cascade)
    1. Detect changes to either StudentId or SchoolId - StudentSchoolAssociation allows identity updates
       1. If none, skip.
       2. If change to StudentId or SchoolId, treat as Delete and Create
    2. Update StudentSchoolAssociation document in Document

  * Delete
    1. Null out EdOrgId array in each StudentId-Securable Document, using indexed StudentId on StudentIdSecurableDocument
    2. Delete StudentSchoolAssociation document
    3. Delete cascade will remove StudentSchoolAssociationAuthorization row

* StudentId-securable Document (Document table)
  * Create
    1. Lookup EdOrgId array on StudentSchoolAssociationAuthorization by indexed StudentId
    2. Insert StudentId-securable document into Document, including EdOrgId array
    3. Create StudentIdSecurableDocument entry for this Document

  * Update (including cascade)
      1. Detect changes to StudentId
         1. If none, skip.
         2. If change, treat as Delete and Create
      2. Update StudentId-securable document in Document

  * Delete
    1. Do nothing security-related

* EducationOrganizationHierarchy - Currently changes in response to EducationOrganization Document activity. We expect such changes after initial load to be quite rare, so we will defer this for RC.

* StudentSchoolAssociationAuthorization - Only changes as a side effect of other Document changes.

# Search Engine Query with Authorization Filters

## Search Engine Indexing Strategy

When the Document table is replicated into the search engine, the EducationOrganizationIds for each authorization pathway will be indexed.

```json
    {
      "DocumentUuid": "...",
      "ProjectName": "...",
      "ResourceName": "...",
      "EdfiDoc": "...",
      // other fields
      "StudentSchoolAuthorizationEdOrgIds":["...", "...", "..."]
      "OtherAuthorizationPathwayEdOrgIds":["...", "...", "..."]
    }
```

***
