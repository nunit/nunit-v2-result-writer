// ***********************************************************************
// Copyright (c) Charlie Poole and contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

using System.Xml;

//////////////////////////////////////////////////////////////////////
// PACKAGE TEST
//////////////////////////////////////////////////////////////////////

public class PackageTest
{
	public string Description { get; set; }
	public string Arguments { get; set; }
	public string[] TestConsoleVersions { get; set; }
	public ExpectedResult ExpectedResult { get; set; }
}

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

	protected abstract string PackageName { get; }
	protected abstract string PackageUnderTest { get; }
	public abstract string InstallDirectory { get; }

	public PackageCheck[] PackageChecks { get; set; }

	// TODO: Package testing for this extension has been modified
	// to deal with two different result files. This logic is
	// specific to the extension itself and should be extracted.

	public void RunPackageTests(IList<PackageTest> packageTests)
    {
		var reporter = new ResultReporter(PackageName);

		foreach (var packageTest in packageTests)
		{
			foreach (var consoleVersion in packageTest.TestConsoleVersions)
			{
				// Delete result files ahead of time so we don't mistakenly
				// read a left-over file from another test run. Leave the
				// files after the run in case we need to debug a failure.
				if (_context.FileExists(NUNIT2_RESULT_FILE))
					_context.DeleteFile(NUNIT2_RESULT_FILE);
				if (_context.FileExists(NUNIT3_RESULT_FILE))
					_context.DeleteFile(NUNIT3_RESULT_FILE);

				DisplayBanner(packageTest.Description + " - Console Version " + consoleVersion);

				RunConsoleTest(consoleVersion, packageTest.Arguments);

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

				try
				{
					var result = new ActualResult(_parameters.ProjectDirectory + NUNIT3_RESULT_FILE);
					var report = new PackageTestReport(packageTest, consoleVersion, result);
					reporter.AddReport(report);

					Console.WriteLine(report.Errors.Count == 0
						? "\nSUCCESS: Test Result matches expected result!"
						: "\nERROR: Test Result not as expected!");
				}
				catch (Exception ex)
				{
					reporter.AddReport(new PackageTestReport(packageTest, consoleVersion, ex));

					Console.WriteLine($"\nERROR: No result found.");
					Console.WriteLine(ex.ToString());
				}

 				DisplayBanner($"Verifying contents of {NUNIT2_RESULT_FILE}");
 				TestRunner.Run(typeof(ResultWriterTests), typeof(SchemaValidationTests));
			}
		}

		bool anyErrors = reporter.ReportResults();
		Console.WriteLine();

		// All package tests are run even if one of them fails. If there are
		// any errors,  we stop the run at this point.
		if (anyErrors)
			throw new Exception("One or more package tests had errors!");
	}

	private void RunConsoleTest(string consoleVersion, string arguments)
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

		_context.StartProcess(runner, arguments);
	}

	private void DisplayBanner(string message)
	{
		Console.WriteLine();
		Console.WriteLine("=======================================================");
		Console.WriteLine(message);
		Console.WriteLine("=======================================================");
	}
}

public class NuGetPackageTester : PackageTester
{
    public NuGetPackageTester(BuildParameters parameters) : base(parameters) { }

	protected override string PackageName => _parameters.NuGetPackageName;
	protected override string PackageUnderTest => _parameters.NuGetPackage;
	public override string InstallDirectory => _parameters.NuGetInstallDirectory;
}

public class ChocolateyPackageTester : PackageTester
{
    public ChocolateyPackageTester(BuildParameters parameters) : base(parameters) { }

	protected override string PackageName => _parameters.ChocolateyPackageName;
	protected override string PackageUnderTest => _parameters.ChocolateyPackage;
	public override string InstallDirectory => _parameters.ChocolateyInstallDirectory;
}
