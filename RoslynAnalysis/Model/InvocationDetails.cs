using Microsoft.CodeAnalysis;

namespace RoslynAnalysis.Model
{
    public class InvocationDetails<TSyntax, TSymbol>
        where TSyntax : SyntaxNode
        where TSymbol : ISymbol
    {
        public WorkspaceSolution Solution { get; }
        public WorkspaceProject LocationProject { get; }
        public INamedTypeSymbol SourceType { get; }
        public SemanticModel SemanticModel { get; }
        public TSyntax CallNode { get; }
        public TSymbol CalledSymbol { get; }

        public InvocationDetails(WorkspaceSolution solution, WorkspaceProject locationProject, INamedTypeSymbol sourceType, SemanticModel semanticModel, TSyntax callNode, TSymbol calledSymbol)
        {
            Solution = solution;
            LocationProject = locationProject;
            SourceType = sourceType;
            SemanticModel = semanticModel;
            CallNode = callNode;
            CalledSymbol = calledSymbol;
        }
    }
}