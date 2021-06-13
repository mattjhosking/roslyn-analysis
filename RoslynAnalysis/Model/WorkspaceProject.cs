using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace RoslynAnalysis.Model
{
    public class WorkspaceProject : WorkspaceBase
    {
        private readonly Project _project;
        private readonly Compilation _compilation;
        public WorkspaceSolution Solution { get; }

        public static async Task<WorkspaceProject> Load(string fileName)
        {
            var workspace = NewMsBuildWorkspace();
            return await LoadProject(pb => workspace.OpenProjectAsync(fileName, pb));
        }

        public static async Task<WorkspaceProject> LoadFromSolution(WorkspaceSolution solution, Project project)
        {
            return project != null
                ? await LoadProject(pb => Task.FromResult(project), solution)
                : null;
        }

        private static async Task<WorkspaceProject> LoadProject(Func<ProgressBarProjectLoadStatus, Task<Project>> getProject, WorkspaceSolution solution = null)
        {
            var project = await getProject(new ProgressBarProjectLoadStatus());
            var compilation = await project.GetCompilationAsync();
            return new WorkspaceProject(solution ?? new WorkspaceSolution(project.Solution), project, compilation);
        }

        private WorkspaceProject(WorkspaceSolution solution, Project project, Compilation compilation)
        {
            Solution = solution;
            _project = project;
            _compilation = compilation;
        }

        public async Task<ProjectFile> LoadFile(string fileName)
        {
            var document = _project.Documents.Single(x => x.Name == fileName);
            var syntaxRoot = await document.GetSyntaxRootAsync();
            return new ProjectFile(this, syntaxRoot);
        }

        public async Task<INamedTypeSymbol> FindType(string typeName)
        {
            await Console.Out.WriteLineAsync($"Finding type {typeName}...");
            var typeSymbol = _compilation.GetTypeByMetadataName(typeName);
            if (typeSymbol == null)
                throw new ArgumentException("Type not found", nameof(typeName));
            return typeSymbol;
        }


        public async Task<IEnumerable<SymbolCallerInfo>> FindCallers(string typeName, string methodName)
        {
            var typeSymbol = await FindType(typeName);
            var methodSymbol = typeSymbol.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();
            if (methodSymbol == null)
                throw new ArgumentException("Method not found", nameof(methodName));

            await Console.Out.WriteLineAsync($"Finding usages of {typeName}.{methodName}...");
            return await SymbolFinder.FindCallersAsync(methodSymbol, _project.Solution);
        }

        public async Task<IEnumerable<INamedTypeSymbol>> FindImplementations(string interfaceName)
        {
            var typeSymbol = _compilation.GetTypeByMetadataName(interfaceName);
            if (typeSymbol == null)
                throw new ArgumentException("Type not found", nameof(interfaceName));

            await Console.Out.WriteLineAsync($"Finding implementations of {interfaceName}...");
            return await SymbolFinder.FindImplementationsAsync(typeSymbol, _project.Solution);
        }
        
        public async Task<IEnumerable<SymbolCallerInfo>> FindMethodCallers(string typeName, string methodName)
        {
            var typeSymbol = await FindType(typeName);
            var methodSymbol = typeSymbol.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();
            if (methodSymbol == null)
                throw new ArgumentException("Method not found", nameof(methodName));
            return await SymbolFinder.FindCallersAsync(methodSymbol, _project.Solution);
        }

        public async Task<IEnumerable<SymbolCallerInfo>> FindMethodCallers(MethodDeclarationSyntax method)
        {
            await Console.Out.WriteLineAsync($"Finding usages of {method}...");
            var semanticModel = _compilation.GetSemanticModel(method.SyntaxTree);
            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            return await SymbolFinder.FindCallersAsync(methodSymbol, _project.Solution);
        }

        public async Task<IEnumerable<SymbolCallerInfo>> FindPropertyCallers(string typeName, string propertyName)
        {
            await Console.Out.WriteLineAsync($"Finding type {typeName}...");
            var typeSymbol = await FindType(typeName);
            var propertySymbol = typeSymbol.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault();
            if (propertySymbol == null)
                throw new ArgumentException("Property not found", nameof(propertyName));
            return await SymbolFinder.FindCallersAsync(propertySymbol, _project.Solution);
        }

        public async Task<IEnumerable<SymbolCallerInfo>> FindPropertyCallers(PropertyDeclarationSyntax property)
        {
            await Console.Out.WriteLineAsync($"Finding instances of set for {property}...");
            var semanticModel = _compilation.GetSemanticModel(property.SyntaxTree);
            var propertySymbol = semanticModel.GetDeclaredSymbol(property);
            return await SymbolFinder.FindCallersAsync(propertySymbol, _project.Solution);
        }

        public async Task<SemanticModel> GetModelForLocation(Location location)
        {
            var document = _project.Solution.GetDocument(location.SourceTree);
            if (document == null)
                throw new ArgumentException("Could not find document for location", nameof(location));

            return await document.GetSemanticModelAsync();
        }
    }
}