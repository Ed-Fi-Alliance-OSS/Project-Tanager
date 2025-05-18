import subprocess
import time
import pyodbc  # For MSSQL
import psycopg2  # For PostgreSQL
import requests  # For OpenSearch and Elasticsearch
import os
from dotenv import load_dotenv


def run_query_mssql(query):
    sqlserverPassword = os.getenv('MSSQL_SERVER', default='abcdefgh1!')
    conn = pyodbc.connect(
       f"DRIVER={{ODBC Driver 17 for SQL Server}};"
       f"SERVER=localhost;"
       f"DATABASE=TestDB;"
       f"UID=sa;"
       f"PWD={sqlserverPassword}"
       )

    try:
        cursor = conn.cursor()
        cursor.execute(query)
        results = cursor.fetchall()
        conn.commit()
        return results
    finally:
        if conn:
           conn.close()

def run_query_postgresql(query):
    postgresPassword = os.getenv('POSTGRES_SERVER',default='abcdefgh1!')
    conn = psycopg2.connect(
       f"dbname=testdb "
       f"user=postgres "
       f"password={postgresPassword} "
       f"host=localhost "
       f"port=5432"
       )

    try:
        cursor = conn.cursor()
        cursor.execute(query)
        results = cursor.fetchall()
        conn.commit()
        return results
    finally:
      if conn:
         conn.close()

def run_query_opensearch(from_offset, size):
    url = f"http://localhost:9200/testdb/_search"
    data = {
        "from": from_offset,
        "size": size
    }
    response = requests.get(url, json=data)
    return response.json()

def capture_docker_stats(container_id, output_file, create_new_file=False):
    try:
      result = subprocess.run(['docker', 'stats', '--no-stream', container_id], capture_output=True, text=True)
      header, *data = result.stdout.splitlines()
      if create_new_file:
          with open(output_file, 'w') as f:
              f.write(header + '\n')
      else:
          with open(output_file, 'a') as f:
            for line in data:
              f.write(line + '\n')
    except Exception as e:
       print(f"Error capturing stats: {e}")

def main():
    sqlserver_container_id = 'sqlserver'
    postgres_container_id = 'postgres'
    opensearch_container_id ='opensearch'
    output_file = 'docker_stats.txt'

    load_dotenv()

    queries_mssql = [
        "SELECT * FROM Records ORDER BY ID OFFSET 10000 ROWS FETCH NEXT 25 ROWS ONLY;",
        "SELECT * FROM Records ORDER BY ID OFFSET 100000 ROWS FETCH NEXT 25 ROWS ONLY;",
        "SELECT * FROM Records ORDER BY ID OFFSET 1000000 ROWS FETCH NEXT 1 ROWS ONLY;"
        "SELECT * FROM Records ORDER BY ID OFFSET 10000 ROWS FETCH NEXT 500 ROWS ONLY;",
        "SELECT * FROM Records ORDER BY ID OFFSET 100000 ROWS FETCH NEXT 500 ROWS ONLY;",
        "SELECT * FROM Records ORDER BY ID OFFSET 1000000 ROWS FETCH NEXT 1 ROWS ONLY;"
    ]

    queries_postgresql = [
        "SELECT * FROM records ORDER BY id OFFSET 10000 LIMIT 25;",
        "SELECT * FROM records ORDER BY id OFFSET 100000 LIMIT 25;",
        "SELECT * FROM records ORDER BY id OFFSET 1000000 LIMIT 1;",
        "SELECT * FROM records ORDER BY id OFFSET 10000 LIMIT 500;",
        "SELECT * FROM records ORDER BY id OFFSET 100000 LIMIT 500;",
        "SELECT * FROM records ORDER BY id OFFSET 1000000 LIMIT 1;"
    ]

    offsets_sizes_opensearch = [
        (10,25),
        (10000, 25),
        (100000, 25),
        (1000000, 1),
        (10000, 500),
        (100000, 500),
        (1000000, 1)
    ]

    capture_docker_stats(sqlserver_container_id, output_file, create_new_file=True) # Create new file and add stats header

    for query in queries_mssql:
      run_query_mssql(query)
      capture_docker_stats(sqlserver_container_id, output_file)
      time.sleep(1)

    for query in queries_postgresql:
      run_query_postgresql(query)
      capture_docker_stats(postgres_container_id, output_file)
      time.sleep(1)

    for offset, size in offsets_sizes_opensearch:
      run_query_opensearch(offset, size)
      capture_docker_stats(opensearch_container_id, output_file)
      time.sleep(1)

if __name__ == "__main__":
    main()
#900
