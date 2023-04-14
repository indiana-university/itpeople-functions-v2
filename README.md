# IT People Functions V2

IT People serverless APIs and Tasks (Version 2)

## Requirements

* [.Net 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/3.1)
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

## Running Locally
### Configure the API Function App
If you want to work on the Web project, or just test an API endpoint directly, you will need to configure the API project to run locally.  This is accomplished by creating an `src/API/local.settings.json` file based on the `src/API/local.settings.json.example` file.  You must populate the fields `OAuthClientId` and `OAuthClientSecret` with the values from "UAA Stg Credentials" in the IT People folder on Vault.  Adittionally you should populate the fields `AdQueryUser` and `AdQueryPassword` with credentials to make LDAP queries against Active Directory, "ADS\opsbot" is available in Vault.

Next you will need to create a local database container for the API to use. Run
```bash
docker run -d -p 5434:5432 -e POSTGRES_USER=SA -e POSTGRES_PASSWORD=abcd1234@ --name itpeople-db-local postgres:11.7-alpine
```
Breaking down the command
* `docker run -d` - Create a new container that runs in the background
* `-p 5434:5432` - Take requests on localhost:5434, and route them to port 5432(the default Postgres port) in the container
	* We use a custom port so that this container does not interfere with the container created when you perform a `dotnet test`
* `-e POSTGRES_USER=SA` - Create a user with full permissions named "SA"
* `-e POSTGRES_PASSWORD=abcd1234@` - Set the "SA" user's password to "abcd1234@"
* `--name itpeople-db-local` - Name the container "itpeople-db-local" so it's easy to find when listing containers
* `postgres:11.7-alpine` - The image, from DockerHub, to use for this container

Make sure the `DatabaseConnectionString` in your `src/API/local.settings.json` is set to `Server=localhost;Port=5434;Database=ItPeople;User Id=SA;Password=abcd1234@;`

In `src/API` perform a
```bash
func start
```

This will build the API app, and start the Function App.  You should see output like
```
Azure Functions Core Tools
Core Tools Version:       4.0.5095 Commit hash: N/A  (64-bit)
Function Runtime Version: 4.16.5.20396

[2023-04-14T14:24:21.160Z] Found /home/jfrancis/Documents/projects/itpeople-functions-v2/src/API/API.csproj. Using for user secrets file configuration.
[Startup] Creating database context for migration...
[Startup] Migrating database...
[Startup] Migrated database.

Functions:

	ArchiveUnit: [DELETE] http://localhost:7071/units/{unitId}/archive

	BuildingRelationshipsGetAll: [GET] http://localhost:7071/buildingRelationships
	
	... 

	UnitsGetAll: [GET] http://localhost:7071/units

	UnitsGetOne: [GET] http://localhost:7071/units/{unitId}

	UpdateUnit: [PUT] http://localhost:7071/units/{unitId}

	UpdateUnitMember: [PUT] http://localhost:7071/memberships/{membershipId}

For detailed output, run func with --verbose flag.

```

This ran the migration to create the `ItPeople` database in the Docker container we created.  Now you must add yourself as an admin user in the `people` table.  Connect to the `DatabaseConnectionString` using your preferred SQL client to run the following queries. Azure Data Studio's [PostgreSQL](https://learn.microsoft.com/en-us/sql/azure-data-studio/quickstart-postgres?view=sql-server-ver16) addon is available if you do not have a preferred SQL client that works with Postgres databases.


Add an example Department
```pgsql
INSERT INTO departments (id, name, description, display_units)
VALUES (1, 'Local Department', 'Used for testing', TRUE)
```

Add an example Unit
```pgsql
INSERT INTO units (id, "name", "description", "url", email, active)
VALUES(1, 'Top Level Test Unit', 'A unit with no parent unit', '', '', TRUE)
```

Add yourself as a member of that Department, with `is_service_admin` set to true.  Just be sure to use your own username and contact information.
```pgsql
INSERT INTO people (id, netid, "name", position, "location", campus, campus_phone, campus_email, "notes", photo_url, department_id, is_service_admin, name_first, name_last)
VALUES(1, 'jkfranci', 'Jason Francis', 'Developer', 'somewhere', 'BL', '63260', 'jkfranci@iu.edu', '', '', 1, TRUE, 'Jason', 'Francis')
```

Make yourself a member of the Unit you created.
```pgsql
INSERT INTO unit_members (unit_id, person_id, title, "role", "percentage", permissions, notes)
VALUES(1, 1, 'Head Honcho', 4, 100, 1, '')
```

### Configuring the Web Project
Update the file `src/Web/wwwroot/appsettings.json` so that the `API_URL` field is set to the base bath of the functions listed when you started the API, in this case `http://localhost:7071`.  With the database container and the function app running you can run the Web project locally and login to it and test out the various views.

## Caveats
### Contract Tests on Windows
Contract tests are using [Pact-Net](https://github.com/pact-foundation/pact-net) which is built using underlying Ruby Gem libraries.  These particular libraries encounter problems with some Windows file paths.
* When the path to the `bin` folder is more than ~260 characters the contract tests will fail to run.
* When the path contains any spaces the tests will fail to run.
The first error can be mitigated by enabling [long paths](https://github.com/pact-foundation/pact-node/blob/master/README.md#enable-long-paths) in the registry.  