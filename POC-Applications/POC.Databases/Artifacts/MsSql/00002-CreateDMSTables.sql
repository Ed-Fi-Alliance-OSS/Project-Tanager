-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

----------------- Documents Table ------------------

USE EdFi_DataManagementService

-- 16 partitions
IF NOT EXISTS (SELECT * FROM sys.partition_functions
    WHERE name = 'partition_function_Documents')
BEGIN
    CREATE PARTITION FUNCTION partition_function_Documents(TINYINT)
           AS RANGE LEFT FOR VALUES (0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
END

IF NOT EXISTS (SELECT * FROM sys.partition_schemes
    WHERE name = 'partition_scheme_Documents')
BEGIN
-- All on the primary filegroup
CREATE PARTITION SCHEME partition_scheme_Documents
  AS PARTITION partition_function_Documents
  ALL TO ('PRIMARY');
END

IF NOT EXISTS (select object_id from sys.objects where object_id = OBJECT_ID(N'[dbo].[Documents]') and type = 'U')
BEGIN
CREATE TABLE [dbo].[Documents] (
  id BIGINT IDENTITY(1,1),
  partition_key TINYINT NOT NULL,
  document_uuid UNIQUEIDENTIFIER NOT NULL,
  resource_name VARCHAR(256) NOT NULL,
  edfi_doc VARBINARY(MAX) NOT NULL,
  PRIMARY KEY CLUSTERED (partition_key ASC, id ASC)
  ON partition_scheme_Documents (partition_key)
);
END

-- edfi_doc stored as a pointer
EXEC sp_tableoption 'dbo.Documents', 'large value types out of row', 1;


------------------ Aliases Table ------------------

-- 16 partitions
IF NOT EXISTS (SELECT * FROM sys.partition_functions
    WHERE name = 'partition_function_Aliases')
BEGIN
CREATE PARTITION FUNCTION partition_function_Aliases(TINYINT)
  AS RANGE LEFT FOR VALUES (0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
END

IF NOT EXISTS (SELECT * FROM sys.partition_schemes
    WHERE name = 'partition_scheme_Aliases')
BEGIN
-- All on the primary filegroup
CREATE PARTITION SCHEME partition_scheme_Aliases
  AS PARTITION partition_function_Aliases
  ALL TO ('PRIMARY');
END

IF NOT EXISTS (select object_id from sys.objects where object_id = OBJECT_ID(N'[dbo].[Aliases]') and type = 'U')
BEGIN
CREATE TABLE [dbo].[Aliases] (
  id BIGINT IDENTITY(1,1),
  partition_key TINYINT NOT NULL,
  referential_id UNIQUEIDENTIFIER NOT NULL,
  document_id BIGINT NOT NULL,
  document_partition_key TINYINT NOT NULL,
  CONSTRAINT FK_Aliases_Documents FOREIGN KEY (document_partition_key, document_id)
    REFERENCES [dbo].[Documents](partition_key, id),
  PRIMARY KEY CLUSTERED (partition_key ASC, id ASC)
  ON partition_scheme_Aliases (partition_key)
);
END

------------------ References Table ------------------

-- 16 partitions
IF NOT EXISTS (SELECT * FROM sys.partition_functions
    WHERE name = 'partition_function_References')
BEGIN
CREATE PARTITION FUNCTION partition_function_References(TINYINT)
  AS RANGE LEFT FOR VALUES (0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
END

IF NOT EXISTS (SELECT * FROM sys.partition_schemes
    WHERE name = 'partition_scheme_References')
BEGIN
-- All on the primary filegroup
CREATE PARTITION SCHEME partition_scheme_References
  AS PARTITION partition_function_References
  ALL TO ('PRIMARY');
END

IF NOT EXISTS (select object_id from sys.objects where object_id = OBJECT_ID(N'[dbo].[References]') and type = 'U')
BEGIN
CREATE TABLE [dbo].[References] (
  id BIGINT IDENTITY(1,1),
  partition_key TINYINT NOT NULL,
  parent_alias_id BIGINT NOT NULL,
  parent_partition_key TINYINT NOT NULL,
  referenced_alias_id BIGINT NOT NULL,
  referenced_partition_key TINYINT NOT NULL,
  CONSTRAINT FK_References_ParentAlias FOREIGN KEY (parent_partition_key, parent_alias_id)
  REFERENCES [dbo].[Aliases](partition_key, id),
  CONSTRAINT FK_References_ReferencedAlias FOREIGN KEY (referenced_partition_key, referenced_alias_id)
  REFERENCES [dbo].[Aliases](partition_key, id),
  PRIMARY KEY CLUSTERED (partition_key ASC, id ASC)
  ON partition_scheme_References (partition_key)
);
END
