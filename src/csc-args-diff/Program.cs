using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

using Microsoft.Build.Logging.StructuredLogger;

namespace csc_args_diff
{
    class Program
    {
        static void Main(string[] args)
        {
            var compilerinvocations1 = CompilerInvocationsReader.ReadInvocations(args[0])
                .ToDictionary(x => x.ProjectFilePath);
            
            var compilerinvocations2 = CompilerInvocationsReader.ReadInvocations(args[1])
                .ToDictionary(x => x.ProjectFilePath);

            var projectsInSecondButNotFirst = new List<string>();
            foreach (var (projectPath, compilerInvocation2) in compilerinvocations2)
            {
                if (!compilerinvocations1.TryGetValue(projectPath, out var compilerInvocation1))
                {
                    projectsInSecondButNotFirst.Add(projectPath);
                    continue;
                }

                var before = string.Join('\n', compilerInvocation1.CommandLineArguments.Split(' ').OrderByDescending(x => x));
                var after = string.Join('\n', compilerInvocation2.CommandLineArguments.Split(' ').OrderByDescending(x => x)); ;
                var diff = InlineDiffBuilder.Diff(before, after);
                PrintDiff(diff);
            }

            if (projectsInSecondButNotFirst.Any())
            {
                Console.WriteLine("Projects in second build but not in the first: ");
                Console.WriteLine(string.Join('\n', projectsInSecondButNotFirst));
            }
        }

        private static void PrintDiff(DiffPaneModel diff, bool printSame = false)
        {
            var savedColor = Console.ForegroundColor;
            foreach (var line in diff.Lines)
            {
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("+ ");
                        break;
                    case ChangeType.Deleted:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("- ");
                        break;
                    default:
                        if (printSame)
                        {
                            Console.ForegroundColor = ConsoleColor.Gray; // compromise for dark or light background
                            Console.Write("  ");
                        }
                        break;
                }

                Console.WriteLine(line.Text);
            }
            Console.ForegroundColor = savedColor;
        }
    }
}
