# DMS Feature: Primary Data Storage

Braindump:

* Initially SQL Server and PostgreSQL
* Also see separate plugins discussion
* Supports all of CRUD, even if Reads may eventually go elsewhere
* Deployment decision: install tables on the fly, use separate utility, or
  hybrid approach. Example: a separate connection string is provided, deployment
  occurs on post to an endpoint, and only sys admins can access that endpoint.
* Generate schemas in MetaEd or C#? Relates closely to the item above.
* Synthetic primary / foreign keys
  * Implication for referential integrity: ODS/API can reject a `Section` (for
    example) if its `LocalCourseCode` does not exist, becuase of the natural key
    as primary key concept.
  * Tanager would need to look up the correct foreign key
  * For relational databases, think about stored procedures so that the lookup
    does not need to be pulled over the network to the API server.
* Store JSON for fast retrieval and for change data capture
  * As column or in a separate outbox table?
  * If outbox, then not valuable for fast retrieval.
  * If column, then do we need immediate updates on cascading key changes?
* Meadowlark stored a few metadata columns and the JSON. That was working well,
  though not great for report writers wanting to query a relational database.
  However, we should not focus on report writers, since we want them to use
  downstream systems. And they can use JSON Path.
* Unlike Meadowlark, we probably need one table per entity.
