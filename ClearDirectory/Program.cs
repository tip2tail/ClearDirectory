/*
MIT License

Copyright (c) 2016 tip2tail

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.IO;
using System.Linq;

namespace ClearDirectory
{
    class Program
    {

        private static bool VerboseMode = true;
        private static long DeleteCountF = 0;
        private static long DeleteCountD = 0;

        static void Main(string[] arArgs)
        {

            string sTarget = "";
            int iMegabyteLimit = -1;
            bool bDisplayOnly = false;

            DateTime oNow = DateTime.Now;

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;

            if (arArgs.Length <= 0)
            {
                // Clear the current dir of files/dirs less than 50 MB in size
                arArgs = new string[4];
                arArgs[0] = "-t";
                arArgs[1] = ".";
                arArgs[2] = "-s";
                arArgs[3] = "50";
            }

            if (arArgs[0] == "-?")
            {
                ShowHelp();
            }
            else if (arArgs.Contains("-?"))
            {
                ShowInvalidArgsMessage();
            }

            bDisplayOnly = (arArgs.Contains("-d"));
            Program.VerboseMode = (arArgs.Contains("-v"));

            // Work out where we are starting
            if (arArgs.Contains("-t"))
            {
                var iIndex = Array.IndexOf(arArgs, "-t") + 1;
                try
                {
                    sTarget = arArgs[iIndex];
                }
                catch (Exception eXcep)
                {
                    ExceptionMessage(eXcep);
                    ShowInvalidArgsMessage();
                }
                if (sTarget == ".")
                {
                    sTarget = Environment.CurrentDirectory;
                }
                if (Directory.Exists(sTarget) == false)
                {
                    Console.WriteLine("Invalid path: " + sTarget);
                    ShowInvalidArgsMessage();
                }
            }

            // Whats our limit lads?
            if (arArgs.Contains("-s"))
            {
                var iIndex = Array.IndexOf(arArgs, "-s") + 1;
                var iLimit = 0;
                try
                {
                    iLimit = Convert.ToInt32(arArgs[iIndex]);
                }
                catch (Exception eXcep)
                {
                    ExceptionMessage(eXcep);
                    ShowInvalidArgsMessage();
                }
                if (iLimit < 1)
                {
                    Console.WriteLine("Limit must be 1 MB or higher.  Default is 50 MB.");
                    ShowInvalidArgsMessage();
                }
                iMegabyteLimit = iLimit;
            }

            // Final checks
            if (sTarget == "") {
                sTarget = Environment.CurrentDirectory;
            }
            if (iMegabyteLimit == -1)
            {
                iMegabyteLimit = 50;
            }

            // Run that funky!
            ExecuteProcess(bDisplayOnly, sTarget, iMegabyteLimit);

            TimeSpan oTS = DateTime.Now - oNow;

            // Done!
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Process Completed.");
            Console.WriteLine("Time Taken:\t\t\t\t" + oTS.ToString());
            Console.WriteLine("Files Deleted (or Displayed):\t\t" + DeleteCountF.ToString());
            Console.WriteLine("Directories Deleted (or Displayed):\t " + DeleteCountD.ToString());
            Console.WriteLine("");
            Console.WriteLine("");

            Console.ResetColor();

            Environment.Exit(0);

        }

        private static long GetDirectorySize(DirectoryInfo oDI)
        {
            long lSize = 0;
            // Add file sizes
            FileInfo[] arFileInfos = oDI.GetFiles();
            foreach (FileInfo oThisFile in arFileInfos)
            {
                lSize += oThisFile.Length;
            }
            // Add subdirectory sizes
            DirectoryInfo[] arDirInfos = oDI.GetDirectories();
            foreach (DirectoryInfo oThisDir in arDirInfos)
            {
                lSize += GetDirectorySize(oThisDir);
            }
            return lSize;
        }

        private static void ExecuteProcess(bool bDisplayOnly, string sTarget, int iMegabyteLimit)
        {
            // Toot Toot! Here's Root!
            DirectoryInfo oRootDir = new DirectoryInfo(sTarget);

            // We work in bytes here
            long lLimitBytes = (iMegabyteLimit * 1024) * 1024;

            OutputHeading();

            // Verbose notes
            VerboseNote("ClearDirectory - running on target:");
            VerboseNote(sTarget);
            VerboseNote("Size Limit (bytes): " + lLimitBytes.ToString());
            VerboseNote("");

            // First lets git rid of any files in the root...
            FileInfo[] arFileInfos = oRootDir.GetFiles();
            DeleteFiles(arFileInfos, lLimitBytes, bDisplayOnly);

            VerboseNote("");

            // Now we start on the directories and the recursive magic...
            DirectoryInfo[] arDirInfos = oRootDir.GetDirectories();
            DeleteDirs(arDirInfos, lLimitBytes, bDisplayOnly);
        }

        private static void DeleteRecursiveContents(string sDirPath, bool bDisplayOnly)
        {
            VerboseNote("Recursively deleting: " + sDirPath);
            VerboseNote("");

            // Files
            FileInfo[] arFileInfos = new DirectoryInfo(sDirPath).GetFiles();
            VerboseNote("Found " + arFileInfos.Length + " files...");
            foreach (var oThisFile in arFileInfos)
            {
                if (bDisplayOnly)
                {
                    Console.WriteLine("DISPLAY (FILE): " + oThisFile.FullName);
                }
                else
                {
                    try
                    {
                        File.Delete(oThisFile.FullName);
                        Console.WriteLine("DELETED (FILE): " + oThisFile.FullName);
                    }
                    catch (Exception eXcep)
                    {
                        ExceptionMessage(eXcep);
                    }
                }
                DeleteCountF++;
            }
            VerboseNote("");

            // Dirs
            DirectoryInfo[] arDirInfos = new DirectoryInfo(sDirPath).GetDirectories();
            VerboseNote("Found " + arDirInfos.Length.ToString() + " files in this directory");
            foreach (var oThisDir in arDirInfos)
            {
                DeleteRecursiveContents(oThisDir.FullName, bDisplayOnly);
                try
                {
                    if (bDisplayOnly)
                    {
                        VerboseNote("DISPLAY (DIR): " + oThisDir.Name);
                    }
                    else
                    {
                        Directory.Delete(oThisDir.FullName);
                        VerboseNote("DELETE (DIR): " + oThisDir.Name);
                    }
                }
                catch (Exception eXcep)
                {
                    ExceptionMessage(eXcep);
                    continue;
                }
                DeleteCountD++;
            }
            VerboseNote("");
        }

        private static void DeleteDirs(DirectoryInfo[] arDirInfos, long lLimitBytes, bool bDisplayOnly, bool bIsRoot = true)
        {
            VerboseNote("Found " + arDirInfos.Length.ToString() + " sub-directories in this directory");
            foreach (var oThisDir in arDirInfos)
            {
                var bDoDelete = (bIsRoot == false);
                if (bIsRoot)
                {
                    VerboseNote("Checking size of (dir): " + oThisDir.Name);
                    var lThisDirSize = GetDirectorySize(oThisDir);
                    bDoDelete = (lThisDirSize < lLimitBytes);
                }
                if (bDoDelete)
                {
                    try
                    {
                        DeleteRecursiveContents(oThisDir.FullName, bDisplayOnly);
                        if (bDisplayOnly)
                        {
                            Console.WriteLine("DISPLAY (DIR): " + oThisDir.Name);
                        }
                        else
                        {
                            Directory.Delete(oThisDir.FullName);
                            Console.WriteLine("DELETE (DIR): " + oThisDir.Name);
                        }
                    }
                    catch (Exception eXcep)
                    {
                        ExceptionMessage(eXcep);
                        continue;
                    }
                    DeleteCountD++;
                }
                else
                {
                    VerboseNote("IGNORED (DIR) " + oThisDir.Name);
                }
            }
        }

        private static void ExceptionMessage(Exception eXcep)
        {
            var bVerboseKeep = VerboseMode;
            VerboseMode = true;
            VerboseNote("ERROR: " + eXcep.Message);
            VerboseMode = bVerboseKeep;
        }

        private static void DeleteFiles(FileInfo[] arFileInfos, long lLimitBytes, bool bDisplayOnly)
        {
            VerboseNote("Found " + arFileInfos.Length.ToString() + " files in this directory");
            foreach (var oThisFile in arFileInfos)
            {
                VerboseNote("Checking size of (file): " + oThisFile.Name);

                if (oThisFile.FullName == System.Reflection.Assembly.GetExecutingAssembly().Location)
                {
                    VerboseNote("Skipping this application");
                    continue;
                }

                if (oThisFile.Length < lLimitBytes)
                {
                    if (bDisplayOnly)
                    {
                        Console.WriteLine("DISPLAY (FILE): " + oThisFile.FullName);
                    }
                    else
                    {
                        try
                        {
                            File.Delete(oThisFile.FullName);
                            Console.WriteLine("DELETED (FILE): " + oThisFile.FullName);
                        }
                        catch (Exception eXcep)
                        {
                            ExceptionMessage(eXcep);
                        }
                    }
                    DeleteCountF++;
                }
                else
                {
                    VerboseNote("IGNORED (FILE) " + oThisFile.Name);
                }
            }
        }

        private static void VerboseNote(string sNote)
        {
            if (Program.VerboseMode)
            {
                var sTimestamp = "[" + DateTime.Now.ToString() + "] ";
                Console.WriteLine(sTimestamp + sNote);
            }
        }

        private static void ShowHelp()
        {
            OutputHeading();
            Console.WriteLine("Help and Usage Information");
            Console.WriteLine("============================================");
            Console.WriteLine("");
            Console.WriteLine("Command Line Parameters:");
            Console.WriteLine("   -?\t\tShows this imformation.");
            Console.WriteLine("   -t {dir}\tSets the target directory.");
            Console.WriteLine("   -d\t\tDisplay only mode.");
            Console.WriteLine("   -v\t\tVerbose output.");
            Console.WriteLine("   -s {mb}\tSets the upper size limit in megabytes for deletion.");
            Console.WriteLine("");
            Console.WriteLine("Default use without any parameters equates to:");
            Console.WriteLine("\t-t . -s 50");
            Console.WriteLine("\tDelete all files/directories less than 50 MB in size below this directory.");
            Console.WriteLine("");
            Console.WriteLine("Copyright © " + DateTime.Now.Year.ToString() + " tip2tail Ltd.");
            Console.WriteLine("Released under the MIT license.");
            Console.WriteLine("");
            Environment.Exit(0);
        }

        private static void OutputHeading()
        {
            Console.WriteLine("============================================");
            Console.WriteLine("ClearDirectory");
            Console.WriteLine("============================================");
        }

        private static void ShowInvalidArgsMessage()
        {
            Console.WriteLine("Invalid arguaments.  Run ClearDirectory -? for help.");
            Environment.Exit(0);
        }
    }
}
