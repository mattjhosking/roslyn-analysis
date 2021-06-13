namespace RoslynAnalysis.Model
{
    public class InvocationBase
    {
        public string AssemblyName { get; }
        public string SourceTypeName { get; }
        public string SourceApiPath { get; }

        public InvocationBase(string assemblyName, string sourceTypeName, string sourceApiPath)
        {
            AssemblyName = assemblyName;
            SourceTypeName = sourceTypeName;
            SourceApiPath = sourceApiPath;
        }
    }
}