// ***********************************************************************
// Copyright (c) Charlie Poole and contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

#tool nuget:?package=GitVersion.CommandLine&version=5.0.0
#tool nuget:?package=GitReleaseManager&version=0.11.0
#tool nuget:?package=NUnit.ConsoleRunner&version=3.10.0
#tool nuget:?package=NUnit.ConsoleRunner&version=3.11.1
#tool nuget:?package=NUnit.Extension.NUnitProjectLoader&version=3.6.0
//#tool nuget:?package=NUnit.ConsoleRunner&version=3.12.0-beta1&prerelease

////////////////////////////////////////////////////////////////////
// CONSTANTS
//////////////////////////////////////////////////////////////////////

const string SOLUTION_FILE = "nunit-v2-result-writer.sln";
const string NUGET_ID = "NUnit.Extension.NUnitV2ResultWriter";
const string CHOCO_ID = "nunit-extension-nunit-v2-result-writer";
const string GITHUB_OWNER = "nunit";
const string GITHUB_REPO = "nunit-v2-result-writer";
const string DEFAULT_VERSION = "3.8.0";
const string DEFAULT_CONFIGURATION = "Release";

// Load scripts after defining constants
#load cake/parameters.cake

////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

// Additional arguments defined in the cake scripts:
//   --configuration
//   --version

//////////////////////////////////////////////////////////////////////
// SETUP AND TEARDOWN
//////////////////////////////////////////////////////////////////////

Setup<BuildParameters>((context) =>
{
	var parameters = BuildParameters.Create(context);

	if (BuildSystem.IsRunningOnAppVeyor)
		AppVeyor.UpdateBuildVersion(parameters.PackageVersion + "-" + AppVeyor.Environment.Build.Number);

	Information("Building {0} version {1} of V2 Result Writer Extension.", parameters.Configuration, parameters.PackageVersion);

	return parameters;
});

//////////////////////////////////////////////////////////////////////
// DUMP SETTINGS
//////////////////////////////////////////////////////////////////////

Task("DumpSettings")
	.Does<BuildParameters>((parameters) =>
	{
		parameters.DumpSettings();
	});

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does<BuildParameters>((parameters) =>
	{
		Information("Cleaning " + parameters.OutputDirectory);
		CleanDirectory(parameters.OutputDirectory);
	});

Task("CleanAll")
	.Does<BuildParameters>((parameters) =>
	{
		Information("Cleaning all output directories");
		CleanDirectory(parameters.ProjectDirectory + "bin/");

		Information("Deleting object directories");
		DeleteObjectDirectories(parameters);
	});

//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("NuGetRestore")
    .Does(() =>
	{
		NuGetRestore(SOLUTION_FILE, new NuGetRestoreSettings()
		{
			Source = new string[]
			{
				"https://www.nuget.org/api/v2",
				"https://www.myget.org/F/nunit/api/v2"
			}
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
		string unitTests = parameters.OutputDirectory + "net20/nunit-v2-result-writer.tests.dll";

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

Task("BuildNuGetPackage")
	.Does<BuildParameters>((parameters) =>
	{
		CreateDirectory(parameters.PackageDirectory);
		BuildNuGetPackage(parameters);
    });

Task("InstallNuGetPackage")
	.Does<BuildParameters>((parameters) =>
	{
		// Ensure we aren't inadvertently using the chocolatey install
		if (DirectoryExists(parameters.ChocolateyInstallDirectory))
			DeleteDirectory(parameters.ChocolateyInstallDirectory, new DeleteDirectorySettings() { Recursive = true });

		CreateDirectory(parameters.NuGetInstallDirectory);
		CleanDirectory(parameters.NuGetInstallDirectory);
		Unzip(parameters.NuGetPackage, parameters.NuGetInstallDirectory);

		Information($"Unzipped {parameters.NuGetPackageName} to { parameters.NuGetInstallDirectory}");
	});

Task("VerifyNuGetPackage")
	.IsDependentOn("InstallNuGetPackage")
	.Does<BuildParameters>((parameters) =>
	{
		Check.That(parameters.NuGetInstallDirectory,
			HasFiles("CHANGES.md", "LICENSE.txt"),
			HasDirectory("tools/net20").WithFile("nunit-v2-result-writer.dll"),
			HasDirectory("tools/netcoreapp2.1").WithFile("nunit-v2-result-writer.dll"));
		Information("Verification was successful!");
	});

Task("TestNuGetPackage")
	.IsDependentOn("InstallNuGetPackage")
	.Does<BuildParameters>((parameters) =>
	{
		new NuGetPackageTester(parameters).RunPackageTests(PackageTests);
	});

Task("BuildChocolateyPackage")
	.Does<BuildParameters>((parameters) =>
	{
		CreateDirectory(parameters.PackageDirectory);
		BuildChocolateyPackage(parameters);
	});

Task("InstallChocolateyPackage")
	.Does<BuildParameters>((parameters) =>
	{
		// Ensure we aren't inadvertently using the nuget install
		if (DirectoryExists(parameters.NuGetInstallDirectory))
			DeleteDirectory(parameters.NuGetInstallDirectory, new DeleteDirectorySettings() { Recursive = true });

		CreateDirectory(parameters.ChocolateyInstallDirectory);
		CleanDirectory(parameters.ChocolateyInstallDirectory);
		Unzip(parameters.ChocolateyPackage, parameters.ChocolateyInstallDirectory);

		Information($"Unzipped {parameters.ChocolateyPackageName} to { parameters.ChocolateyInstallDirectory}");
	});

Task("VerifyChocolateyPackage")
	.IsDependentOn("InstallChocolateyPackage")
	.Does<BuildParameters>((parameters) =>
	{
		Check.That(parameters.ChocolateyInstallDirectory,
			HasDirectory("tools").WithFiles("CHANGES.md", "LICENSE.txt", "VERIFICATION.txt"),
			HasDirectory("tools/net20").WithFile("nunit-v2-result-writer.dll"),
			HasDirectory("tools/netcoreapp2.1").WithFile("nunit-v2-result-writer.dll"));
		Information("Verification was successful!");
	});

Task("TestChocolateyPackage")
	.IsDependentOn("InstallChocolateyPackage")
	.Does<BuildParameters>((parameters) =>
	{
		new ChocolateyPackageTester(parameters).RunPackageTests(PackageTests);
	});

PackageTest[] PackageTests = new PackageTest[]
{
	new PackageTest()
	{
		Description = "Run mock-assembly",
		Arguments = $"bin/Release/net20/mock-assembly.dll --result={NUNIT3_RESULT_FILE} --result={NUNIT2_RESULT_FILE};format=nunit2",
		TestConsoleVersions = new [] { "3.10.0", "3.11.1" },
		ExpectedResult = new ExpectedResult("Failed")
		{
		 	Assemblies = new[] { new ExpectedAssemblyResult("mock-assembly.dll", "net-2.0") }
		}
	},
	new PackageTest()
	{
		Description = "Run two copies of mock-assembly",
		Arguments = $"bin/Release/net20/mock-assembly.dll bin/Release/net20/mock-assembly.dll --result={NUNIT3_RESULT_FILE} --result={NUNIT2_RESULT_FILE};format=nunit2",
		TestConsoleVersions = new[] { "3.11.1" },
		ExpectedResult = new ExpectedResult("Failed")
		{
			Assemblies = new[] {
						 new ExpectedAssemblyResult("mock-assembly.dll", "net-2.0"),
						 new ExpectedAssemblyResult("mock-assembly.dll", "net-2.0")
					}
		}
	},
	new PackageTest()
	{
		Description = "Run NUnit project with two assemblies",
		Arguments = $"TwoMockAssemblies.nunit --result={NUNIT3_RESULT_FILE} --result={NUNIT2_RESULT_FILE};format=nunit2",
		TestConsoleVersions = new[] { "3.11.1" },
		ExpectedResult = new ExpectedResult("Failed")
		{
			Assemblies = new[] {
						 new ExpectedAssemblyResult("mock-assembly.dll", "net-2.0"),
						 new ExpectedAssemblyResult("mock-assembly.dll", "net-2.0")
					}
		}
	}
};

//////////////////////////////////////////////////////////////////////
// PUBLISH
//////////////////////////////////////////////////////////////////////

static bool hadPublishingErrors = false;

Task("PublishPackages")
	.Description("Publish nuget and chocolatey packages according to the current settings")
	.IsDependentOn("PublishToMyGet")
	.IsDependentOn("PublishToNuGet")
	.IsDependentOn("PublishToChocolatey")
	.Does(() =>
	{
		if (hadPublishingErrors)
			throw new Exception("One of the publishing steps failed.");
	});

// This task may either be run by the PublishPackages task,
// which depends on it, or directly when recovering from errors.
Task("PublishToMyGet")
	.Description("Publish packages to MyGet")
	.Does<BuildParameters>((parameters) =>
	{
		if (!parameters.ShouldPublishToMyGet)
			Information("Nothing to publish to MyGet from this run.");
		else
			try
			{
				PushNuGetPackage(parameters.NuGetPackage, parameters.MyGetApiKey, parameters.MyGetPushUrl);
				PushChocolateyPackage(parameters.ChocolateyPackage, parameters.MyGetApiKey, parameters.MyGetPushUrl);
			}
			catch (Exception)
			{
				hadPublishingErrors = true;
			}
	});

// This task may either be run by the PublishPackages task,
// which depends on it, or directly when recovering from errors.
Task("PublishToNuGet")
	.Description("Publish packages to NuGet")
	.Does<BuildParameters>((parameters) =>
	{
		if (!parameters.ShouldPublishToNuGet)
			Information("Nothing to publish to NuGet from this run.");
		else
			try
			{
				PushNuGetPackage(parameters.NuGetPackage, parameters.NuGetApiKey, parameters.NuGetPushUrl);
			}
			catch (Exception)
			{
				hadPublishingErrors = true;
			}
	});

// This task may either be run by the PublishPackages task,
// which depends on it, or directly when recovering from errors.
Task("PublishToChocolatey")
	.Description("Publish packages to Chocolatey")
	.Does<BuildParameters>((parameters) =>
	{
		if (!parameters.ShouldPublishToChocolatey)
			Information("Nothing to publish to Chocolatey from this run.");
		else
			try
			{
				PushChocolateyPackage(parameters.ChocolateyPackage, parameters.ChocolateyApiKey, parameters.ChocolateyPushUrl);
			}
			catch (Exception)
			{
				hadPublishingErrors = true;
			}
	});

//////////////////////////////////////////////////////////////////////
// CREATE A DRAFT RELEASE
//////////////////////////////////////////////////////////////////////

Task("CreateDraftRelease")
	.Does<BuildParameters>((parameters) =>
	{
		if (parameters.IsReleaseBranch)
		{
			// NOTE: Since this is a release branch, the pre-release label
			// is "pre", which we don't want to use for the draft release.
			// The branch name contains the full information to be used
			// for both the name of the draft release and the milestone,
			// i.e. release-2.0.0, release-2.0.0-beta2, etc.
			string milestone = parameters.BranchName.Substring(8);
			string releaseName = $"NUnit V2 Result Writer Extension {milestone}";

			Information($"Creating draft release...");

			try
			{
				GitReleaseManagerCreate(parameters.GitHubAccessToken, GITHUB_OWNER, GITHUB_REPO, new GitReleaseManagerCreateSettings()
				{
					Name = releaseName,
					Milestone = milestone
				});
			}
			catch
			{
				Error($"Unable to create draft release for {releaseName}.");
				Error($"Check that there is a {milestone} milestone with at least one closed issue.");
				Error("");
				throw;
			}
		}
		else
		{
			Information("Skipping Release creation because this is not a release branch");
		}
	});

Task("ExportDraftRelease")
	.Description("Export draft release locally for use in updating CHANGES.md")
	.Does<BuildParameters>((parameters) =>
	{
		if (parameters.IsReleaseBranch && parameters.IsLocalBuild)
		{
			string milestone = parameters.BranchName.Substring(8);

			GitReleaseManagerExport(parameters.GitHubAccessToken, GITHUB_OWNER, GITHUB_REPO, "DraftRelease.md",
				new GitReleaseManagerExportSettings() { TagName = milestone });
		}
		else
			Error("ExportDraftRelease may only be run locally, using a release branch!");
	});

//////////////////////////////////////////////////////////////////////
// CREATE A PRODUCTION RELEASE
//////////////////////////////////////////////////////////////////////

Task("CreateProductionRelease")
	.Does<BuildParameters>((parameters) =>
	{
		if (parameters.IsProductionRelease)
		{
			string token = parameters.GitHubAccessToken;
			string tagName = parameters.PackageVersion;
			string assets = $"\"{parameters.NuGetPackage},{parameters.ChocolateyPackage}\"";

			Information($"Publishing release {tagName} to GitHub");

			GitReleaseManagerAddAssets(token, GITHUB_OWNER, GITHUB_REPO, tagName, assets);
			GitReleaseManagerClose(token, GITHUB_OWNER, GITHUB_REPO, tagName);
		}
		else
		{
			Information("Skipping CreateProductionRelease because this is not a production release");
		}
	});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Package")
	.IsDependentOn("Build")
	.IsDependentOn("PackageNuGet")
	.IsDependentOn("PackageChocolatey");

Task("PackageNuGet")
	.IsDependentOn("BuildNuGetPackage")
	.IsDependentOn("VerifyNuGetPackage")
	.IsDependentOn("TestNuGetPackage");

Task("PackageChocolatey")
	.IsDependentOn("BuildChocolateyPackage")
	.IsDependentOn("VerifyChocolateyPackage")
	.IsDependentOn("TestChocolateyPackage");

Task("Full")
	.IsDependentOn("Clean")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package");

Task("Appveyor")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package")
	.IsDependentOn("PublishPackages")
	.IsDependentOn("CreateDraftRelease")
	.IsDependentOn("CreateProductionRelease");

Task("Travis")
	.IsDependentOn("Build")
	.IsDependentOn("Test");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
