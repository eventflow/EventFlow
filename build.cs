var target = Argument("target", "Default");

Task("Default")
  .Does(() =>
{
    BuildProject("EventFlow.sln");
});

void BuildProject(string projectPath)
{
    MSBuild(projectPath, new MSBuildSettings()
        .SetConfiguration("Release")
        .SetMSBuildPlatform(MSBuildPlatform.Automatic)
        .SetVerbosity(Verbosity.Minimal)
        .SetNodeReuse(false)
    );
}

RunTarget(target);