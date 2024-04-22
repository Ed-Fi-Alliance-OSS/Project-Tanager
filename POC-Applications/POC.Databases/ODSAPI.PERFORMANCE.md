# Database matrices

| Table Name                 | OperationName | ExecutionTimeInSeconds | Number Of Rows    | StartTime           | EndTime             |
|----------------------------|---------------|------------------------|-------------------|---------------------|---------------------|
| Student                    | SELECT        | 50                     | 100               | 2024-04-13 09:00:00 | 2024-04-13 09:00:50 |
| StudentSchoolAssociation   | INSERT        | 20                     | 1                 | 2024-04-13 09:01:00 | 2024-04-13 09:01:20 |
| StudentSchoolAssociation   | SELECT        | 20                     | 100               | 2024-04-13 09:01:00 | 2024-04-13 09:01:20 |
| StudentSectionAssociation  | UPDATE        | 30                     | 10                | 2024-04-13 09:02:00 | 2024-04-13 09:02:30 |


| Table Name                 | Number Of Rows | Reserved Size(KB) |Index Size(KB) | Data Size(KB) | Unused Size(KB) |
|----------------------------|----------------|-------------------|---------------|---------------|-----------------|
| Student                    | 136            | 50                | 100           |               |                 |
| StudentSchoolAssociation   | 136            | 20                | 1             |               |                 |
| StudentSectionAssociation  | 200            | 30                | 10            |               |                 |


