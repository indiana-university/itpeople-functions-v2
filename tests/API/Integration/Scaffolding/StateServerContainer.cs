using System.IO;

namespace Integration
{
    public class StateServerContainer : FunctionAppContainerBase
    {
        public StateServerContainer(TextWriter progress, TextWriter error) 
            : base(progress, error, "integration-test-state:dev", $"integration-test-state", "Dockerfile.State", 8081)
        {
        }
    }
}