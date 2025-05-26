# Large Reads Performance Testing

This directory contains scripts and resources for benchmarking the performance of large read and write operations across different database systems, including SQL Server, PostgreSQL, and OpenSearch.

## Contents

* `run.ps1` — PowerShell script to automate the setup and loading of sample data.
* `large-read.py` - Python script for benchmarking deep `limit`/`offset` type queries.
* Other supporting files and scripts for performance testing.

## Usage

1. **Prerequisites:**
   * Docker Desktop or compatible system
   * PowerShell 7+
   * Python 3.10+
   * Poetry
   * Sufficient system resources for large data loads

2. **Running the Benchmark:**
   * Open a PowerShell terminal in this directory.
   * Run the script

     ```pwsh
     ./run.ps1
     ```

     * The script will:
       * Start database containers
       * Create databases and tables
       * Insert a large number of records into each database
       * Output timing statistics for each operation
   * Install Python dependencies

     ```pwsh
     poetry install
     ```

   * Run the Python script:

     ```pwsh
     poetry run python ./large-read.py
     ```

3. **Stopping and Cleaning Up:**
   * To stop services and optionally remove volumes:

     ```pwsh
     ./run.ps1 -d         # Stop services
     ./run.ps1 -d -v      # Stop and remove volumes
     ```

## Purpose

This suite is intended for:

* Comparing bulk data load and read performance across different database backends
* Stress-testing database configurations
* Generating large datasets for downstream testing

---

For more details, see the comments in `run.ps1` or contact the Project Tanager maintainers.
