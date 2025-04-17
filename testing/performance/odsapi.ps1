# This script creates a vendor, application, and a school year, and then creates
# a number of students and their education organization associations. It uses
# the ODS/API to create the students and their associations. It uses a backdoor
# approach with a SQL script to create the vendor and application.

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

  return $response
}

$discoveryResponse = Invoke-RestMethod -Uri "http://localhost:$apiPort" `
  -Method GET

$tokenUrl = $discoveryResponse[0].urls.oauth
$dataApi = $discoveryResponse[0].urls.dataManagementApi.TrimEnd("/")

"Create an ODS/API token" | Out-Host
$credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($ClientId):$ClientSecret"))

$tokenRequest = Invoke-RestMethod -Uri $tokenUrl `
  -Method POST `
  -Headers @{ Authorization = "Basic $credentials" } `
  -ContentType "application/x-www-form-urlencoded" `
  -Body @{
  grant_type = "client_credentials"
}

$script:token = $tokenRequest.access_token

"State Education Agency" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/stateEducationAgencies" `
  -Method POST `
  -Body @{
  stateEducationAgencyId      = 255
  nameOfInstitution           = "Texas Education Agency"
  stateAbbreviationDescriptor = "uri://ed-fi.org/StateAbbreviationDescriptor#TX"
  categories                  = @(
    @{
      educationOrganizationCategoryDescriptor = "uri://ed-fi.org/EducationOrganizationCategoryDescriptor#State Education Agency"
    }
  )
} | Out-Null

"Local Education Agency" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/localEducationAgencies" `
  -Method POST `
  -Body @{
  localEducationAgencyId                 = 255901
  nameOfInstitution                      = "Grand Bend SD"
  categories                             = @(
    @{
      educationOrganizationCategoryDescriptor = "uri://ed-fi.org/EducationOrganizationCategoryDescriptor#Local Education Agency"
    }
  )
  localEducationAgencyCategoryDescriptor = "uri://ed-fi.org/LocalEducationAgencyCategoryDescriptor#Regular public school district"
  stateEducationAgencyReference          = @{
    stateEducationAgencyId = 255
  }
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
  localEducationAgencyReference   = @{localEducationAgencyId = 255901 }
} | Out-Null

"Starting performance test. This will take a while..." | Out-Host
$output = $(poetry run python odsapi.py `
  --student_count $StudentCount `
  --api_port $ApiPort `
  --client_id $ClientId `
  --client_secret $ClientSecret `
  --system ods)

$output | Out-Host

$split = $output.Split("`n")
$split[$split.Length - 1] | Add-Content -Path "performance.csv"

$(docker stats ed-fi-ods-api --no-stream --format "{{ json . }}") | Add-Content -Path "performance_stats.jsonl"
$(docker stats ed-fi-db-ods --no-stream --format "{{ json . }}") | Add-Content -Path "performance_stats.jsonl"
