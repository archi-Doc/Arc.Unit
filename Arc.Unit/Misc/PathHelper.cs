// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public static class PathHelper
{
    /// <summary>
    /// Creates a path from <paramref name="directory"/>.<br/>
    /// If <paramref name="directory"/> is empty, returns <paramref name="baseDirectory"/>.<br/>
    /// If <paramref name="directory"/> is an absolute path, returns <paramref name="directory"/>.<br/>
    /// If <paramref name="directory"/> is a relative path, returns the path combined with <paramref name="baseDirectory"/> and <paramref name="directory"/>.
    /// </summary>
    /// <param name="baseDirectory">The base directory path.</param>
    /// <param name="directory">The target directory path.</param>
    /// <returns>The created path.</returns>
    public static string CombineDirectory(string baseDirectory, string directory)
    {
        if (string.IsNullOrEmpty(directory))
        {
            return baseDirectory;
        }
        else if (Path.IsPathRooted(directory))
        {// Contains a root.
            return directory;
        }
        else
        {
            return Path.Combine(baseDirectory, directory);
        }
    }

    /// <summary>
    /// Deletes the specified file (no exception will be thrown).
    /// </summary>
    /// <param name="file">File path.</param>
    /// <returns><see langword="true"/>; File is successfully deleted.</returns>
    public static bool TryDeleteFile(string file)
    {
        try
        {
            File.Delete(file);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes the specified directory recursively (no exception will be thrown).
    /// </summary>
    /// <param name="directory">Directory path.</param>
    /// <returns><see langword="true"/>; Directory is successfully deleted.</returns>
    public static bool TryDeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
