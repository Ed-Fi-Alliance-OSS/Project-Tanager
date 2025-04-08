# Overview

This design builds on the existing [Student-EducationOrganization relationship-based authorization design](./STUDENT-EDORG-AUTHORIZATION.md)

The goal of this design is to enable Contact-EducationOrganization relationship-based authorization through a new authorization pathway called ContactStudentSchoolAuthorization that is similar to the StudentSchoolAssociationAuthorization pathway. This pathway builds on the existing StudentSchoolAssociationAuthorization and EducationOrganizationHierarchy tables.

# Primary Datastore Support

## ContactStudentSchoolAuthorization Pathway Table

This pathway links a Contact to a School's EducationOrganizationHierarchy via a combination of StudentContactAssociation and StudentSchoolAssociation.

```mermaid
erDiagram
    ContactStudentSchoolAuthorization o{--|| EducationOrganizationHierarchy : "Hierarchy for SchoolId"
    ContactStudentSchoolAuthorization ||--|| Document : "Links to originating StudentContactAssociation"
    ContactStudentSchoolAuthorization ||--|| Document : "Links to matching StudentSchoolAssociation"

    ContactStudentSchoolAuthorization {
        bigint Id PK "Sequential key pattern, clustered"
        varchar(32) ContactUniqueId "Indexed for lookup by ContactUniqueId"
        varchar(32) StudentUniqueId "Indexed for lookup by StudentUniqueId (for sync)"
        bigint HierarchySchoolId FK "FK to EducationOrganizationHierarchy.EducationOrganizationId with delete cascade"
        jsonb ContactStudentSchoolAuthorizationEdOrgIds "Denormalized array of EdOrgIds for the Contact, derived from EducationOrganizationHierarchy"
        bigint StudentContactAssociationId FK "FK to Document.Id (StudentContactAssociation), cascade delete"
        tinyint StudentContactAssociationPartitionKey "Partition key of StudentContactAssociation Document"
        bigint StudentSchoolAssociationId FK "FK to Document.Id (StudentSchoolAssociation), cascade delete"
        tinyint StudentSchoolAssociationPartitionKey "Partition key of StudentSchoolAssociation Document"
    }

    EducationOrganizationHierarchy {
        bigint Id PK "Sequential key pattern, clustered"
        string ProjectName "Example: Ed-Fi (for DS)"
        string ResourceName "Example: School"
        bigint EducationOrganizationId "*Needs to be indexed - Example: 123"
        bigint ParentId "Parent-child relationship"
    }

    Document {
        bigint Id PK "Sequential key pattern, clustered"
        tinyint DocumentPartitionKey PK "Partition key for this table, derived from DocumentUuid"
         _ _ "Other Document columns"
        jsonb ContactStudentSchoolAuthorizationEdOrgIds "Nullable, array of EdOrgIds via StudentContactAssociation->StudentSchoolAssociation"
        jsonb OtherAuthorizationPathwayEdOrgIds "Nullable, example of array of EducationOrganizationIds for another pathway"
    }
```

The ContactStudentSchoolAuthorization table links a ContactUniqueId from StudentContactAssociation to a SchoolId from StudentSchoolAssociation. Note that there can be multiple Contacts for a Student and so multiple entries for a Student in this table. It uses EducationOrganizationHierarchy via SchoolId for the EdOrg hierarchy. The ContactStudentSchoolAuthorizationEdOrgIds column will be a JSONB column with a simple denormalized array taken from EducationOrganizationHierarchy for the School.

Records are created/updated/deleted primarily based on StudentContactAssociation document activity but also affected by changes to StudentSchoolAssociation documents.

# Denormalization for Search Engine Support

Search engine support requires a new denormalized EdOrgId array on the Document table called ContactStudentSchoolAuthorizationEdOrgIds which will be copied from the denormalized column provided by ContactStudentSchoolAuthorization.

Like with Student-securable documents, there will be a new ContactSecurableDocument table used to index those Documents by ContactUniqueId.

```mermaid
erDiagram
    ContactSecurableDocument ||--|| Document : "Only for Documents with a securable ContactUniqueId"

    Document {
        bigint Id PK "Sequential key pattern, clustered"
        tinyint DocumentPartitionKey PK "Partition key for this table, derived from DocumentUuid"
        _ _ "Other Document columns"
        jsonb ContactStudentSchoolAuthorizationEdOrgIds "Nullable, array of EducationOrganizationIds through StudentContact and StudentSchool Associations"
        jsonb OtherAuthorizationPathwayEdOrgIds "Nullable, example of array of EducationOrganizationIds for another pathway"
    }
    ContactSecurableDocument {
        bigint Id PK "Sequential key pattern, clustered"
        varchar(32) ContactUniqueId "Indexed for lookup by ContactUniqueId"
        bigint ContactSecurableDocumentId FK "FK to Document.Id (Contact-securable), cascade delete"
        tinyint ContactSecurableDocumentPartitionKey "Partition key of Contact-securable document"
    }
```

## Authorization Algorithm for Create/Update/Delete/Get-by-ID of a Contact-securable Document

As with Student-securable Documents, the backend interfaces need to know which authorization pathways apply to a document. DMS Core compares the relevant denormalized EdOrgId array(s) on the document with client authorizations. Contact-securable Documents will use ContactStudentSchoolAuthorizationEdOrgIds on Document in the same manner for Create/Update/Delete/Get-by-ID as Student-securable Documents use StudentSchoolAuthorizationEdOrgIds.

## Synchronization between ContactSchoolAssociation document (Document table), StudentSchoolAssociation document (Document table), ContactSchoolAssociationAuthorization, ContactSecurableDocument, and Contact-securable document (Document table)

This is the denormalization synchronization phase. The synchronization logic becomes more complex due to the need to union EdOrg IDs from potentially multiple associations.

* StudentSchoolAssociation - Additional Behavior for Contact Support
    * Create
        1. Find the Contacts for this Student via the ContactStudentSchoolAuthorization table.
        2. Insert a new ContactStudentSchoolAuthorization row linking the Contacts to this new StudentSchoolAssociation. StudentSchoolAssociation-related columns will be the same as those created for the StudentSchoolAssociationAuthorization table.
        3. For each of the Contacts, find the relevant Contact-securable Documents via ContactSecurableDocument. Update their Document.ContactStudentSchoolAuthorizationEdOrgIds with the union of all ContactStudentSchoolAuthorization.ContactStudentSchoolAuthorizationEdOrgIds for the Contact.

    * Update
        1.  Nothing new

    * Delete (#2 and #3 are the same as #1 and #3 in Create)
        1. Delete the StudentSchoolAssociation document, which cascades to ContactStudentSchoolAuthorization
        2. Find the Contacts for this Student via the ContactStudentSchoolAuthorization table.
        3. For each of the Contacts, find the relevant Contact-securable Documents via ContactSecurableDocument. Update their Document.ContactStudentSchoolAuthorizationEdOrgIds with the union of all ContactStudentSchoolAuthorization.ContactStudentSchoolAuthorizationEdOrgIds for the Contact.

* StudentContactAssociation
    * Create
        1. Insert StudentContactAssociation document into Document
        2. Find the rows for this Student in the StudentSchoolAssociationAuthorization table.
        3. For each row, insert a corresponding ContactStudentSchoolAuthorization row, bringing over the StudentSchoolAssociation-related columns from StudentSchoolAssociationAuthorization.
        4. For the Contact, find the relevant Contact-securable Documents via ContactSecurableDocument. Update their Document.ContactStudentSchoolAuthorizationEdOrgIds with the union of all ContactStudentSchoolAuthorization.ContactStudentSchoolAuthorizationEdOrgIds for the Contact.

    * Update
        1. No action - identity is immutable

    * Delete (#3 same as #3 in Create, #2 same as #1 in Create of StudentSchoolAssociation)
        1. Delete the StudentContactAssociation document, which cascades to ContactStudentSchoolAuthorization.
        2. Find the Contacts for this Student via the ContactStudentSchoolAuthorization table.
        3. For the Contact, find the relevant Contact-securable Documents via ContactSecurableDocument. Update their Document.ContactStudentSchoolAuthorizationEdOrgIds with the union of all ContactStudentSchoolAuthorization.ContactStudentSchoolAuthorizationEdOrgIds for the Contact.


* Contact-securable Document (Document table)
    * Create
        1. Find the Contacts for this ContactUniqueId in the ContactStudentSchoolAuthorization table.
        2. Get the union of all ContactStudentSchoolAuthorization.ContactStudentSchoolAuthorizationEdOrgIds for those
        rows.
        2. Insert Contact-securable document into Document, including the unioned ContactStudentSchoolAuthorizationEdOrgIds array
        3. Create ContactSecurableDocument entry for this Document

  * Update (including cascade)
      1. Detect changes to ContactUniqueId
         1. If none, skip.
         2. If change, treat as Delete and Create
      2. Update Contact-securable document in Document

  * Delete
    1. Nothing synchronization-related


# Search Engine Query with Authorization Filters

## Search Engine Indexing

The denormalized EdOrgId array column ContactStudentSchoolAuthorizationEdOrgIds is included in the search engine index and keyword indexed.

```json
    {
      "DocumentUuid": "...",
      "ProjectName": "...",
      "ResourceName": "...",
      "EdfiDoc": "...",

      // Authorization pathway fields
      "ContactStudentSchoolAuthorizationEdOrgIds":["...", "...", "..."],
      "OtherAuthorizationPathwayEdOrgIds":["...", "...", "..."]
    }
```

## Search Engine Query Formulation

This is identical to the StudentSchoolAuthorization pathway, instead using ContactStudentSchoolAuthorizationEdOrgIds for filtering.
Authorization filtering applies client-authorized EdOrgIds against the relevant pathway fields using `terms` filters, ANDed with other query terms.

## Possible Future Improvements

* Partitioning of ContactSecurableDocument and ContactStudentSchoolAuthorization with partition key derived from ContactUniqueId.
* Support for multiple `StudentSchoolAssociations` per Student.
* Handling `EducationOrganizationHierarchy` changes.
* Analysis of tolerance for synchronization delays.

# Possible Future Issues

Future partitioning of ContactStudentSchoolAuthorization may be a problem because of the need to index on both StudentUniqueId and ContactUniqueId. Most index access is via ContactUniqueId, but a delete of StudentContactAssociation requires access via StudentUniqueId. It would make the most sense for a partition key to be derivable from ContactUniqueId. Therefore, an index of StudentUniqueId would have to be cross-partition.
