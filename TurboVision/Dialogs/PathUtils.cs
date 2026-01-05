using System.Runtime.InteropServices;

namespace TurboVision.Dialogs;

/// <summary>
/// File path utility functions matching upstream C++ functionality.
/// Uses BCL Path, Directory, and FileInfo classes per BCL_MAPPING.md.
/// </summary>
public static class PathUtils
{
    /// <summary>
    /// Maximum path length.
    /// </summary>
    public const int MAXPATH = 260;

    /// <summary>
    /// Maximum drive length including colon.
    /// </summary>
    public const int MAXDRIVE = 3;

    /// <summary>
    /// Maximum directory path length.
    /// </summary>
    public const int MAXDIR = 256;

    /// <summary>
    /// Maximum filename length (without extension).
    /// </summary>
    public const int MAXFILE = 256;

    /// <summary>
    /// Maximum extension length (including dot).
    /// </summary>
    public const int MAXEXT = 256;

    // File attribute constants matching upstream FA_* values
    public const byte FA_RDONLY = 0x01;
    public const byte FA_HIDDEN = 0x02;
    public const byte FA_SYSTEM = 0x04;
    public const byte FA_DIREC = 0x10;
    public const byte FA_ARCH = 0x20;

    /// <summary>
    /// Checks if a drive letter is valid.
    /// Maps to upstream driveValid() function.
    /// </summary>
    public static bool DriveValid(char drive)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On non-Windows, only the "current" drive is valid
            return char.ToUpperInvariant(drive) - 'A' == GetDisk();
        }

        drive = char.ToUpperInvariant(drive);
        if (drive < 'A' || drive > 'Z')
        {
            return false;
        }

        try
        {
            var drives = DriveInfo.GetDrives();
            foreach (var d in drives)
            {
                if (d.Name.Length > 0 && char.ToUpperInvariant(d.Name[0]) == drive)
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignore errors during drive enumeration
        }

        return false;
    }

    /// <summary>
    /// Checks if a path is a directory.
    /// Maps to upstream isDir() function.
    /// </summary>
    public static bool IsDir(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a path is valid (directory exists).
    /// Maps to upstream pathValid() function.
    /// </summary>
    public static bool PathValid(string path)
    {
        try
        {
            string expPath = Path.GetFullPath(path);

            // Root directory is always valid
            if (Path.GetPathRoot(expPath) == expPath)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Check if drive is valid
                    if (expPath.Length >= 1)
                    {
                        return DriveValid(expPath[0]);
                    }
                }
                return true;
            }

            // Remove trailing separator if present
            expPath = expPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return Directory.Exists(expPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a filename is valid.
    /// Maps to upstream validFileName() function.
    /// </summary>
    public static bool ValidFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        try
        {
            // Check for invalid characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string name = Path.GetFileName(fileName);

            if (name.IndexOfAny(invalidChars) >= 0)
            {
                return false;
            }

            // Check if directory portion is valid
            string? dir = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(dir) && !PathValid(dir))
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current directory for the specified drive.
    /// Maps to upstream getCurDir() function.
    /// </summary>
    public static string GetCurDir(int drive = -1)
    {
        try
        {
            string curDir = Directory.GetCurrentDirectory();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Ensure path ends with separator
                if (!curDir.EndsWith(Path.DirectorySeparatorChar))
                {
                    curDir += Path.DirectorySeparatorChar;
                }
            }

            return curDir;
        }
        catch
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\" : "/";
        }
    }

    /// <summary>
    /// Checks if a string contains wildcard characters.
    /// Maps to upstream isWild() function.
    /// </summary>
    public static bool IsWild(string path)
    {
        return path.Contains('*') || path.Contains('?');
    }

    /// <summary>
    /// Gets the user's home directory.
    /// Maps to upstream getHomeDir() function.
    /// </summary>
    public static string GetHomeDir()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    /// <summary>
    /// Gets the current disk as drive number (0=A, 1=B, ..., 25=Z).
    /// Maps to upstream getdisk() function.
    /// </summary>
    public static int GetDisk()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string curDir = Directory.GetCurrentDirectory();
            if (curDir.Length >= 1 && curDir[1] == ':')
            {
                return char.ToUpperInvariant(curDir[0]) - 'A';
            }
            return 2; // C drive
        }
        return 2; // Default for non-Windows (emulate C drive)
    }

    /// <summary>
    /// Sets the current disk by drive number (0=A, 1=B, ..., 25=Z).
    /// Maps to upstream setdisk() function.
    /// Returns the total number of valid drives on success, or 0 on failure.
    /// </summary>
    public static int SetDisk(int drive)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                char driveLetter = (char)('A' + drive);
                string newPath = $"{driveLetter}:\\";
                if (Directory.Exists(newPath))
                {
                    Directory.SetCurrentDirectory(newPath);
                    // Return count of valid drives
                    return DriveInfo.GetDrives().Length;
                }
            }
            catch
            {
                // Ignore errors
            }
        }
        else
        {
            // On non-Windows, only succeed if it's the default drive
            if (drive == GetDisk())
            {
                return 1;
            }
        }
        return 0;
    }

    /// <summary>
    /// Expands a relative path to an absolute path.
    /// Maps to upstream fexpand() function.
    /// </summary>
    public static string FExpand(string path)
    {
        return FExpand(path, GetCurDir());
    }

    /// <summary>
    /// Expands a relative path to an absolute path, relative to a base directory.
    /// Maps to upstream fexpand(path, relativeTo) function.
    /// </summary>
    public static string FExpand(string path, string relativeTo)
    {
        if (string.IsNullOrEmpty(path))
        {
            return relativeTo;
        }

        try
        {
            // Handle home directory expansion
            if (path.StartsWith("~/") || path.StartsWith("~\\"))
            {
                string home = GetHomeDir();
                path = Path.Combine(home, path.Substring(2));
            }

            // If path is already absolute, just normalize it
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            // Combine with relative base
            string basePath = relativeTo;
            if (!Path.IsPathRooted(basePath))
            {
                basePath = GetCurDir();
            }

            return Path.GetFullPath(Path.Combine(basePath, path));
        }
        catch
        {
            return path;
        }
    }

    /// <summary>
    /// Splits a path into its components.
    /// Maps to upstream fnsplit() function.
    /// </summary>
    public static void FnSplit(string path, out string drive, out string dir, out string name, out string ext)
    {
        drive = "";
        dir = "";
        name = "";
        ext = "";

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            // Extract drive
            if (path.Length >= 2 && path[1] == ':')
            {
                drive = path.Substring(0, 2);
                path = path.Substring(2);
            }

            // Extract directory
            string? dirPart = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dirPart))
            {
                dir = dirPart;
                if (!dir.EndsWith(Path.DirectorySeparatorChar) && !dir.EndsWith(Path.AltDirectorySeparatorChar))
                {
                    dir += Path.DirectorySeparatorChar;
                }
            }
            else if (path.StartsWith(Path.DirectorySeparatorChar.ToString()) || path.StartsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                dir = path.Substring(0, 1);
            }

            // Extract name and extension
            string fileName = Path.GetFileNameWithoutExtension(path);
            ext = Path.GetExtension(path);
            name = fileName;
        }
        catch
        {
            // On error, return path as name
            name = path;
        }
    }

    /// <summary>
    /// Merges path components into a single path.
    /// Maps to upstream fnmerge() function.
    /// </summary>
    public static string FnMerge(string drive, string dir, string name, string ext)
    {
        string result = drive;

        if (!string.IsNullOrEmpty(dir))
        {
            result += dir;
        }

        if (!string.IsNullOrEmpty(name))
        {
            result += name;
        }

        if (!string.IsNullOrEmpty(ext))
        {
            if (!ext.StartsWith('.'))
            {
                result += '.';
            }
            result += ext;
        }

        return result;
    }

    /// <summary>
    /// Normalizes a path by resolving . and .. components.
    /// Maps to upstream squeeze() function.
    /// </summary>
    public static string Squeeze(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }

    /// <summary>
    /// Checks if a character is a path separator.
    /// </summary>
    public static bool IsSeparator(char c)
    {
        return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
    }

    /// <summary>
    /// Converts FileAttributes to upstream attribute byte format.
    /// </summary>
    public static byte ToUpstreamAttr(FileAttributes attr)
    {
        byte result = 0;

        if ((attr & FileAttributes.ReadOnly) != 0)
        {
            result |= FA_RDONLY;
        }
        if ((attr & FileAttributes.Hidden) != 0)
        {
            result |= FA_HIDDEN;
        }
        if ((attr & FileAttributes.System) != 0)
        {
            result |= FA_SYSTEM;
        }
        if ((attr & FileAttributes.Directory) != 0)
        {
            result |= FA_DIREC;
        }
        if ((attr & FileAttributes.Archive) != 0)
        {
            result |= FA_ARCH;
        }

        return result;
    }

    /// <summary>
    /// Converts a DateTime to DOS-style packed time format.
    /// </summary>
    public static int ToPackedTime(DateTime dateTime)
    {
        // DOS time format: bits 0-4: seconds/2, 5-10: minutes, 11-15: hours
        // DOS date format: bits 0-4: day, 5-8: month, 9-15: year-1980
        int time = (dateTime.Second / 2) | (dateTime.Minute << 5) | (dateTime.Hour << 11);
        int date = dateTime.Day | (dateTime.Month << 5) | ((dateTime.Year - 1980) << 9);
        return (date << 16) | time;
    }

    /// <summary>
    /// Converts a DOS-style packed time to DateTime.
    /// </summary>
    public static DateTime FromPackedTime(int packedTime)
    {
        int time = packedTime & 0xFFFF;
        int date = (packedTime >> 16) & 0xFFFF;

        int second = (time & 0x1F) * 2;
        int minute = (time >> 5) & 0x3F;
        int hour = (time >> 11) & 0x1F;

        int day = date & 0x1F;
        int month = (date >> 5) & 0x0F;
        int year = ((date >> 9) & 0x7F) + 1980;

        try
        {
            return new DateTime(year, Math.Max(1, month), Math.Max(1, day), hour, minute, second);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Trims trailing path separator from a path.
    /// </summary>
    public static string TrimEndSeparator(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        // Don't trim root paths (e.g., "C:\" or "/")
        if (path.Length <= 3 && Path.IsPathRooted(path))
        {
            return path;
        }

        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Ensures path ends with a separator.
    /// </summary>
    public static string AddFinalSeparator(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return Path.DirectorySeparatorChar.ToString();
        }

        if (!path.EndsWith(Path.DirectorySeparatorChar) && !path.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return path + Path.DirectorySeparatorChar;
        }

        return path;
    }
}
