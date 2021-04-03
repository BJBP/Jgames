# Unity Build Helper
A simple set of functions for build scripts of the Unity projects with a version generation, uniquely related to the Git commit.  
The generated versions look like this:

* **2.1.234** - if the current Git branch for release builds
* **2.1.d-1685c5a** - if the current Git branch for develop builds

The first part of the version (in the example above 2.1) is specified in Unity Player Settings.

## Features
* Build for all selected platforms in one click
* Build with different Unity Player Settings (for example ARMv7 and x86 for Android)
* Versions are generated during any build process, caused both in runtime and in Unity Build Settings
* Generation of build location depending on the platform, version, target device
* Version generation, uniquely related to the Git commit
* Search for a commit for a given version of the build
* Build from selected Git branch
* Installation apk on android device after build
* Running installed apk on android device
* Selecting android device, if there are more than one
* It is suggested to enter an app identifier, if it was not specified before the build
* Workaround UnityShaderCompiler bug with Android on Linux editor (see details below)

## Requirements
* Unity 5.6.1 or newer
* Project related to the Git repository

## How to use
Copy into your Unity project. Modify [UserBuildCommands.cs](Editor/UserBuildCommands.cs) in *Assets/BuildHelper/Editor* for your needs.  
In the Unity Editor you will find the Build Menu in the main menu.  
To get the build version in runtime, use:  
> `Application.version`

If needed, specify the path to Git executable in `BuildHelperStrings.GIT_EXEC_PATH`
in [Assets/BuildHelper/Editor/Core/BuildHelperStrings.cs](Editor/Core/BuildHelperStrings.cs).

### Workaround UnityShaderCompiler bug with Android on Linux editor
I have an error in Unity Editor for Linux after the first successful build for Android: 
> IOException: Failed to Move File / Directory from 'Temp/StagingArea/Data' to 'Temp/StagingArea/assets/bin/Data'.

This bug workarounds by killing all *UnityShaderCompiler* processes.

If you are a Linux user and do not encounter the same error, you can comment the line `#define WORKAROUND_ANDROID_UNITY_SHADER_COMPILER_BUG` in [Assets/BuildHelper/Editor/Core/BuildTime.cs](Editor/Core/BuildTime.cs).