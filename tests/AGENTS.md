# AGENTS.md for Automated Tests

## General Guidelines

- All test projects use .NET 10 and xunit v3.
- For the assertion phase of a test, always use FluentAssertions.
- Do not use libraries like Moq or NSubstitute for creating test doubles. Implement test doubles manually.
- Do not write test methods in nested classes. Test classes can have nested types, typically for types that represent
  test doubles, but test methods should always be placed in a top-level class residing directly in a C# namespace.
- Try to avoid code duplication across several test methods. Refactor code by using, for example, data-driven tests, test fixtures, or factory methods. Honor the DRY and the Single Point of Truth principles.
- Many types of Light.Results are DDD Value Objects. In the assertion phase of a test, try to create a single expected instance to be compared with the actual SUT instead of calling `Should()` several times on properties of the SUT. 

## How to Structure Tests

### Three Different Types of Tests

In general, we distinguish between three types of tests:

1. Unit Tests: these are xunit test methods that completely run in memory when executed, that is, they do not make I/O
   calls to third-party systems like databases, web services, message brokers, etc. There is one exception to this rule:
   Unit Tests are allowed to read from and write to the local file system as it is always present on the executing dev
   machine or within CI job runners. This also involves things like `WebApplicationFactory<T>`: the corresponding
   ASP.NET Core app is hosted inside the test runner process and HTTP calls are usually performed via ASP.NET Core's
   Test Server which acts in memory. When a called endpoint executed completely in memory (because database calls are
   replaced with test doubles), this is still considered a Unit Test.
2. Integration tests: these are test methods where I/O calls to third-party systems (as mentioned above) are performed.
   Typically, these third-party systems are made available with Testcontainers or with Aspire. In integration tests, it
   is allowed to replace some dependencies (to third-party systems or services) with test doubles. The defining factor
   is the existence of at least one I/O call that does not involve the local file system.
3. An End-to-End test is a modification of an integration test where test doubles are not allowed. Everything has to be
   in place as it would run in a staging or production environment. They usually test the whole chain of services and
   dependencies.

### We Follow the Test Pyramid

We follow the test pyramid where Unit Tests are the most important part of our test suite. Integration tests and
especially End-to-End tests are more complex to set up and maintain, they also take longer to run, which is why we treat
them as a second line of defense. The production code should be primarily tested via Unit Tests (happy path and
error/edge cases), integration tests and End-to-End tests should only cover the happy path and check if things work as
expected when everything is in place.

An alternative to the Test Pyramid is the Test Diamond which focuses on Integration Tests - we do not follow this
approach.

### We Prefer Sociable Unit Tests

In Sociable Unit Tests, the SUT's dependencies are usually not replaced with test doubles. Instead, the actual
production code types are used within the test (the target unit can reference other production code units).

When designing Unit Tests, first go for the type that provides the highest-level API for the test scenario. Include all
types that the high-level API depends upon. Only leave out types that perform I/O calls and replace these with test
doubles to keep the Unit Test isolated and fast.

Use Code Coverage to find out which dependencies are not covered fully by the highest-level API tests and add these as
additional tests.

An alternative to this approach is the Solitary Unit Test where each dependency is replaced with a test double. We want
to avoid this approach.

### Test Doubles

We use the Test Double types defined in the book Xunit Test Patterns by Gerard Meszaros:

- Dummy/Null Object: An object or value which is passed to the System Under Test which is irrelevant for the test
  scenario. A Dummy will not be called by the System Under Test, only forwarded to other dependencies. A Null Object
  will be called by the SUT, but the corresponding methods have no return value, the implementation of the Null Object
  is empty.
- Stub: an object which is called by the SUT, the corresponding methods have return values and the Stube returns
  preconfigured data. This data is either hard-coded or can be injected by the test or test fixture.
- Spy: an object which is called by the SUT, the corresponding methods have no return value and the Spy captures
  information about the calls. This information can then be used in the assertion phase of a test.
- Mock: a combination of a Stub and a Spy.
- Fake: this is a test double that replaces entire system (so called Dependent-On Component, DOC) like a database, a
  message broker, or a simulation. The SUT typically uses drivers which are redirected to use the Fake instead of the
  real third-party system. Thus, the SUT is not really aware that a Fake is in place.

Avoid Fakes if possible. They usually do not behave exactly as the third-party system they replace. It is usually better
to write custom test doubles which are tailored to the test scenario.
