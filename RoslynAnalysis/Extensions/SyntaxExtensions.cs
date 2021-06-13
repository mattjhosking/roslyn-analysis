using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalysis.Extensions
{
    public static class SyntaxExtensions
    {
        public static string GetName(this TypeSyntax type)
        {
            if (type is QualifiedNameSyntax qualifiedName)
                return qualifiedName.Left.GetName();
            return type.ToString();
        }

        public static string GetName(this NameSyntax name)
        {
            if (name is IdentifierNameSyntax identifierName)
                return identifierName.Identifier.Text;
            return name.ToString();
        }

        public static MethodDeclarationSyntax GetMethod(this SyntaxNode syntaxNode, string methodName)
        {
            return syntaxNode.GetSyntax<MethodDeclarationSyntax>(x => x.Identifier.Text == methodName);
        }

        public static PropertyDeclarationSyntax GetProperty(this SyntaxNode syntaxNode, string propertyName)
        {
            return syntaxNode.GetSyntax<PropertyDeclarationSyntax>(x => x.Identifier.Text == propertyName);
        }

        public static T GetSyntax<T>(this SyntaxNode syntaxNode, Func<T, bool> matchCriteria)
            where T : SyntaxNode
        {
            return syntaxNode.DescendantNodes().OfType<T>().Single(matchCriteria);
        }
    }
}