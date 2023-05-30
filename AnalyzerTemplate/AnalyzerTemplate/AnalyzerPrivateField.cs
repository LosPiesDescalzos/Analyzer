using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AnalyzerTemplate
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AnalyzerPrivateField : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AnalyzerTemplate";
        
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(PrivateField, SyntaxKind.ClassDeclaration);
        }

        private void PrivateField(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            var tree = CSharpSyntaxTree.ParseText(classDeclaration.ToString());
            var root = (CompilationUnitSyntax) tree.GetRoot();
            
            var fieldDeclarations = root.DescendantNodes().OfType<FieldDeclarationSyntax>();
            
            foreach (var fieldDeclaration in fieldDeclarations)
            {
                foreach (var modifier in fieldDeclaration.Modifiers)
                {
                    if (modifier.ToString().Equals("private"))
                    {
                        VariableDeclarationSyntax variableDeclaration = fieldDeclaration.Declaration;
                        var nameField = variableDeclaration.Variables.First().Identifier;
                        var variableAssignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();

                        int count = 0;
                        foreach (var variableAssignment in variableAssignments)
                        {
                            if ((variableAssignment.Left.ToString() == nameField.ToString()) || (variableAssignment.Right.ToString() == nameField.ToString()))
                            {
                                count++;
                            }
                        }
                        
                        if (count < 2)
                        {
                            var diagnostic = Diagnostic.Create(Rule, classDeclaration.GetLocation());
                            context.ReportDiagnostic(diagnostic);  
                        }
                    }
                }
            }
        }
    }
}