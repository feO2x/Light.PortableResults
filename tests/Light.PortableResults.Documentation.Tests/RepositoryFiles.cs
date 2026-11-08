using System;
using System.IO;

namespace Light.PortableResults.Documentation.Tests;

public static class RepositoryFiles
{
    private const string SolutionFileName = "Light.PortableResults.slnx";

    private static readonly Lazy<string> LazyRootDirectory = new (FindRootDirectory);

    public static string RootDirectory => LazyRootDirectory.Value;

    public static string GetFullPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(RootDirectory, relativePath));

    public static string ToRepositoryRelativePath(string fullPath) =>
        Path.GetRelativePath(RootDirectory, fullPath).Replace('\\', '/');

    private static string FindRootDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not find the repository root: no parent directory of '{AppContext.BaseDirectory}' contains {SolutionFileName}."
        );
    }
}
