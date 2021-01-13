using System.IO;
using NUnit.Framework;
using PactNet;
using PactNet.Infrastructure.Outputters;

namespace Integration
{
    public class ContractTests
    {
        // [Test] // Ignore this until we get more of the API implemented
        public void VerifyContract()
        {
            var pactOutputs = new[] { new PactOutput(TestContext.Progress) };
            var config = new PactVerifierConfig() { /* Outputters = pactOutputs */ };
            new PactVerifier(config)
                .ServiceProvider("API", "http://localhost:8080")
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