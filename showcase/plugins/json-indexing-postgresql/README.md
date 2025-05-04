# Experimenting with PostgreSQL Indexes on JSON Data

What if we wanted to perform queries on the PostgreSQL database, instead of
using OpenSearch? We could implement the GET ALL and GET by Query interfaces in
the PostgreSQL backend, with a little index magic.

Great reference article: [Understanding Postgres GIN Indexes: The Good and the
Bad](https://pganalyze.com/blog/gin-index)

## GIN for Full Text Search

```sql
CREATE EXTENSION btree_gin;

CREATE INDEX IX_Document_GIN on dms.document USING gin(EdfiDoc jsonb_path_ops, ResourceName);

SELECT * FROM dms.document WHERE ResourceName = 'Student' AND EdfiDoc @> '{"studentUniqueId": "4732904"}';
```

After running a [performance test](../../../testing/performance/README.md)
loading 20,006 records into the `dms.document` table, this query took 0.011
seconds to execute. (warning: single observation).

How big is the index?

```sql
SELECT pg_size_pretty(sum(pg_relation_size(inhrelid)))
FROM pg_inherits
WHERE inhparent = 'IX_Document_GIN'::regclass;
```

Answer: 4032 kB

> [!TIP]
> Note the use of `pg_inherits` in the index size query above. This is needed
> for summing across all of the _partitioned_ indexes.

## BTree Indexing

```sql
CREATE INDEX IX_Document_Btree_StudentUniqueId on dms.document USING btree (ResourceName, (EdfiDoc ->> 'studentUniqueId'));
SELECT * FROM dms.document WHERE ResourceName = 'Student' AND EdfiDoc->>'studentUniqueId' = '4732904';
```

This query also took 0.011 seconds.

What about index size? Naturally this should be smaller than the full GIN index.

```sql
SELECT pg_size_pretty(sum(pg_relation_size(inhrelid)))
FROM pg_inherits
WHERE inhparent = 'IX_Document_Btree_StudentUniqueId'::regclass;
```

Answer: 608 kB. Creating many of these indexes would likely be expensive, so it
might not be advisable to do this for every queryable field name.

## Impact on Data Load

We can load another 20,000 records (9,999 `Student` documents and 9,999
`StudentSchoolAssociation` documents). Let's see how these indexes impact the
performance, one index at a time.

### Performance with GIN Index

Loading 20,000 more records took about 10% longer than the first data load,
without the index. This is significantly less time than loading the _first_ set
of data into the ODS/API, following the same procedure. Index size is now 7,232
kB.

### Performance with BTree Index

## Compare to OpenSearch

OpenSearch has separate indexes for each resource type.

| Index                      | Count  | Size   |
| -------------------------- | ------ | ------ |
| Student School Association | 9,999  | 4.3 mb |
| Student                    | 9,999  | 4.7 mb |
| Student School Association | 19,998 | 7.9 mb |
| Student                    | 19,998 | 8.6 mb |

Combined index size is larger than the GIN index in PostgreSQL.

## Conclusions

These two indexing approaches have real potential for providing acceptable
performance on PostgreSQL without needing OpenSearch. More work should be done
to implement this path and test with larger, more complex data sets.
