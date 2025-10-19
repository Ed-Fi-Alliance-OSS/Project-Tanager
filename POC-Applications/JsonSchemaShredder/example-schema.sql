-- PostgreSQL script for schema: ed-fi
CREATE SCHEMA IF NOT EXISTS "ed-fi";

CREATE TABLE "ed-fi"."studentEducationOrganizationAssociations" (
    "id" BIGSERIAL NOT NULL PRIMARY KEY,
    "barrierToInternetAccessInResidenceDescriptor" TEXT NULL,
    "educationOrganizationId" INTEGER NOT NULL,
    "studentUniqueId" VARCHAR(32) NOT NULL
);

CREATE TABLE "ed-fi"."studentEducationOrganizationAssociations_addresses" (
    "id" BIGSERIAL NOT NULL PRIMARY KEY,
    "studentEducationOrganizationAssociation_educationOrganizationId" INTEGER NOT NULL,
    "studentEducationOrganizationAssociation_studentUniqueId" VARCHAR(32) NOT NULL,
    "addressTypeDescriptor" TEXT NOT NULL,
    "apartmentRoomSuiteNumber" VARCHAR(50) NULL,
    "streetNumberName" VARCHAR(150) NOT NULL
);

CREATE INDEX "nk_studentEducationOrganizationAssociations" ON "ed-fi"."studentEducationOrganizationAssociations" ("educationOrganizationId", "studentUniqueId");
CREATE INDEX "nk_studentEducationOrganizationAssociations_addresses" ON "ed-fi"."studentEducationOrganizationAssociations_addresses" ("studentEducationOrganizationAssociation_educationOrganizationId", "studentEducationOrganizationAssociation_studentUniqueId", "streetNumberName", "addressTypeDescriptor");
