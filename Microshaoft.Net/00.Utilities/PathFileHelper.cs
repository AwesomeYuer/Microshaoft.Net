namespace Microshaoft
{
    using System;
    using System.IO;
    using System.Linq;
    public static class PathFileHelper
    {
        public static bool MoveFileTo
                            (
                                string sourceFullPathFileName
                                , string sourceDirectoryPath
                                , string destDirectorytPath
                                , bool deleteExistsDestFile = false
                            )
        {
            var r = false;
            var destFullPathFileName = GetNewPath(sourceDirectoryPath, destDirectorytPath, sourceFullPathFileName);
            var directory = Path.GetDirectoryName(destFullPathFileName);
            if (File.Exists(directory))
            {
                File.Delete(directory);
            }
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (deleteExistsDestFile)
            {
                if (File.Exists(destFullPathFileName))
                {
                    File.Delete(destFullPathFileName);
                }
            }
            File.Move(sourceFullPathFileName, destFullPathFileName);
            r = true;
            return r;
        }
        public static string GetValidPathOrFileName(string path, string replacement)
        {
            string s = string.Empty;
            var chars = Path.GetInvalidPathChars();
            chars = chars.Union(Path.GetInvalidFileNameChars()).ToArray();
            Array
                .ForEach
                    (
                        chars
                        , (x) =>
                        {
                            s = s.Replace(x.ToString(), replacement);
                        }
                    );
            return s;
        }
        public static string GetNewPath(string oldDirectoryPath, string newDirectoryPath, string originalFileFullPath)
        {
            string newPath = newDirectoryPath;
            originalFileFullPath = Path.GetFullPath(originalFileFullPath);
            var directorySeparator = Path.DirectorySeparatorChar.ToString();
            oldDirectoryPath = Path.GetFullPath(oldDirectoryPath);
            newDirectoryPath = Path.GetFullPath(newDirectoryPath);
            if (!oldDirectoryPath.EndsWith(directorySeparator))
            {
                oldDirectoryPath += directorySeparator;
            }
            if (!newDirectoryPath.EndsWith(directorySeparator))
            {
                newDirectoryPath += directorySeparator;
            }
            string relativeDirectoryPath = string.Empty;
            int p = originalFileFullPath
                        .ToLower()
                        .IndexOf(oldDirectoryPath.ToLower());
            if (p >= 0)
            {
                p += oldDirectoryPath.Length;
                relativeDirectoryPath = originalFileFullPath.Substring(p);
                newPath = Path.Combine(newPath, relativeDirectoryPath);
            }
            newPath = Path.GetFullPath(newPath);
            return newPath;
        }
    }
}

