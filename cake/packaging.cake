using System.Xml;

//////////////////////////////////////////////////////////////////////
// PACKAGE METADATA
//////////////////////////////////////////////////////////////////////

const string TITLE = "NUnit 3 - NUnit V2 Result Writer Extension";
static readonly string[] AUTHORS = new[] { "Charlie Poole" };
static readonly string[] OWNERS = new[] { "Charlie Poole" };
const string DESCRIPTION = "This extension allows NUnit to create result files in the V2 format, which is used by many CI servers.";
const string SUMMARY = "NUnit Engine extension for writing test result files in NUnit V2 format.";
const string COPYRIGHT = "Copyright (c) 2016 Charlie Poole";
static readonly string[] RELEASE_NOTES = new [] { "See https://raw.githubusercontent.com/nunit/nunit-v2-result-writer/main/CHANGES.txt" };
static readonly string[] TAGS = new[] { "nunit", "test", "testing", "tdd", "runner" };
static readonly Uri PROJECT_URL = new Uri("http://nunit.org");
static readonly Uri ICON_URL = new Uri("https://cdn.rawgit.com/nunit/resources/master/images/icon/nunit_256.png");
static readonly Uri LICENSE_URL = new Uri("http://nunit.org/nuget/nunit3-license.txt");
const string GITHUB_SITE = "https://github.com/nunit/nunit-v2-result-writer";
const string WIKI_PAGE = "https://github.com/nunit/docs/wiki/Console-Command-Line";
static readonly Uri PROJECT_SOURCE_URL = new Uri(GITHUB_SITE);
static readonly Uri PACKAGE_SOURCE_URL = new Uri(GITHUB_SITE);
static readonly Uri BUG_TRACKER_URL = new Uri(GITHUB_SITE + "/issues");
static readonly Uri DOCS_URL = new Uri(WIKI_PAGE);
static readonly Uri MAILING_LIST_URL = new Uri("https://groups.google.com/forum/#!forum/nunit-discuss");

//////////////////////////////////////////////////////////////////////
// PACKAGE TESTER
//////////////////////////////////////////////////////////////////////

const string NUNIT3_RESULT_FILE = "NUnit3TestResult.xml";
const string NUNIT2_RESULT_FILE = "NUnit2TestResult.xml";

public abstract class PackageTester
{
	protected BuildParameters _parameters;
	protected ICakeContext _context;

	public PackageTester(BuildParameters parameters)
    {
		_parameters = parameters;
		_context = parameters.Context;
    }

	public abstract string Package { get; }
	public abstract string InstallDirectory { get; }
	public abstract PackageCheck[] PackageChecks { get; }
	public PackageTest[] PackageTests = new PackageTest[]
	{
		new PackageTest()
		{
			Description = "Run mock-assembly under 3.10.0 console",
			Files = new [] {
				$"bin/Release/net20/mock-assembly.dll" },
			ConsoleVersion = "3.10.0"
		},
		new PackageTest()
		{
			Description = "Run mock-assembly under 3.11.1 console",
			Files = new [] {
				$"bin/Release/net20/mock-assembly.dll" },
			ConsoleVersion = "3.11.1"
		},
		new PackageTest()
		{
			Description = "Run two copies of mock-assembly under 3.11.1 console",
			Files = new [] {
				$"bin/Release/net20/mock-assembly.dll",
				$"bin/Release/net20/mock-assembly.dll" },
			ConsoleVersion = "3.11.1"
		},
		new PackageTest()
		{
			Description = "Run NUnit project with files under 3.11.1 console",
			Files = new [] {
				$"TwoMockAssemblies.nunit" },
			ConsoleVersion = "3.11.1"
		},
	};

	public void InstallPackage()
	{
		_context.CleanDirectory(InstallDirectory);
		_context.Unzip(Package, InstallDirectory);
	}

	public void VerifyPackage()
    {
		_context.Information("Verifying NuGet package content...");
		Check.That(InstallDirectory, PackageChecks);
		_context.Information("Verification was successful!");
	}

	public void RunPackageTests()
    {
		foreach (var packageTest in PackageTests)
		{
			Banner(packageTest.Description);
			RunConsoleTests(packageTest.ConsoleVersion, packageTest.Files);

			Banner($"Verifying contents of {NUNIT2_RESULT_FILE}");
			TestRunner.Run(typeof(ResultWriterTests), typeof(SchemaValidationTests));
		}
	}

	public void UninstallPackage()
	{
		_context.DeleteDirectory(InstallDirectory, new DeleteDirectorySettings() { Recursive = true });
	}

	private void RunConsoleTests(string consoleVersion, string[] assemblies)
    {
		string runner = _parameters.GetPathToConsoleRunner(consoleVersion);

		if (InstallDirectory.EndsWith(CHOCO_ID + "/"))
		{
			// We are using nuget packages for the runner, so add an extra
			// addins file to allow detecting chocolatey packages
			string runnerDir = System.IO.Path.GetDirectoryName(runner);
			using (var writer = new StreamWriter(runnerDir + "/choco.engine.addins"))
				writer.WriteLine("../../nunit-extension-*/tools/");
		}

		if (_context.FileExists(NUNIT2_RESULT_FILE))
			_context.DeleteFile(NUNIT2_RESULT_FILE);
		if (_context.FileExists(NUNIT3_RESULT_FILE))
			_context.DeleteFile(NUNIT3_RESULT_FILE);

		var args = string.Join(" ", assemblies) + $" --result:{NUNIT3_RESULT_FILE} --result:{NUNIT2_RESULT_FILE};format=nunit2";

		_context.StartProcess(runner, args);
		// We don't check the error code because we know that
		// mock-assembly returns -4 due to a bad fixture.

		// Should have created two result files
		bool file1 = _context.FileExists(NUNIT3_RESULT_FILE);
		bool file2 = _context.FileExists(NUNIT2_RESULT_FILE);
		if (!file1 || !file2)
		{
			string msg = file1
				? "The nunit2 result file was not created."
				: file2
					? "The nunit3 result file was not created."
					: "The nunit3 and nunit2 result files were not created.";

			throw new System.Exception(msg);
		}
	}

	private void Banner(string message)
	{
		_context.Information("\n=======================================================");
		_context.Information(message);
		_context.Information("=======================================================");
	}
}

public class PackageTest
{
	public string Description { get; set; }
	public string[] Files { get; set; }
	public string ConsoleVersion { get; set; }
}

public class NuGetPackageTester : PackageTester
{
    public NuGetPackageTester(BuildParameters parameters) : base(parameters) { }

	public override string Package => _parameters.NuGetPackage;
	public override string InstallDirectory => _parameters.NuGetInstallDirectory;
	public override PackageCheck[] PackageChecks => new PackageCheck[]
	{
		HasFiles("CHANGES.txt", "LICENSE.txt"),
		HasDirectory("tools/net20").WithFile("nunit-v2-result-writer.dll"),
		HasDirectory("tools/netcoreapp2.1").WithFile("nunit-v2-result-writer.dll")
    };
}

public class ChocolateyPackageTester : PackageTester
{
    public ChocolateyPackageTester(BuildParameters parameters) : base(parameters) { }

	public override string Package => _parameters.ChocolateyPackage;
	public override string InstallDirectory => _parameters.ChocolateyInstallDirectory;
	public override PackageCheck[] PackageChecks => new PackageCheck[]
	{
		HasDirectory("tools").WithFiles("CHANGES.txt", "LICENSE.txt", "VERIFICATION.txt"),
		HasDirectory("tools/net20").WithFile("nunit-v2-result-writer.dll"),
		HasDirectory("tools/netcoreapp2.1").WithFile("nunit-v2-result-writer.dll")
    };
}

//////////////////////////////////////////////////////////////////////
// BUILD NUGET PACKAGE
//////////////////////////////////////////////////////////////////////

public void BuildNuGetPackage(BuildParameters parameters)
{
	NuGetPack(
		new NuGetPackSettings()
		{
			Id = NUGET_ID,
			Version = parameters.PackageVersion,
			Title = TITLE,
			Authors = AUTHORS,
			Owners = OWNERS,
			Description = DESCRIPTION,
			Summary = SUMMARY,
			ProjectUrl = PROJECT_URL,
			IconUrl = ICON_URL,
				//Icon = "nunit.ico", // Waiting for Cake release
				License = new NuSpecLicense() { Type = "expression", Value = "MIT" },
				//LicenseUrl = LICENSE_URL,
				RequireLicenseAcceptance = false,
			Copyright = COPYRIGHT,
			ReleaseNotes = RELEASE_NOTES,
			Tags = TAGS,
				//Language = "en-US",
				OutputDirectory = parameters.PackageDirectory,
			KeepTemporaryNuSpecFile = false,
			Files = new[] {
					new NuSpecContent { Source = parameters.ProjectDirectory + "LICENSE.txt" },
					new NuSpecContent { Source = parameters.ProjectDirectory + "CHANGES.txt" },
					new NuSpecContent { Source = parameters.ProjectDirectory + "net20.engine.addins", Target = "tools" },
					new NuSpecContent { Source = parameters.Net20OutputDirectory + OUTPUT_ASSEMBLY, Target = "tools/net20" },
					new NuSpecContent { Source = parameters.NetCore21OutputDirectory + OUTPUT_ASSEMBLY, Target = "tools/netcoreapp2.1" }
			}
		});
}

//////////////////////////////////////////////////////////////////////
// BUILD CHOCOLATEY PACKAGE
//////////////////////////////////////////////////////////////////////

public void BUildChocolateyPackage(BuildParameters parameters)
{
	ChocolateyPack(
		new ChocolateyPackSettings()
		{
			Id = CHOCO_ID,
			Version = parameters.PackageVersion,
			Title = TITLE,
			Authors = AUTHORS,
			Owners = OWNERS,
			Description = DESCRIPTION,
			Summary = SUMMARY,
			ProjectUrl = PROJECT_URL,
			IconUrl = ICON_URL,
			LicenseUrl = LICENSE_URL,
			RequireLicenseAcceptance = false,
			Copyright = COPYRIGHT,
			ProjectSourceUrl = PROJECT_SOURCE_URL,
			DocsUrl = DOCS_URL,
			BugTrackerUrl = BUG_TRACKER_URL,
			PackageSourceUrl = PACKAGE_SOURCE_URL,
			MailingListUrl = MAILING_LIST_URL,
			ReleaseNotes = RELEASE_NOTES,
			Tags = TAGS,
				//Language = "en-US",
				OutputDirectory = parameters.PackageDirectory,
			Files = new[] {
					new ChocolateyNuSpecContent { Source = parameters.ProjectDirectory + "LICENSE.txt", Target = "tools" },
					new ChocolateyNuSpecContent { Source = parameters.ProjectDirectory + "CHANGES.txt", Target = "tools" },
					new ChocolateyNuSpecContent { Source = parameters.ProjectDirectory + "VERIFICATION.txt", Target = "tools" },
					new ChocolateyNuSpecContent { Source = parameters.ProjectDirectory + "net20.engine.addins", Target = "tools" },
					new ChocolateyNuSpecContent { Source = parameters.Net20OutputDirectory + OUTPUT_ASSEMBLY, Target = "tools/net20" },
					new ChocolateyNuSpecContent { Source = parameters.NetCore21OutputDirectory + OUTPUT_ASSEMBLY, Target = "tools/netcoreapp2.1" }
			}
		});
}

//////////////////////////////////////////////////////////////////////
// PACKAGE CHECKS
//////////////////////////////////////////////////////////////////////

private static class Check
{
	public static void That(string testDir, params PackageCheck[] checks)
    {
		foreach (var check in checks)
			check.ApplyTo(testDir);
    }
}

private static FileCheck HasFile(string file) => HasFiles(new[] { file });
private static FileCheck HasFiles(params string[] files) => new FileCheck(files);

private static DirectoryCheck HasDirectory(string dir) => new DirectoryCheck(dir);

public abstract class PackageCheck
{
	public abstract void ApplyTo(string testDir);
}

public class FileCheck : PackageCheck
{
	string[] _files;

	public FileCheck(string[] files)
	{
		_files = files;
	}

	public override void ApplyTo(string testDir)
	{
		foreach (string file in _files)
		{
			if (!System.IO.File.Exists(System.IO.Path.Combine(testDir, file)))
				throw new Exception($"File {file} was not found.");
		}
	}
}

public class DirectoryCheck : PackageCheck
{
	private string _path;
	private List<string> _files = new List<string>();

	public DirectoryCheck(string path)
	{
		_path = path;
	}

	public DirectoryCheck WithFiles(params string[] files)
	{
		_files.AddRange(files);
		return this;
	}

	public DirectoryCheck WithFile(string file)
	{
		_files.Add(file);
		return this;
	}

	public override void ApplyTo(string testDir)
	{
		string combinedPath = System.IO.Path.Combine(testDir, _path);

		if (!System.IO.Directory.Exists(combinedPath))
			throw new Exception($"Directory {_path} was not found.");

		if (_files != null)
		{
			foreach (var file in _files)
			{
				if (!System.IO.File.Exists(System.IO.Path.Combine(combinedPath, file)))
					throw new Exception($"File {file} was not found in directory {_path}.");
			}
		}
	}
}

private void PushNuGetPackage(FilePath package, string apiKey, string url)
{
	CheckPackageExists(package);
	NuGetPush(package, new NuGetPushSettings() { ApiKey = apiKey, Source = url });
}

private void PushChocolateyPackage(FilePath package, string apiKey, string url)
{
	CheckPackageExists(package);
	ChocolateyPush(package, new ChocolateyPushSettings() { ApiKey = apiKey, Source = url });
}

private void CheckPackageExists(FilePath package)
{
	if (!FileExists(package))
		throw new InvalidOperationException(
			$"Package not found: {package.GetFilename()}.\nCode may have changed since package was last built.");
}
