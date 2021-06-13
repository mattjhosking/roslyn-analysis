using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using RoslynAnalysis.Extensions;

namespace RoslynAnalysis.Model
{
    public class ProjectFile
    {
        private readonly WorkspaceProject _project;
        private readonly SyntaxNode _fileRoot;

        public ProjectFile(WorkspaceProject project, SyntaxNode fileRoot)
        {
            _project = project;
            _fileRoot = fileRoot;
        }

        public async Task<ILookup<string, MethodCall>> FindUsagesOfMethod(string methodName)
        {
            var methodDeclaration = _fileRoot.GetMethod(methodName);
            var calls = await _project.FindMethodCallers(methodDeclaration);
            return await calls.ToMethodCalls(_project.Solution);
        }

        public async Task<ILookup<string, PropertyCall>> FindUsagesOfProperty(string propertyName)
        {
            var propertyDeclaration = _fileRoot.GetProperty(propertyName);
            var calls = await _project.FindPropertyCallers(propertyDeclaration);
            return await calls.ToPropertySets(propertyName, _project.Solution);
        }
    }
}