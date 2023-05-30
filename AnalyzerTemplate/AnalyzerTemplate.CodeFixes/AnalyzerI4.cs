using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;

namespace AnalyzerTemplate
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerTemplateCodeFixProvider)), Shared]
    public class AnalyzerI4 : CodeFixProvider
    {
      
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AnalyzerPrivateField.DiagnosticId); }
        }
        
        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => MakeNotVariable(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Document> MakeNotVariable(Document document, MethodDeclarationSyntax declarationSyntax, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken) as CompilationUnitSyntax;
            var newDocument = document;
         
            var statements = declarationSyntax.Body.Statements;
            foreach (var statement in statements)
            {
                if (statement is LocalDeclarationStatementSyntax localDeclaration)
                {
                    if (localDeclaration.Declaration.Type.ToString() == "bool")
                    {
                        foreach (var variable in localDeclaration.Declaration.Variables)
                        {
                            if (variable.Identifier.ToString().StartsWith("not"))
                            {
                                var newName = variable.Identifier.ToString().Replace("not", "").ToLower();
                                var compilation = await newDocument.Project.GetCompilationAsync();
                                var model = compilation.GetSemanticModel(root.SyntaxTree);
                                var originalSymbol = model.GetDeclaredSymbol(variable);
                                var newSolution = await Renamer.RenameSymbolAsync(newDocument.Project.Solution, originalSymbol, newName, newDocument.Project.Solution.Workspace.Options);
                                newDocument = newSolution.GetDocument(document.Id);
                            }
                        }
                    }
                }
            }

            Console.WriteLine(newDocument.FilePath);
            return newDocument;
        }
    }
}