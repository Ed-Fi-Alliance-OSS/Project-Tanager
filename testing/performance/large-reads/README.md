# Large Reads Performance Testing

This directory contains scripts and resources for benchmarking the performance of large read and write operations across different database systems, including SQL Server, PostgreSQL, and OpenSearch.

## Contents

* `run.ps1` — PowerShell script to automate the setup, data loading, and benchmarking of large-scale read/write operations for supported databases.
* Other supporting files and scripts for performance testing.

## Usage

1. **Prerequisites:**
   * Docker and Docker Compose installed
   * PowerShell 7+
   * Sufficient system resources for large data loads

2. **Running the Benchmark:**
   * Open a PowerShell terminal in this directory.
   * Run the script:

     ```pwsh
     ./run.ps1
     ```

   * The script will:
     * Start database containers
     * Create databases and tables
     * Insert a large number of records into each database
     * Output timing statistics for each operation

3. **Stopping and Cleaning Up:**
   * To stop services and optionally remove volumes:

     ```pwsh
     ./run.ps1 -d         # Stop services
     ./run.ps1 -d -v      # Stop and remove volumes
     ```

## Customization

* You can adjust the number of records and batch sizes by editing variables at the top of `run.ps1`.

* The script can be extended to support additional databases or custom data payloads.

## Purpose

This suite is intended for:

* Comparing bulk data load and read performance across different database backends
* Stress-testing database configurations
* Generating large datasets for downstream testing

---

For more details, see the comments in `run.ps1` or contact the Project Tanager maintainers.
