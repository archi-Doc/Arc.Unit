// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

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

    public static DirectoryInfo? TryCreateDirectory(string directory)
    {
        try
        {
            return Directory.CreateDirectory(directory);
        }
        catch
        {
            return null;
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

    /// <summary>
    /// Gets the rooted directory path.<br/>
    /// If the directory is rooted, it is returned as is; if it is not the root path, root and directory path are combined.
    /// </summary>
    /// <param name="rootDirectory">Root path.</param>
    /// <param name="directory">Directory path.</param>
    /// <returns>Rooted directory path.</returns>
    public static string GetRootedDirectory(string rootDirectory, string directory)
    {
        try
        {
            if (Path.IsPathRooted(directory))
            {// File.GetAttributes(directory).HasFlag(FileAttributes.Directory)
                return directory;
            }
            else
            {
                return Path.Combine(rootDirectory, directory);
            }
        }
        catch
        {
            return Path.Combine(rootDirectory, directory);
        }
    }

    /// <summary>
    /// Gets the rooted file path.<br/>
    /// If the file is rooted, it is returned as is; if it is not the root path, root and file path are combined.
    /// </summary>
    /// <param name="rootDirectory">Root path.</param>
    /// <param name="file">File path.</param>
    /// <returns>Rooted file path.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetRootedFile(string rootDirectory, string file)
    {
        if (Path.IsPathRooted(file))
        {
            return file;
        }
        else
        {
            return Path.Combine(rootDirectory, file);
        }
    }
}
