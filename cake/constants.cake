// This file contains both constants and static readonly values, which
// are used as constants. The latter must not depend in any way on the
// contents of other cake files, which are loaded after this one.

// NOTE: Since GitVersion is only used when running under
// Windows, the default version should be updated to the 
// next version after each release.
const string DEFAULT_VERSION = "3.7.0";
const string DEFAULT_CONFIGURATION = "Release";

// Files
const string SOLUTION_FILE = "nunit-v2-result-writer.sln";
const string OUTPUT_ASSEMBLY = "nunit-v2-result-writer.dll";
const string UNIT_TEST_ASSEMBLY = "nunit-v2-result-writer.tests.dll";
const string MOCK_ASSEMBLY = "mock-assembly.dll";

// Packaging
const string NUGET_ID = "NUnit.Extension.NUnitV2ResultWriter";
const string CHOCO_ID = "nunit-extension-nunit-v2-result-writer";
const string MYGET_PUSH_URL =  "https://www.myget.org/F/nunit/api/v2";
const string MYGET_API_KEY = "MYGET_API_KEY";

// Package sources for nuget restore
static readonly string[] PACKAGE_SOURCES = new string[]
{
	"https://www.nuget.org/api/v2",
	"https://www.myget.org/F/nunit/api/v2"
};

//const string DEFAULT_TEST_RESULT_FILE = "TestResult.xml";

//// URLs for uploading packages
//private const string MYGET_PUSH_URL = "https://www.myget.org/F/testcentric/api/v2";
//private const string NUGET_PUSH_URL = "https://api.nuget.org/v3/index.json";
//private const string CHOCO_PUSH_URL = "https://push.chocolatey.org/";

//// Environment Variable names holding API keys
//private const string MYGET_API_KEY = "MYGET_API_KEY";
//private const string NUGET_API_KEY = "NUGET_API_KEY";
//private const string CHOCO_API_KEY = "CHOCO_API_KEY";

//// Environment Variable names holding GitHub identity of user
//private const string GITHUB_OWNER = "TestCentric";
//private const string GITHUB_REPO = "testcentric-gui";	
//// Access token is used by GitReleaseManager
//private const string GITHUB_ACCESS_TOKEN = "GITHUB_ACCESS_TOKEN";

//// Pre-release labels that we publish
//private static readonly string[] LABELS_WE_PUBLISH_ON_MYGET = { "dev", "pre" };
//private static readonly string[] LABELS_WE_PUBLISH_ON_NUGET = { "alpha", "beta", "rc" };
//private static readonly string[] LABELS_WE_PUBLISH_ON_CHOCOLATEY = { "alpha", "beta", "rc" };
