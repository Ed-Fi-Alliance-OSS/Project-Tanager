# DMS Testing Guidelines

When contributing code to a Project Tanager project it is crucial to ensure that
the code is well tested. The Data Management Service has three types of test
projects which should be maintained as new code is added to the solution.

## Unit Tests

Unit tests exist to test small isolated units of source code. Methods where
logic is performed should be thoroughly tested with as many branches (logical
decision points) tested as possible. Classes like validators are one example of
excellent candidates for unit tests. We will mock objects and components not
directly responsible for the unit of logic we are testing.

In Project Tanager, unit test project names end with the suffix `Tests.Unit`.
Some packages our unit test projects utilize:

- [Nunit](https://nunit.org/) General .Net testing framework
- [Fakeiteasy](https://fakeiteasy.github.io/) For mocking / stubbing objects
- [Impromptuinterface](https://github.com/ekonbenefits/impromptu-interface) Wrap
  any object with static interface

## Integration Tests

Integration tests are tests that ensure different parts of a system are working
together. In Data Management System we use an integration test to test the low
level database interactions. In unit tests, database interaction is generally
mocked leaving a hole where this type of coverage is necessary, thus the need
for integration tests. Because the integration tests directly interact with a
database, you must provide a connection string. `appsettings.json` in the
integration test project will have the connection string used by the build
server, if your local connection string differs from that create an
`appsettings.test.json` (this file will be gitignored) and override the
connection string with your own.

In Project Tanager, integration tests end with the suffix `Tests.Integration`.
Some packages our integration tests projects utilize:

- [Nunit]("https://nunit.org/") General .Net testing framework
- [Fakeiteasy]("https://fakeiteasy.github.io/") For mocking / stubbing objects
- [Impromptuinterface]("https://github.com/ekonbenefits/impromptu-interface")
  Wrap any object with static interface
- [Respawn]("https://github.com/jbogard/Respawn") - Reset database between test
  runs

## End to End Tests

End to End (E2E) Tests are the most high level test meant to mimic exactly how
the application will be utilized by api clients in real world scenarios. All E2E
tests are executed directly against the API and no "backdoor" data manipulation
is performed before, during or after a test. Our E2E tests are created with the
[Reqnroll]("https://reqnroll.net/") test automation framework and are presented
in a series of `.feature` files written in the standard Gherkin syntax
(_Given-When-Then_) and should be understandable to non programmers. At run
time, each `.feature` file is processed in turn by spinning up a docker
container for the API and database and then running the scenarios in series -
always starting with the _Given_ statement at the beginning of the file.

In Project Tanager, E2E tests end with the suffix `Tests.E2E`. Some packages our
E2E tests utilize:

- [Nunit]("https://nunit.org/") General .Net testing framework
- [Reqnroll]("https://reqnroll.net/") Framework for Gherkin style automated E2WE
  tests
- [Testcontainers]("https://dotnet.testcontainers.org/") Easily create and
  destroy docker containers for testing purposes
