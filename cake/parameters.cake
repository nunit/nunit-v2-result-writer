#load "./constants.cake"
#load "./packaging.cake"
#load "./test-runner.cake"
#load "./tests.cake"

using System;

public class BuildParameters
{
	private ISetupContext _context;
	private BuildSystem _buildSystem;

	public BuildParameters(ISetupContext context)
	{
		_context = context;
		_buildSystem = context.BuildSystem();

		ProjectDirectory = _context.Environment.WorkingDirectory.FullPath + "/";

		Configuration = _context.Argument("configuration", DEFAULT_CONFIGURATION);
		var dbgSuffix = Configuration == "Debug" ? "-dbg" : "";
		PackageVersion = DEFAULT_VERSION + dbgSuffix;

		if (_context.BuildSystem().IsRunningOnAppVeyor)
		{
			var appVeyor = _context.AppVeyor();
			var tag = appVeyor.Environment.Repository.Tag;

			if (tag.IsTag)
			{
				PackageVersion = tag.Name;
			}
			else
			{
				var buildNumber = appVeyor.Environment.Build.Number.ToString("00000");
				var branch = appVeyor.Environment.Repository.Branch;
				var isPullRequest = appVeyor.Environment.PullRequest.IsPullRequest;

				if (branch == "main" && !isPullRequest)
				{
					PackageVersion = DEFAULT_VERSION + "-dev-" + buildNumber + dbgSuffix;
				}
				else
				{
					var suffix = "-ci-" + buildNumber + dbgSuffix;

					if (isPullRequest)
						suffix += "-pr-" + appVeyor.Environment.PullRequest.Number;
					else
						suffix += "-" + branch;

					// Nuget limits "special version part" to 20 chars. Add one for the hyphen.
					if (suffix.Length > 21)
						suffix = suffix.Substring(0, 21);

					suffix = suffix.Replace(".", "");

					PackageVersion = DEFAULT_VERSION + suffix;
				}

				appVeyor.UpdateBuildVersion(PackageVersion + "-" + appVeyor.Environment.Build.Number);
			}
		}
	}

	public ICakeContext Context => _context;

	public string Configuration { get; }
	public string PackageVersion { get; set; }

	// Directories
	public string ProjectDirectory { get; }
	public string OutputDirectory => ProjectDirectory + "bin/" + Configuration + "/";
	public string Net20OutputDirectory => OutputDirectory + "net20/";
	public string NetCore21OutputDirectory => OutputDirectory + "netcoreapp2.1/";
	public string PackageDirectory => ProjectDirectory + "output/";
	public string ToolsDirectory => ProjectDirectory + "tools/";
	public string NuGetInstallDirectory => ToolsDirectory + NUGET_ID + "/";
	public string ChocolateyInstallDirectory => ToolsDirectory + CHOCO_ID + "/";

	// Files
	public string NuGetPackage => PackageDirectory + NUGET_ID + "." + PackageVersion + ".nupkg";
	public string ChocolateyPackage => PackageDirectory + CHOCO_ID + "." + PackageVersion + ".nupkg";

	// These are all used for the package tests. There must be
	// a #tool directive for each one at the start of this file.
	public string[] SupportedConsoleVersions => new string[] {
		"3.10.0",
		"3.11.1",
		//"3.12.0-beta1"
	};

	public string GetPathToConsoleRunner(string version)
	{
		return ToolsDirectory + "NUnit.ConsoleRunner." + version + "/tools/nunit3-console.exe";
	}
}
