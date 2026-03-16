using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace TwentyMinTwentySecondsUp
{
    internal static class Program
    {
        private static readonly string[] IgnoreFiles =
        {
        };

        private static void Main(string[] args)
        {
            Console.WriteLine($"20min20s updater ({Assembly.GetExecutingAssembly().GetName().Version})");
            if (args.Length != 2)
            {
                Console.WriteLine("Please start the updater from the app.");
                return;
            }

            try
            {
                RunUpdate(args[0], args[1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Update failed:");
                Console.WriteLine(ex);
                Environment.ExitCode = 1;
            }
        }

        private static void RunUpdate(string zipPath, string extractPath)
        {
            zipPath = zipPath.Replace("\"", string.Empty);
            extractPath = extractPath.Replace("\"", string.Empty);

            ExtractZipFile(zipPath, extractPath);

            string mainExe = Path.Combine(extractPath, "20min20s.exe");
            Process.Start(mainExe);
            File.Delete(zipPath);
        }

        private static void ExtractZipFile(string zipPath, string extractPath)
        {
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                extractPath += Path.DirectorySeparatorChar;
            }

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (IsIgnoreFile(entry.FullName))
                    {
                        continue;
                    }

                    string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                    if (!destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (IsDirectory(entry))
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }

                    Console.WriteLine($"Extract: {destinationPath}");
                    entry.ExtractToFile(destinationPath);
                }
            }
        }

        private static bool IsIgnoreFile(string fileName)
        {
            return Array.IndexOf(IgnoreFiles, fileName) != -1;
        }

        private static bool IsDirectory(ZipArchiveEntry entry)
        {
            return entry.FullName.LastOrDefault() == '\\' || entry.FullName.LastOrDefault() == '/';
        }
    }
}
