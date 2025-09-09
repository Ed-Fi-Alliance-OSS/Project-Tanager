# Relational Flattening Design Summary

## Overview

This document summarizes the updated relational flattening design, which provides SQL-accessible views of JSON documents while maintaining the existing three-table architecture as the source of truth.

## Key Design Decisions

### Architecture Approach
- Read-only flattened tables alongside existing Document/Reference/Alias tables
- Document table remains the authoritative source
- Reference validation continues through existing tables
- Flattening is optional and can be enabled/disabled
- No deferred synchronization - updates happen atomically

### Core Principles

1. **Minimize Complexity** - Simple table structures for quicker operations
2. **Complementary Architecture** - Three-table design unchanged; flattened tables purely for relational access
3. **Surrogate Keys Everywhere** - All tables use BIGINT IDENTITY(1,1) for performance
4. **Natural Key Resolution Through Views** - Views JOIN tables to expose business identifiers
5. **No Update Cascade Required** - Surrogate keys avoid denormalizing natural keys across tables
5. **Uniqueness Constraints** - Natural keys become unique constraints, not primary keys
6. **Delete Cascade Behavior** - Document deletion cascades through all related flattened tables
7. **Clear Naming Conventions** - No abbreviations; hash suffixes for length limits; reserved names (Id, DocumentUuid, DocumentPartitionKey)

## Table Structure Patterns

### DomainEntity Tables (e.g., Student, School)
- Surrogate primary key (Id)
- Foreign key to Document table (Document_Id, Document_PartitionKey)
- Natural key columns with unique constraint
- Data columns

### Association Tables (e.g., StudentSchoolAssociation)
- Surrogate primary key (Id)
- Foreign key to Document table
- Foreign keys to referenced entities using surrogate keys (Student_Id, School_Id)
- Natural key components (e.g., EntryDate)
- Unique constraint on combined natural keys

### Collection Tables (e.g., StudentOtherName)
- Surrogate primary key (Id)
- Foreign key to parent table (Student_Id)
- Foreign key to Document table
- Unique constraint on parent FK + identifying properties from collection

### Multi-Level Collections
- Second-level tables reference immediate parent, not root
- Example: StudentEducationOrganizationAssociationAddressPeriod → StudentEducationOrganizationAssociationAddress → StudentEducationOrganizationAssociation
- Each level maintains Document table reference

## Natural Key Resolution

Views provide business-friendly querying by:
- Joining flattened tables to resolve natural keys
- Exposing natural keys with clear naming (e.g., Student_StudentUniqueId)
- Supporting multi-level hierarchy resolution
- Eliminating need for complex joins in user queries

Example: `StudentSchoolAssociation_View` resolves Student_Id and School_Id (surrogate keys) to StudentUniqueId and SchoolId respectively.

## MetaEd Generation Strategy

The design includes three generation types:

### 1. DDL Script Generation
- TypeScript functions traverse entity models
- Generate CREATE TABLE statements for root and nested collections
- Handle multi-level hierarchies
- Apply proper constraints and foreign keys

### 2. Metadata-Driven Flattening for DMS

#### MetaEd Side
- Extends ApiSchema.json with FlatteningMetadata object on ResourceSchema
- Includes:
  - Table naming including sub-tables
  - Column mappings (JSON path → SQL column)
  - Natural key information
  - Resource reference resolution
  - SQL type hints

#### DMS Side
- Runtime interpretation by DMS (no code generation)
- Single flattener C# class handles all resources

### 3. View Generation
- Generate CREATE VIEW statements for natural key resolution views
- Handles nested references recursively
- Supports collection views with parent context
- Views to be used for direct database access use cases

## Benefits

1. **Performance** - Surrogate keys optimize JOINs
2. **Simplicity** - Straightforward table structures
3. **Flexibility** - Optional flattening, views for natural keys
4. **Compatibility** - Existing three-table design unchanged
5. **Usability** - SQL users get familiar relational access
6. **Maintainability** - Clear patterns, no complex cascades

## Trade-offs

- Storage overhead from flattened tables
- Insert/update performance impact
- Views add query overhead
