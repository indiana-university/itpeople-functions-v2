# IT People Functions V2
IT People serverless APIs and tasks (Version 2)

## Requirements
* [.Net 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
* [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools) >= 3.0.3160

## Caveats
### Contract Tests on Windows
Contract tests are using [Pact-Net](https://github.com/pact-foundation/pact-net) which is built using underlying Ruby Gem libraries.  These particular libraries encounter problems with some Windows file paths.
* When the path to the `bin` folder is more than ~260 characters the contract tests will fail to run.
* When the path contains any spaces the tests will fail to run.
The first error can be mitigated by enabling [long paths](https://github.com/pact-foundation/pact-node/blob/master/README.md#enable-long-paths) in the registry.  