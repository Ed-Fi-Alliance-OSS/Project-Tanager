# Performance Testing Milestone 0.5.0

This is a small test suite that:

1. Creates require descriptors and education organizations, as prerequisite to
   the next steps.
2. Creates _n_ `student` documents
3. Creates _n_ corresponding `studentSchoolAssociation` documents
4. Logs the time required for the two operations above.
5. Retrieves all `n` student records by direct URL, and records the time taken.
6. Retrieves all `n` student records using "GET ALL" with limit/offset, and
   records the time taken.

Steps 2-6 are achieved with a Python script that uses async / await for
efficiency. Step 1 is orchestrated by PowerShell scripts because of some
differences in the two applications at this time.

Caution: the out-of-the-box OpenSearch deployment will throw an error if you try
to retrieve (limit+offset >= 10,000). So restrict to max 9,999 records.

## DMS Execution

1. Startup the DMS Platform from the [DMS source code
   repository](https://github.com/Ed-Fi-Alliance-OSS/Data-Management-Service).
   This command checks a specific alpha release tag from April 11.

   ```powershell
   Data-Management-Service
   # Snapshot from April 11, 2025
   git checkout dms-pre-0.4.1-alpha.0.35
   cd cd eng/docker-compose
   cp .env.example .env
   ./start-local-dms.ps1 -EnableConfig -r
   ```

2. Run `dms.ps1 -studentCount 9999` in this repository (open the file to inspect
   other optional parameters).
3. Stop and restart before executing another run, so that you have a clean
   slate:

   ```powershell
   ./start-local-dms.ps1 -EnableConfig -d -v
   ./start-local-dms.ps1 -EnableConfig
   ```

4. Copy and past the final output log, which has the timings in it, into a file
   for safekeeping.

## ODS/API Execution

This repository has a simple Docker Compose file for starting ODS/API 7.1 and
running the tests. Simply call `odsapi.ps1`; it will handle starting and
stopping the containers for you.
