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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EventFlow.CodeStyle
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TestCategoryAttributeAnalyzer : DiagnosticAnalyzer
    {
        public const string MissingDiagnosticId = "EventFlowMissingTestCategoryAttribute";
        public const string InvalidDiagnosticId = "EventFlowInvalidTestCategoryAttribute";

        private const string CodeStyleCategory = "Code Style";

        private const string CategoryAttributeName = "NUnit.Framework.CategoryAttribute";
        private const string CategoriesName = "EventFlow.TestHelpers.Categories";

        private static readonly DiagnosticDescriptor MissingCategoryRule = 
            new DiagnosticDescriptor(MissingDiagnosticId, 
                "Missing test category", "{0} doesn't have a Category attribute",
                CodeStyleCategory, DiagnosticSeverity.Error, true);

        private static readonly DiagnosticDescriptor InvalidCategoryRule = 
            new DiagnosticDescriptor(InvalidDiagnosticId, 
                "Invalid test category", "Please use EventFlow.TestHelpers.Categories as argument",
                CodeStyleCategory, DiagnosticSeverity.Error, true);

        private static readonly ImmutableHashSet<string> TestAttributeNames =
            ImmutableHashSet.Create(
                "NUnit.Framework.TestAttribute", 
                "NUnit.Framework.TestCaseAttribute",
                "NUnit.Framework.TestCaseSourceAttribute");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(MissingCategoryRule, InvalidCategoryRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol) context.Symbol;

            if (namedTypeSymbol.IsAbstract) return;

            IEnumerable<INamedTypeSymbol> TypeWithBaseTypes()
            {
                var type = namedTypeSymbol;
                while (type != null)
                {
                    yield return type;
                    type = type.BaseType;
                }
            }

            bool IsTestAttribute(AttributeData a) => 
                TestAttributeNames.Contains(a.AttributeClass.ToDisplayString());

            bool hasTestMethods = TypeWithBaseTypes()
                .SelectMany(type => type.GetMembers())
                .OfType<IMethodSymbol>()
                .Any(m => m.GetAttributes().Any(IsTestAttribute));

            if (!hasTestMethods) return;

            var attribute = TypeWithBaseTypes()
                .SelectMany(type => type.GetAttributes())
                .FirstOrDefault(a => a.AttributeClass.ToDisplayString() == CategoryAttributeName);

            if (attribute == null)
            {
                context.ReportDiagnostic(Diagnostic
                    .Create(MissingCategoryRule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name));

                return;
            }

            var attributeSyntax = (AttributeSyntax)attribute.ApplicationSyntaxReference.GetSyntax();
            var argument = attributeSyntax.ArgumentList.Arguments.SingleOrDefault();
            if (argument?.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var semanticModel = context.Compilation.GetSemanticModel(memberAccess.SyntaxTree);
                var typeInfo = semanticModel.GetTypeInfo(memberAccess.ChildNodes().First());
                if (typeInfo.Type.ToDisplayString() == CategoriesName)
                {
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(InvalidCategoryRule, attributeSyntax.GetLocation()));
        }
    }
}
