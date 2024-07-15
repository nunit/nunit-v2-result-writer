#tool nuget:?package=NUnit.ConsoleRunner&version=3.15.5
#tool nuget:?package=NUnit.ConsoleRunner&version=3.17.0
#tool nuget:?package=NUnit.ConsoleRunner&version=3.18.0-dev00037
#tool nuget:?package=NUnit.ConsoleRunner.NetCore&version=3.18.0-dev00037
#tool nuget:?package=NUnit.Extension.NUnitProjectLoader&version=3.6.0

// Load the recipe
#load nuget:?package=NUnit.Cake.Recipe&version=1.0.0-dev00001
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../NUnit.Cake.Recipe/recipe/*.cake

// Initialize BuildSettings
BuildSettings.Initialize(
    Context,
    "NUnit Project Loader",
    "nunit-v2-result-writer",
    solutionFile: "nunit-v2-result-writer.sln",
	unitTestRunner: new NUnitLiteRunner(),
	unitTests: "**/*.Tests.exe");

const string NUNIT3_RESULT_FILE = "TestResult.xml";
const string NUNIT2_RESULT_FILE = "NUnit2TestResult.xml";

PackageTest[] PackageTests = new PackageTest[]
{
	new PackageTest(
		1, "SingleAssembly",
		"Run mock-assembly",
		$"net462/mock-assembly.dll --result={NUNIT3_RESULT_FILE} --result={NUNIT2_RESULT_FILE};format=nunit2",
		new ExpectedResult("Failed")
		{
		 	Assemblies = new[] { new ExpectedAssemblyResult("mock-assembly.dll", "net-4.6.2") }
		} ),
	
	new PackageTest(
		1, "TwoAssemblies",
		"Run two copies of mock-assembly",
		$"net462/mock-assembly.dll net6.0/mock-assembly.dll --result={NUNIT3_RESULT_FILE} --result={NUNIT2_RESULT_FILE};format=nunit2",
		new ExpectedResult("Failed") {
			Assemblies = new[] {
				new ExpectedAssemblyResult("mock-assembly.dll", "net-4.6.2"),
				new ExpectedAssemblyResult("mock-assembly.dll", "netcore-6.0")
			}
		} ),

	new PackageTest(
		1, "NUnitProject",
		"Run NUnit project with two assemblies",
		$"../../TwoMockAssemblies.nunit --result={NUNIT3_RESULT_FILE} --result={NUNIT2_RESULT_FILE};format=nunit2",
		new ExpectedResult("Failed")
		{
			Assemblies = new[] {
				new ExpectedAssemblyResult("mock-assembly.dll", "net-4.6.2"),
				new ExpectedAssemblyResult("mock-assembly.dll", "netcore-6.0")
			}
		},
		KnownExtensions.NUnitProjectLoader )
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
		testRunnerSource: new TestRunnerSource(new NUnitConsoleRunner("3.17.0"), new NUnitConsoleRunner("3.15.5"), new NUnitConsoleRunner("3.18.0-dev00037"))
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
		testRunnerSource: new TestRunnerSource(new NUnitConsoleRunner("3.17.0"), new NUnitConsoleRunner("3.15.5"), new NUnitConsoleRunner("3.18.0-dev00037"))
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
