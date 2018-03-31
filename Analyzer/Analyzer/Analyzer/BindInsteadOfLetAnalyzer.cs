using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NContext.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BindInsteadOfLetAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NContext_0001";
        private const string _Category = "Safety";
        private const string _Title = "Unsafe use of IServiceResponse<T> inside Let expression";
        private const string _MessageFormat = "Unsafe use of IServiceResponse<T> inside Let expression.";
        private const string _Description = "If calling any methods that return IServiceResponse<T>, use Bind instead of Let. " + 
            "Otherwise, any returned ErrorResponses will be lost, execution will continue as if no error occurred, and no error will be logged.";

        private static DiagnosticDescriptor _Rule = 
            new DiagnosticDescriptor(
                DiagnosticId, 
                _Title, 
                _MessageFormat,
                _Category, 
                DiagnosticSeverity.Warning, 
                isEnabledByDefault: true, 
                description: _Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(_Rule);

        public override void Initialize(AnalysisContext context)
        { 
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var functionChain = (InvocationExpressionSyntax) context.Node;

            //When invoking an extension method, the first child node should be a MemberAccessExpression
            var memberAccess = functionChain.ChildNodes().First() as MemberAccessExpressionSyntax;
            if (memberAccess == null)
            {
                return;
            }

            //When invoking an extension method, the last child node of the member access should be an IdentifierName
            var letIdentifier = memberAccess.ChildNodes().Last() as IdentifierNameSyntax;
            if (letIdentifier == null)
            {
                return;
            }

            //Ignore method invocations that do not have "Let" in the name
            if (!letIdentifier.GetText().ToString().Contains("Let")) 
            {
               return;
            }
            
            var semanticModel = context.SemanticModel;

            var unsafeNestedInvocations = functionChain.ArgumentList
                //Get nested method calls
                .DescendantNodes().OfType<InvocationExpressionSyntax>()
                //Get any identifier names in those calls
                .SelectMany(node => node.DescendantNodes().OfType<IdentifierNameSyntax>())
                //Get tuples of syntax nodes and the methods they refer to
                .Select(node => new 
                {
                    Node = node,
                    Symbol = semanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol
                })
                //Ignore identifiers that do not refer to methods
                .Where(x => x.Symbol != null
                //Ignore methods that do not have "IServiceResponse" in the return type
                    && x.Symbol.ReturnType.ToDisplayString().Contains("IServiceResponse"));

            //Just report the first one to reduce error log clutter
            var firstUnsafe = unsafeNestedInvocations.FirstOrDefault();
            if (firstUnsafe != null)
            {
                var diagnostic = Diagnostic.Create(_Rule, firstUnsafe.Node.GetLocation(), firstUnsafe.Node.GetText().ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
