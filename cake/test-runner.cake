#load ./assertions.cake
#load ./constraints.cake

using System.Reflection;

//////////////////////////////////////////////////////////////////////
// A tiny test framework for use in cake scripts.
//////////////////////////////////////////////////////////////////////

public static class TestRunner
{
    public static void Run(params Type[] types)
    {
        Assert.FailureCount = 0;

        foreach (Type type in types)
        {
            Console.WriteLine($"\n=> {type.Name}");

            int testCount = 0;
            int failCount = 0;

            foreach (var method in type.GetMethods())
            {
                if (!method.IsDefined(typeof(TestAttribute)))
                    continue;

                Assert.FailureMessages.Clear();
                testCount++;

                try
                {
                    var obj = !type.IsStatic() ? System.Activator.CreateInstance(type) : null;
                    method.Invoke(obj, new object[0]);
                }
                catch (Exception ex)
                {
                    if (ex is TargetInvocationException)
                        ex = ex.InnerException;
                    Assert.ReportFailure(ex.Message);
                }

                if (Assert.FailureMessages.Count == 0)
                {
                    Console.WriteLine($"  => {method.Name}");
                }
                else
                {
                    failCount++;
                    Console.WriteLine($"  => {method.Name} FAILED!");
                    foreach (string message in Assert.FailureMessages)
                        Console.WriteLine($"     {message}");
                    Assert.FailureMessages.Clear();
                }
            }

            bool failed = Assert.FailureCount > 0;
            string runResult = failed ? "FAILED" : "PASSED";

            Console.WriteLine($"\nTest Run Summary - {runResult}");
            Console.WriteLine($"  Tests: {testCount}, Passed: {testCount - failCount}, Failed: {failCount}");
            Console.WriteLine($"  Asserts: {Assert.AssertCount}, Passed: {Assert.SuccessCount}, Failed: {Assert.FailureCount}\n");

            if (failed)
                throw new System.Exception();
        }
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class TestAttribute : Attribute { }
