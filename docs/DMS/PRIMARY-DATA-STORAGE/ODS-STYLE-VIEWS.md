# ODS Style Views

## Rationale

Some implementations will require legacy "ODS-Style" database views or tables to support existing queries and reports. There are several ways to accomplish this. Below are some examples.

### Database Views

One approach is to construct views that exactly mimic the tables in the ODS database.

#### Descriptor

```sql
CREATE OR REPLACE VIEW dms.vw_Descriptor
AS
SELECT
 id as descriptorId
 ,edfidoc ->> 'namespace' as namespace
 ,edfidoc ->> 'codeValue' as codeValue
 ,edfidoc ->> 'shortDescription' as shortDescription
 ,edfidoc ->> 'description' as description
 ,null::bigint as PriorDescriptorId
 ,cast(edfidoc ->> 'effectiveBeginDate' as date) as effectiveBeginDate
 ,cast(edfidoc ->> 'effectiveEndDate' as date) as effectiveEndDate
 ,null as discriminator
 ,CONCAT(edfidoc ->> 'namespace', '#', edfidoc ->> 'codeValue') as uri
 ,createdat as createDate
 ,lastmodifiedat as lastModifiedDate
 ,documentUuid as id
 ,null::bigint as changeVersion
 ,null::smallint as createdByOwnershipTokenId
FROM
 dms.document
WHERE
 resourcename LIKE '%Descriptor'
```

#### BarrierToInternetAccessInResidenceDescriptor

```sql
CREATE OR REPLACE VIEW dms.vw_barrierToInternetAccessInResidenceDescriptor
AS
SELECT
 id as barrierToInternetAccessInResidenceDescriptorId
FROM
 dms.document
WHERE
 resourcename = 'BarrierToInternetAccessInResidenceDescriptor'
```

#### StudentEducationOrganizationAssociation

```sql
CREATE OR REPLACE VIEW dms.vw_studentEducationOrganizationAssociation
AS
SELECT
 seoa.edfidoc -> 'educationOrganizationReference' ->> 'educationOrganizationId' as educationOrganizationId
 ,s.id as studentUsi
 ,seoa.edfidoc ->> 'barrierToInternetAccessInResidenceDescriptorId' as barrierToInternetAccessInResidenceDescriptorId
 ,seoa.edfidoc ->> 'genderIdentity' as genderIdentity
 ,cast(seoa.edfidoc ->>'hispanicLatinoEthnicity' as boolean) as hispanicLatinoEthnicity
 ,cast(seoa.edfidoc -> 'internetAccessInResidence' as boolean) as internetAccessInResidence
 ,iatird.id as internetAccessTypeInResidenceDescriptorId
 ,ipird.id as internetPerformanceInResidenceDescriptorId
 ,lepd.id as limitedEnglishProficiencyDescriptorId
 ,seoa.edfidoc ->> 'loginId' as loginId
 ,pldad.id as primaryLearningDeviceAccessDescriptorId
 ,pldafsd.id as primaryLearningDeviceAwayFromSchoolDescriptorId
 ,pldpd.id as primaryLearningDeviceProviderDescriptorId
 ,seoa.edfidoc ->> 'profileThumbnail' as profileThumbnail
 ,sd.id as sexDescriptorId
 ,smcd.id as supporterMilitaryConnectionDescriptorId
 ,null as discriminator
 ,seoa.createdat as createDate
 ,seoa.lastmodifiedat as lastModifiedDate
 ,seoa.documentUuid as id
 ,null::bigint as changeVersion
 ,null::smallint as createdByOwnershipTokenId
FROM
 dms.document as seoa
 INNER JOIN dms.document as s
  ON s.resourcename = 'Student'
  AND s.edfidoc ->> 'studentUniqueId' = seoa.edfidoc -> 'studentReference' ->> 'studentUniqueId'
 LEFT OUTER JOIN dms.document as iatird
  ON iatird.resourcename = 'InternetAccessTypeInResidenceDescriptor'
  AND CONCAT(iatird.edfidoc ->> 'namespace', '#', iatird.edfidoc ->> 'codeValue') = seoa.edfidoc ->> 'internetAccessTypeInResidenceDescriptor'
 LEFT OUTER JOIN dms.document as ipird
  ON ipird.resourcename = 'InternetPerformanceInResidenceDescriptor'
  AND CONCAT(ipird.edfidoc ->> 'namespace', '#', ipird.edfidoc ->> 'codeValue') = seoa.edfidoc ->> 'internetPerformanceInResidenceDescriptor'
 LEFT OUTER JOIN dms.document as lepd
  ON lepd.resourcename = 'LimitedEnglishProficiencyDescriptor'
  AND CONCAT(lepd.edfidoc ->> 'namespace', '#', lepd.edfidoc ->> 'codeValue') = seoa.edfidoc ->> 'limitedEnglishProficiencyDescriptor'
 LEFT OUTER JOIN dms.document as pldad
  ON pldad.resourcename = 'PrimaryLearningDeviceAccessDescriptor'
  AND CONCAT(pldad.edfidoc ->> 'namespace', '#', pldad.edfidoc ->> 'codeValue') = seoa.edfidoc ->> 'primaryLearningDeviceAccessDescriptor'
 LEFT OUTER JOIN dms.document as pldafsd
  ON pldafsd.resourcename = 'PrimaryLearningDeviceAwayFromSchoolDescriptor'
  AND CONCAT(pldafsd.edfidoc ->> 'namespace', '#', pldafsd.edfidoc ->> 'codeValue') = seoa.edfidoc ->> 'primaryLearningDeviceAwayFromSchoolDescriptor'
 LEFT OUTER JOIN dms.document as pldpd
  ON pldpd.resourcename = 'PrimaryLearningDeviceProviderDescriptor'
  AND CONCAT(pldpd.edfidoc ->> 'namespace', '#', pldpd.edfidoc ->> 'codeValue') = seoa.edfidoc ->> 'primaryLearningDeviceProviderDescriptor'
 LEFT OUTER JOIN dms.document as sd
  ON sd.resourcename = 'SexDescriptor'
  AND CONCAT(sd.edfidoc ->> 'namespace', '#', sd.edfidoc ->> 'codeValue') = seoa.edfidoc ->> 'sexDescriptor'
 LEFT OUTER JOIN dms.document as smcd
  ON smcd.resourcename = 'SupporterMilitaryConnectionDescriptor'
  AND CONCAT(smcd.edfidoc ->> 'namespace', '#', smcd.edfidoc ->> 'codeValue') = seoa.edfidoc ->> 'supporterMilitaryConnectionDescriptor'
WHERE
 seoa.resourcename = 'StudentEducationOrganizationAssociation'
```

### StudentEducationOrganizationAssociationAddress

```sql
CREATE OR REPLACE VIEW dms.vw_studentEducationOrganizationAssociationAddress
AS
SELECT
 seoa.edfidoc -> 'educationOrganizationReference' ->> 'educationOrganizationId' as educationOrganizationId
 ,s.id as studentUsi
 ,atd.id as addressTypeDescriptorId
 ,address ->> 'city' as city
 ,address ->> 'postalCode' as postalCode
 ,sad.id as stateAbbreviationDescriptorId
 ,address ->> 'streetNumberName' as streetNumberName
 ,address ->> 'apartmentRoomSuiteNumber' as apartmentRoomSuiteNumber
 ,address ->> 'buildingSiteNumber' as buildingSiteNumber
 ,address ->> 'congressionalDistrict' as congressionalDistrict
 ,address ->> 'countyFIPSCode' as countyFIPSCode
 ,cast(address ->> 'doNotPublishIndicator' as boolean) as doNotPublishIndicator
 ,address ->> 'latitude' as latitude
 ,ld.id as localeDescriptorId
 ,address ->> 'longitude' as longitude
 ,address ->> 'nameOfCounty' as nameOfCounty
 ,seoa.createdat as createDate
FROM
 dms.document as seoa
INNER JOIN dms.document as s
  ON s.resourcename = 'Student'
  AND s.edfidoc ->> 'studentUniqueId' = seoa.edfidoc -> 'studentReference' ->> 'studentUniqueId'
, jsonb_array_elements(seoa.edfidoc -> 'addresses') as address
LEFT OUTER JOIN dms.document as atd
  ON atd.resourcename = 'AddressTypeDescriptor'
  AND CONCAT(atd.edfidoc ->> 'namespace', '#', atd.edfidoc ->> 'codeValue') = address ->> 'addressTypeDescriptor'
LEFT OUTER JOIN dms.document as sad
  ON sad.resourcename = 'StateAbbreviationDescriptor'
  AND CONCAT(sad.edfidoc ->> 'namespace', '#', sad.edfidoc ->> 'codeValue') = address ->> 'stateAbbreviationDescriptor'
LEFT OUTER JOIN dms.document as ld
  ON ld.resourcename = 'LocaleDescriptor'
  AND CONCAT(ld.edfidoc ->> 'namespace', '#', ld.edfidoc ->> 'codeValue') = address ->> 'localeDescriptor'
WHERE
 seoa.resourcename = 'StudentEducationOrganizationAssociation'
```
