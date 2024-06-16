# Relational Support for Client Authorization

:::note

Draft design notes

:::

```mermaid
erDiagram
    Documents {
        bigint id PK "Sequential key pattern, clustered"
        tinyint partition_key PK "Partition key for this table, derived from document_uuid"
        Guid document_uuid "API resource id, unique non-clustered, partition-aligned"
        string project_name "Example: Ed-Fi (for DS)"
        string resource_name "Example: Student"
        string resource_version "Example: 5.0.0"
        JSON edfi_doc "The document"
    }
    StudentSchoolAssociationSecurity ||--|| Documents : ""
    StudentSchoolAssociationSecurity {
        bigint id PK "Sequential key pattern, clustered"
        bigint document_id FK "SSA document indexed for security"
        tinyint document_partition_key FK "Partition key of SSA document indexed for security"
        string student_usi "Student unique id in this SSA document"
        string school_id "School id in this SSAdocument"
        string et_cetera
    }
```

We expect that security will be handled structurally the same way as queries, with sidecar tables generated
per resource with the fields relevant to security extracted into columns. In these cases however, indexes on
the security fields may be required.
