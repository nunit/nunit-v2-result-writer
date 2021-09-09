#load "./constants.cake"
#load "./packaging.cake"
#load "./test-runner.cake"
#load "./tests.cake"

using System;

public class BuildParameters
{
	private ISetupContext _context;
	private BuildSystem _buildSystem;

	public static BuildParameters Create(ISetupContext context)
	{
		var parameters = new BuildParameters(context);
		//parameters.Validate();

		return parameters;
	}

	private BuildParameters(ISetupContext context)
	{
		_context = context;
		_buildSystem = context.BuildSystem();

		Target = _context.TargetTask.Name;
		TasksToExecute = _context.TasksToExecute.Select(t => t.Name);

		ProjectDirectory = _context.Environment.WorkingDirectory.FullPath + "/";

		Configuration = _context.Argument("configuration", DEFAULT_CONFIGURATION);
		var dbgSuffix = Configuration == "Debug" ? "-dbg" : "";
		PackageVersion = DEFAULT_VERSION + dbgSuffix;

		MyGetApiKey = _context.EnvironmentVariable(MYGET_API_KEY);

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
					ShouldPublishToMyGet = true;
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

	public string Target { get; }
	public IEnumerable<string> TasksToExecute { get; }

	public string Configuration { get; }
	public string PackageVersion { get; set; }

	public bool IsLocalBuild => _buildSystem.IsLocalBuild;
	public bool IsRunningOnUnix => _context.IsRunningOnUnix();
	public bool IsRunningOnWindows => _context.IsRunningOnWindows();
	public bool IsRunningOnAppVeyor => _buildSystem.AppVeyor.IsRunningOnAppVeyor;

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

	public bool ShouldPublishToMyGet { get; } = false;
	public string MyGetPushUrl => MYGET_PUSH_URL;
	public string MyGetApiKey { get; }

	public string GetPathToConsoleRunner(string version)
	{
		return ToolsDirectory + "NUnit.ConsoleRunner." + version + "/tools/nunit3-console.exe";
	}

	public void DumpSettings()
	{
		Console.WriteLine("\nTASKS");
		Console.WriteLine("Target:                       " + Target);
		Console.WriteLine("TasksToExecute:               " + string.Join(", ", TasksToExecute));

		Console.WriteLine("\nENVIRONMENT");
		Console.WriteLine("IsLocalBuild:                 " + IsLocalBuild);
		Console.WriteLine("IsRunningOnWindows:           " + IsRunningOnWindows);
		Console.WriteLine("IsRunningOnUnix:              " + IsRunningOnUnix);
		Console.WriteLine("IsRunningOnAppVeyor:          " + IsRunningOnAppVeyor);

		Console.WriteLine("\nVERSIONING");
		Console.WriteLine("PackageVersion:               " + PackageVersion);
		// Console.WriteLine("AssemblyVersion:              " + AssemblyVersion);
		// Console.WriteLine("AssemblyFileVersion:          " + AssemblyFileVersion);
		// Console.WriteLine("AssemblyInformationalVersion: " + AssemblyInformationalVersion);
		// Console.WriteLine("SemVer:                       " + BuildVersion.SemVer);
		// Console.WriteLine("IsPreRelease:                 " + BuildVersion.IsPreRelease);
		// Console.WriteLine("PreReleaseLabel:              " + BuildVersion.PreReleaseLabel);
		// Console.WriteLine("PreReleaseSuffix:             " + BuildVersion.PreReleaseSuffix);

		Console.WriteLine("\nDIRECTORIES");
		Console.WriteLine("Project:   " + ProjectDirectory);
		Console.WriteLine("Output:    " + OutputDirectory);
		//Console.WriteLine("Source:    " + SourceDirectory);
		//Console.WriteLine("NuGet:     " + NuGetDirectory);
		//Console.WriteLine("Choco:     " + ChocoDirectory);
		Console.WriteLine("Package:   " + PackageDirectory);
		//Console.WriteLine("ZipImage:  " + ZipImageDirectory);
		//Console.WriteLine("ZipTest:   " + ZipTestDirectory);
		//Console.WriteLine("NuGetTest: " + NuGetTestDirectory);
		//Console.WriteLine("ChocoTest: " + ChocolateyTestDirectory);

		Console.WriteLine("\nBUILD");
		//Console.WriteLine("Build With:      " + (UsingXBuild ? "XBuild" : "MSBuild"));
		Console.WriteLine("Configuration:   " + Configuration);
		//Console.WriteLine("Engine Runtimes: " + string.Join(", ", SupportedEngineRuntimes));
		//Console.WriteLine("Core Runtimes:   " + string.Join(", ", SupportedCoreRuntimes));
		//Console.WriteLine("Agent Runtimes:  " + string.Join(", ", SupportedAgentRuntimes));

		Console.WriteLine("\nPACKAGING");
		Console.WriteLine("MyGetPushUrl:              " + MyGetPushUrl);
		//Console.WriteLine("NuGetPushUrl:              " + NuGetPushUrl);
		//Console.WriteLine("ChocolateyPushUrl:         " + ChocolateyPushUrl);
		Console.WriteLine("MyGetApiKey:               " + (!string.IsNullOrEmpty(MyGetApiKey) ? "AVAILABLE" : "NOT AVAILABLE"));
		//Console.WriteLine("NuGetApiKey:               " + (!string.IsNullOrEmpty(NuGetApiKey) ? "AVAILABLE" : "NOT AVAILABLE"));
		//Console.WriteLine("ChocolateyApiKey:          " + (!string.IsNullOrEmpty(ChocolateyApiKey) ? "AVAILABLE" : "NOT AVAILABLE"));

		Console.WriteLine("\nPUBLISHING");
		Console.WriteLine("ShouldPublishToMyGet:      " + ShouldPublishToMyGet);
		// Console.WriteLine("ShouldPublishToNuGet:      " + ShouldPublishToNuGet);
		// Console.WriteLine("ShouldPublishToChocolatey: " + ShouldPublishToChocolatey);

		//Console.WriteLine("\nRELEASING");
		//Console.WriteLine("BranchName:                   " + BranchName);
		//Console.WriteLine("IsReleaseBranch:              " + IsReleaseBranch);
		//Console.WriteLine("IsProductionRelease:          " + IsProductionRelease);
	}
}
