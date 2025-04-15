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

poetry run python odsapi.py --student_count $StudentCount --api_port $ApiPort --client_id $ClientId --client_secret $ClientSecret
