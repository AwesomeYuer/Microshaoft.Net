
namespace Microshaoft
{
    using System;
    using System.IO;
    using System.Linq;
    public static class PathFileHelper
    {
        public static void MoveFileTo
                            (
                                string sourceFullPathFileName
                                , string sourceRootPath
                                , string destRootPath
                            )
        {
            var destFullPathFileName = GetNewPath(sourceRootPath, destRootPath, sourceFullPathFileName);
            var directory = Path.GetDirectoryName(destFullPathFileName);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.Move(sourceFullPathFileName, destFullPathFileName);
        }
        public static string GetValidPathOrFileName(string path, string replacement)
        {
            string s = string.Empty;
            var chars = Path.GetInvalidPathChars();
            chars = chars.Union(Path.GetInvalidFileNameChars()).ToArray();
            Array.ForEach
                    (
                        chars
                        , (x) =>
                        {
                            s = s.Replace(x.ToString(), replacement);
                        }
                    );
            return s;
        }
        public static string GetNewPath(string oldRootPath, string newRootPath, string path)
        {
            path = Path.GetFullPath(path);
            var directorySeparator = Path.DirectorySeparatorChar.ToString().ToLower();
            oldRootPath = Path.GetFullPath(oldRootPath).ToLower();
            newRootPath = Path.GetFullPath(newRootPath).ToLower();
            if (!oldRootPath.EndsWith(directorySeparator))
            {
                oldRootPath += directorySeparator;
            }
            if (!newRootPath.EndsWith(directorySeparator))
            {
                newRootPath += directorySeparator;
            }
            path = path.ToLower();
            int p = path.IndexOf(oldRootPath);
            if (p >= 0)
            {
                p += oldRootPath.Length;
            }
            string relativePath = path.Substring(p);
            ///			if (!relativePath.StartsWith(directorySeparator))
            ///			{
            ///				 relativePath = directorySeparator + relativePath;
            ///			}
            string newPath = string.Format
                                            (
                                                @"{1}{0}{2}"
                                                , ""
                                                , newRootPath
                                                , relativePath
                                            );
            return newPath;
        }
    }
}
