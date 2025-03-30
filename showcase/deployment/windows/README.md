# On-premises Deployment in Windows Server

## Objective

Layout the design of an on-premises deployment of the Data Management Service
Platform in a Windows Server environment.

## Technical Requirements

* No Docker.
* Ensure that all required systems have a stable Windows installation.
* Provide pointers to service installation instructions, rather than provide
  detailed instructions here.
* Provide detailed instructions and/or scripts for installing the DMS and DMS
  Configuration Service in IIS.
* Provide instructions on app settings configuration, including logging to file.
* And, provide introductory notes for next steps toward securing the system.

## Context

> [!INFO]
> TODO: C4 context diagram as a reminder of what systems we are deploying.

## Containers

> [!INFO]
> TODO: C4 container diagram(s) showing more detail.

## Deployment

> [!INFO]
> TODO: C4 deployment diagram(s). Are all three levels truly useful in this
> situation?

## Installation

> [!INFO]
> TODO: links to install the following services in Windows:
>
> * Keycloak (including external database)
> * PostgreSQL
> * Kafka, Kafka UI
> * OpenSearch, OpenSearch Dashboard

## Configuration

> [!INFO]
> TODO: appsettings. Anything at the service level?

## Security

> [!INFO]
> TODO: probably won't (shouldn't?) be able to provide detailed guidance here.
> At minimum, provide tips on what needs to be considered (TLS! Certificate
> management. Encryption at rest?).
