--begin tran
------------------------------------------------------------ Masive -------------------------------------------------------------
Declare @InitialTime datetime, @TotalTime datetime
Set @InitialTime = GETDATE()

-----------------------------------------------------------------------------------------------------------------------------------
Declare @Rows int
-- Please Update the number of records that you what to insert for: Student, StudentSchoolAssociation And StudentSectionAssociation
Set @Rows = 1000
-----------------------------------------------------------------------------------------------------------------------------------

-- Declaration of some variables required during the process
Declare @FirstName nvarchar(75), @LastSurname nvarchar(75), @StudentUniqueId nvarchar(32)
Declare @Counter int, @Student int, @EntryGradeLevelDescriptor nvarchar(50)
Declare @BeginTimeStd datetime, @EndTimeStd datetime
Declare @BeginTimeStdSch datetime, @EndTimeStdSch datetime
Declare @BeginTimeStdSec datetime, @EndTimeStdSec datetime
--

-- First Number for SudentUniqueId
Set @Student = 604823 

-- One Time consulting GradeLevelDescriptor
SELECT @EntryGradeLevelDescriptor = DescriptorId FROM EDFI.Descriptor WHERE Namespace = 'uri://ed-fi.org/GradeLevelDescriptor' And CodeValue = 'Ninth grade'

Set @BeginTimeStd = GETDATE()

-- Initialize Counter for insert Students
Set @Counter = 1
WHILE (@Counter <= @Rows)
BEGIN
	Set @FirstName = 'ODS'
	Set @LastSurname = 'Test'
	Set @Student = @Student + 1
	Set @StudentUniqueId = Convert(nvarchar,@Student)

	-- 1. Students
	INSERT INTO edfi.Student 
	(LastModifiedDate, CreateDate, Id, BirthCity, BirthCountryDescriptorId, BirthDate, BirthInternationalProvince, BirthSexDescriptorId, BirthStateAbbreviationDescriptorId, 
		CitizenshipStatusDescriptorId, DateEnteredUS, FirstName, GenerationCodeSuffix, LastSurname, MaidenName, MiddleName, MultipleBirthStatus, PersonalTitlePrefix, PersonId, 
		PreferredFirstName, PreferredLastSurname, SourceSystemDescriptorId, StudentUniqueId, Discriminator) 
	VALUES (GETDATE(), GETDATE(), NEWID(), NULL, NULL, '2010-01-13', NULL, NULL, NULL, NULL, NULL, 
		@FirstName, NULL, @LastSurname, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, @StudentUniqueId, NULL)

	SET @Counter  = @Counter  + 1
END
Set @EndTimeStd = GETDATE()

Declare @StudentUSI int

-- 2. StudentSchoolAssociation
Set @BeginTimeStdSch = GETDATE()

DECLARE studentSchoolAssociation_cursor CURSOR FOR
SELECT StudentUSI FROM edfi.Student
OPEN studentSchoolAssociation_cursor  
FETCH NEXT FROM studentSchoolAssociation_cursor INTO @StudentUSI 

WHILE @@FETCH_STATUS = 0  
BEGIN	
	INSERT INTO edfi.StudentSchoolAssociation 
	(LastModifiedDate, CreateDate, Id, CalendarCode, ClassOfSchoolYear, EducationOrganizationId, EmployedWhileEnrolled, EnrollmentTypeDescriptorId, 
		EntryGradeLevelDescriptorId, 
		EntryGradeLevelReasonDescriptorId, EntryTypeDescriptorId, ExitWithdrawDate, ExitWithdrawTypeDescriptorId, FullTimeEquivalency, GraduationPlanTypeDescriptorId, 
		GraduationSchoolYear, NextYearGradeLevelDescriptorId, NextYearSchoolId, PrimarySchool, RepeatGradeIndicator, ResidencyStatusDescriptorId, SchoolChoice, 
		SchoolChoiceBasisDescriptorId, SchoolChoiceTransfer, SchoolYear, TermCompletionIndicator, Discriminator, EntryDate, SchoolId, StudentUSI) 		
	VALUES (GETDATE(), GETDATE(), NEWID(), NULL, NULL, NULL, NULL, NULL, 
		@EntryGradeLevelDescriptor, 
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, '2021-08-23', 255911001, @StudentUSI)
		
	FETCH NEXT FROM studentSchoolAssociation_cursor INTO @StudentUSI 
END 

CLOSE studentSchoolAssociation_cursor  
DEALLOCATE studentSchoolAssociation_cursor

Set @EndTimeStdSch = GETDATE()



-- 3. StudentSectionAssociations
Set @BeginTimeStdSec = GETDATE()

DECLARE studentSectionAssociations_cursor CURSOR FOR
SELECT StudentUSI FROM edfi.Student
OPEN studentSectionAssociations_cursor  
FETCH NEXT FROM studentSectionAssociations_cursor INTO @StudentUSI 

WHILE @@FETCH_STATUS = 0  
BEGIN
	INSERT INTO edfi.StudentSectionAssociation 
	(LastModifiedDate, CreateDate, Id, AttemptStatusDescriptorId, EndDate, HomeroomIndicator, RepeatIdentifierDescriptorId, TeacherStudentDataLinkExclusion, Discriminator, BeginDate, 
		LocalCourseCode, SchoolId, SchoolYear, SectionIdentifier, SessionName, StudentUSI) 
	VALUES (GETDATE(), GETDATE(), NEWID(), NULL, '2021-12-17', 0, NULL, NULL, NULL, '2021-08-23', 'ALG-1', 255911001, 2022, '25591100102Trad220ALG112011', 
		'2021-2022 Fall Semester', @StudentUSI)
		
	FETCH NEXT FROM studentSectionAssociations_cursor INTO @StudentUSI 
END 

CLOSE studentSectionAssociations_cursor  
DEALLOCATE studentSectionAssociations_cursor

Set @EndTimeStdSec = GETDATE()

----------------------------------------------------------

Select Count(1) as Students, @BeginTimeStd, @EndTimeStd, (@EndTimeStd - @BeginTimeStd) From edfi.Student
Select Count(1) as StudentSectionAssociations, @BeginTimeStdSch, @EndTimeStdSch, (@EndTimeStdSch - @BeginTimeStdSch) From edfi.StudentSchoolAssociation
Select Count(1) as StudentSchoolAssociations, @BeginTimeStdSec, @EndTimeStdSec, (@EndTimeStdSec - @BeginTimeStdSec) From edfi.StudentSectionAssociation


Set @TotalTime = GETDATE()
Select 'TotalTime', (@TotalTime - @InitialTime)

--Select * From edfi.Student

---------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------

DECLARE @tmpTableSizes TABLE
(
    tableName    VARCHAR(100),
    numberofRows VARCHAR(100),
    reservedSize VARCHAR(50),
    dataSize     VARCHAR(50),
    indexSize    VARCHAR(50),
    unusedSize   VARCHAR(50)
)

INSERT @tmpTableSizes 
    EXEC sp_MSforeachtable @command1="EXEC sp_spaceused '?'"

SELECT
    tableName,
    CAST(numberofRows AS INT)                              'numberOfRows',
    CAST(LEFT(reservedSize, LEN(reservedSize) - 3) AS INT) 'reservedSize KB',
    CAST(LEFT(dataSize, LEN(dataSize) - 3) AS INT)         'dataSize KB',
    CAST(LEFT(indexSize, LEN(indexSize) - 3) AS INT)       'indexSize KB',
    CAST(LEFT(unusedSize, LEN(unusedSize) - 3) AS INT)     'unusedSize KB'
    FROM
        @tmpTableSizes
	Where tableName  IN ('Student','StudentSectionAssociation','StudentSchoolAssociation') And numberofRows > 0
    ORDER BY
        [reservedSize KB] DESC


-- rollback
