using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CompilationUnitSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax;
using ExpressionStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax;
using FieldDeclarationSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax;
using StatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax;

namespace AnalyzerTemplate
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeFixerPrivateField)), Shared]
    public class CodeFixerPrivateField : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AnalyzerPrivateField.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                .OfType<ClassDeclarationSyntax>().First();


            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => FixPrivateField(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Document> FixPrivateField(Document document, ClassDeclarationSyntax classDeclarationSyntax,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            CompilationUnitSyntax root = await tree.GetRootAsync(cancellationToken) as CompilationUnitSyntax;
            var editor = DocumentEditor.CreateAsync(document).Result;

            var classDeclaration = classDeclarationSyntax;
            var methods = classDeclaration.Members;
            bool equals = false;
            bool privat = false;
            int count = 0;
            foreach (MemberDeclarationSyntax memberDeclaration in methods)
            {
                if (memberDeclaration is FieldDeclarationSyntax fieldDeclaration)
                {
                    foreach (SyntaxToken modifier in fieldDeclaration.Modifiers)
                    {
                        if (modifier.ValueText.Contains("private"))
                        {
                            privat = true;
                        }
                    }
                }

                if (memberDeclaration is MethodDeclarationSyntax methodDeclarations)
                {
                    SyntaxList<StatementSyntax> statements = methodDeclarations.Body.Statements;
                    foreach (var statement in statements)
                    {
                        if (statement is ExpressionStatementSyntax expressionSyntax)
                        {
                            if (expressionSyntax.Expression is AssignmentExpressionSyntax assignmentExpression)
                            {
                                if (assignmentExpression.OperatorToken.ToString() == "=")
                                {
                                    equals = true;
                                    count++;
                                }
                            }
                        }
                    }
                }
            }

            if (equals && privat && count < 2)
            {
                foreach (MemberDeclarationSyntax memberDeclaration in methods)
                {
                    if (memberDeclaration is FieldDeclarationSyntax fieldDeclaration)
                    {
                        foreach (SyntaxToken modifier in fieldDeclaration.Modifiers)
                        {
                            if (modifier.ValueText.Contains("private"))
                            {
                                editor.RemoveNode(fieldDeclaration, SyntaxRemoveOptions.KeepEndOfLine);
                            }
                        }
                    }

                    if (memberDeclaration is MethodDeclarationSyntax methodDeclarations)
                    {
                        SyntaxList<StatementSyntax> statements = methodDeclarations.Body.Statements;
                        foreach (var statement in statements)
                        {
                            if (statement is ExpressionStatementSyntax expressionSyntax)
                            {
                                if (expressionSyntax.Expression is AssignmentExpressionSyntax assignmentExpression)
                                {
                                    editor.RemoveNode(expressionSyntax, SyntaxRemoveOptions.KeepEndOfLine);
                                }
                            }
                        }
                    }
                }
            }
           
            
            var newDocument = editor.GetChangedDocument();
            var text = newDocument.GetTextAsync().Result.ToString();
            SyntaxTree newTree = CSharpSyntaxTree.ParseText(text);
            var newRoot = await newTree.GetRootAsync(cancellationToken) as CompilationUnitSyntax;
            Console.WriteLine(newRoot.NormalizeWhitespace());
            return document.WithSyntaxRoot(newRoot.NormalizeWhitespace());
        }
    }
}

