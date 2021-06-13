namespace RoslynAnalysis.Model
{
    public class InterfaceImplementation
    {
        public string AssemblyName { get; }
        public string ClassName { get; }

        public InterfaceImplementation(string assemblyName, string className)
        {
            AssemblyName = assemblyName;
            ClassName = className;
        }
    }
}