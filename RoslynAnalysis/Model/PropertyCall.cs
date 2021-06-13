namespace RoslynAnalysis.Model
{
    public class PropertyCall : InvocationBase
    {
        public string Property { get; }
        public string Value { get; }

        public PropertyCall(string assemblyName, string sourceTypeName, string sourceApiPath, string property, string value)
            : base(assemblyName, sourceTypeName, sourceApiPath)
        {
            Property = property;
            Value = value;
        }
    }
}