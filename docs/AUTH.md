# Authentication and Authorization Requirements

All HTTP requests to Ed-Fi services, except for endpoints from the Ed-Fi
Discovery API, must be authenticated using an OAuth 2.0 (bearer) access token
via the client credentials flow. That token should be a JSON Web Token (JWT), so
that it can encode basic claim information.

Various authorization schemes will be applicable depending on the API being
accessed, and will be described in more detail in that application's
documentation.

* [Authorization in the Data Management Service](./DMS/DMS-AUTH.md)
* [Authorization in the Configuration Service](./CS/CS-AUTH.md)

## Multiplicity of Providers

This project aims to support a multiplicity of Identity Providers (IdP's), so
that platform hosts may choose from among available managed services. Through
use of various OAuth 2 specifications, the application code for accepting and
inspecting bearer tokens should be interoperable with any platform. However,
each platform has its own unique client management system and its own style of
supporting custom claims.

To that end, the Data Management Service and the Configuration Service will need
to have customized translation layers: a Mapper class that can translate between
an IdP's idiom and an internal token representation, and a Gateway class for
interacting with the remote IdP when managing client credentials. (Also see:
[Plugin Architecture](./PLUGIN.md)).

Initially, the project will develop support for using
[Keycloak](https://www.keycloak.org/) as the OAuth Identity Provider.
