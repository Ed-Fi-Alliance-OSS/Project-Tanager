# Operational Concerns

> [!WARNING]
> Initial notes, subject to full rewrite.

### Deployment

The software built in Project Tanager will be designed for operation on-premises
or using Cloud-based managed services. The applications will be built in a
(Docker) container-first fashion, with a basic Kubernetes topology. These
services could also run on "bare" (virtual) metal, but the application testing
process will focus on the container-based integration.

### Logging and Observability

Logging should be a first class concern, with all applications using a good
logging framework. Log entry levels should follow clear guidance that helps the
system administrator know how and when to react:

* `FATAL`: the system cannot start or operate properly. Demands immediate attention.
* `ERROR`: something is fundamentally wrong _at the system level_, needs to be
  fixed, but the system is still able to operate in most situations. User errors
  should not be logged with `ERROR`.
* `WARNING` / `WARN`: something unexpected and/or undesirable occurred that
  the administrator may want to keep an eye on, as it could indicate a bigger
  problem in the long run. Typically used only on system issues, not user
  errors.
* `INFORMATION` / `INFO`: information about function entry points, application
  startup, etc. Generally: do not log function arguments.
* `DEBUG` / `DBG`: additional detail beyond what is provided at the `INFO`
  level, for example function arguments (so long as they do not contain
  privileged information).

Wherever a log entry relates to the processing of a specific HTTP request, that
entry should contain a correlation or trace identifier, that passes through the
code along with the HTTP request.

Open Telemetry

### Authorization

Beyond OAuth / Open ID...
