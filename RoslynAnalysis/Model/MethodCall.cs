namespace RoslynAnalysis.Model
{
    public class MethodCall : InvocationBase
    {
        public string[] Arguments { get; }

        public MethodCall(string assemblyName, string sourceTypeName, string sourceApiPath, string[] arguments)
            : base(assemblyName, sourceTypeName, sourceApiPath)
        {
            Arguments = arguments;
        }
    }
}