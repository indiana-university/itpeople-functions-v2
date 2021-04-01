# IT People Functions V2

IT People serverless APIs and Tasks (Version 2)

## Requirements

* [.Net 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1)
* [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools) >= 3.0.3160
* [Docker Desktop](https://www.docker.com/products/docker-desktop)

## Getting Started

To build the project, install the requirements (above), clone this repo and run:

```
dotnet build
```

To execute unit and integration tests, run:

```
dotnet test
```

## Structure

### API

The IT People Web API supports user interaction through the [frontend application](https://github.com/indiana-university/itpeople-app) and server-to-server interaction for other IU consumers. Most requests require authentication through the UITS UAA. Refer to the [OpenAPI documentation](https://api.itpeople.iu.edu) for information on specific endpoints. 

### Tasks

Scheduled tasks keep IT People up-to-date in sync with other systems around IU.

#### People

Directory information (job title, contact info) is regularly pulled from the [IMS Profile API](https://prs.apps.iu.edu/docs/index.html).

#### Buildings

Campus building information (name, code, location) is regularly pulled from a canonical list maintined by IU Facilities.

#### Tools

Tools assigned to unit members in IT People are regularly pushed to their respective AD groups.

## Caveats
### Contract Tests on Windows
Contract tests are using [Pact-Net](https://github.com/pact-foundation/pact-net) which is built using underlying Ruby Gem libraries.  These particular libraries encounter problems with some Windows file paths.
* When the path to the `bin` folder is more than ~260 characters the contract tests will fail to run.
* When the path contains any spaces the tests will fail to run.
The first error can be mitigated by enabling [long paths](https://github.com/pact-foundation/pact-node/blob/master/README.md#enable-long-paths) in the registry.  