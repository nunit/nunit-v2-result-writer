public static class Assert
{
    internal static List<string> FailureMessages = new List<string>();
    internal static int AssertCount { get; set; }
    internal static int FailureCount { get; set; }
    internal static int SuccessCount => AssertCount - FailureCount;

    public static void That<T>(T actual, Constraint<T> constraint, string message = null)
    {
        Assert.AssertCount++;
        
        if (!constraint.Matches(actual))
        {
            if (message != null)
                FailureMessages.Add(message);

            ReportFailure(constraint.Message);
        }
    }

    public static void That(bool condition, string message)
    {
        Assert.AssertCount++;
        if (!condition)
            ReportFailure(message);
    }

    public static void Fail(string message = null)
    {
        throw new System.Exception(message);
    }

    internal static void ReportFailure(string message)
    {
        // Note that this version does not automatically
        // terminate the test upon failure of an Assert.
        // Use Assert.Fail if you want the test to end.
        if (!string.IsNullOrEmpty(message))
            FailureMessages.Add(message);
        FailureCount++;
    }
}
