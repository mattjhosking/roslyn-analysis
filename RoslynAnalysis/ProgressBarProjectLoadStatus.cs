using System;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynAnalysis
{
    public class ProgressBarProjectLoadStatus : IProgress<ProjectLoadProgress>
    {
        public void Report(ProjectLoadProgress value)
        {
            Console.Out.WriteLine($"{value.Operation} {value.FilePath}");
        }
    }
}