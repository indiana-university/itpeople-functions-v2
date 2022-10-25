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

## Testing locally with Docker
### Dev Database Setup
Create a Postgres database container to use.  This should be independent of the Postgres container used by the integration tests, with its own port.
```
docker run --name itepeople-db-dev -e "POSTGRES_USER=SA" -e "POSTGRES_PASSWORD=abcd1234@" -p 5434:5432 -d postgres:11.7-alpine
```

This gives us a connection string for our new container of 
```
Server=localhost;Port=5434;Database=ItPeople;User Id=SA;Password=abcd1234@
```

### Local Function App (API)
Ensure that your `src/API/local.settings.json` file contains the following properties:
```json
{
    "Values": {
      "FUNCTIONS_WORKER_RUNTIME": "dotnet",
      "AzureWebJobsStorage": "",
      "AzureWebJobsDisableHomepage": "true",
      "DatabaseConnectionString": "Server=localhost;Port=5434;Database=ItPeople;User Id=SA;Password=abcd1234@;",
      "JwtPublicKey":"-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAjU8NG3DQtS84VF52Azk3VM7uB5aGxSofjlv5BWGCu0285MCMwx62y7n1WYdkRPJkADGJUNST4Ux3kkmgj3djn6a10p4FWXGSPBH9V4GaHfMevlDc1o2CfZOAuLT5/FPF+H2XOnSQT6H6cTVU8Yp0iy2Fm21dk3JEDwred9QDpuegjTG8F/2WS403Qv4s5Nq5ATcHvKPQOBHsnXc2LUH7Mu09g02ro4aXiF2h5ytRsQDVCN3hkzMgR6O06XjUs57bwtmwMSXwhH1u4cexdhOgQcGZ2OH+X9uYjfAAluT8OkDTFMT32OZUdB6qTHZnR91tf5xe/oPc/BEL9TSsWk+O7wIDAQAB\n-----END PUBLIC KEY-----",
      "OAuthClientId": "<ClientId from IT People UAA Stage OAuth in LastPass>",
      "OAuthClientSecret": "<Secret from IT People UAA Stage OAuth in LastPass>",
      "OAuthTokenUrl": "https://apps-test.iu.edu/uaa-stg/oauth/token",
      "OAuthRedirectUrl": "https://localhost:5001/signin",
      "AdQueryUser":"opsbot",
      "AdQueryPassword":"<opsbot password from LastPass>"
    },
    "Host": {
      "CORS": "*"
    }
}
```

The most important things to note:
* `DatabaseConnectionString` is for the new container, with the distinct port, that we created above.
* `JwtPublicKey` is the public key for the UAA Oauth test environment, not the key we use for integration tests. **NB**: This value should be all on one line.
* `OAuthClientId`, `OAuthTokenUrl`, are `AdQueryUser` all set to the values we'd expect to use in the test environment.
* `OAuthRedirectUrl` points to the SignIn URL for your local `src/Web` project runs at.
* `Host` You need this whole section to avoid CORS headaches.

Start the function app by running `func start --build` in the `src/API` directory. You should get a result like:
```
Azure Functions Core Tools
Core Tools Version:       3.0.4806 Commit hash: N/A  (64-bit)
Function Runtime Version: 3.13.1.0

[2022-10-25T20:15:28.620Z] Found /home/jfrancis/Documents/projects/itpeople/src/API/API.csproj. Using for user secrets file configuration.
[Startup] Creating database context for migration...
[Startup] Migrating database...
[Startup] Migrated database.

Functions:

	ArchiveUnit: [DELETE] http://localhost:7071/units/{unitId}/archive

	BuildingRelationshipsGetAll: [GET] http://localhost:7071/buildingRelationships

	BuildingRelationshipsGetOne: [GET] http://localhost:7071/buildingRelationships/{relationshipId}
	...
	UpdateUnit: [PUT] http://localhost:7071/units/{unitId}

	UpdateUnitMember: [PUT] http://localhost:7071/memberships/{membershipId}

For detailed output, run func with --verbose flag.

```

### Running the Web Project

Update `src/Web/wwwroot/appsettings.json` to read:
```json
{
	"APP_WEB_URL": "https://localhost:5001",
	"API_URL": "http://localhost:7071",
	"UAA_OAUTH2_AUTH_URL": "https://apps-test.iu.edu/uaa-stg/oauth/authorize",
	"UAA_OAUTH2_CLIENT_ID": "<ClientId from IT People UAA Stage OAuth in LastPass>"
}
```

Note that `API_URL` is the URL and port that our local function app is running on.

Now you can do a `dotnet run` in the `src/Web` directory and you should get output like:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /home/jfrancis/Documents/projects/itpeople/src/Web
```

Following the https link.  You must use the https link or the sign-in process will fail.  At this point you should be able to sign-in to the locally running website.

### Add Minimum Data
You will get an error `Failed to fetch UnitMemberships for jkfranci.` if you try to list units when no data is available.  You will need to run two INSERT queries to make the database functional for local testing.

To do that you'll need to setup a database client and connect to the container we created.  [pgAdmin](https://www.pgadmin.org/) and the Azure Data Studio [PostgreSQL extension](https://learn.microsoft.com/en-us/sql/azure-data-studio/extensions/postgres-extension) are good options. Just be sure that you use the port, 5434, that we used when creating our container. 

```SQL
-- Add a department
INSERT INTO departments (name, description, display_units)
VALUES ('Local Test Dept.', 'A department solely used for testing on the local machine.', True)
```

```SQL
-- Add a user entry for yourself
-- The department ID should be the Id of the department you created earlier
INSERT INTO people (netid, name, position, location, campus, campus_phone, campus_email, photo_url, responsibilities, department_id, is_service_admin, name_first, name_last, notes)
VALUES ('jkfranci', 'Jason Francis', 'dev', 'Bloomington', 'Bloomington', '812 856 3260', 'jkfranci@iu.edu', 'https://fake.com/fake.jpg', 1, 1, True, 'Jason', 'Francis', 'Notes cannot be null.')
```