# rapise-testadapter

Rapise TestAdapter for [VSTest](https://github.com/microsoft/vstest). Use it to run [Rapise](https://www.inflectra.com/Rapise/) tests with [Azure Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/?view=azure-devops) and [VSTest.Console.exe](https://docs.microsoft.com/en-us/visualstudio/test/vstest-console-options).

The package is published on NuGet.org: [Rapise.TestAdapter](https://www.nuget.org/packages/Rapise.TestAdapter/).

## Usage

### Azure Pipelines

1. Add [NuGet tool installer](https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/tool/nuget?view=azure-devops) task to install NuGet.exe.
    
    Example:
    
    ```yaml
    steps:
    - task: NuGetToolInstaller@0
      displayName: 'Use NuGet'
      inputs:
        versionSpec: 4.4.1
    ```
2. Add [NuGet](https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/package/nuget?view=azure-devops) task. Set **command** to `custom` and specify the command line:

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
3. To run tests you need [Visual Studio Test](https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/test/vstest?view=azure-devops) task.

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
    
    
