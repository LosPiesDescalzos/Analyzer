using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject;
using VerifyCS = AnalyzerTemplate.Test.CSharpCodeFixVerifier<
    AnalyzerTemplate.AnalyzerPrivateField,
    AnalyzerTemplate.CodeFixerPrivateField>;

namespace AnalyzerTemplate.Test
{
    [TestClass]
    public class AnalyzerTemplateUnitTest
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"namespace test1
{
    class Program
    {   
        public void Method1(int b)
        {
            int i = 2;
            bool notA = true;
            bool EverythingOk = false;
            if (notA)
            {
                notA = false;
            }
            if (!notA)
            {
                notA = false;
            }
            while (notA)
            {
                int notT = 5;
                notA = false;
            }
        }

        static void Main()
        {
        }
    }
}";
            var fixtest = @"namespace test1
{
    class Program
    {   
        public void Method1(int b)
        {
            int i = 2;
            bool a = false;
            bool EverythingOk = false;
            if (a)
            {
                a = true;
            }

            if (a)
            {
                a = true;
            }

            while (a)
            {
                a = true;
            }
        }

        static void Main()
        {
        }
    }
}";

            var (diagnostics, document, workspace) = await UtilitiesBoolWithNot.GetDiagnosticsAdvanced(test);

            var diagnostic = diagnostics[0];

           // var codeFixProvider = new AnalyzerTemplateCodeFixProvider();
            var codeFixProvider = new AnalyzerI4();
            
            CodeAction registeredCodeAction = null;

            var context = new CodeFixContext(document, diagnostic, (codeAction, _) =>
            {
                if (registeredCodeAction != null)
                    throw new Exception("Code action was registered more than once");

                registeredCodeAction = codeAction;

            }, CancellationToken.None);

            await codeFixProvider.RegisterCodeFixesAsync(context);

            if (registeredCodeAction == null)
                throw new Exception("Code action was not registered");

            var operations = await registeredCodeAction.GetOperationsAsync(CancellationToken.None);

            foreach (var operation in operations)
            {
                operation.Apply(workspace, CancellationToken.None);
            }

            var updatedDocument = workspace.CurrentSolution.GetDocument(document.Id);
            var newCode = (await updatedDocument.GetTextAsync()).ToString();
            Assert.AreEqual(fixtest.Replace(" ", ""), newCode.Replace(" ", ""));
            
        }


        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"namespace test2
{
    class Program
    {
        private int _count;
        public void SomeMethod(int count, List<int> items)
        {
            _count = count;
            Console.WriteLine(""5"");
        }

        static void Main()
        {
        }
    }
}";
            var fixtest = @"namespace test2
{
    class Program
    {
        public void SomeMethod(int count, List<int> items)
        {
            Console.WriteLine(""5"");
        }

        static void Main()
        {
        }
    }
}";

            var (diagnostics, document, workspace) = await UtilitiesPrivateField.GetDiagnosticsAdvanced(test);
            var diagnostic = diagnostics[0];
            var codeFixProvider = new CodeFixerPrivateField();
            CodeAction registeredCodeAction = null;

            var context = new CodeFixContext(document, diagnostic, (codeAction, _) =>
            {
                if (registeredCodeAction != null)
                    throw new Exception("Code action was registered more than once");

                registeredCodeAction = codeAction;

            }, CancellationToken.None);

            await codeFixProvider.RegisterCodeFixesAsync(context);
            if (registeredCodeAction == null)
                throw new Exception("Code action was not registered");

            var operations = await registeredCodeAction.GetOperationsAsync(CancellationToken.None);
            foreach (var operation in operations)
            {
                operation.Apply(workspace, CancellationToken.None);
            }

            var updatedDocument = workspace.CurrentSolution.GetDocument(document.Id);
            var newCode = (await updatedDocument.GetTextAsync()).ToString();

            Assert.AreEqual(fixtest.Replace(" ", ""), newCode.Replace(" ", ""));
        }
        
        [TestMethod]
        public async Task TestMethod3()
        {
            var test = @"namespace test2
{
    class Program
    {
        private int _count;
        public void SomeMethod1(int count, List<int> items)
        {
            _count = count;
            Console.WriteLine(""5"");
        }

        public void SomeMethod2(int count, List<int> items)
        {
            _count = 6;
            Console.WriteLine(""5"");
        }

        static void Main()
        {
        }
    }
}";
            
            var fixtest = @"namespace test2
{
    class Program
    {
        private int _count;
        public void SomeMethod1(int count, List<int> items)
        {
            _count = count;
            Console.WriteLine(""5"");
        }

        public void SomeMethod2(int count, List<int> items)
        {
            _count = 6;
            Console.WriteLine(""5"");
        }


        static void Main()
        {
        }
    }
}";

            var (diagnostics, document, workspace) = await UtilitiesPrivateField.GetDiagnosticsAdvanced(test);
            var diagnostic = diagnostics[0];
            var codeFixProvider = new CodeFixerPrivateField();
            CodeAction registeredCodeAction = null;

            var context = new CodeFixContext(document, diagnostic, (codeAction, _) =>
            {
                if (registeredCodeAction != null)
                    throw new Exception("Code action was registered more than once");

                registeredCodeAction = codeAction;

            }, CancellationToken.None);

            await codeFixProvider.RegisterCodeFixesAsync(context);
            if (registeredCodeAction == null)
                throw new Exception("Code action was not registered");

            var operations = await registeredCodeAction.GetOperationsAsync(CancellationToken.None);
            foreach (var operation in operations)
            {
                operation.Apply(workspace, CancellationToken.None);
            }

            var updatedDocument = workspace.CurrentSolution.GetDocument(document.Id);
            var newCode = (await updatedDocument.GetTextAsync()).ToString();

            Assert.AreEqual(fixtest.Replace(" ", ""), newCode.Replace(" ", ""));
        }
        
        
        [TestMethod]
        public async Task TestMethod4()
        {
            var test = @"namespace test2
{
    class Program
    {
        private int _count = 0;
        public void SomeMethod(int count, List<int> items)
        {
            _count++;
            Console.WriteLine(""5"");
        }

    static void Main()
        {
        }
    }
}";
            
            var fixtest = @"namespace test2
{
    class Program
    {
        private int _count = 0;
        public void SomeMethod(int count, List<int> items)
        {
            _count++;
            Console.WriteLine(""5"");
        }

    static void Main()
        {
        }
    }
}";

            var (diagnostics, document, workspace) = await UtilitiesPrivateField.GetDiagnosticsAdvanced(test);
            var diagnostic = diagnostics[0];
            var codeFixProvider = new CodeFixerPrivateField();
            CodeAction registeredCodeAction = null;

            var context = new CodeFixContext(document, diagnostic, (codeAction, _) =>
            {
                if (registeredCodeAction != null)
                    throw new Exception("Code action was registered more than once");

                registeredCodeAction = codeAction;

            }, CancellationToken.None);

            await codeFixProvider.RegisterCodeFixesAsync(context);
            if (registeredCodeAction == null)
                throw new Exception("Code action was not registered");

            var operations = await registeredCodeAction.GetOperationsAsync(CancellationToken.None);
            foreach (var operation in operations)
            {
                operation.Apply(workspace, CancellationToken.None);
            }

            var updatedDocument = workspace.CurrentSolution.GetDocument(document.Id);
            var newCode = (await updatedDocument.GetTextAsync()).ToString();

            Assert.AreEqual(fixtest.Replace(" ", ""), newCode.Replace(" ", ""));
        }

    }
}
