using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace AnalyzerTemplate
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerTemplateCodeFixProvider)), Shared]
    public class AnalyzerTemplateCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AnalyzerBoolWithNot.DiagnosticId); }
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
                    createChangedDocument: c => FixBoolWithNot(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }


        private async Task<Document> FixBoolWithNot(Document document, ClassDeclarationSyntax classDeclarationSyntax,
            CancellationToken cancellationToken)
        {
            var editor = DocumentEditor.CreateAsync(document).Result;
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            CompilationUnitSyntax root = await tree.GetRootAsync(cancellationToken) as CompilationUnitSyntax;
            
            var classDeclaration = classDeclarationSyntax;
            var methods = classDeclaration.Members;
            foreach (MemberDeclarationSyntax memberDeclaration in methods)
            {
                var method = (MethodDeclarationSyntax) memberDeclaration;
                SyntaxList<StatementSyntax> statements = method.Body.Statements;
                List<SyntaxToken> boolList = new List<SyntaxToken>();
                foreach (StatementSyntax statement in statements)
                {
                    if (statement is LocalDeclarationStatementSyntax localDeclaration)
                     {
                         if (localDeclaration.Declaration.Type.ToString() == "bool")
                         {
                             var name = localDeclaration.Declaration.Variables[0].Identifier;
                             if (name.ToString().Length > 3 && name.ToString().Substring(0, 3) == "not")
                             {
                                 SyntaxKind value = SyntaxKind.NullKeyword;
                                 if (localDeclaration.Declaration.Variables[0].Initializer.Value.Kind() ==
                                     SyntaxKind.TrueLiteralExpression)
                                 {
                                     value = SyntaxKind.FalseLiteralExpression;
                                 }
                                 else
                                 {
                                     value = SyntaxKind.TrueLiteralExpression;
                                 }
 
                                 var a = SyntaxFactory.LocalDeclarationStatement(
                                     SyntaxFactory.VariableDeclaration(
                                             SyntaxFactory.PredefinedType(
                                                 SyntaxFactory.Token(SyntaxKind.BoolKeyword)
                                             )
                                         )
                                         .WithVariables(
                                             SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                                 SyntaxFactory.VariableDeclarator(
                                                         SyntaxFactory.Identifier(name.ToString().Remove(0, 3))
                                                     )
                                                     .WithInitializer(
                                                         SyntaxFactory.EqualsValueClause(
                                                             SyntaxFactory.LiteralExpression(
                                                                 value
                                                             )
                                                         )
                                                     )
                                             )
                                         )
                                 ).NormalizeWhitespace();
                                 boolList.Add(name);
                                 editor.ReplaceNode(root.FindNode(statement.Span), a);
                              //   Console.WriteLine(a);
                             }
                         }
                     }

                     if (statement is IfStatementSyntax ifStatementSyntax)
                      {
                          ExpressionSyntax condition = ifStatementSyntax.Condition;
  
                          if (condition is IdentifierNameSyntax nameSyntax)
                          {
                              if (condition.ToString().Substring(0, 3) == "not")
                              {
                                  SyntaxKind value = SyntaxKind.NullKeyword;
                                  if (condition.Kind() == SyntaxKind.TrueLiteralExpression)
                                  {
                                      value = SyntaxKind.FalseLiteralExpression;
                                  }
                                  else
                                  {
                                      value = SyntaxKind.TrueLiteralExpression;
                                  }
  
                                  var newName = condition.ToString().Remove(0, 3);
                                  var d =
                                      SyntaxFactory.IfStatement(
                                          SyntaxFactory.IdentifierName(newName),
                                          SyntaxFactory.Block(
                                              SyntaxFactory.SingletonList<StatementSyntax>(
                                                  SyntaxFactory.ExpressionStatement(
                                                      SyntaxFactory.AssignmentExpression(
                                                          SyntaxKind.SimpleAssignmentExpression,
                                                          SyntaxFactory.IdentifierName(newName),
                                                          SyntaxFactory.LiteralExpression(
                                                              value
                                                          )
                                                      )
                                                  )
                                              )
                                          )
                                      ).NormalizeWhitespace();
                                  editor.ReplaceNode(root.FindNode(statement.Span), d);
                               //   Console.WriteLine(d);
                              }
                          }
  
                         if (condition.Kind() is SyntaxKind.LogicalNotExpression)
                          {
                              if (condition.ToString().Substring(0, 3) == "!no")
                              {
                                  SyntaxKind value = SyntaxKind.NullKeyword;
                                  if (condition.Kind() == SyntaxKind.TrueLiteralExpression)
                                  {
                                      value = SyntaxKind.FalseLiteralExpression;
                                  }
                                  else
                                  {
                                      value = SyntaxKind.TrueLiteralExpression;
                                  }
  
                                  var newName = condition.ToString().Remove(0, 4);
  
                                  var d = SyntaxFactory.IfStatement(
                                      SyntaxFactory.IdentifierName(newName),
                                      SyntaxFactory.Block(
                                          SyntaxFactory.SingletonList<StatementSyntax>(
                                              SyntaxFactory.ExpressionStatement(
                                                  SyntaxFactory.AssignmentExpression(
                                                      SyntaxKind.SimpleAssignmentExpression,
                                                      SyntaxFactory.IdentifierName(newName),
                                                      SyntaxFactory.LiteralExpression(
                                                          value
                                                      )
                                                  )
                                              )
                                          )
                                      )
                                  ).NormalizeWhitespace();
                                  editor.ReplaceNode(root.FindNode(statement.Span), d);
                                 // Console.WriteLine(d);
                              }
                          }
                      }

                    if (statement is WhileStatementSyntax whileStatementSyntax)
                    {
                        ExpressionSyntax condition = whileStatementSyntax.Condition;
  
                          if (condition is IdentifierNameSyntax nameSyntax)
                          {
                           if (condition.ToString().Substring(0, 3) == "not")
                              {
                                  SyntaxKind value = SyntaxKind.NullKeyword;
                                  if (condition.Kind() == SyntaxKind.TrueLiteralExpression)
                                  {
                                      value = SyntaxKind.FalseLiteralExpression;
                                  }
                                  else
                                  {
                                      value = SyntaxKind.TrueLiteralExpression;
                                  }
  
                                  var newName = condition.ToString().Remove(0, 3);
                                  var d =
                                      SyntaxFactory.WhileStatement(
                                          SyntaxFactory.IdentifierName(newName),
                                          SyntaxFactory.Block(
                                              SyntaxFactory.SingletonList<StatementSyntax>(
                                                  SyntaxFactory.ExpressionStatement(
                                                      SyntaxFactory.AssignmentExpression(
                                                          SyntaxKind.SimpleAssignmentExpression,
                                                          SyntaxFactory.IdentifierName(newName),
                                                          SyntaxFactory.LiteralExpression(
                                                              value
                                                          )
                                                      )
                                                  )
                                              )
                                          )
                                      ).NormalizeWhitespace();
                                  editor.ReplaceNode(root.FindNode(statement.Span), d);
                                //  Console.WriteLine(d);
                              }
                          }
                    }
                }
            }

              var newDocument = editor.GetChangedDocument();
              var text = newDocument.GetTextAsync().Result.ToString();
              SyntaxTree newTree = CSharpSyntaxTree.ParseText(text);
              CompilationUnitSyntax newRoot = await newTree.GetRootAsync(cancellationToken) as CompilationUnitSyntax;
              Console.WriteLine(newRoot.NormalizeWhitespace());
              return document.WithSyntaxRoot(newRoot.NormalizeWhitespace());
        }

    }
}

