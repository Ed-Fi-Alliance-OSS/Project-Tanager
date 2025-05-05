# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

[CmdletBinding()]
param (
    # Stop services instead of starting them
    [Switch]
    $d,

    # Delete volumes after stopping services
    [Switch]
    $v
)

#Requires -Version 7
$ErrorActionPreference = "Stop"

if ($d) {
    if ($v) {
        Write-Output "Shutting down services and deleting volumes"
        docker compose down -v
    }
    else {
        Write-Output "Shutting down services"
        docker compose down
    }
}
else {
    $pull = "never"
    if ($p) {
        $pull = "always"
    }

    New-Item -Type Directory -Path ./.logs -Force | Out-Null

    Write-Output "Starting services"
    docker compose up -d --pull $pull

    Start-Sleep 15

    # Need to copy a file into the running container. There is a bug in podman that makes
    # it difficult to copy from Windows, so we need a work around.
    $bootstrapPath = Resolve-Path ./bootstrap.sql
    if ($PSVersionTable.Platform -eq "Win32NT") {
      if ($(docker --version) -match "Podman") {
        $fix = $bootstrapPath.Path.Replace("C:\", "c/").Replace("d:\", "d/").Replace("\", "/")
        podman machine ssh "podman cp /mnt/$fix ed-fi-db-admin:/tmp/bootstrap.sql"
      } else {
        docker cp $bootstrapPath ed-fi-db-admin:/tmp/bootstrap.sql
      }
    } else {
      docker cp $bootstrapPath ed-fi-db-admin:/tmp/bootstrap.sql
    }

    docker exec -i ed-fi-db-admin sh -c "psql -U postgres -d EdFi_Admin -f /tmp/bootstrap.sql"
    Start-Sleep 1
    docker exec -i ed-fi-db-admin sh -c "rm /tmp/bootstrap.sql"
}
