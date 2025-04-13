$ErrorActionPreference = "Stop"

$studentCount = 5

# Define ports and credentials
$dmsPort = 8080
$configPort = 8081

$sysAdminId = "DmsConfigurationService"
$sysAdminSecret = "s3creT@09"

function Invoke-Request {
  param (
    [string]$Uri,
    [string]$Method,
    [hashtable]$Body
  )

  Write-Information $Uri
  Write-Information ($Body | ConvertTo-Json -Depth 10)

  $response = Invoke-RestMethod -Uri $Uri `
    -Method $Method `
    -ContentType "application/json" `
    -Headers @{ Authorization = "Bearer $script:token" }  `
    -Body ($Body | ConvertTo-Json -Depth 10) `
    -SkipHttpErrorCheck

  if ($response.StatusCode -ge 400) {
    Write-Error "Request failed with status code $($response.StatusCode): $($response.Content)"
    return $null
  }

  return $response
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

$vendorId = $vendorResponse.id

"Create a new application" | Out-Host
$applicationResponse = Invoke-Request -Uri "http://localhost:$configPort/v2/applications" `
  -Method POST `
  -Body @{
  vendorId        = $vendorId
  applicationName = "Demo application"
  claimSetName    = "E2E-NoFurtherAuthRequiredClaimSet"
}

$clientKey = $applicationResponse.key
$clientSecret = $applicationResponse.secret

# DMS demonstration
# Read the Token URL from the Discovery API
$discoveryResponse = Invoke-RestMethod -Uri "http://localhost:$dmsPort" `
  -Method GET

$tokenUrl = $discoveryResponse.urls.oauth

$dataApi = $discoveryResponse.urls.dataManagementApi

"Create a DMS token" | Out-Host
$credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($clientKey):$clientSecret"))

$dmsTokenRequest = Invoke-RestMethod -Uri $tokenUrl `
  -Method POST `
  -Headers @{ Authorization = "Basic $credentials" } `
  -ContentType "application/x-www-form-urlencoded" `
  -Body @{
  grant_type = "client_credentials"
}

$script:token = $dmsTokenRequest.access_token

"Create a school year" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/schoolYearTypes" `
  -Method POST `
  -Body @{
  schoolYear = 2024
  beginDate  = "2024-08-01"
  endDate    = "2025-05-31"
  schoolYearDescription = "2024-2025"
  currentSchoolYear = $true
}

"Grade level descriptors" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/gradeLevelDescriptors" `
  -Method POST `
  -Body @{
  namespace = "uri://ed-fi.org/GradeLevelDescriptor"
  codeValue = "Ninth grade"
  shortDescription = "9th Grade"
}

"Education organization category descriptors" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/educationOrganizationCategoryDescriptors" `
  -Method POST `
  -Body @{
  namespace = "uri://ed-fi.org/EducationOrganizationCategoryDescriptor"
  codeValue = "School"
  shortDescription = "School"
}

"Create a school" | Out-Host
Invoke-Request -Uri "$dataApi/ed-fi/schools" `
  -Method POST `
  -Body @{
  schoolId = 1
  nameOfInstitution = "Grand Bend High School"
  shortNameOfInstitution = "GBMS"
  webSite = "http://www.GBISD.edu/GBMS/"
  educationOrganizationCategories = @(
    @{
      educationOrganizationCategoryDescriptor = "uri://ed-fi.org/EducationOrganizationCategoryDescriptor#School"
    }
  )
  gradeLevels = @(
    @{
      gradeLevelDescriptor = "uri://ed-fi.org/GradeLevelDescriptor#Ninth grade"
    }
  )
}

$timed = Measure-Command {
  # Loop to create students and their education organization associations
  for ($i = 1; $i -le $studentCount; $i++) {
    $studentUniqueId = "12345678$i"

    # "Create student $i" | Out-Host
    Invoke-Request -Uri "$dataApi/ed-fi/students" `
      -Method POST `
      -Body @{
        studentUniqueId = $studentUniqueId
        firstName = "Student$i"
        lastSurname = "LastName$i"
        birthDate = "2012-01-01"
      }

    # "Create student education organization association for student $i" | Out-Host
    Invoke-Request -Uri "$dataApi/ed-fi/studentEducationOrganizationAssociations" `
      -Method POST `
      -Body @{
        studentReference = @{
          studentUniqueId = $studentUniqueId
        }
        educationOrganizationReference = @{
          educationOrganizationId = 1
        }
        beginDate = "2024-08-01"
        endDate = "2025-05-31"
      }
  }
}

"Created $studentCount students and their education organization associations in $($timed.TotalSeconds) seconds" | Out-Host
