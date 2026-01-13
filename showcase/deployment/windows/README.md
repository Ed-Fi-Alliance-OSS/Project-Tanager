# On-premises Deployment in Windows Server

This section describes how to set up DMS in a Windows environment using native binaries (without Docker or Linux virtualization).
The instructions below deploy to 3 separate servers (PostgreSQL, IIS, and Kafka), but it's also possible to deploy them to a single server.

> [!WARNING]
> As a proof of concept, this will not be a well-secured system. Every service
> is capable of certificate-based encryption, but the configuration steps are
> outside the scope of the article.

## Setting up the DB server (PostgreSQL)
1) Install using the PostgreSQL [installer](https://docs.ed-fi.org/reference/ods-api/getting-started/binary-installation/postgresql-installation-notes/). CMS/DMS are compatible with PostgreSQL versions 16 and 18.
2) Ensure that `psql` is available in the path by running `psql --version` in a terminal window.
3) Install PowerShell v7 by following [these instructions](https://learn.microsoft.com/en-us/powershell/scripting/install/install-powershell-on-windows?view=powershell-7.5). PowerShell v7 is needed by the `setup-openiddict.ps1` script that we'll use later.
4) Set `wal_level = logical` in `postgresql.conf`, and then restart PostgreSQL. This step is only needed if you plan to use Kafka streaming.
5) Manually create an empty DB named `edfi_configurationservice` (you could use PgAdmin).

## Setting up the application server (IIS)
1) Enable IIS. CMS/DMS are compatible with Windows Server 2022, Windows Server 2025, and Windows 11.
2) Download and install .net 10 Hosting Bundle [from here](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
3) Create the `C:\inetpub\edfi\cms` and `C:\inetpub\edfi\dms` folders.
4) In a computer with .net 10 **SDK** (or with Visual Studio installed), follow these steps:
    1) Navigate to `Data-Management-Service\src\config\frontend\EdFi.DmsConfigurationService.Frontend.AspNetCore` and run `dotnet publish`. This will publish CMS into a directory.
    2) Copy the published files (usually located in `Data-Management-Service\src\config\frontend\EdFi.DmsConfigurationService.Frontend.AspNetCore\bin\Release\net10.0\publish`) and paste them in the `C:\inetpub\edfi\cms` folder in the IIS server.
    3) Similarly, navigate to `Data-Management-Service\src\dms\frontend\EdFi.DataManagementService.Frontend.AspNetCore` and run `dotnet publish`. This will publish DMS into a directory.
    4) Copy the published files (usually located in `Data-Management-Service\src\dms\frontend\EdFi.DataManagementService.Frontend.AspNetCore\bin\Release\net10.0\publish`) and paste them in the `C:\inetpub\edfi\dms` folder in the IIS server.
5) Open IIS.
6) Right-click on the `Sites` folder, click on `Add Website`, set `edfi` as the Site Name, select the `C:\inetpub\edfi` folder, and click on `OK`.
7) Right-click on `Application Pools` and add an Application Pool named `cms`. Repeat this step to add an Application Pool named `dms`.
8) Expand the `edfi` site, right-click on the `cms` folder, click on `Convert to Application`, select the `cms` Application Pool, and click on `OK`. Repeat this step for the `dms` folder, select the `dms` Application Pool.

## Configure DMS and CMS
If you browse the newly created sites, you will see that CMS and DMS fail to start. This is because we still need to make a few configuration changes:

### In the DB server:
1) Download CMS/DMS's code by cloning (or downloading as .zip) [the repository](https://github.com/Ed-Fi-Alliance-OSS/Data-Management-Service).
2) In PowerShell v7, open the `Data-Management-Service\eng\docker-compose` directory and execute `./setup-openiddict.ps1 -InitDb -ConnectionString "host=localhost;port=5432;username=postgres;password=;database=edfi_configurationservice;Application Name=CMS" -EncryptionKey "QWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXo0NTY3ODkwMTIz" -EnvironmentFile $null -PostgresContainerName $null`. Change the `EncryptionKey` to a different, random string and take note of it. This command generates a 2048-bit RSA key pair for JWT signing and stores it in the `dmscs.OpenIddictKey` table, together with the `EncryptionKey`.

### In the IIS server:
1) Go to `C:\inetpub\edfi\cms` and edit the `appsettings.json`.
2) Set `AppSettings.DeployDatabaseOnStartup` to `true` so that the CMS tables get created when it starts.
3) Set `AppSettings.PathBase` to `"/cms"`.
4) Set `IdentitySettings.Authority` to `http://localhost/cms`.
5) Update `IdentitySettings.EncryptionKey` with the encryption key that you used in the `setup-openiddict.ps1` script above.
6) Set a random `IdentitySettings.ClientSecret` and take note of it.
7) Update `DatabaseSettings.DatabaseConnection` with the connection string of the CMS database.
8) Update `Serilog.WriteTo.Args.path` to the folder where logs should be stored.
9) In IIS, restart the `cms` Application Pool so that these changes take effect.
10) Open `http://localhost/cms` in a browser.
11) Open `cms` logs and verify that it started successfully.

### In the DB server:
1) At this point, CMS's tables should have been automatically created.
2) In PowerShell v7, open the `Data-Management-Service\eng\docker-compose` directory and execute `./setup-openiddict.ps1 -InsertData -NewClientId "DmsConfigurationService" -NewClientName "DMS Configuration Service" -NewClientSecret "s3creT@09" -ClientScopeName "edfi_admin_api/full_access" -ConnectionString "host=localhost;port=5435;username=postgres;password=abcdefgh1!;database=edfi_configurationservice;Application Name=CMS" -HashIterations 210000 -EnvironmentFile $null -PostgresContainerName $null`. This command creates the client that CMS will use to authenticate with OpenIddict, set the `NewClientSecret` to the value you set for `IdentitySettings.ClientSecret` in CMS's `appsettings.json`.
3) Execute `./setup-openiddict.ps1 -InsertData -NewClientId "CMSReadOnlyAccess" -NewClientName "CMS ReadOnly Access" -NewClientSecret "s3creT@09" -ClientScopeName "edfi_admin_api/readonly_access" -ConnectionString "host=localhost;port=5435;username=postgres;password=abcdefgh1!;database=edfi_configurationservice;Application Name=CMS" -HashIterations 210000 -EnvironmentFile $null -PostgresContainerName $null`. This command creates the client that DMS will use to authenticate with CMS, feel free to change the `NewClientSecret` and take note of it.

### In your develoment machine:
We will use CMS API to create an Instance, a Vendor and an Application. First, we have to generate a CMS token.

#### Open Postman or a similar tool and execute
```
POST <CMS base path>/connect/token
x-www-form-urlencoded request parameters:
  client_id: DmsConfigurationService
  client_secret: s3creT@09 <replace with the NewClientSecret that you used above>
  grant_type: client_credentials
  scope: edfi_admin_api/full_access
```
Take note of the returned token.

Then execute:
```
POST <CMS base path>/v2/dmsInstances
request JSON:
{
    "instanceType": "Local",
    "instanceName": "Local DMS Instance 1",
    "connectionString": "host=<DB server IP or Hostname>;port=5432;username=postgres;password=abcdefgh1!;database=edfi_datamanagementservice;Application Name=DMS"
}

Initialize the `<DB server IP or Hostname>` placeholder, and specify the CMS token as `Bearer Token`.
```
For this example, we will use *topic-per-instance* architecture where each instance publishes to its own dedicated Kafka topic.

Then execute:
```
POST `<CMS base path>/v2/vendors`
request JSON:
{
    "company": "Test Vendor",
    "contactName": "Test",
    "contactEmailAddress": "test@gmail.com",
    "namespacePrefixes": "uri://ed-fi.org,uri://gbisd.edu,uri://tpdm.ed-fi.org"
}

Specify the CMS token as `Bearer Token`.
```

Then execute:
```
POST `<CMS base path>/v2/applications`
request JSON:
{
  "vendorId": 1,
  "applicationName": "Test",
  "claimSetName": "EdFiSandbox",
  "educationOrganizationIds": [255901],
  "dmsInstanceIds":[1]
}
Specify the CMS token as `Bearer Token`.
```
Take note of the returned Client ID and Client Secret

### In the IIS server:
1) Go to `C:\inetpub\edfi\dms` and edit the `appsettings.json`.
2) Set `AppSettings.DeployDatabaseOnStartup` to `true` so that the DMS tables get created when it starts.
3) Set `AppSettings.PathBase` to `"/dms"`.
4) Set `IdentitySettings.ClientSecret` to the value you set when creating DMS's client.
5) Replace all occurrences of `http://localhost:5126` with `http://localhost/cms`.
6) Update `Serilog.WriteTo.Args.path` to the folder where logs should be stored.
7) In IIS, restart the `dms` Application Pool so that these changes take effect.
8) Open `http://localhost/dms` in a browser.
9) Open `dms` logs and verify that it started successfully.

### In your develoment machine:
Let's test that DMS is working as expected by calling the `Students` endpoint, which should return empty. First we have to generate a DMS token.
#### Open Postman or a similar tool and execute
```
POST `<DMS base path>/oauth/token`
request JSON:
{
    "grant_type":"client_credentials"
}

Set `Basic Authorization` and specify the Client ID and Client Secret (returned from the Applications endpoint above).
```
Take note of the returned token.

Then execute:
```
GET `<DMS base path>/data/ed-fi/students`
Specify the DMS token as `Bearer Token`.
```

## Optional: Setting up the streaming server (Kafka)
Kafka can be installed on either Linux or Windows; however, consider that hosting it on Windows is discouraged per [Apache Kafka's documentation](https://kafka.apache.org/41/operations/hardware-and-os/#os):
> We have seen a few issues running on Windows and Windows is not currently a well supported platform ...

For this example, we will install both Kafka and Kafka Connect on the same server, in standalone mode.

1) Download and install Java runtime [from here](https://adoptium.net/temurin/releases?version=17). The minimum supported version is 17.
2) Set up kafka by following its [quickstart guide](https://kafka.apache.org/quickstart/). If installing on Windows, notice that the bash scripts have equivalent `.bat` scripts.
3) Set up Kafka Connect by following [this guide](https://kafka.apache.org/41/kafka-connect/user-guide/).
4) Set up Debezium by following [this guide](https://debezium.io/documentation/reference/stable/connectors/postgresql.html#postgresql-deployment).
5) Download and install the `Expandjsonsmt` SMT plugin [from here](https://github.com/RedHatInsights/expandjsonsmt/releases/tag/0.0.7), unzip the JAR into your `plugin.path` folder.
6) Restart Kafka Connect so that the plugins get loaded.
7) On the DB server, execute this SQL for the `edfi_datamanagementservice` database: `CREATE PUBLICATION to_debezium_instance_1 FOR TABLE dms.document, dms.educationorganizationhierarchytermslookup;`.
8) Modify the connector template located at `Data-Management-Service\eng\docker-compose\instance_connector_template.json` to set the next placeholders:
    1) `{{INSTANCE_ID}}` set it to `1`
    2) `{{DATABASE_NAME}}` set it to `edfi_datamanagementservice`
    3) `{{POSTGRES_PASSWORD}}` set it to the database's password 
    4) `database.hostname` set it to the DB server IP or Hostname
9) Open Postman or a similar tool and execute
```
      POST `<Kafka Connect base path>/connectors`
      Copy and paste the updated `instance_connector_template.json` as the request's JSON
      No authorization is required for this endpoint
```
10) Go to Kafka's download directory and execute `./bin/kafka-console-consumer.sh --bootstrap-server localhost:9092 --topic edfi.dms.1.document --from-beginning`. This will subscribe and show you the messages on the `edfi.dms.1.document` topic. Try posting a resource in DMS to get a test message.
11) Optionally, you can install [kafka-ui](https://github.com/provectus/kafka-ui), which is a more comprehensive utility for looking at messages in Kafka topics.
