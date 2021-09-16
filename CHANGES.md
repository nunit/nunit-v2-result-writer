## NUnit V2 Result Writer Extension 3.7.0 - September 16, 2021

This release consists primarily of improvements to the build process but also
corrects several problems in the content of the V2 result file.

### Bugs

* 9 NUnit3 result format output as nunit2 reports test dll as ignored if one or more child tests marked as ignored
* 11 Wrong version information of NUnit 3 in NUnit 2 export format
* 13 Results xml-file in nunit2 format does not confirm to xsd

### Build

* 16 Automate the GitHub release process
* 17 Change default branch from master to main
* 21 Add functional package tests to build
* 25 Upgrade to latest Cake Release
* 27 Publish dev builds on MyGet
* 28 Publish production builds on NuGet and Chocolatey
* 30 Standardize build scripts for extensions

## NUnit V2 Result Writer Extension 3.6.0 - August 1, 2017

  This release fixes an error in the build and adds a chocolatey package.

### Issues Resolved

* 3 No license file in NuGet package
* 4 Integrate chocolatey package in build script

## NUnit V2 Result Writer Extension 3.5.0 - October 6, 2016

  The first independent release of the nunit-v2-result-writer extension.
