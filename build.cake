#tool nuget:?package=NUnit.ConsoleRunner&version=3.19.2
#tool nuget:?package=NUnit.ConsoleRunner&version=3.18.3
#tool nuget:?package=NUnit.ConsoleRUnner&version=3.17.0
#tool nuget:?package=NUnit.ConsoleRunner&version=3.15.5
#tool nuget:?package=NUnit.ConsoleRunner.NetCore&version=3.19.2
#tool nuget:?package=NUnit.ConsoleRunner.NetCore&version=3.18.3
#tool nuget:?package=NUnit.ConsoleRunner.NetCore&version=3.17.0
#tool nuget:?package=NUnit.ConsoleRunner.NetCore&version=3.15.5

// NOTE: Because of permission issues in installing the chocolatey
// versions of the NUnit console runner, all tests use the nuget
// packages. The test runners included in the recipe adjust the
// search path for extensions accordingly.

// StandardRunners is used as the default setting for our tests,
// since the standard runner can execute all of them
var StandardRunners = new IPackageTestRunner[] {
    new NUnitConsoleRunner("NUnit.ConsoleRunner", "3.19.2"),
    new NUnitConsoleRunner("NUnit.ConsoleRunner", "3.18.3"),
    new NUnitConsoleRunner("NUnit.ConsoleRunner", "3.17.0"),
    new NUnitConsoleRunner("NUnit.ConsoleRunner", "3.15.5"),
};

var NetCoreRunners = new IPackageTestRunner[] {
    new NUnit3NetCoreConsoleRunner("NUnit.ConsoleRunner.NetCore", "3.19.2", "tools/net8.0/any/nunit3-console.exe"),
    new NUnit3NetCoreConsoleRunner("NUnit.ConsoleRunner.NetCore", "3.18.3", "tools/net6.0/any/nunit3-console.exe"),
    new NUnit3NetCoreConsoleRunner("NUnit.ConsoleRunner.NetCore", "3.17.0", "tools/net6.0/any/nunit3-console.exe"),
    new NUnit3NetCoreConsoleRunner("NUnit.ConsoleRunner.NetCore", "3.15.5", "tools/net6.0/any/nunit3-console.exe")
};

// For .NET Core tests, we override the default and use AllRunners,
// since both the standard and netcore runners can execute them.
var AllRunners = new List<IPackageTestRunner>(StandardRunners.Concat(NetCoreRunners)).ToArray();

// Load the recipe
#load nuget:?package=NUnit.Cake.Recipe&version=1.4.0-alpha.5
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../NUnit.Cake.Recipe/recipe/*.cake

// Initialize BuildSettings
BuildSettings.Initialize(
    Context,
    "NUnit Project Loader",
    "nunit-v2-result-writer",
    solutionFile: "nunit-v2-result-writer.sln",
	unitTestRunner: new NUnitLiteRunner(),
	unitTests: "**/*.tests.exe");

const string NUNIT3_RESULT_FILE = "TestResult.xml";
const string NUNIT2_RESULT_FILE = "NUnit2TestResult.xml";

PackageTest[] PackageTests = new PackageTest[]
{
    new PackageTest(1, "SingleAssembly")
    {
        Description = "Run mock-assembly",
        Arguments = $"net462/mock-assembly.dll --result={NUNIT3_RESULT_FILE} --result={NUNIT2_RESULT_FILE};format=nunit2",
        ExpectedResult = new ExpectedResult("Failed")
        {
             Assemblies = new[] { new ExpectedAssemblyResult("mock-assembly.dll", "net-4.6.2") }
        }
    },

    new PackageTest(1, "SingleAssembly_NetCoreRunner")
    {
        Description = "Run mock-assembly under NetCore runner",
        Arguments = $"net6.0/mock-assembly.dll --result={NUNIT3_RESULT_FILE} --result={NUNIT2_RESULT_FILE};format=nunit2",
        ExpectedResult = new ExpectedResult("Failed")
        {
             Assemblies = new[] { new ExpectedAssemblyResult("mock-assembly.dll", "netcore-6.0") }
        },
		TestRunners = AllRunners
    },

    new PackageTest(1, "TwoAssembliesTogether")
    {
        Description = "Run two copies of mock-assembly",
        Arguments = $"net462/mock-assembly.dll net6.0/mock-assembly.dll --result={NUNIT3_RESULT_FILE} --result={NUNIT2_RESULT_FILE};format=nunit2",
        ExpectedResult =new ExpectedResult("Failed") {
            Assemblies = new[] {
                new ExpectedAssemblyResult("mock-assembly.dll", "net-4.6.2"),
                new ExpectedAssemblyResult("mock-assembly.dll", "netcore-6.0")
            }
        }
    },

	new PackageTest(1, "NUnitProject")
	{
		Description = "Run NUnit project with two assemblies",
		Arguments = $"../../TwoMockAssemblies.nunit --result={NUNIT3_RESULT_FILE} --result={NUNIT2_RESULT_FILE};format=nunit2",
		ExpectedResult = new ExpectedResult("Failed")
		{
			Assemblies = new[] {
				new ExpectedAssemblyResult("mock-assembly.dll", "net-4.6.2"),
				new ExpectedAssemblyResult("mock-assembly.dll", "netcore-6.0")
			}
		},
		ExtensionsNeeded = new[] { KnownExtensions.NUnitProjectLoader.SetVersion("3.8.0") }
	}
};

//////////////////////////////////////////////////////////////////////
// NUGET PACKAGE
//////////////////////////////////////////////////////////////////////

BuildSettings.Packages.Add(
	new NuGetPackage(
		"NUnit.Extension.NUnitV2ResultWriter",
		"nuget/nunit-v2-result-writer.nuspec",
		checks: new PackageCheck[] {
			HasFiles("LICENSE.txt", "nunit_256.png"),
			HasDirectory("tools/net462").WithFile("nunit-v2-result-writer.dll"),
			HasDirectory("tools/net462").WithFile("nunit.engine.api.dll"),
			HasDirectory("tools/net6.0").WithFile("nunit-v2-result-writer.dll"),
			HasDirectory("tools/net6.0").WithFile("nunit.engine.api.dll") },
		tests: PackageTests,
		testRunners: StandardRunners
	));

//////////////////////////////////////////////////////////////////////
// CHOCOLATEY PACKAGE
//////////////////////////////////////////////////////////////////////

BuildSettings.Packages.Add(
	new ChocolateyPackage(
		"nunit-extension-nunit-v2-result-writer",
		"choco/nunit-v2-result-writer.nuspec",
		checks: new PackageCheck[] {
			HasDirectory("tools").WithFiles("LICENSE.txt", "VERIFICATION.txt", "nunit_256.png", "nunit-v2-result-writer.legacy.addins"),
			HasDirectory("tools/net462").WithFile("nunit.engine.api.dll"),
			HasDirectory("tools/net6.0").WithFile("nunit-v2-result-writer.dll"),
			HasDirectory("tools/net6.0").WithFile("nunit.engine.api.dll") },
		tests: PackageTests,
		testRunners: StandardRunners
	));

//////////////////////////////////////////////////////////////////////
// FINAL CHECK AFTER PACKAGE TESTS HAVE RUN
//////////////////////////////////////////////////////////////////////

// The recipe handles checking the standard nunit3 test result file but
// not the nunit2 format file we produce in the package tests. We handle
// that check in a TaskTeardown event. Currently, we only check that the
// file exists.

// TODO: Perform this check for each test as it is run rather than
// after the Package task completes. This requires changes to the
// recipe itself.

// TODO: Check the internal format of the files.

TaskTeardown(teardownContext =>
{
	if (teardownContext.Task.Name == "Package")
	{
		Banner.Display("Check the NUnit2 format result files", '=', 78);

		foreach(var package in BuildSettings.Packages)
		{
			Banner.Display( package.PackageFileName );

			//Console.WriteLine( package.PackageResultDirectory);
			int index = 0;
			foreach (var dir in teardownContext.GetDirectories(package.PackageResultDirectory + "*"))
			{
				var nunit2ResultFile = dir.CombineWithFilePath("NUnit2TestResult.xml");
				Console.WriteLine($"{++index}. {dir.GetDirectoryName()}");
				Console.WriteLine();

				if (SIO.File.Exists($"{nunit2ResultFile}"))
					Console.WriteLine("  SUCCESS: NUnit2 Test Result FOUND");
				else
					Console.WriteLine("  FAILED: NUnit2 TestResult NOT FOUND");
				Console.WriteLine();
			}
		}
	}
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run();
