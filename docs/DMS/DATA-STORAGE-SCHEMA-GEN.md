# DMS Feature: Data Storage Schema Generation and Deployment

## Problems to solve

### Ease of DB bootstrapping

We would like for DB schema creation to be either a zero-step or at most one-step process. Meadowlark was
designed with a zero-step process, meaning that if the database did not exist it was created automatically.
This was of major benefit both for testing and onboarding new users.

### Responsibility for and time of schema generation

We need to decide which parts of the Tanager system will have the responsibility of creating the database
schema, and at what stage of the deployment lifecycle generation will occur. The major system division is
between MetaEd, which performs pre-processing for Tanager, and Tanager itself. Example lifecycle stages
include software development time, pre-processing time, and runtime.

We expect there to be three categories of tables in a Tanager relational datastore. The first is the core set
of three tables that are always used regardless of configuration. The shape of these are known at software
development time and do not need to be generated.

The second category is the set of query tables, which are optionally installed for Tanager deployments that
choose not to use a search engine for queries. The shape of these tables are specific to each resource, and so
must be generated either in a pre-process like the ApiSchema.json file or at runtime.

The final category is the set of security tables. Detailed security design is still forthcoming, but we expect
these tables to also be shaped per resource and so be generated. It is possible that these need to be
generated at runtime, as security configuration choices at deployment time might have effects that cannot be
determined at pre-processing time.

### Security

Database and database table creation usually requires elevated database privileges. Since these privileges are
only needed on a first run of Tanager, it would be a violation of the principal of least privilege for Tanager
to always have that much power. Our design should limit Tanager database privileges during normal operation.

## Implementation

### Core tables

The structure of these tables will be deeply embedded in the backend datastore code and do not need to be
generated. It makes sense that these SQL scripts are directly bundled with the backend code.

### Query tables

The structure of these tables needs to be generated, and will be known at pre-processing time. MetaEd can
easily generate these scripts alongside the ApiSchema.json for bundling in a NuGet package.

### Security tables

The structure of these tables needs to be generated. Further design work is need to determine whether they can
be created by MetaEd at pre-processing time. If possible, this should be a design goal for consistency and
simplification of the Tanager codebase.

### Bootstrapping

We should implement bootstrapping the database schema as a separate library and allow for it to be called by
either Tanager directly or a standalone CLI. Tanager will have a opt-in "create if not exists" flag where it
can run the bootstrapping library if needed. This would provide a zero-step process for testing and other
non-production environments, and would of course require Tanager to be provided with a database connection
string with elevated database privileges.

The standalone CLI would be used to provide a one-step process to bootstrap sensitive environments.
