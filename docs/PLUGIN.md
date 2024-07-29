# Plugin Architecture

Where these application need to interact with external services, the Gateway
code for those services will be in custom C# assemblies with a facade that
implements a shared interface. The custom assembly will utilize C# [plugin
support](
https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)
so that different plugins can be supplied at runtime without having to recompile
the application.

Examples of assemblies that should be plugin aware:

* Backend support for different data stores (PostgreSQL, Microsoft SQL Server, Opensearch, etc.)
* Identity Provider token mapper and client management
  [gateway](https://martinfowler.com/articles/gateway-pattern.html).

## Plugin Detection

TBD
