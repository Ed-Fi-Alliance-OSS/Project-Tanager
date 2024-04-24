-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

USE EdFi_DataManagementService

-- Query table for StudentSchoolAssociation
IF NOT EXISTS (select object_id from sys.objects where object_id = OBJECT_ID(N'[dbo].[QueryStudentSchoolAssociation]') and type = 'U')
BEGIN
CREATE TABLE [dbo].[QueryStudentSchoolAssociation] (
  id BIGINT IDENTITY(1,1),
  document_partition_key TINYINT NOT NULL,
  document_id BIGINT NOT NULL,
  entryDate DATETIME2 NULL,
  schoolId BIGINT NULL,
  studentUniqueId VARCHAR(256) NULL,
  CONSTRAINT FK_QueryStudentSchoolAssociation_Documents FOREIGN KEY (document_partition_key, document_id)
    REFERENCES [dbo].[Documents](document_partition_key, id),
  PRIMARY KEY CLUSTERED (id)
);
END
