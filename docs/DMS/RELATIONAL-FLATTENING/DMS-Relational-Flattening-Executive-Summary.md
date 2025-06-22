# Executive Summary: DMS Flattening to Relational Tables

## Overview

This Relational Flattening Design provides a comprehensive architecture for creating relational table representations of JSON documents stored in the DMS Documents table, enabling backwards compatibility with legacy ODS/API systems while maintaining the benefits of the modern DMS document-based approach.

## Key Design Principles

**Schema-Based Architecture**: The design employs a multi-schema approach with the `edfi` schema for core Ed-Fi tables, individual schemas for each extension (`tpdm`, `sample`, etc.), and the `dms` schema for infrastructure and registry tables. This eliminates naming conflicts and provides clear organizational boundaries.

**Dynamic Extension System**: A registry-based framework allows extensions to be added at deployment time without modifying core flattening logic. Extensions register their flattening functions and target tables through metadata tables, enabling complete decoupling between core and extension functionality.

**Identifier Length Management**: PostgreSQL's 63-character limit for table and constraint names is handled transparently through hash suffix generation. Names exceeding the limit are automatically shortened using the first 56 characters plus a 6-character hash, with mappings tracked in a registry table.

## Technical Architecture

**Document-Relational Duality**: The existing partitioned Documents table remains unchanged while relational tables are synchronized via triggers. This approach maintains API performance while providing relational access patterns for legacy compatibility.

**Pure Relational Design**: Flattened tables contain no JSON or full-text columns, using proper data types, foreign keys, and constraints. Complex JSON structures are decomposed into normalized parent-child table relationships. All flattened tables maintain foreign key relationships exclusively to the source Documents table, ensuring referential integrity while avoiding complex cascading relationships between flattened tables.

**Performance Optimization**: The system supports both synchronous and asynchronous flattening modes, with batch processing capabilities for high-volume scenarios. Partitioning awareness ensures optimal performance across the distributed document storage. All flattening logic is implemented as database functions, executing data transformations directly within PostgreSQL for maximum efficiency.

**Database-Centric Processing**: Flattening operations are implemented entirely as PostgreSQL functions rather than application code, leveraging the database's native JSON processing capabilities and simplifying this DMS deployment configuration

## Extension Framework

**Zero-Touch Core Logic**: New extensions require no modifications to core Data Standard flattening functions. The dynamic registry system automatically discovers and executes extension handlers based on configuration.

**Namespace Isolation**: Extensions operate in separate schemas, allowing multiple extensions to define resources with identical names without conflicts. Priority systems handle cases where multiple extensions target the same core resource.

**MetaEd Integration**: All DDL generation, flattening functions, and registration scripts are generated automatically by MetaEd, ensuring consistency and eliminating manual coding errors.

## Quality Assurance

**Comprehensive Testing Strategy**: The design includes unit tests for individual flattening functions, integration tests for full document lifecycles, performance tests for bulk operations, and validation tests for data accuracy across document and relational representations.

**Error Isolation**: Extension failures do not impact core flattening operations, with comprehensive logging and graceful error handling throughout the system.

## Implementation Strategy

**Phased Development**: Work is divided across six specialized teams covering core infrastructure, flattening logic, extension framework, MetaEd integration, testing, and deployment. This parallel development approach minimizes dependencies and accelerates delivery.

**Migration Support**: Initial population tooling handles existing document conversion with batch processing and queue management. Rollback procedures ensure safe deployment in production environments.

## Benefits

- **Legacy Compatibility**: Existing ODS/API queries work unchanged against flattened tables
- **Extension Flexibility**: New Ed-Fi extensions can be deployed without core system modifications
- **Performance**: Query patterns optimized for both document and relational access
- **Maintainability**: Clear separation of concerns and automated generation reduce maintenance overhead
- **Scalability**: Partitioning and async processing support high-volume educational data scenarios
- **Simplified Data Integrity**: Foreign key relationships point exclusively to the Documents table, eliminating complex cascading constraints and simplifying data lifecycle management
- **Atomic Operations**: Document deletion automatically cascades to all related flattened tables through single-level foreign key relationships, ensuring consistent cleanup without dependency chains
- **Reduced Lock Contention**: Avoiding inter-table foreign keys between flattened tables minimizes database locking during concurrent operations and bulk processing
- **Maximum Performance**: Database function implementation eliminates network overhead, leverages PostgreSQL's optimized JSON operators, and enables server-side processing with minimal data movement
- **Reduced Application Complexity**: Moving flattening logic to the database reduces DMS complexity. Enabling relational flattening support is as simple as running database scripts

## Risk Mitigation

The design addresses key technical risks through validation of dynamic function names, registry-based configuration management, and performance optimization. Operational risks are managed through comprehensive testing, version tracking, and detailed monitoring capabilities.

This architecture positions the DMS platform to support both current document-based APIs and legacy relational access patterns while providing a clear path for future Ed-Fi extension adoption and evolution.