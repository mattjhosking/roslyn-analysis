using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using RoslynAnalysis.Model;

namespace RoslynAnalysis.Extensions
{
    public static class LocationExtensions
    {
        public static async Task<SyntaxNode> GetNode(this Location location)
        {
            if (!location.IsInSource || location.SourceTree == null)
                return null;

            var node = (await location.SourceTree.GetRootAsync())
                .FindToken(location.SourceSpan.Start)
                .Parent;

            return node;
        }

        public static async Task<SemanticModel> GetSemanticModel(this Location location, WorkspaceProject project)
        {
            if (!location.IsInSource || location.SourceTree == null)
                return null;

            return await project.GetModelForLocation(location);
        }
    }
}
