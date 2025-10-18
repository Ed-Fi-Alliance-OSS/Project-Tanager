-- PostgreSQL script for schema: ed-fi
CREATE SCHEMA IF NOT EXISTS "ed-fi";

CREATE TABLE "ed-fi"."studentEducationOrganizationAssociations" (
    "id" BIGSERIAL NOT NULL PRIMARY KEY,
    "barrierToInternetAccessInResidenceDescriptor" TEXT NULL,
    "educationOrganizationReference_educationOrganizationId" INTEGER NOT NULL,
    "studentReference_studentUniqueId" VARCHAR(32) NOT NULL
);

CREATE TABLE "ed-fi"."studentEducationOrganizationAssociations_addresses" (
    "id" BIGSERIAL NOT NULL PRIMARY KEY,
    "studentEducationOrganizationAssociation_educationOrganizationReference_educationOrganizationId" INTEGER NOT NULL,
    "studentEducationOrganizationAssociation_studentReference_studentUniqueId" VARCHAR(32) NOT NULL,
    "addressTypeDescriptor" TEXT NOT NULL,
    "apartmentRoomSuiteNumber" VARCHAR(50) NULL,
    "streetNumberName" VARCHAR(150) NOT NULL
);

CREATE INDEX "nk_studentEducationOrganizationAssociations" ON "ed-fi"."studentEducationOrganizationAssociations" ("educationOrganizationReference_educationOrganizationId", "studentReference_studentUniqueId");
CREATE INDEX "nk_studentEducationOrganizationAssociations_addresses" ON "ed-fi"."studentEducationOrganizationAssociations_addresses" ("studentEducationOrganizationAssociation_educationOrganizationReference_educationOrganizationId", "studentEducationOrganizationAssociation_studentReference_studentUniqueId", "streetNumberName", "addressTypeDescriptor");
