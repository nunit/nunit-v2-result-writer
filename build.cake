#tool nuget:?package=NUnit.ConsoleRunner&version=3.10.0
#tool nuget:?package=NUnit.ConsoleRunner&version=3.11.1
//#tool nuget:?package=NUnit.ConsoleRunner&version=3.12.0-beta1&prerelease

////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

#load cake/parameters.cake

//////////////////////////////////////////////////////////////////////
// SETUP AND TEARDOWN
//////////////////////////////////////////////////////////////////////

Setup<BuildParameters>((context) =>
{
	var parameters = new BuildParameters(context);

	Information("Building {0} version {1} of TestCentric GUI.", parameters.Configuration, parameters.PackageVersion);

	return parameters;
});

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does<BuildParameters>((parameters) =>
{
    CleanDirectory(parameters.OutputDirectory);
});


//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("NuGetRestore")
    .Does(() =>
{
    NuGetRestore(SOLUTION_FILE, new NuGetRestoreSettings()
	{
		Source = PACKAGE_SOURCES
	});
});

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("NuGetRestore")
    .Does<BuildParameters>((parameters) => 
	{
		if(IsRunningOnWindows())
		{
			MSBuild(SOLUTION_FILE, new MSBuildSettings()
				.SetConfiguration(parameters.Configuration)
				.SetMSBuildPlatform(MSBuildPlatform.Automatic)
				.SetVerbosity(Verbosity.Minimal)
				.SetNodeReuse(false)
				.SetPlatformTarget(PlatformTarget.MSIL)
			);
		}
		else
		{
			XBuild(SOLUTION_FILE, new XBuildSettings()
				.WithTarget("Build")
				.WithProperty("Configuration", parameters.Configuration)
				.SetVerbosity(Verbosity.Minimal)
			);
		}
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

Task("Test")
	.IsDependentOn("Build")
	.Does<BuildParameters>((parameters) =>
	{
		// This version is used for the unit tests
		var runner = parameters.GetPathToConsoleRunner("3.11.1");
		string unitTests = parameters.Net20OutputDirectory + UNIT_TEST_ASSEMBLY;

		int rc = StartProcess(runner, unitTests);
		if (rc == 1)
			throw new System.Exception($"{rc} test failed.");
		else if (rc > 1)
			throw new System.Exception($"{rc} tests failed.");
		else if (rc < 0)
			throw new System.Exception($"Error code {rc}.");
	});

//////////////////////////////////////////////////////////////////////
// PACKAGING
//////////////////////////////////////////////////////////////////////

Task("PackageNuGet")
	.IsDependentOn("Build")
	.Does<BuildParameters>((parameters) =>
	{
		CreateDirectory(parameters.PackageDirectory);

		BuildNuGetPackage(parameters);

		var tester = new NuGetPackageTester(parameters);

		tester.InstallPackage();
		tester.VerifyPackage();
		tester.RunPackageTests();

		// In case of error, this will not be executed, leaving the directory available for examination
		tester.UninstallPackage();
    });

Task("PackageChocolatey")
	.IsDependentOn("Build")
	.Does<BuildParameters>((parameters) =>
	{
		CreateDirectory(parameters.PackageDirectory);

		BUildChocolateyPackage(parameters);

		var tester = new ChocolateyPackageTester(parameters);

		tester.InstallPackage();
		tester.VerifyPackage();
		tester.RunPackageTests();

		// In case of error, this will not be executed, leaving the directory available for examination
		tester.UninstallPackage();
	});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Package")
	.IsDependentOn("PackageNuGet")
	.IsDependentOn("PackageChocolatey");

Task("All")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package");

Task("Appveyor")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package");

Task("Travis")
	.IsDependentOn("Build")
	.IsDependentOn("Test");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
