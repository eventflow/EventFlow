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

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.CodeFixVerifier<
    EventFlow.CodeStyle.TestCategoryAttributeAnalyzer,
    EventFlow.CodeStyle.TestCategoryAttributeCodeFixProvider>;

namespace EventFlow.CodeStyle.Tests
{
    [Category("unit")]
    public class TestCategoryAnalyzerTests
    {
        private const int Indentation = 12;

        private string SourceCode(string code)
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EventFlow.TestHelpers
{
    class Categories { public const string Unit = ""unit""; }
}

namespace NUnit.Framework
{
    class TestAttribute : Attribute {}
    class CategoryAttribute : Attribute { public CategoryAttribute(string s){} }
}

namespace ConsoleApplication1
{
" + string.Join('\n', code.Split('\n').Skip(1).Select(line => line.Substring(Indentation))) + @"
}";

            return Regex.Replace(text, "\r?\n", Environment.NewLine);
        }

        [Test]
        public async Task MissingCategoryGetsFixed()
        {
            var test = SourceCode(@"
                class TypeName
                {
                    [NUnit.Framework.Test]
                    public void Blah() {}
                }");

            var fixtest = SourceCode(@"
                [NUnit.Framework.Category(EventFlow.TestHelpers.Categories.Unit)]
                class TypeName
                {
                    [NUnit.Framework.Test]
                    public void Blah() {}
                }");

            DiagnosticResult expected = Verify
                .Diagnostic(TestCategoryAttributeAnalyzer.MissingDiagnosticId)
                .WithLocation(22, 11)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithArguments("TypeName");

            await Verify.VerifyCodeFixAsync(test, expected, fixtest);
        }
        
        [Test]
        public async Task NoErrors()
        {
            var test = SourceCode(@"
                [NUnit.Framework.Category(EventFlow.TestHelpers.Categories.Unit)]
                class TypeName
                {   
                    [NUnit.Framework.Test]
                    public void Blah() {}
                }");

            await Verify.VerifyAnalyzerAsync(test);
        }

        [Test]
        public async Task NoErrorsForAbstractClass()
        {
            var test = SourceCode(@"
                abstract class TypeName
                {   
                    [NUnit.Framework.Test]
                    public void Blah() {}
                }");

            await Verify.VerifyAnalyzerAsync(test);
        }

        [Test]
        public async Task DerivedTest()
        {
            var test = SourceCode(@"
                abstract class TypeName
                {   
                    [NUnit.Framework.Test]
                    public void Blah() {}
                }
                
                class DerivedTest : TypeName {}");

            DiagnosticResult expected = Verify
                .Diagnostic(TestCategoryAttributeAnalyzer.MissingDiagnosticId)
                .WithLocation(28, 11)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithArguments("DerivedTest");

            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Test]
        public async Task DerivedCategoryAttribute()
        {
            var test = SourceCode(@"
                [NUnit.Framework.Category(EventFlow.TestHelpers.Categories.Unit)]
                abstract class TypeName
                {   
                    [NUnit.Framework.Test]
                    public void Blah() {}
                }
                
                class DerivedTest : TypeName {}");

            await Verify.VerifyAnalyzerAsync(test);
        }

        [Test]
        public async Task InvalidCategory()
        {
            var test = SourceCode(@"
                [NUnit.Framework.Category(""Invalid Category"")]
                class TypeName
                {   
                    [NUnit.Framework.Test]
                    public void Blah() {}
                }");

            DiagnosticResult expected = Verify
                .Diagnostic(TestCategoryAttributeAnalyzer.InvalidDiagnosticId)
                .WithLocation(22, 6)
                .WithSeverity(DiagnosticSeverity.Error);

            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
