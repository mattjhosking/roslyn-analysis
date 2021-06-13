using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace RoslynAnalysis.Model
{
    public class WorkspaceSolution : WorkspaceBase
    {
        private readonly Solution _solution;

        private readonly IDictionary<string, WorkspaceProject> _openProjects =
            new Dictionary<string, WorkspaceProject>();

        public static async Task<WorkspaceSolution> Load(string fileName)
        {
            await Console.Out.WriteLineAsync("Loading solution...");
            var workspace = NewMsBuildWorkspace();
            var solution = await workspace.OpenSolutionAsync(fileName, new ProgressBarProjectLoadStatus());
            return new WorkspaceSolution(solution);
        }

        public WorkspaceSolution(Solution solution)
        {
            _solution = solution;
        }

        public async Task<WorkspaceProject> GetProject(string projectName)
        {
            if (!_openProjects.ContainsKey(projectName))
                _openProjects[projectName] = await WorkspaceProject.LoadFromSolution(this, _solution.Projects.SingleOrDefault(x =>
                    string.Equals(x.Name, projectName, StringComparison.OrdinalIgnoreCase)));

            return _openProjects[projectName];
        }
    }
}