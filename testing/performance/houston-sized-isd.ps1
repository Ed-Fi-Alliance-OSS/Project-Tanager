
# Houston Independent School District. HISD has 276 schools and over 194,000
# students. How long does it take to load 276 schools? Use this to confirm that
# the education organization hierarchy population process for a single large
# district does not become a problem.

param (
  [Parameter(Mandatory = $false)]
  [int]$ApiPort = 8080,

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
    -ResponseHeadersVariable responseHeaders `
    -StatusCodeVariable statusCode

  if ($statusCode -ge 400) {
    Write-Error "Request failed with status code $($statusCode): $($response.Content)"
    return $null
  }

  return $response
}

"Create a Management API token" | Out-Host
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

$vendorId = $vendorResponse.id

"Create a new application for managing education organizations" | Out-Host
$applicationResponse = Invoke-Request -Uri "http://localhost:$configPort/v2/applications" `
  -Method POST `
  -Body @{
  vendorId                 = $vendorId
  applicationName          = "Education Organizations"
  claimSetName             = "E2E-RelationshipsWithEdOrgsOnlyClaimSet"
  educationOrganizationIds = @( 255, 255901 )
}

$clientKey = $applicationResponse.key
$clientSecret = $applicationResponse.secret


$discoveryResponse = Invoke-RestMethod -Uri "http://localhost:$ApiPort" `
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


"Grade level descriptors" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/gradeLevelDescriptors" `
  -Method POST `
  -Body @{
  namespace        = "uri://ed-fi.org/GradeLevelDescriptor"
  codeValue        = "Ninth Grade"
  shortDescription = "9th Grade"
} | Out-Null

"Education organization category descriptor" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/educationOrganizationCategoryDescriptors" `
  -Method POST `
  -Body @{
  namespace        = "uri://ed-fi.org/EducationOrganizationCategoryDescriptor"
  codeValue        = "School"
  shortDescription = "School"
} | Out-Null
Invoke-Request -Uri "$dataApi/ed-fi/educationOrganizationCategoryDescriptors" `
  -Method POST `
  -Body @{
  namespace        = "uri://ed-fi.org/EducationOrganizationCategoryDescriptor"
  codeValue        = "Local Education Agency"
  shortDescription = "Local Education Agency"
} | Out-Null
Invoke-Request -Uri "$dataApi/ed-fi/educationOrganizationCategoryDescriptors" `
  -Method POST `
  -Body @{
  namespace        = "uri://ed-fi.org/EducationOrganizationCategoryDescriptor"
  codeValue        = "State Education Agency"
  shortDescription = "State Education Agency"
} | Out-Null

"Local education agency category descriptor" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/localEducationAgencyCategoryDescriptors" `
  -Method POST `
  -Body @{
  namespace        = "uri://ed-fi.org/LocalEducationAgencyCategoryDescriptor"
  codeValue        = "Regular public school district"
  shortDescription = "Regular public school district"
} | Out-Null


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


Measure-Command {
  for ($i = 0; $i -lt 2; $i++) {
    $id = [int]("255901" + $i)
    "School $id" | Out-Host
    Invoke-Request -Uri "$dataApi/ed-fi/schools" `
      -Method POST `
      -Body @{
      schoolId                        = $id
      nameOfInstitution               = "Houston $id"
      shortNameOfInstitution          = "Houston $id"
      educationOrganizationCategories = @(
        @{
          educationOrganizationCategoryDescriptor = "uri://ed-fi.org/EducationOrganizationCategoryDescriptor#School"
        }
      )
      gradeLevels                     = @(
        @{
          gradeLevelDescriptor = "uri://ed-fi.org/GradeLevelDescriptor#Ninth Grade"
        }
      )
      localEducationAgencyReference   = @{localEducationAgencyId = 255901 }
    } | Out-Null
  }
}
