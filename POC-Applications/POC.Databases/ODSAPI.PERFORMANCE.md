# Database matrices

This test aims to obtain some statistics that will allow us to compare the performance of the ODS databases with that of the proposed database for DMS.
In this section, we will analyze the performance of the ODS database. To do so, we have taken as an example three of the most commonly used tables: Student, StudentSchoolAssociation, and StudentSectionAssociation.

#### Conditions:
*-*	Because we selected the minimum template database (EdFi_Ods_Minimal_Template), we must perform an initial configuration data load. For this, we take as an example the execution (POST) of some endpoints necessary to work:

 *	EducationServiceCenters
 *	LocalEducationAgencies
 *	Schools
 *	GrandingPeriods
 *	Sessions
 *	Courses
 *	ClassPeriods
 *	CourseOfferings
 *	Locations
 *	Sections

 *-*	A new environment was created for the execution of the tests. Some values configured in the environment are:
 *	Windows Server 2020
 *	Microsoft SQL Server 2022
 *	32GB RAM
 *	4 CPU 2.56 GHz

#### Requirements:
*-*	Download the following artifacts from the Project-Tanager repository (POC-Applications\POC.Databases\Artifacts\MsSql):
*	Pre-DB-Configuration-Inserts.sql
*	Masive-Inserts.sql

*-* Have a backup of the database EdFi_Ods_Minimal_Template (We will name it OdsMinimalTemp_71.bak).

*-*	Run the "Pre-DB-Configuration-Inserts.sql" script, which will insert all the required configurations (EducationOrganization, LocalEducationAgency, School, GradingPeriod, Session, among others).

#### Configuration:
*-*	First Time:

* Take the database backup (OdsMinimalTemp_71.bak) and pull it up via SQL Management Studio.
*	Open the Pre-DB-Configuration-Inserts.sql script and run it.
*	After the previous step and in order not to have to execute so many steps in each stage of the test, a backup of the database was made, and this will be the one that will be raised in each stage of testing; the backup is called OdsMinimalTemp_71_PreConfigured.bak.

*-*	The next time the procedure is executed, the backup OdsMinimalTemp_71_PreConfigured.bak must be restored.

#### Procedure:
-



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


