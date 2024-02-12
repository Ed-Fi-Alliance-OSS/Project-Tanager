# Operational Concerns

These notes generally apply to any application developed as part of Project
Tanager.

## Deployment

The software built in Project Tanager will be designed for operation on-premises
or using Cloud-based managed services. However, the Ed-Fi Alliance will not
necessarily provide detailed deployment orchestration for various environments.
The Alliance developers do not have the required expertise in deployments to
support them.

The applications will be built in a (Docker) container-first fashion. A
Kubernetes topology, and potentially Docker Compose topology, will be provided
for basic testing and demonstration purposes. These artifacts might be useful
for production deployments into Kubernetes. Anyone using as such should review
carefully, particularly with respect to security concerns.

Although the application testing process will focus on the container-based
integration, these applications should be able to run on "bare metal" (or
virtual machine) without a container.

## Logging

See [Logging Policy](./LOGGING.md)

## Observability

Observability is closely related to logging, but goes beyond it. Open Telemetry
is an emerging standard for observability. The Ed-Fi development teams do not
yet have enough experience with Open Telemetry to fully understand how it will
fit in with the logging policy described above.

The following article provides additional information about Open Telemetry and
how it might be useful in Project Tanager. The article is in the Project
Meadowlark repository and references that application stack, but is equally
applicable to Project Tanager applications: [What Is Open
Telemetry?](https://github.com/Ed-Fi-Exchange-OSS/Meadowlark/blob/main/docs/design/open-telemetry/README.md)

## Security

### Transport Encryption

Those who are hosting the application are strongly encouraged to use TLS binding
at least at the gateway level. When running a container network, mutual TLS will
provide greater security in case someone is able to elevate privileges on one of
the services.

The development team will investigate use of mutual TLS within its "starter kit"
Kubernetes topology, provided that we can establish a feasible plan for managing
and trusting (where appropriate) development certificates.

### Rate Limiting

Rate limiting should be employed to limit both denial of service (DoS) attacks
and brute-force authentication attempts. While the application gateway is the
best place to apply rate limiting, the Ed-Fi Data Management Service and Ed-Fi
Configuration Service will both have built-in rate limiting capabilities to fall
back on.

### Authentication and Authorization

See [Authentication and Authorization Requirements](./AUTH.md)
