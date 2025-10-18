# JSON Schema Shredder

A .NET 8 console application that reads JSON schema documents and generates PostgreSQL database scripts.

## Features

* Reads JSON documents containing schema definitions
* Creates PostgreSQL table creation scripts
* Handles various JSON schema types (string, integer, boolean, array, object)
* Flattens nested object properties
* Creates separate tables for array properties
* Generates natural key indexes based on identity JSON paths
* Skips resource schemas ending with "Descriptors"

## Usage

```bash
dotnet run [schema-file.json]
```

If no file is specified, it uses `example-schema.json`.

## Example

Given a JSON schema document like:

```json
{
    "projectSchema": {
        "projectEndpointName": "ed-fi",
        "resourceSchemas": {
            "studentEducationOrganizationAssociations": {
                "identityJsonPaths": [
                    "$.educationOrganizationReference.educationOrganizationId",
                    "$.studentReference.studentUniqueId"
                ],
                "jsonSchemaForInsert": {
                    "properties": {
                        "barrierToInternetAccessInResidenceDescriptor": {
                            "type": "string"
                        },
                        "educationOrganizationReference": {
                            "properties": {
                                "educationOrganizationId": {
                                    "type": "integer"
                                }
                            },
                            "required": ["educationOrganizationId"],
                            "type": "object"
                        },
                        "studentReference": {
                            "properties": {
                                "studentUniqueId": {
                                    "maxLength": 32,
                                    "type": "string"
                                }
                            },
                            "required": ["studentUniqueId"],
                            "type": "object"
                        }
                    },
                    "required": [
                        "studentReference",
                        "educationOrganizationReference"
                    ]
                }
            }
        }
    }
}
```

It generates:

```sql
-- PostgreSQL script for schema: ed-fi
CREATE SCHEMA IF NOT EXISTS "ed-fi";

CREATE TABLE "ed-fi"."studentEducationOrganizationAssociations" (
    "id" BIGSERIAL NOT NULL PRIMARY KEY,
    "barrierToInternetAccessInResidenceDescriptor" TEXT NULL,
    "educationOrganizationReference_educationOrganizationId" INTEGER NOT NULL,
    "studentReference_studentUniqueId" VARCHAR(32) NOT NULL
);

CREATE INDEX "nk_studentEducationOrganizationAssociations" ON "ed-fi"."studentEducationOrganizationAssociations" ("educationOrganizationReference_educationOrganizationId", "studentReference_studentUniqueId");
```

## Schema Processing Rules

1. **Schema Name**: Uses `projectSchema.projectEndpointName` as the PostgreSQL schema name
2. **Table Creation**: Creates a table for each object in `resourceSchemas`, except those ending with "Descriptors"
3. **Primary Key**: Every table gets an auto-incrementing `id` column as primary key
4. **Data Types**:
   * `string` → `TEXT` (or `VARCHAR(n)` if `maxLength` is specified)
   * `integer` → `INTEGER`
   * `boolean` → `BOOLEAN`
   * `array` → Creates a separate child table
   * `object` → Flattens properties with underscore naming
5. **Required Fields**: Properties listed in `required` arrays are marked as `NOT NULL`
6. **Natural Key Indexes**: Uses `identityJsonPaths` to create indexes on natural keys
7. **Child Tables**: For array properties, creates separate tables with foreign key references

## Running Tests

```bash
dotnet test
```

Tests cover:
* Correct table and column generation
* Skipping descriptor resource schemas
* Handling various data types
* Natural key index creation