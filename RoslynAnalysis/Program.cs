using Microsoft.Build.Locator;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using RoslynAnalysis.Extensions;
using RoslynAnalysis.Model;

namespace RoslynAnalysis
{
    class Program
    {
        static async Task Main()
        {
            if (!MSBuildLocator.IsRegistered)
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
                MSBuildLocator.RegisterInstance(instances.OrderByDescending(x => x.Version).First());
            }

            var solution = await WorkspaceSolution.Load(@"../../../../RoslynAnalysis.sln");
            var roslynAnalysisProject = await solution.GetProject("RoslynAnalysis");

            var progressHandlers = (await roslynAnalysisProject.FindImplementations("System.IProgress`1"))
                .ToInterfaceImplementations("IProgress");

            var msBuildLocatorIsRegisteredUsages =
                await roslynAnalysisProject.FindPropertyCallers("Microsoft.Build.Locator.MSBuildLocator", "IsRegistered");
            var msBuildLocatorRegisterInstanceUsages =
                await roslynAnalysisProject.FindMethodCallers("Microsoft.Build.Locator.MSBuildLocator", "RegisterInstance");

            var consoleOutUsages = await roslynAnalysisProject.FindPropertyCallers("System.Console", "Out");
            var consoleOutWriteLineCalls = (await Task.WhenAll(consoleOutUsages.Select(async x =>
                    new { Usage = x, IsWriteLine = await x.IsCallingMethod("WriteLine") })))
                .Where(x => x.IsWriteLine)
                .ToArray();

            var workspaceSolutionFile = await roslynAnalysisProject.LoadFile("WorkspaceSolution.cs");
            var loadUsages = await workspaceSolutionFile.FindUsagesOfMethod("Load");
        }
    }
}
