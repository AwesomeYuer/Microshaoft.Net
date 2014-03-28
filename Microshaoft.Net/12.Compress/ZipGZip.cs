#if NET45
// /r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\Profile\Client\WindowsBase.dll"
namespace Microshaoft
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.IO.Packaging;
    public class ZipHandler
    {
        public static void Compress(FileInfo fi, DirectoryInfo dir)
        {
            if (fi.Exists)
            {
                fi.Delete();
            }
            Package zipFilePackage = ZipPackage.Open(fi.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            foreach (FileInfo physicalFile in dir.GetFiles())
            {
                string physicalfilePath = physicalFile.FullName;
                Uri partUri = PackUriHelper.CreatePartUri(new Uri(physicalFile.Name, UriKind.Relative));
                PackagePart newFilePackagePart = zipFilePackage.CreatePart(partUri, System.Net.Mime.MediaTypeNames.Text.Xml);
                byte[] fileContent = File.ReadAllBytes(physicalfilePath);
                newFilePackagePart.GetStream().Write(fileContent, 0, fileContent.Length);
            }
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                foreach (FileInfo physicalFile in subDir.GetFiles())
                {
                    string physicalfilePath = physicalFile.FullName;
                    Uri partUri = PackUriHelper.CreatePartUri(new Uri(subDir.Name + "/" + physicalFile.Name, UriKind.Relative));
                    PackagePart newFilePackagePart = zipFilePackage.CreatePart(partUri, System.Net.Mime.MediaTypeNames.Text.Xml);
                    byte[] fileContent = File.ReadAllBytes(physicalfilePath);
                    newFilePackagePart.GetStream().Write(fileContent, 0, fileContent.Length);
                }
            }
            zipFilePackage.Close();
        }
        public static bool Decompress(FileInfo fi, string origName)
        {
            bool returnVal = false;
            string curFile = fi.FullName;
            Package zipFilePackage = ZipPackage.Open(curFile, FileMode.Open, FileAccess.ReadWrite);
            foreach (ZipPackagePart contentFile in zipFilePackage.GetParts())
            {
                CreateFile(origName, contentFile);
                returnVal = true;
            }
            zipFilePackage.Close();
            return returnVal;
        }
        private static void CreateFile(string rootFolder, ZipPackagePart contentFile)
        {
            // Initially create file under the folder specified
            string contentFilePath = string.Empty;
            contentFilePath = contentFile.Uri.OriginalString.Replace('/',
                             System.IO.Path.DirectorySeparatorChar);
            if (contentFilePath.StartsWith(
                System.IO.Path.DirectorySeparatorChar.ToString()))
            {
                contentFilePath = contentFilePath.TrimStart(
                                         System.IO.Path.DirectorySeparatorChar);
            }
            else
            {
                //do nothing
            }
            contentFilePath = System.IO.Path.Combine(rootFolder, contentFilePath);
            //contentFilePath =  System.IO.Path.Combine(rootFolder, contentFilePath); 
            //Check for the folder already exists. If not then create that folder
            if (System.IO.Directory.Exists(
                System.IO.Path.GetDirectoryName(contentFilePath)) != true)
            {
                System.IO.Directory.CreateDirectory(
                          System.IO.Path.GetDirectoryName(contentFilePath));
            }
            else
            {
                //do nothing
            }
            System.IO.FileStream newFileStream =
                    System.IO.File.Create(contentFilePath);
            newFileStream.Close();
            byte[] content = new byte[contentFile.GetStream().Length];
            contentFile.GetStream().Read(content, 0, content.Length);
            System.IO.File.WriteAllBytes(contentFilePath, content);
        }
    }
}

namespace ConsoleApplication
{
    using System;
    using System.IO;
    using System.IO.Compression;
    class Program
    {
        static void Main(string[] args)
        {
            string startPath = @"c:\example\start";
            string zipPath = @"c:\example\result.zip";
            string extractPath = @"c:\example\extract";
            ZipFile.CreateFromDirectory(startPath, zipPath);
            ZipFile.ExtractToDirectory(zipPath, extractPath);
        }
    }
}
namespace ConsoleApplication1
{
    using System;
    using System.IO;
    using System.IO.Compression;
    class Program
    {
        static void Main(string[] args)
        {
            string zipPath = @"c:\example\start.zip";
            string extractPath = @"c:\example\extract";
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.ExtractToFile(Path.Combine(extractPath, entry.FullName));
                    }
                }
            }
        }
    }
}
namespace ConsoleApplication2
{
    using System;
    using System.IO;
    using System.IO.Compression;
    class Program
    {
        static void Main(string[] args)
        {
            using (FileStream zipToOpen = new FileStream(@"c:\users\exampleuser\release.zip", FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry readmeEntry = archive.CreateEntry("Readme.txt");
                    using (StreamWriter writer = new StreamWriter(readmeEntry.Open()))
                    {
                        writer.WriteLine("Information about this package.");
                        writer.WriteLine("========================");
                    }
                }
            }
        }
    }
}

namespace GZIP
{
    using System;
    using System.IO;
    using System.IO.Compression;
    public class Program
    {
        public static void Main()
        {
            string directoryPath = @"c:\users\public\reports";
            DirectoryInfo directorySelected = new DirectoryInfo(directoryPath);
            foreach (FileInfo fileToCompress in directorySelected.GetFiles())
            {
                Compress(fileToCompress);
            }
            foreach (FileInfo fileToDecompress in directorySelected.GetFiles("*.gz"))
            {
                Decompress(fileToDecompress);
            }
        }
        public static void Compress(FileInfo fileToCompress)
        {
            using (FileStream originalFileStream = fileToCompress.OpenRead())
            {
                if ((File.GetAttributes(fileToCompress.FullName) & FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
                {
                    using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + ".gz"))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);
                            Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
                                fileToCompress.Name, fileToCompress.Length.ToString(), compressedFileStream.Length.ToString());
                        }
                    }
                }
            }
        }
        public static void Decompress(FileInfo fileToDecompress)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);
                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine("Decompressed: {0}", fileToDecompress.Name);
                    }
                }
            }
        }
    }
}
#endif