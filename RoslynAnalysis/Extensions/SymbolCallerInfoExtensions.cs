using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using RoslynAnalysis.Model;

namespace RoslynAnalysis.Extensions
{
    public static class SymbolCallerInfoExtensions
    {
        public static async Task<ILookup<string, MethodCall>> ToMethodCalls(this IEnumerable<SymbolCallerInfo> calls,
            WorkspaceSolution solution)
        {
            return await calls.MapToLookup(solution, CreateMethodCalls);
        }

        public static async Task<ILookup<string, PropertyCall>> ToPropertySets(this IEnumerable<SymbolCallerInfo> calls,
            string propertyName, WorkspaceSolution solution)
        {
            return await calls.MapToLookup(solution, (location, callingSymbol, s) => CreatePropertySets(propertyName, location, callingSymbol, s));
        }

        private static async Task<ILookup<string, T>> MapToLookup<T>(this IEnumerable<SymbolCallerInfo> calls, WorkspaceSolution solution, Func<Location, ISymbol, WorkspaceSolution, Task<T[]>> mapCall)
            where T : InvocationBase
        {
            var mappedItems = new List<T>();

            foreach (var call in calls)
            {
                foreach (var location in call.Locations)
                {
                    mappedItems.AddRange(await mapCall(location, call.CallingSymbol, solution));
                }
            }

            return mappedItems.ToLookup(x => x.AssemblyName);
        }

        public static async Task<MethodCall[]> CreateMethodCalls(this Location location, ISymbol sourceMethod, WorkspaceSolution solution)
        {
            var invocationDetails = await GetInvocationDetails<InvocationExpressionSyntax, IMethodSymbol>(location, sourceMethod, solution);
            if (invocationDetails == null)
                return Array.Empty<MethodCall>();

            var sourceType = sourceMethod.ContainingType;
            string sourceApiPath = await invocationDetails.GetSourceApiPath();

            var arguments = invocationDetails.CallNode.ArgumentList.Arguments
                .Select(x => GetExpressionValue(invocationDetails.SemanticModel, x.Expression) ?? GetExpressionIdentifier(x.Expression) ?? "Unknown").ToArray();

            return new[]
            {
                new MethodCall(sourceMethod.ContainingAssembly.Name, sourceType.GetNestedTypeName(), sourceApiPath, arguments)
            };
        }

        public static async Task<PropertyCall[]> CreatePropertySets(string propertyName, Location location,
            ISymbol sourceMethod, WorkspaceSolution solution)
        {
            var invocationDetails =
                await GetInvocationDetails<AssignmentExpressionSyntax, IPropertySymbol>(location, sourceMethod,
                    solution);
            if (invocationDetails == null)
                return Array.Empty<PropertyCall>();

            var sourceType = sourceMethod.ContainingType;

            return new[]
            {
                new PropertyCall(sourceMethod.ContainingAssembly.Name, sourceType.GetNestedTypeName(),
                    null, propertyName, GetExpressionValue(invocationDetails.SemanticModel, invocationDetails.CallNode.Right) ?? "Unknown")
            };
        }


        private static async Task<InvocationDetails<TSyntax, TSymbol>> GetInvocationDetails<TSyntax, TSymbol>(Location location, ISymbol sourceMethod, WorkspaceSolution solution)
            where TSyntax : SyntaxNode
            where TSymbol : ISymbol
        {
            var locationProject = await sourceMethod.GetProject(solution);
            var semanticModel = await location.GetSemanticModel(locationProject);
            var node = await location.GetNode();

            var currentNode = node;
            TSyntax callNode = null;
            while (currentNode != null)
            {
                if (currentNode is TSyntax syntaxNode)
                {
                    callNode = syntaxNode;
                    break;
                }

                currentNode = currentNode.Parent;
            }

            if (currentNode == null)
                return null;

            var symbolInfo = semanticModel.GetSymbolInfo(node);
            var foundSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            if (!(foundSymbol is TSymbol calledSymbol))
                return null;

            var sourceType = sourceMethod.ContainingType;

            return new InvocationDetails<TSyntax, TSymbol>(solution, locationProject, sourceType, semanticModel, callNode, calledSymbol);
        }

        private static string GetExpressionIdentifier(ExpressionSyntax expression)
        {
            return expression switch
            {
                IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
                LiteralExpressionSyntax literal => literal.Token.Text.Trim('\"'),
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
                ObjectCreationExpressionSyntax objectCreation => objectCreation.Type.GetName(),
                InvocationExpressionSyntax _ => expression.ToString(),
                _ => null
            };
        }

        private static string GetExpressionValue(SemanticModel semanticModel, ExpressionSyntax expression)
        {
            switch (expression)
            {
                case IdentifierNameSyntax identifierName:
                {
                    var identifierSymbol = semanticModel.GetSymbolInfo(identifierName);
                    return identifierSymbol.Symbol switch
                    {
                        ILocalSymbol localSymbol => localSymbol.ConstantValue?.ToString() ?? identifierName.Identifier.Text,
                        IFieldSymbol fieldSymbol => fieldSymbol.ConstantValue?.ToString() ?? identifierName.Identifier.Text,
                        _ => null
                    };
                }
                case MemberAccessExpressionSyntax memberAccess:
                {
                    var finalMemberExpression = memberAccess;
                    while (finalMemberExpression.Expression is MemberAccessExpressionSyntax subMember)
                        finalMemberExpression = subMember;

                    if (finalMemberExpression.Expression is IdentifierNameSyntax identifierName)
                    {
                        var identifierSymbol = semanticModel.GetSymbolInfo(identifierName);
                        if (identifierSymbol.Symbol is INamedTypeSymbol namedTypeSymbol)
                        {
                            var fieldSymbol = namedTypeSymbol.GetMembers(finalMemberExpression.Name.Identifier.Text)
                                .OfType<IFieldSymbol>().FirstOrDefault();
                            if (fieldSymbol != null)
                                return fieldSymbol.ConstantValue?.ToString() ?? finalMemberExpression.Name.Identifier.Text;
                        }
                    }

                    return null;
                }
                case LiteralExpressionSyntax literal:
                    return literal.Token.Text.Trim('\"');
                default:
                    return null;
            }
        }

        private static string GetMethodArgument(SyntaxNode syntaxNode, SemanticModel semanticModel, Predicate<ITypeSymbol> matchesCriteria)
        {
            if (!(syntaxNode is InvocationExpressionSyntax invocationExpression)) return null;
            return invocationExpression.ArgumentList.Arguments
                .Select(argument => semanticModel.GetTypeInfo(argument.Expression).Type)
                .Where(typeSymbol => matchesCriteria(typeSymbol))
                .Select(typeSymbol => typeSymbol.GetParentTypeName())
                .FirstOrDefault();
        }

        public static async Task<bool> IsCallingMethod(this SymbolCallerInfo call, string methodName)
        {
            return (await Task.WhenAll(call.Locations.Select(async x =>
            {
                var node = await x.GetNode();
                while (node != null)
                {
                    if (node is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.Text == methodName)
                        return true;
                    node = node.Parent;
                }

                return false;
            })))
                .Any(x => x);
        }
    }
}