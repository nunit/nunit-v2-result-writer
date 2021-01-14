public static class ResultWriterTests
{
    static XmlNode Fixture;

    static ResultWriterTests()
    {
        var doc = new XmlDocument();
        doc.Load("NUnit2TestResult.xml");
        Fixture = doc.DocumentElement;
    }

    [Test]
    public static void TopLevelHierarchy()
    {
        Assert.That(Fixture.Name, Is.EqualTo("test-results"));
        Assert.That(Fixture, Has.One.Element("environment"));
        Assert.That(Fixture, Has.One.Element("culture-info"));
        Assert.That(Fixture, Has.One.Element("test-suite"));
    }

    [Test]
    public static void TestSuitesHaveOneResultsElement()
    {
        var suites = Fixture.SelectNodes("//test-suite");
        Assert.That(suites.Count > 0, "No <test-suite> elements found in file");

        var resultElements = Fixture.SelectNodes("//results");
        Assert.That(resultElements.Count, Is.EqualTo(suites.Count),
            "Number of <results> elements should equal number of <test-suite> elements");

        foreach (XmlNode suite in suites)
            Assert.That(suite, Has.One.Element("results"));
    }

    [Test]
    public static void TestCasesHaveResultsElementAsParent()
    {
        var testCases = Fixture.SelectNodes("//test-case");
        foreach (XmlNode testCase in testCases)
            Assert.That(testCase.ParentNode.Name, Is.EqualTo("results"));
    }

    [Test]
    public static void TestResultsElement()
    {
        Assert.That(Fixture, Has.Attribute("name").EqualTo("mock-assembly.dll"));
        Assert.That(Fixture, Has.Attribute("total").EqualTo("31"));
        Assert.That(Fixture, Has.Attribute("errors").EqualTo("1"));
        Assert.That(Fixture, Has.Attribute("failures").EqualTo("1"));
        Assert.That(Fixture, Has.Attribute("not-run").EqualTo("10"));
        Assert.That(Fixture, Has.Attribute("inconclusive").EqualTo("1"));
        Assert.That(Fixture, Has.Attribute("ignored").EqualTo("4"));
        Assert.That(Fixture, Has.Attribute("skipped").EqualTo("3"));
        Assert.That(Fixture, Has.Attribute("invalid").EqualTo("3"));
        Assert.That(Fixture, Has.Attribute("date"));
        Assert.That(Fixture, Has.Attribute("time"));
    }

    [Test]
    public static void TopLevelTestSuite()
    {
        XmlNode suite = Fixture.SelectSingleNode("test-suite");
        Assert.That(suite, Has.Attribute("type").EqualTo("Assembly"));
        Assert.That(suite, Has.Attribute("name").EqualTo("mock-assembly.dll"));
        Assert.That(suite, Has.Attribute("executed").EqualTo("True"));
        Assert.That(suite, Has.Attribute("result").EqualTo("Failure"));
        Assert.That(suite, Has.Attribute("success").EqualTo("False"));
        Assert.That(suite, Has.Attribute("time"));
        Assert.That(suite, Has.Attribute("asserts").EqualTo("2"));
    }

    [Test]
    public static void EnvironmentElement()
    {
        XmlNode environment = Fixture.SelectSingleNode("environment");
        Assert.That(environment, Has.Attribute("nunit-version").EqualTo("3.11.0.0"));
        Assert.That(environment, Has.Attribute("clr-version"));
        Assert.That(environment, Has.Attribute("os-version"));
        Assert.That(environment, Has.Attribute("platform"));
        Assert.That(environment, Has.Attribute("cwd"));
        Assert.That(environment, Has.Attribute("machine-name"));
        Assert.That(environment, Has.Attribute("user"));
        Assert.That(environment, Has.Attribute("user-domain"));
    }

    [Test]
    public static void CultureInfoElement()
    {
        XmlNode cultureInfo = Fixture.SelectSingleNode("culture-info");
        Assert.That(cultureInfo, Has.Attribute("current-culture"));
        Assert.That(cultureInfo, Has.Attribute("current-uiculture"));
    }
}
