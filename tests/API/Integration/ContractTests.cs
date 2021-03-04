using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using PactNet;
using PactNet.Infrastructure.Outputters;

namespace Integration
{
    public class ContractTests
    {
        [Test]
        public void VerifyContract()
        {
            /*
                Pact has some troubles with Windows paths
                * If the path to the bin folder is more than ~260 characters it will fail unless "long paths" are enabled in the registry . https://github.com/pact-foundation/pact-node/blob/master/README.md#enable-long-paths
                * Ruby gems can fail if the path contains any spaces. It may blow up with an error like:
                    /provider_verifier/app.rb:4:in `require': cannot load such file -- pact/provider_verifier/provider_states/remove_provider_states_header_middleware (LoadError)
            */
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if(System.AppDomain.CurrentDomain.BaseDirectory.Length > 269 || System.AppDomain.CurrentDomain.BaseDirectory.Contains(" "))
                {
                    throw new System.Exception("Pact has known issues running from certain paths on Windows.  Please see readme.md about contract tests.");
                }
            }

            var pactOutputs = new[] { new PactOutput(TestContext.Progress) };
            var config = new PactVerifierConfig() { /* Outputters = pactOutputs */ };
            new PactVerifier(config)
                .ServiceProvider("API", "http://localhost:8080")
                .ProviderState("http://localhost:8081/state")
                .PactUri("https://raw.githubusercontent.com/indiana-university/itpeople-app/develop/contracts/itpeople-app-itpeople-functions.json")
                .HonoursPactWith("Client")
                .Verify();
        }

        internal class PactOutput : IOutput
        {
            private readonly TextWriter _progress;

            public PactOutput(TextWriter progress)
            {
                _progress = progress;
            }

            public void WriteLine(string line) 
                => _progress.WriteLine(line);
        }

        /*

   type PactTests(output: ITestOutputHelper)=
        inherit HttpTestBase(output)

        let stateServerScriptPath = "../../../../functions.tests.stateserver/bin/Debug/netcoreapp2.1"
        let stateServerPort = 9092

        let verifyPact output = 
            let stateServerUrl = sprintf "http://localhost:%d/state" stateServerPort
            let outputters = ResizeArray<IOutput> [XUnitOutput(output) :> IOutput]
            let verifier = PactVerifierConfig(Outputters=outputters, Verbose=false, PublishVerificationResults=false) |> PactVerifier
            verifier
                .ProviderState(stateServerUrl)
                .ServiceProvider("API", functionServerUrl)
                .HonoursPactWith("Client")
                .PactUri("https://raw.githubusercontent.com/indiana-university/itpeople-app/develop/contracts/itpeople-app-itpeople-functions.json")
                .Verify()

        [<Fact>]
        member __.``Verify Contracts`` () = async {
            let mutable stateServer = None
            try
                // "---> Starting state server host..." |> Console.WriteLine
                stateServer <- startTestServer stateServerPort stateServerScriptPath output |> Async.RunSynchronously
                verifyPact output
            finally 
                // "---> Stopping state server host..." |> Console.WriteLine
                stopTestServer stateServer

        }

        */
    }
}
