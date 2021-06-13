using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynAnalysis.Model;

namespace RoslynAnalysis.Extensions
{
    public static class SymbolExtensions
    {
        public static ILookup<string, InterfaceImplementation> ToInterfaceImplementations(this IEnumerable<ITypeSymbol> implementations, string interfaceName)
        {
            var mappedItems = new List<InterfaceImplementation>();

            foreach (var implementation in implementations)
            {
                var interfaceImp = implementation.FindInterfaceImplementation(interfaceName);
                if (interfaceImp != null)
                {
                    mappedItems.Add(new InterfaceImplementation(implementation.ContainingAssembly.Name, implementation.GetNestedTypeName()));
                }
            }

            return mappedItems.ToLookup(x => x.AssemblyName);
        }

        private static INamedTypeSymbol FindInterfaceImplementation(this ITypeSymbol typeSymbol, string interfaceName)
        {
            if (typeSymbol == null || typeSymbol.Interfaces == null)
                return null;

            foreach (var interfaceImp in typeSymbol.Interfaces)
            {
                if (interfaceImp.Name.StartsWith(interfaceName))
                    return interfaceImp;
            }

            foreach (var interfaceImp in typeSymbol.Interfaces)
            {
                var subImp = interfaceImp.FindInterfaceImplementation(interfaceName);
                if (subImp != null)
                    return subImp;
            }

            return null;
        }

        public static async Task<WorkspaceProject> GetProject(this ISymbol sourceMethod, WorkspaceSolution solution)
        {
            return await solution.GetProject(Path.GetFileNameWithoutExtension(sourceMethod.ContainingModule.Name));
        }

        public static string GetParentTypeName(this ISymbol symbol)
        {
            return symbol.ContainingType != null ? symbol.ContainingType.Name : symbol.Name;
        }

        public static string GetNestedTypeName(this ISymbol symbol)
        {
            return symbol.ContainingType != null ? $"{symbol.ContainingType.Name}.{symbol.Name}" : symbol.Name;
        }

        public static bool HasInterface(this ITypeSymbol typeSymbol, string interfaceName)
        {
            return typeSymbol != null
                   && (typeSymbol.Interfaces.Any(x => x.Name == interfaceName || x.HasInterface(interfaceName))
                       || typeSymbol.BaseType != null && typeSymbol.BaseType.HasInterface(interfaceName));
        }

        private static readonly ISet<string> RouteAttributeNames = new HashSet<string>(new[]
        {
            "HttpGetAttribute", "HttpPostAttribute", "HttpPutAttribute", "HttpDeleteAttribute", "HttpPatchAttribute"
        });


        public static async Task<string> GetSourceApiPath(this InvocationDetails<InvocationExpressionSyntax, IMethodSymbol> invocationDetails)
        {
            var methodSymbol = invocationDetails.SourceType;
            var sourceType = methodSymbol.ContainingType;
            if (sourceType?.BaseType?.Name != "Controller" && sourceType?.BaseType?.Name != "ControllerBase")
                return null;

            var controllerName = sourceType.Name.Replace("Controller", "");
            var controllerRouteAttribute = sourceType.GetAttributes()
                .SingleOrDefault(x => x.AttributeClass?.Name == "RouteAttribute")?.ConstructorArguments.Single()
                .Value?.ToString();
            var controllerPath = controllerRouteAttribute?.Replace("[controller]", controllerName) ??
                                 $"/api/{controllerName}";

            AttributeData routeTemplateAttribute = null;
            AttributeData methodRouteMethodAttribute = null;
            while (routeTemplateAttribute == null)
            {
                var methodRouteAttribute = methodSymbol.GetAttributes()
                    .FirstOrDefault(x => x.AttributeClass?.Name == "RouteAttribute");
                methodRouteMethodAttribute = methodSymbol.GetAttributes()
                    .FirstOrDefault(x => RouteAttributeNames.Contains(x.AttributeClass?.Name));
                routeTemplateAttribute = methodRouteAttribute ?? methodRouteMethodAttribute;
                var declaringSyntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                if (declaringSyntaxReference != null)
                {
                    var declaringSyntax = await declaringSyntaxReference.GetSyntaxAsync();
                    if (declaringSyntax is MethodDeclarationSyntax methodDeclarationSyntax)
                    {
                        var callingMethodSymbol = invocationDetails.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                        if (callingMethodSymbol != null && callingMethodSymbol.ContainingType.Name == sourceType.Name)
                        {
                            var callers = (await invocationDetails.LocationProject.FindMethodCallers(methodDeclarationSyntax)).ToArray();
                            if (callers.Length == 0)
                                return null;
                            methodSymbol = callers[0].CallingSymbol as INamedTypeSymbol;
                            if (methodSymbol == null)
                                return null;
                        }
                    }
                }

            }

            var methodPath = routeTemplateAttribute?.ConstructorArguments.SingleOrDefault().Value?.ToString() ?? "";
            var routeComponents = new List<string> { controllerPath.Trim() };
            if (!string.IsNullOrWhiteSpace(methodPath))
                routeComponents.Add(methodPath.Trim('/'));
            var sourceApiMethod = (methodRouteMethodAttribute?.AttributeClass?.Name ?? "Unknown")
                .Replace("Http", "").Replace("Attribute", "").ToUpper();
            return $"{sourceApiMethod} {string.Join("/", routeComponents)}";
        }
    }
}