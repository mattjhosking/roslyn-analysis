using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynAnalysis.Model
{
    public abstract class WorkspaceBase
    {
        protected static MSBuildWorkspace NewMsBuildWorkspace()
        {
            var workspace = MSBuildWorkspace.Create();
            workspace.SkipUnrecognizedProjects = true;
            return ConfigureWorkspace(workspace);
        }

        protected static AdhocWorkspace NewAdHocWorkspace()
        {
            var workspace = new AdhocWorkspace();
            return ConfigureWorkspace(workspace);
        }

        private static T ConfigureWorkspace<T>(T workspace)
            where T : Workspace
        {
            workspace.WorkspaceFailed += (sender, args) =>
            {
                if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                {
                    Console.Error.WriteLine(args.Diagnostic.Message);
                    //throw new Exception();
                }
            };
            return workspace;
        }
    }
}