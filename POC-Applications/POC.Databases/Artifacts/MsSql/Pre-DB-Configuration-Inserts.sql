
begin tran
---------------------------------------------------------------------------------------------------------------------------------
-- 1. EducationServiceCenter
INSERT INTO edfi.EducationOrganization 
(LastModifiedDate, CreateDate, Id, NameOfInstitution, OperationalStatusDescriptorId, ShortNameOfInstitution, WebSite, Discriminator, EducationOrganizationId) 
VALUES (GETDATE(), GETDATE(), 'FBCA5C20-8F8E-4EAE-99A8-905D0CF65F12', 'Region 99 Education Service Center', NULL, NULL, NULL, 'edfi.EducationServiceCenter', 255950)
GO

INSERT INTO edfi.EducationServiceCenter (StateEducationAgencyId, EducationServiceCenterId) VALUES (NULL, 255950)
GO

INSERT INTO edfi.EducationOrganizationAddress 
(CreateDate, ApartmentRoomSuiteNumber, BuildingSiteNumber, CongressionalDistrict, CountyFIPSCode, DoNotPublishIndicator, Latitude, LocaleDescriptorId, Longitude, NameOfCounty, AddressTypeDescriptorId, City, PostalCode, StateAbbreviationDescriptorId, StreetNumberName, EducationOrganizationId) 
VALUES ('2024-04-18 15:25:40.5252383', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'Dallas', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%AddressTypeDescriptor' And CodeValue = 'Physical'),
	'Dallas', '75217', 	
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%StateAbbreviationDescriptor' And CodeValue = 'TX'),
	'P.O. Box 898', 255950);
GO

INSERT INTO edfi.EducationOrganizationCategory 
(CreateDate, EducationOrganizationCategoryDescriptorId, EducationOrganizationId) 
VALUES (GETDATE(), (SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%EducationOrganizationCategoryDescriptor' And CodeValue = 'Education Service Center'), 255950)
GO

INSERT INTO edfi.EducationOrganizationIdentificationCode 
(CreateDate, IdentificationCode, EducationOrganizationIdentificationSystemDescriptorId, EducationOrganizationId) 
VALUES (GETDATE(), 255950, 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%EducationOrganizationIdentificationSystemDescriptor' And CodeValue = 'NCES'), 255950)
GO

INSERT INTO edfi.EducationOrganizationInstitutionTelephone 
(CreateDate, TelephoneNumber, InstitutionTelephoneNumberTypeDescriptorId, EducationOrganizationId) 
VALUES (GETDATE(), '(214) 876-8921', (SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%InstitutionTelephoneNumberTypeDescriptor' And CodeValue = 'Main'), 255950);
GO

---------------------------------------------------------------------------------------------------------------------------------
-- 2. LocalEducationAgencies
INSERT INTO edfi.EducationOrganization 
(LastModifiedDate, CreateDate, Id, NameOfInstitution, OperationalStatusDescriptorId, ShortNameOfInstitution, WebSite, Discriminator, EducationOrganizationId) 
VALUES (GETDATE(), GETDATE(), '09998E24-D94C-4D27-A982-CF08225BBF73', 'TEST Data Insertion', NULL, 'TEST', 'http://www.TEST.edu/', 'edfi.LocalEducationAgency', 255911)
GO

INSERT INTO edfi.LocalEducationAgency 
(CharterStatusDescriptorId, EducationServiceCenterId, LocalEducationAgencyCategoryDescriptorId, ParentLocalEducationAgencyId, StateEducationAgencyId, LocalEducationAgencyId) 
VALUES (NULL, 255950, 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%LocalEducationAgencyCategoryDescriptor' And CodeValue = 'Public school district part of a supervisory union'), NULL, NULL, 255911)
GO

INSERT INTO edfi.EducationOrganizationAddress 
(CreateDate, ApartmentRoomSuiteNumber, BuildingSiteNumber, CongressionalDistrict, CountyFIPSCode, DoNotPublishIndicator, Latitude, LocaleDescriptorId, Longitude, NameOfCounty, AddressTypeDescriptorId, City, PostalCode, StateAbbreviationDescriptorId, StreetNumberName, EducationOrganizationId) 
VALUES (GETDATE(), NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'Williston', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%AddressTypeDescriptor' And CodeValue = 'Physical'), 'Grand Bend', '73334-9376', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%StateAbbreviationDescriptor' And CodeValue = 'TX'), 'P.O. Box 9376', 255911)
GO

INSERT INTO edfi.EducationOrganizationCategory 
(CreateDate, EducationOrganizationCategoryDescriptorId, EducationOrganizationId) 
VALUES (GETDATE(), (SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%EducationOrganizationCategoryDescriptor' And CodeValue = 'Local Education Agency'), 255911)
GO

INSERT INTO edfi.EducationOrganizationIdentificationCode 
(CreateDate, IdentificationCode, EducationOrganizationIdentificationSystemDescriptorId, EducationOrganizationId) 
VALUES (GETDATE(), '255911', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%EducationOrganizationIdentificationSystemDescriptor' And CodeValue = 'SEA'), 255911)
GO

INSERT INTO edfi.EducationOrganizationInstitutionTelephone 
(CreateDate, TelephoneNumber, InstitutionTelephoneNumberTypeDescriptorId, EducationOrganizationId) 
VALUES (GETDATE(), '(950) 366-2320', (SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%InstitutionTelephoneNumberTypeDescriptor' And CodeValue = 'Main'), 255911)
GO

---------------------------------------------------------------------------------------------------------------------------------
-- 3. Schools
INSERT INTO edfi.EducationOrganization 
(LastModifiedDate, CreateDate, Id, NameOfInstitution, OperationalStatusDescriptorId, ShortNameOfInstitution, WebSite, Discriminator, EducationOrganizationId) 
VALUES (GETDATE(), GETDATE(), '131199BB-EEB4-47BB-9870-DF5CF69B3E2D', 'Grand Bend High School', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%OperationalStatusDescriptor' And CodeValue = 'Active'), 'GBHS', 'http://www.GBISD.edu/GBHS/', 'edfi.School', 255911001)
GO

INSERT INTO edfi.School (AdministrativeFundingControlDescriptorId, CharterApprovalAgencyTypeDescriptorId, CharterApprovalSchoolYear, CharterStatusDescriptorId, InternetAccessDescriptorId, LocalEducationAgencyId, MagnetSpecialProgramEmphasisSchoolDescriptorId, SchoolTypeDescriptorId, TitleIPartASchoolDesignationDescriptorId, SchoolId) 
VALUES ((SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%AdministrativeFundingControlDescriptor' And CodeValue = 'Public School'), NULL, NULL, 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%CharterStatusDescriptor' And CodeValue = 'Not a Charter School'), NULL, 255911, NULL, 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%SchoolTypeDescriptor' And CodeValue = 'Regular'), 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%TitleIPartASchoolDesignationDescriptor' And CodeValue = 'Not A Title I School'), 255911001)
GO

INSERT INTO edfi.EducationOrganizationAddress 
(CreateDate, ApartmentRoomSuiteNumber, BuildingSiteNumber, CongressionalDistrict, CountyFIPSCode, DoNotPublishIndicator, Latitude, LocaleDescriptorId, Longitude, NameOfCounty, AddressTypeDescriptorId, City, PostalCode, StateAbbreviationDescriptorId, StreetNumberName, EducationOrganizationId) 
VALUES (GETDATE(), NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'Dallas', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%AddressTypeDescriptor' And CodeValue = 'Physical'),
	'Dallas', '75217', 	
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%StateAbbreviationDescriptor' And CodeValue = 'TX'),
	'P.O. Box 898', 255911001);
GO

INSERT INTO edfi.EducationOrganizationCategory 
(CreateDate, EducationOrganizationCategoryDescriptorId, EducationOrganizationId) 
VALUES (GETDATE(), (SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%EducationOrganizationCategoryDescriptor' And CodeValue = 'School'), 255911001)
GO

INSERT INTO edfi.EducationOrganizationIdentificationCode 
(CreateDate, IdentificationCode, EducationOrganizationIdentificationSystemDescriptorId, EducationOrganizationId) 
VALUES (GETDATE(), 255950, (SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%EducationOrganizationIdentificationSystemDescriptor' And CodeValue = 'SEA'), 255911001)
GO

INSERT INTO edfi.EducationOrganizationIndicator 
(CreateDate, DesignatedBy, IndicatorGroupDescriptorId, IndicatorLevelDescriptorId, IndicatorValue, IndicatorDescriptorId, EducationOrganizationId) 
VALUES (GETDATE(), NULL, 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%IndicatorGroupDescriptor' And CodeValue = 'Staff Indicator'),
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%IndicatorLevelDescriptor' And CodeValue = 'High Retention'), '90', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%IndicatorDescriptor' And CodeValue = 'Retention Rate'), 255911001)
GO

INSERT INTO edfi.EducationOrganizationIndicatorPeriod 
(CreateDate, EndDate, BeginDate, IndicatorDescriptorId, EducationOrganizationId) 
VALUES (GETDATE(), '2022-06-30', '2021-08-29', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%IndicatorDescriptor' And CodeValue = 'Retention Rate'), 255911001)
GO

INSERT INTO edfi.EducationOrganizationInstitutionTelephone 
(CreateDate, TelephoneNumber, InstitutionTelephoneNumberTypeDescriptorId, EducationOrganizationId) 
VALUES (GETDATE(), '(950) 393-3156', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%InstitutionTelephoneNumberTypeDescriptor' And CodeValue = 'Main'), 255911001);
GO

INSERT INTO edfi.SchoolCategory 
(CreateDate, SchoolCategoryDescriptorId, SchoolId) 
VALUES (GETDATE(), (SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%SchoolCategoryDescriptor' And CodeValue = 'High School'), 255911001)
GO

INSERT INTO edfi.SchoolGradeLevel 
(CreateDate, GradeLevelDescriptorId, SchoolId) 
VALUES (GETDATE(), (SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%GradeLevelDescriptor' And CodeValue = 'Tenth grade'), 255911001)
GO

INSERT INTO tpdm.SchoolExtension 
(CreateDate, PostSecondaryInstitutionId, SchoolId) 
VALUES (GETDATE(), NULL, 255911001)
GO

---------------------------------------------------------------------------------------------------------------------------------
-- 4. GradingPeriods
INSERT INTO edfi.GradingPeriod 
(LastModifiedDate, CreateDate, Id, BeginDate, EndDate, PeriodSequence, TotalInstructionalDays, Discriminator, GradingPeriodDescriptorId, GradingPeriodName, SchoolId, SchoolYear) 
VALUES (GETDATE(), GETDATE(), '39712057-C6F7-42CF-A8C7-F3221680131A', '2021-08-23', '2021-10-03', 1, 29, NULL, 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%GradingPeriodDescriptor' And CodeValue = 'First Six Weeks'), '2021-2022 Fall Semester Exam 1', 255911001, 2022)
GO

---------------------------------------------------------------------------------------------------------------------------------
-- 5. Sessions
INSERT INTO edfi.Session (LastModifiedDate, CreateDate, Id, BeginDate, EndDate, TermDescriptorId, TotalInstructionalDays, Discriminator, SchoolId, SchoolYear, SessionName) 
VALUES (GETDATE(), GETDATE(), '4CDB09AC-5E28-402C-8FD9-89ABF86D57A7', '2021-08-23', '2021-12-17', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%TermDescriptor' And CodeValue = 'Fall Semester'), 
	81, NULL, 255911001, 2022, '2021-2022 Fall Semester')
GO

INSERT INTO edfi.SessionGradingPeriod 
(CreateDate, GradingPeriodDescriptorId, GradingPeriodName, SchoolId, SchoolYear, SessionName) 
VALUES (GETDATE(), (SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%GradingPeriodDescriptor' And CodeValue = 'First Six Weeks'), 
	'2021-2022 Fall Semester Exam 1', 255911001, 2022, '2021-2022 Fall Semester')
GO

---------------------------------------------------------------------------------------------------------------------------------
-- 6. Corses

INSERT INTO edfi.Course 
(LastModifiedDate, CreateDate, Id, CareerPathwayDescriptorId, CourseDefinedByDescriptorId, CourseDescription, CourseGPAApplicabilityDescriptorId, CourseTitle, DateCourseAdopted, HighSchoolCourseRequirement, 
MaxCompletionsForCredit, MaximumAvailableCreditConversion, MaximumAvailableCredits, MaximumAvailableCreditTypeDescriptorId, MinimumAvailableCreditConversion, MinimumAvailableCredits, 
MinimumAvailableCreditTypeDescriptorId, NumberOfParts, TimeRequiredForCompletion, Discriminator, CourseCode, EducationOrganizationId) 
VALUES (GETDATE(), GETDATE(), 'D6DD86D2-0FA7-42D8-8101-93D74EB904C9', NULL, 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%CourseDefinedByDescriptor' And CodeValue = 'SEA'), 
	'Algebra I', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%CourseGPAApplicabilityDescriptor' And CodeValue = 'Applicable'), 
	'Algebra I', NULL, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL, 'ALG-1', 255911001)
GO

INSERT INTO edfi.CourseIdentificationCode 
(CreateDate, AssigningOrganizationIdentificationCode, CourseCatalogURL, IdentificationCode, CourseIdentificationSystemDescriptorId, CourseCode, EducationOrganizationId) 
VALUES (GETDATE(), NULL, 'http://www.GBISD.edu/coursecatalog', 'ALG-1', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%CourseIdentificationSystemDescriptor' And CodeValue = 'LEA course code'), 'ALG-1', 255911001);
GO

INSERT INTO edfi.CourseIdentificationCode 
(CreateDate, AssigningOrganizationIdentificationCode, CourseCatalogURL, IdentificationCode, CourseIdentificationSystemDescriptorId, CourseCode, EducationOrganizationId) 
VALUES (GETDATE(), NULL, 'http://www.GBISD.edu/coursecatalog', 'ALG-1', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%CourseIdentificationSystemDescriptor' And CodeValue = 'State course code'), 'ALG-1', 255911001);
GO

---------------------------------------------------------------------------------------------------------------------------------
-- 7. ClassPeriods

INSERT INTO edfi.ClassPeriod 
(LastModifiedDate, CreateDate, Id, OfficialAttendancePeriod, Discriminator, ClassPeriodName, SchoolId) 
VALUES (GETDATE(), GETDATE(), 'F272A895-FB51-4EA8-9D49-94C98937BE5E', NULL, NULL, '01 - Traditional', 255911001)
GO

INSERT INTO edfi.ClassPeriodMeetingTime 
(CreateDate, EndTime, StartTime, ClassPeriodName, SchoolId) 
VALUES (GETDATE(), '09:25:00', '08:35:00', '01 - Traditional', 255911001)
GO

---------------------------------------------------------------------------------------------------------------------------------
-- 8. CourseOfferings
INSERT INTO edfi.CourseOffering 
(LastModifiedDate, CreateDate, Id, CourseCode, EducationOrganizationId, InstructionalTimePlanned, LocalCourseTitle, Discriminator, LocalCourseCode, SchoolId, SchoolYear, SessionName) 
VALUES (GETDATE(), GETDATE(), 'E917B320-3051-4A95-8DE3-933897F96C1A', 'ALG-1', 255911001, NULL, NULL, NULL, 'ALG-1', 255911001, 2022, '2021-2022 Fall Semester')
GO

---------------------------------------------------------------------------------------------------------------------------------
-- 9. Locations
INSERT INTO edfi.Location 
(LastModifiedDate, CreateDate, Id, MaximumNumberOfSeats, OptimalNumberOfSeats, Discriminator, ClassroomIdentificationCode, SchoolId) 
VALUES (GETDATE(), GETDATE(), '0F797D81-3D98-47D2-BF4D-85D1C78CB2F4', 20, 18, NULL, '220', 255911001)
GO

---------------------------------------------------------------------------------------------------------------------------------
-- 10. Sections
INSERT INTO edfi.Section 
(LastModifiedDate, CreateDate, Id, AvailableCreditConversion, AvailableCredits, AvailableCreditTypeDescriptorId, 
	EducationalEnvironmentDescriptorId, 
	InstructionLanguageDescriptorId, LocationClassroomIdentificationCode, LocationSchoolId, MediumOfInstructionDescriptorId, OfficialAttendancePeriod, PopulationServedDescriptorId, SectionName, 
	SectionTypeDescriptorId, 
	SequenceOfCourse, Discriminator, LocalCourseCode, SchoolId, SchoolYear, SectionIdentifier, SessionName) 
VALUES (GETDATE(), GETDATE(), '7FCDD9B5-DC8A-4E4D-8507-0C6F3B44920F', NULL, 1, NULL, 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%EducationalEnvironmentDescriptor' And CodeValue = 'Classroom'), 
	NULL, '220', 255911001, NULL, NULL, NULL, N'Algebra 1', 
	(SELECT DescriptorId FROM EDFI.Descriptor WHERE Namespace like '%SectionTypeDescriptor' And CodeValue = 'Attendance and Credit'), 
	1, NULL, 'ALG-1', 255911001, 2022, '25591100102Trad220ALG112011', '2021-2022 Fall Semester')
GO

INSERT INTO edfi.SectionClassPeriod 
(CreateDate, ClassPeriodName, LocalCourseCode, SchoolId, SchoolYear, SectionIdentifier, SessionName) 
VALUES (GETDATE(), '01 - Traditional', 'ALG-1', 255911001, 2022, '25591100102Trad220ALG112011', '2021-2022 Fall Semester')
GO

---------------------------------------------------------------------------------------------------------------------------------


rollback
--commit