# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ApiPublisher/ApiPublisher.csproj ApiPublisher/
RUN dotnet restore ApiPublisher/ApiPublisher.csproj

# Copy source code and build
COPY ApiPublisher/ ApiPublisher/
RUN dotnet build ApiPublisher/ApiPublisher.csproj -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish ApiPublisher/ApiPublisher.csproj -c Release -o /app/publish

# Final stage - runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ApiPublisher.dll"]
