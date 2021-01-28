const string NUNIT2_RESULT_SCHEMA = "NUnit2Results.xsd";

public class ResultWriterTests
{
    // NOTE: These test assume that we are only loading mock-assembly.dll in one
    // or more copies. If other assemblies are to be used, then we will need to
    // specify the expected results as part of package test.
    const int TOTAL = 31;
    const int ERRORS = 1;
    const int FAILURES = 1;
    const int NOTRUN = 10;
    const int INCONCLUSIVE = 1;
    const int IGNORED = 4;
    const int SKIPPED = 3;
    const int INVALID = 3;

    XmlNode Fixture;
    XmlNodeList Assemblies;

    public ResultWriterTests()
    {
        var doc = new XmlDocument();
        doc.Load(NUNIT2_RESULT_FILE);
        Fixture = doc.DocumentElement;
        Assemblies = Fixture.SelectNodes("//test-suite[@type='Assembly']");
    }

    [Test]
    public void TopLevelHierarchy()
    {
        Assert.That(Fixture.Name, Is.EqualTo("test-results"));
        Assert.That(Fixture, Has.One.Element("environment"));
        Assert.That(Fixture, Has.One.Element("culture-info"));
        Assert.That(Assemblies.Count, Is.GreaterThan(0));
    }

    [Test]
    public void TestSuitesHaveOneResultsElement()
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
    public void TestCasesHaveResultsElementAsParent()
    {
        var testCases = Fixture.SelectNodes("//test-case");
        foreach (XmlNode testCase in testCases)
            Assert.That(testCase.ParentNode.Name, Is.EqualTo("results"));
    }

    [Test]
    public void TestResultsElement()
    {
        int n = Assemblies.Count;
        Assert.That(Fixture, Has.Attribute("name").EqualTo("mock-assembly.dll"));
        Assert.That(Fixture, Has.Attribute("total").EqualTo((TOTAL * n).ToString()));
        Assert.That(Fixture, Has.Attribute("errors").EqualTo((ERRORS * n).ToString()));
        Assert.That(Fixture, Has.Attribute("failures").EqualTo((FAILURES * n).ToString()));
        Assert.That(Fixture, Has.Attribute("not-run").EqualTo((NOTRUN * n).ToString()));
        Assert.That(Fixture, Has.Attribute("inconclusive").EqualTo((INCONCLUSIVE * n).ToString()));
        Assert.That(Fixture, Has.Attribute("ignored").EqualTo((IGNORED * n).ToString()));
        Assert.That(Fixture, Has.Attribute("skipped").EqualTo((SKIPPED * n).ToString()));
        Assert.That(Fixture, Has.Attribute("invalid").EqualTo((INVALID * n).ToString()));
        Assert.That(Fixture, Has.Attribute("date"));
        Assert.That(Fixture, Has.Attribute("time"));
    }

    [Test]
    public void TopLevelTestSuites()
    {
        foreach (XmlNode suite in Assemblies)
        {
            Assert.That(suite, Has.Attribute("type").EqualTo("Assembly"));
            Assert.That(suite, Has.Attribute("name").EqualTo("mock-assembly.dll"));
            Assert.That(suite, Has.Attribute("executed").EqualTo("True"));
            Assert.That(suite, Has.Attribute("result").EqualTo("Failure"));
            Assert.That(suite, Has.Attribute("success").EqualTo("False"));
            Assert.That(suite, Has.Attribute("time"));
            Assert.That(suite, Has.Attribute("asserts").EqualTo("2"));
        }
    }

    [Test]
    public void EnvironmentElement()
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
    public void CultureInfoElement()
    {
        XmlNode cultureInfo = Fixture.SelectSingleNode("culture-info");
        Assert.That(cultureInfo, Has.Attribute("current-culture"));
        Assert.That(cultureInfo, Has.Attribute("current-uiculture"));
    }
}

public class SchemaValidationTests
{
    [Test]
    public void ValidateAgainstSchema()
    {
        SchemaValidator validator = new SchemaValidator(NUNIT2_RESULT_FILE, NUNIT2_RESULT_SCHEMA);
        Assert.That(validator.Validate(), "validate failed");
    }
}

using System.Xml.Schema;

public class SchemaValidator
{
    [Obsolete]
    private XmlValidatingReader myXmlValidatingReader;
    private bool success;

    [Obsolete]
    public SchemaValidator(string xmlFile, string schemaFile)
    {
        XmlSchemaCollection myXmlSchemaCollection = new XmlSchemaCollection();
        XmlTextReader xmlTextReader = new XmlTextReader(schemaFile);
        try
        {
            myXmlSchemaCollection.Add(XmlSchema.Read(xmlTextReader, null));
        }
        finally
        {
            xmlTextReader.Close();
        }

        // Validate the XML file with the schema
        XmlTextReader myXmlTextReader = new XmlTextReader(xmlFile);
        myXmlValidatingReader = new XmlValidatingReader(myXmlTextReader);
        myXmlValidatingReader.Schemas.Add(myXmlSchemaCollection);
        myXmlValidatingReader.ValidationType = ValidationType.Schema;
    }

    [Obsolete]
    public bool Validate()
    {
        success = true;

        try
        {
            // Set the validation event handler
            myXmlValidatingReader.ValidationEventHandler += new ValidationEventHandler(this.ValidationEventHandle);

            // Read XML data
            while (myXmlValidatingReader.Read()) { }
        }
        catch (Exception e)
        {
            throw new Exception(e.Message, e);
        }
        finally
        {
            myXmlValidatingReader.Close();
        }

        return success;
    }

    public void ValidationEventHandle(object sender, ValidationEventArgs args)
    {
        success = false;
        Console.WriteLine("\tValidation error: " + args.Message);

        if (args.Severity == XmlSeverityType.Warning)
        {
            Console.WriteLine("No schema found to enforce validation.");
        }
        else
            if (args.Severity == XmlSeverityType.Error)
        {
            Console.WriteLine("validation error occurred when validating the instance document.");
        }

        if (args.Exception != null) // XSD schema validation error
        {
            Console.WriteLine(args.Exception.SourceUri + "," + args.Exception.LinePosition + "," + args.Exception.LineNumber);
        }
    }
}
