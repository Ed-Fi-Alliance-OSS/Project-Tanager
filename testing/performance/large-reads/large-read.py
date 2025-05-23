import asyncio
import subprocess
import pyodbc  # For MSSQL
import psycopg2  # For PostgreSQL
import requests  # For OpenSearch and Elasticsearch
import os
import time
from dotenv import load_dotenv


async def run_query_mssql(from_offset, limit):
    query = f"SELECT * FROM Records ORDER BY ID OFFSET {from_offset} ROWS FETCH NEXT {limit} ROWS ONLY;"
    sqlserver_password = os.getenv("MSSQL_SERVER", default="abcdefgh1!")
    conn = pyodbc.connect(
        f"DRIVER={{ODBC Driver 17 for SQL Server}};"
        f"SERVER=localhost;"
        f"DATABASE=TestDB;"
        f"UID=sa;"
        f"PWD={sqlserver_password}"
    )

    try:
        cursor = conn.cursor()
        start_time = time.time()
        cursor.execute(query)
        results = cursor.fetchall()
        response_time = time.time() - start_time
        return results, response_time
    finally:
        if conn:
            conn.close()


async def run_query_postgresql(from_offset, limit):
    query = f"SELECT * FROM records ORDER BY id OFFSET {from_offset} LIMIT {limit};"
    postgres_password = os.getenv("POSTGRES_SERVER", default="abcdefgh1!")
    conn = psycopg2.connect(
        f"dbname=testdb "
        f"user=postgres "
        f"password={postgres_password} "
        f"host=localhost "
        f"port=5432"
    )

    try:
        cursor = conn.cursor()
        start_time = time.time()
        cursor.execute(query)
        results = cursor.fetchall()
        response_time = time.time() - start_time
        return results, response_time
    finally:
        if conn:
            conn.close()


async def run_query_opensearch(from_offset, size):
    url = f"http://localhost:9200/testdb/_search"
    data = {"from": from_offset, "size": size}
    start_time = time.time()
    response = requests.get(url, json=data)
    response_time = time.time() - start_time
    return response.json(), response_time


async def capture_docker_stats(container_id, stats_file, add_header=False):
    try:
        result = subprocess.run(
            ["docker", "stats", "--no-stream", container_id],
            capture_output=True,
            text=True,
        )
        header, *data = result.stdout.splitlines()
        with open(stats_file, "a") as f:
            if add_header:
                f.write(header + "\n")
            for line in data:
                f.write(line + "\n")
    except Exception as e:
        print(f"Error capturing stats for container {container_id}: {e}")


def capture_response_stats(
    stats_file, from_offset, size, response_time, add_header=False
):
    try:
        with open(stats_file, "a") as f:
            if add_header:
                f.write("from_offset,size,response_time\n")
            else:
                f.write(f"{from_offset},{size},{response_time}\n")
    except Exception as e:
        print(f"Error capturing response stats in {stats_file}: {e}")


def reset_stats(stats_file):
    try:
        with open(stats_file, "w") as f:
            f.write("")
    except Exception as e:
        print(f"Error resetting stats file: {e}")


async def execute_queries_and_capture_stats(
    container_id,
    offsets_sizes,
    query_function,
    container_stats_file,
    response_stats_file,
    times=20,
):
    reset_stats(container_stats_file)
    reset_stats(response_stats_file)
    capture_response_stats(response_stats_file, "", "", "", add_header=True)
    await capture_docker_stats(container_id, container_stats_file, add_header=True)
    tasks = []
    docker_stats_tasks = []
    for _ in range(times):
        for offset, size in offsets_sizes:
            tasks.append(query_function(offset, size))
        docker_stats_tasks.append(
            capture_docker_stats(container_id, container_stats_file)
        )

    results = await asyncio.gather(*tasks)
    await asyncio.gather(*docker_stats_tasks)
    for (offset, size), (result, response_time) in zip(offsets_sizes * times, results):
        capture_response_stats(response_stats_file, offset, size, response_time)


async def main():
    sqlserver_stats_file = "docker_stats_sqlserver.txt"
    postgres_stats_file = "docker_stats_postgres.txt"
    opensearch_stats_file = "docker_stats_opensearch.txt"

    sqlserver_response_file = "response_stats_sqlserver.csv"
    postgres_response_file = "response_stats_postgres.csv"
    opensearch_response_file = "response_stats_opensearch.csv"

    sqlserver_container_id = "sqlserver"
    postgres_container_id = "postgres"
    opensearch_container_id = "opensearch"
    run_count = 100

    offsets_sizes = [
        (10000, 1),
        (100000, 1),
        (1000000, 1),
        # (10000, 25),
        # (100000, 25),
        # (1000000, 25),
        # (10000, 500),
        # (100000, 500),
        # (1000000, 500),
    ]

    load_dotenv()

    print("Starting performance test. This will take a while...")
    print("Starting SQL Server tests")
    await execute_queries_and_capture_stats(
        sqlserver_container_id,
        offsets_sizes,
        run_query_mssql,
        sqlserver_stats_file,
        sqlserver_response_file,
        run_count,
    )

    print("Starting PostgreSQL tests")
    await execute_queries_and_capture_stats(
        postgres_container_id,
        offsets_sizes,
        run_query_postgresql,
        postgres_stats_file,
        postgres_response_file,
        run_count,
    )

    print("Starting OpenSearch tests")
    await execute_queries_and_capture_stats(
        opensearch_container_id,
        offsets_sizes,
        run_query_opensearch,
        opensearch_stats_file,
        opensearch_response_file,
        run_count,
    )


# Run the script
asyncio.run(main())
