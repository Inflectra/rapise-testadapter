# rapise-testadapter

Rapise TestAdapter for [VSTest](https://github.com/microsoft/vstest). Use it to run [Rapise](https://www.inflectra.com/Rapise/) tests with [Azure Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/?view=azure-devops) and [VSTest.Console.exe](https://docs.microsoft.com/en-us/visualstudio/test/vstest-console-options).

The package is published on NuGet.org: [Rapise.TestAdapter](https://www.nuget.org/packages/Rapise.TestAdapter/).

## Usage

### Azure Pipelines

1. If you plan to run tests on Azure Hosted agents you need to configure the installtion step for Rapise. Add [PowerShell](https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/utility/powershell?view=azure-devops) task.

```yaml
steps:
- task: PowerShell@2
  displayName: 'Install Rapise'
  inputs:
    targetType: filePath
    filePath: ./RapiseInstall.ps1
    arguments: '-RapiseVersion "6.5.20.21"'
```

`RapiseInstall.ps1` is located in the root of this repository. Place it into your Git repository and reference in the PowerShell task. This script downloads and installs Rapise. It also installs Rapise extension into Chrome browser.

2. Add [NuGet tool installer](https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/tool/nuget?view=azure-devops) task to install NuGet.exe.
    
    Example:
    
    ```yaml
    steps:
    - task: NuGetToolInstaller@0
      displayName: 'Use NuGet'
      inputs:
        versionSpec: 4.4.1
    ```
3. Add [NuGet](https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/package/nuget?view=azure-devops) task. Set **command** to `custom` and specify the command line:

    ```
    install Rapise.TestAdapter -Version $(RapiseTestAdapterVersion)
    ```
    
    Example
    
    ```yaml
    steps:
    - task: NuGetCommand@2
      displayName: 'NuGet Install Rapise.TestAdapter'
      inputs:
        command: custom
        arguments: 'install Rapise.TestAdapter -Version $(RapiseTestAdapterVersion)'    
    ```
    
    In the pipeline settings set **RapiseTestAdapterVersion** variable to the Rapise.TestAdapter version you want to install (e.g. 1.0.11). The task will install Rapise.TestAdapter into 
    
    ```
    $(Build.Repository.LocalPath)\Rapise.TestAdapter.$(RapiseTestAdapterVersion)\lib\net472
    ```
4. To run tests you need [Visual Studio Test](https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/test/vstest?view=azure-devops) task.

    Example:
    
    ```yaml
    steps:
    - task: VSTest@2
      displayName: 'VsTest - Run Rapise Tests'
      inputs:
        testAssemblyVer2: |
         $(Build.Repository.LocalPath)\Tests\*.sstest
         $(Build.Repository.LocalPath)\Tests\*\*.sstest
        runSettingsFile: '$(System.DefaultWorkingDirectory)\Pipeline\azure.runsettings'
        overrideTestrunParameters: '-g_baseURL $(Dynamics365CrmBaseURL) -g_password $(Dynamics365CrmPassword) -g_browserLibrary $(RapiseBrowserProfile)'
        pathtoCustomTestAdapters: '$(Build.Repository.LocalPath)\Rapise.TestAdapter.$(RapiseTestAdapterVersion)\lib\net472'
        platform: '$(BuildPlatform)'
        configuration: '$(BuildConfiguration)'
    ```

    Make sure that path to custom test set adapters is set to
    
    ```
    $(Build.Repository.LocalPath)\Rapise.TestAdapter.$(RapiseTestAdapterVersion)\lib\net472
    ```
    
    Specify patterns to search for `*.sstest` files in the **test files** section (`testAssemblyVer2` in YAML).
    
5. To publish test results (for later review and downloading) use [Publish Build Artifacts](https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/utility/publish-build-artifacts?view=azure-devops) task. Execution results are copied to `$(Agent.TempDirectory)\TestResults`.

    Example:
    
    ```yaml
    steps:
    - task: PublishBuildArtifacts@1
      displayName: 'Publish Artifact: TestResults'
      inputs:
        PathtoPublish: '$(Agent.TempDirectory)\TestResults'
        ArtifactName: TestResults
    ```

#### Visual Studio Test Task 

Rapise.TestAdapter also supports filtering, parameters and .runsettings files.
   
##### Test Filter Cirteria  

Rapise.TestAdapter supports [filter criteria](https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md) based on FullyQualifiedName test property (equals to  *.sstest file name). To specify a filter set `testFiltercriteria` in YAML or `Test filter criteria` in the form-based task editor.

Example:

```
FullyQualifiedName~LIS
```

##### Parameters

Parameters can be set via

- .runsettings file,
- `overrideTestrunParameters` YAML option or
- **Override test run parameters** field in the form-based task editor.

> Note: Parameter names must be prefixed with `g_`.

Example:

```
-g_baseURL $(Dynamics365CrmBaseURL) -g_password $(Dynamics365CrmPassword) -g_browserLibrary $(RapiseBrowserProfile)
```

> Note: $(name) - references a pipeline variable

##### .runsettings

[.runsettings file](https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file) is used to pass parameters and to enable [video recorder](https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?view=vs-2019#videorecorder-data-collector).

Example:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <!-- Parameters used by tests at runtime -->
  <TestRunParameters>
    <Parameter name="g_browserLibrary" value="Chrome" />
  </TestRunParameters>

  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector uri="datacollector://microsoft/VideoRecorder/1.0" assemblyQualifiedName="Microsoft.VisualStudio.TestTools.DataCollection.VideoRecorder.VideoRecorderDataCollector, Microsoft.VisualStudio.TestTools.DataCollection.VideoRecorder, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" friendlyName="video" enabled="True">
        <Configuration>
          <MediaRecorder sendRecordedMediaForPassedTestCase="true"  xmlns="">           
            <ScreenCaptureVideo bitRate="512" frameRate="2" quality="20" />
          </MediaRecorder>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>  
  
</RunSettings>
```

### Run Tests with VSTest.Console.exe

It is also possible to run Rapise tests on a VM that has VSTest.Console.exe installed.

#### Setup Microsoft.TestPlatform on a VM

Let's assume that the working folder is `C:\Tools`.

1. Download and install [NuGet](https://www.nuget.org/downloads).
2. Install [Microsoft.TestPlatform](https://www.nuget.org/packages/Microsoft.TestPlatform) with a command

    ```
    nuget install Microsoft.TestPlatform
    ```
Find `VSTest.Console.exe` in `C:\Tools\Microsoft.TestPlatform.16.7.1\tools\net451\Common7\IDE\Extensions\TestPlatform`. Add this folder to the PATH environment variable.

3. Install Rapise.TestAdapter

    ```
    nuget install Rapise.TestAdapter
    ```
    
4. You may now create a .cmd file and put it near Rapise tests. E.g.

    ```
    vstest.console.exe /TestAdapterPath:C:\Tools\Rapise.TestAdapter.1.0.11\lib\net472 /Settings:local.runsettings /TestCaseFilter:FullyQualifiedName~LIS *\*.sstest
    ```
