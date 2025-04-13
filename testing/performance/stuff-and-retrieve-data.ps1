# This script creates a vendor, application, and a school year, and then creates
# a number of students and their education organization associations. It uses
# the DMS API to create the students and their associations. It uses the Config
# Service API to create the vendor and application.
param (
  [Parameter(Mandatory = $false)]
  [int]$StudentCount = 5,

  [Parameter(Mandatory = $false)]
  [int]$DmsPort = 8080,

  [Parameter(Mandatory = $false)]
  [int]$ConfigPort = 8081,

  [Parameter(Mandatory = $false)]
  [string]$SysAdminId = "DmsConfigurationService",

  [Parameter(Mandatory = $false)]
  [string]$SysAdminSecret = "s3creT@09"
)

#Requires -Version 7
$ErrorActionPreference = "Stop"

function Invoke-Request {
  param (
    [string]$Uri,
    [string]$Method,
    [hashtable]$Body
  )

  $response = Invoke-RestMethod -Uri $Uri `
    -Method $Method `
    -ContentType "application/json" `
    -Headers @{ Authorization = "Bearer $script:token" }  `
    -Body ($Body | ConvertTo-Json -Depth 10) `
    -SkipHttpErrorCheck `
    -ResponseHeadersVariable responseHeaders

  if ($response.StatusCode -ge 400) {
    Write-Error "Request failed with status code $($response.StatusCode): $($response.Content)"
    return $null
  }

  $location = ""
  if ($null -ne $responseHeaders["Location"]) {
    $location = $responseHeaders["Location"]
  }
  return @($response, $location)
}

"Create a Config Service token" | Out-Host
$configTokenRequest = Invoke-RestMethod -Uri "http://localhost:$configPort/connect/token" `
  -Method POST `
  -ContentType "application/x-www-form-urlencoded" `
  -Body @{
  client_id     = $sysAdminId
  client_secret = $sysAdminSecret
  grant_type    = "client_credentials"
  scope         = "edfi_admin_api/full_access"
}

$script:token = $configTokenRequest.access_token

"Create a new vendor" | Out-Host
$vendorResponse = Invoke-Request -Uri "http://localhost:$configPort/v2/vendors" `
  -Method POST `
  -Body @{
  company             = "Demo Vendor $([math]::floor((Get-Random) * 10000000))"
  contactName         = "George Washington"
  contactEmailAddress = "george@example.com"
  namespacePrefixes   = "uri://ed-fi.org"
}

$vendorId = $vendorResponse[0].id

"Create a new application" | Out-Host
$applicationResponse = Invoke-Request -Uri "http://localhost:$configPort/v2/applications" `
  -Method POST `
  -Body @{
  vendorId        = $vendorId
  applicationName = "Demo application"
  claimSetName    = "E2E-NoFurtherAuthRequiredClaimSet"
}

$clientKey = $applicationResponse[0].key
$clientSecret = $applicationResponse[0].secret

# DMS demonstration
# Read the Token URL from the Discovery API
$discoveryResponse = Invoke-RestMethod -Uri "http://localhost:$dmsPort" `
  -Method GET

$tokenUrl = $discoveryResponse[0].urls.oauth

$dataApi = $discoveryResponse[0].urls.dataManagementApi

"Create a DMS token" | Out-Host
$credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($clientKey):$clientSecret"))

$dmsTokenRequest = Invoke-RestMethod -Uri $tokenUrl `
  -Method POST `
  -Headers @{ Authorization = "Basic $credentials" } `
  -ContentType "application/x-www-form-urlencoded" `
  -Body @{
  grant_type = "client_credentials"
}

$script:token = $dmsTokenRequest[0].access_token

"Create a school year" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/schoolYearTypes" `
  -Method POST `
  -Body @{
  schoolYear            = 2024
  beginDate             = "2024-08-01"
  endDate               = "2025-05-31"
  schoolYearDescription = "2024-2025"
  currentSchoolYear     = $true
} | Out-Null

"Grade level descriptors" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/gradeLevelDescriptors" `
  -Method POST `
  -Body @{
  namespace        = "uri://ed-fi.org/GradeLevelDescriptor"
  codeValue        = "Ninth grade"
  shortDescription = "9th Grade"
} | Out-Null

"Education organization category descriptors" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/educationOrganizationCategoryDescriptors" `
  -Method POST `
  -Body @{
  namespace        = "uri://ed-fi.org/EducationOrganizationCategoryDescriptor"
  codeValue        = "School"
  shortDescription = "School"
} | Out-Null

"Create a school" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/schools" `
  -Method POST `
  -Body @{
  schoolId                        = 1
  nameOfInstitution               = "Grand Bend High School"
  shortNameOfInstitution          = "GBMS"
  webSite                         = "http://www.GBISD.edu/GBMS/"
  educationOrganizationCategories = @(
    @{
      educationOrganizationCategoryDescriptor = "uri://ed-fi.org/EducationOrganizationCategoryDescriptor#School"
    }
  )
  gradeLevels                     = @(
    @{
      gradeLevelDescriptor = "uri://ed-fi.org/GradeLevelDescriptor#Ninth grade"
    }
  )
} | Out-Null

$studentIdList = [System.Collections.ArrayList]::new()

$timed = Measure-Command {
  # Loop to create students and their education organization associations
  for ($i = 1; $i -le $studentCount; $i++) {
    $studentUniqueId = "12345678$i"

    # "Create student $i" | Out-Host
    $studentResponse = Invoke-Request -Uri "$dataApi/ed-fi/students" `
      -Method POST `
      -Body @{
      studentUniqueId = $studentUniqueId
      firstName       = "Student$i"
      lastSurname     = "LastName$i"
      birthDate       = "2012-01-01"
    }

    $studentIdList.Add($studentResponse[1]) | Out-Null

    # "Create student education organization association for student $i" | Out-Host
    Invoke-Request -Uri "$dataApi/ed-fi/studentEducationOrganizationAssociations" `
      -Method POST `
      -Body @{
      studentReference               = @{
        studentUniqueId = $studentUniqueId
      }
      educationOrganizationReference = @{
        educationOrganizationId = 1
      }
      beginDate                      = "2024-08-01"
      endDate                        = "2025-05-31"
    } | Out-Null
  }
}
$times = @{
  "CreateStudents" = $timed.TotalSeconds
}
"Created $studentCount students and their education organization associations in $($timed.TotalSeconds) seconds" | Out-Host

$timed = Measure-Command {
  $studentIdList | ForEach-Object {
    # "Retrieve student $_" | Out-Host
    Invoke-Request -Uri $_ -Method GET | Out-Null
  }
}

$times["RetrieveStudents"] = $timed.TotalSeconds
"Retrieved individual student records by ID in $($timed.TotalSeconds) seconds" | Out-Host


$timed = Measure-Command {
  for ($i = 1; $i -le $studentCount; $i++) {
    $studentUniqueId = "12345678$i"

    # "Retrieve student $i" | Out-Host
    Invoke-Request -Uri "$dataApi/ed-fi/students?studentUniqueId=$studentUniqueId" `
      -Method GET | Out-Null
  }
}

$times["RetrieveStudentsByQuery"] = $timed.TotalSeconds
"Retrieved individual student records by query in $($timed.TotalSeconds) seconds" | Out-Host

$timed = Measure-Command {
  $offset = 0

  do {
    # "Retrieve students with offset $offset" | Out-Host
    $students = Invoke-Request -Uri "$dataApi/ed-fi/students?limit=500&offset=$offset" -Method GET
    # "$dataApi/ed-fi/students?limit=500&offset=$offset", $students.Count | Out-Host

    $offset += 500
  } while ($students[0].Count -gt 0)
}

$times["RetrieveAllStudents"] = $timed.TotalSeconds
"Retrieved all students in $($timed.TotalSeconds) seconds" | Out-Host

$times | ConvertTo-Csv | Out-Host
