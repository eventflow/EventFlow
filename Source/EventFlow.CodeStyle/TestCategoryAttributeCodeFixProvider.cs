// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EventFlow.CodeStyle
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestCategoryAttributeCodeFixProvider))]
    [Shared]
    public class TestCategoryAttributeCodeFixProvider : CodeFixProvider
    {
        private static readonly AttributeSyntax Attribute = Attribute(
                QualifiedName(
                    QualifiedName(
                        IdentifierName("NUnit"),
                        IdentifierName("Framework")),
                    IdentifierName("Category")))
            .WithArgumentList(
                AttributeArgumentList(
                    SingletonSeparatedList(
                        AttributeArgument(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("EventFlow"),
                                        IdentifierName("TestHelpers")),
                                    IdentifierName("Categories")),
                                IdentifierName("Unit"))))));

        private const string title = "Add test category";

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(TestCategoryAttributeAnalyzer.MissingDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root =
                await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            Diagnostic diagnostic = context.Diagnostics.First();
            TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

            TypeDeclarationSyntax declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                .OfType<TypeDeclarationSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    c => MakeUppercaseAsync(context.Document, root, declaration, c),
                    title),
                diagnostic);
        }

        private Task<Solution> MakeUppercaseAsync(Document document, SyntaxNode root,
            TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            TypeDeclarationSyntax newDeclaration = typeDecl
                .AddAttributeLists(
                    AttributeList(SingletonSeparatedList(Attribute))
                        .WithAdditionalAnnotations(Formatter.Annotation));

            SyntaxNode newRoot = root.ReplaceNode(typeDecl, newDeclaration);
            Solution newSolution = document.WithSyntaxRoot(newRoot).Project.Solution;

            return Task.FromResult(newSolution);
        }
    }
}
