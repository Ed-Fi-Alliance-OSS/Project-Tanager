# This script creates a vendor, application, and a school year, and then creates
# a number of students and their education organization associations. It uses
# the DMS API to create the students and their associations. It uses the Config
# Service API to create the vendor and application.
param (
  # Must be in multiples of 10
  [Parameter(Mandatory = $false)]
  [int]$StudentCount = 10,

  [Parameter(Mandatory = $false)]
  [int]$ApiPort = 8001,

  [Parameter(Mandatory = $false)]
  [string]$ClientId = "minimalKey",

  [Parameter(Mandatory = $false)]
  [string]$ClientSecret = "minimalSecret"
)

#Requires -Version 7
$ErrorActionPreference = "Stop"

if ($StudentCount % 10 -ne 0) {
  Write-Error "The `StudentCount` parameter must be in a multiple of ten."
}


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
    -StatusCodeVariable statusCode `
    -ResponseHeadersVariable responseHeaders

  if ($statusCode -ge 400) {
    Write-Error "Request failed with status code $($statusCode): $($response)"
    return $null
  }

  $location = ""
  if ($null -ne $responseHeaders["Location"]) {
    $location = $responseHeaders["Location"]
  }
  return @($response, $location)
}

# Clear existing ODS/API containers and start from scratch
Push-Location .\ods-api
try {
  ./run.ps1 -d -v
  ./run.ps1
}
finally {
  Pop-Location
}

Start-Sleep 90

# Read the Token URL from the Discovery API
$discoveryResponse = Invoke-RestMethod -Uri "http://localhost:$ApiPort" `
  -Method GET

$tokenUrl = $discoveryResponse[0].urls.oauth

$dataApi = $discoveryResponse[0].urls.dataManagementApi.Trim("/")

"Create a token" | Out-Host
$credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($ClientId):$ClientSecret"))

$tokenRequest = Invoke-RestMethod -Uri $tokenUrl `
  -Method POST `
  -Headers @{ Authorization = "Basic $credentials" } `
  -ContentType "application/x-www-form-urlencoded" `
  -Body @{
  grant_type = "client_credentials"
}

$script:token = $tokenRequest[0].access_token

"Create a Local Education Agency" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/localEducationAgencies" `
  -Method POST `
  -Body @{
  localEducationAgencyId                 = 255901
  nameOfInstitution                      = "Grand Bend SD"
  categories                             = @(
    @{ educationOrganizationCategoryDescriptor = "uri://ed-fi.org/EducationOrganizationCategoryDescriptor#Local Education Agency" }
  )
  localEducationAgencyCategoryDescriptor = "uri://ed-fi.org/LocalEducationAgencyCategoryDescriptor#Regular public school district"
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
  localEducationAgencyReference   = @{
    localEducationAgencyId = 255901
  }
} | Out-Null

$studentIdList = [System.Collections.ArrayList]::new()

$timed = Measure-Command {
  # Loop to create students and their education organization associations
  for ($i = 1; $i -le $studentCount; $i = $i + 10) {
    $studentUniqueId = "32345678$i"

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
    Invoke-Request -Uri "$dataApi/ed-fi/studentSchoolAssociations" `
      -Method POST `
      -Body @{
      studentReference          = @{
        studentUniqueId = $studentUniqueId
      }
      schoolReference           = @{
        schoolId = 1
      }
      entryDate                 = "2024-08-01"
      entryGradeLevelDescriptor = "uri://ed-fi.org/GradeLevelDescriptor#Ninth Grade"
    } | Out-Null
  }
}
$times = @{
  "CreateStudents" = $timed.TotalSeconds
}
"Created $studentCount students and their education organization associations in $($timed.TotalSeconds) seconds" | Out-Host

$timed = Measure-Command {
  $studentIdList | ForEach-Object {
    "Retrieve student $_" | Out-Host
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

    $offset += 500
  } while ($students[0].Count -gt 0)
}

$times["RetrieveAllStudents"] = $timed.TotalSeconds
"Retrieved all students in $($timed.TotalSeconds) seconds" | Out-Host

$times | ConvertTo-Csv | Out-Host
