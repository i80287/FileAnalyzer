using System;
using System.IO;
using System.Text;

namespace FileAnalyzer {
    /// <summary>
    /// This class contains methods to work 
    /// with file system and input-output
    /// interface (console).
    /// </summary>
    internal static class IOTools {
        private const string filePathRequestReport = "Type file name or full path to the csv file with data\n\n"
                                                   + "By default file will be searched in:\n{0}\n\n> ";
        private const string badPathReport = "Invalid name or path format";
        private const string missingFileReport = "File not found";
        private const string badEncodingReport = "File is corrupted or data is represented in unsupported encoding";
        private const string fileReadErrorReport = "An error occured while reading the file. Please type file name or path again";
        private const string fileHeadersErrorReport = "Structure of the csv file is incorrect. Program can't parse that file";
        private const string fileColumnsWarning = "Structure of the provided csv file doesn't match the\n"
                                                     + "structure of weatherAUS.csv source file from the dataset\n"
                                                     + "Data may be read incorrectly\n";
        private const string fileNameRequestReport = "Type file name or full path to the file for saving data in it\n\n> ";

        // First line of the source file
        // weatherAUS.csv from the dataset.
        private const string sourceFileHeader = "Date,Location,MinTemp,MaxTemp,Rainfall,Evaporation,Sunshine,"
                                              + "WindGustDir,WindGustSpeed,WindDir9am,WindDir3pm,WindSpeed9am,"
                                              + "WindSpeed3pm,Humidity9am,Humidity3pm,Pressure9am,Pressure3pm,"
                                              + "Cloud9am,Cloud3pm,Temp9am,Temp3pm,RainToday,RainTomorrow";

        private static readonly char[] invalidPathChars = Path.GetInvalidPathChars();
        private static readonly char[] invalidNameChars = Path.GetInvalidFileNameChars();

        private static readonly char[] pathSeparators = new char[2] { '/', '\\' };

        private static readonly string currentWorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;

        internal static void Print(object data, string end = "\n")
        {// Function to write string representation of the object in the console.
            if (data is null) { return; }
            end ??= "\n";
            Console.Write(data.ToString());
            Console.Write(end);
        }

        internal static void Print(StringBuilder data, string end = "\n")
        {// Function to write data from the StringBuilder in the console.
            if (data is null) { return; }
            end ??= "\n";
            Console.Write(data.ToString());
            Console.Write(end);
        }

        internal static void ClearConsole()
        {// Function to clear console.
            Console.ResetColor();
            Console.Clear();
            Console.SetCursorPosition(0, 0);
        }

        internal static string GetCurrentWorkingDir()
            => currentWorkingDirectory;

        internal static string[] RequestDataFromFile() 
        {// Function to get date from file.
         // Path is selected by the user.
            string path = RequestExistingFilePath();

            Encoding encoding = GetFileEncoding(path);
            while (encoding is null)
            {
                Console.WriteLine(badEncodingReport);
                path = RequestExistingFilePath();
                encoding = GetFileEncoding(path);
            }

            string[] fileData;
            try { fileData = File.ReadAllLines(path, encoding: encoding); }
            catch { fileData = null; }

            if (fileData is null)
            {
                Console.WriteLine(fileReadErrorReport);
                return RequestDataFromFile();
            }

            if (!ValidateDataHeaders(fileData))
            {
                Console.WriteLine(fileHeadersErrorReport);
                return RequestDataFromFile();
            }

            return fileData;
        }

        internal static bool WriteToFile(string path, StringBuilder content)
        {// Function to write data to the file.
         // If file exists it will be overwritten.
            if (!ValidatePathAndName(path)) { return false; }
            try { File.WriteAllText(path, content.ToString()); } 
            catch { return false; }
            return true;
        }

        internal static string RequestFilePathOrName()
        {// Function to request file name
         // or path to the file from user
         // to save data in.
            Console.Write(fileNameRequestReport);
            string userInput = GetCsvFormatFilePath();

            while (!ValidatePathAndName(userInput))
            {
                Console.WriteLine(badPathReport);
                Console.Write(fileNameRequestReport);
                userInput = Console.ReadLine();
            }

            return userInput;
        }

        private static string RequestExistingFilePath()
        {// Function to request file name or
         // path to the existing file.
            Console.Write(filePathRequestReport, currentWorkingDirectory);
            string userInput = GetCsvFormatFilePath();

            while (!ValidatePathAndName(userInput) || !File.Exists(userInput)) 
            {
                if (!ValidatePathAndName(userInput)) 
                { Console.WriteLine(badPathReport); }
                else 
                { Console.WriteLine(missingFileReport); }
                Console.Write(filePathRequestReport, currentWorkingDirectory);

                userInput = GetCsvFormatFilePath();
            }

            return userInput;
        }

        private static bool ValidatePathAndName(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
            { return false; }
            
            // Get file name and path to the
            // file from the userInput.
            string fileName = string.Empty;
            string pathToFile = string.Empty;
            string[] splittedPath = userInput
                .Trim()
                .Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (!(splittedPath is null) && splittedPath.Length > 0)
            {
                fileName = splittedPath[^1];
                // If path to the file is provived.
                int lastSepIndex = userInput.LastIndexOfAny(pathSeparators);
                if (lastSepIndex != -1)
                { pathToFile = userInput[..lastSepIndex]; }
            }

            return !string.IsNullOrWhiteSpace(fileName) &&
                pathToFile.IndexOfAny(invalidPathChars) == -1 &&
                fileName.IndexOfAny(invalidNameChars) == -1;
        }

        private static Encoding GetFileEncoding(string path)
        {// Function to read first bytes of the file and peek an Encoding.
            FileStream fileStream;
            try { fileStream = File.OpenRead(path); }
            catch { return null; }
            
            fileStream.Position = 0;
            long bomBytesLength = fileStream.Length > 4 ? 4 : fileStream.Length;
            byte[] bomBytes = new byte[bomBytesLength];
            fileStream.Read(bomBytes, 0, bomBytes.Length);
            fileStream.Close();

            return SelectEncoding(bomBytes);
        }

        private static Encoding SelectEncoding(byte[] bomBytes)
        {// Function to select file's encoding based on first bytes of the file.
            // UTF-16 BE starts with FE FF.
            if (bomBytes[0] == 0xFE && bomBytes[1] == 0xFF)
            { return Encoding.BigEndianUnicode; }

            // UTF-8 starts with EF BB BF.
            if (bomBytes[0] == 0xEF &&
                bomBytes[1] == 0xBB &&
                bomBytes[2] == 0xBF)
            { return Encoding.UTF8; }

            // UTF-7 starts with 2B 2F 76.
            if (bomBytes[0] == 0x2B && 
                bomBytes[1] == 0x2F && 
                bomBytes[2] == 0x76)
            { return Encoding.UTF7; }

            // Both UTF-16 LE and UTF-32 LE 
            // start with FF FE.
            if (bomBytes[0] == 0xFF && 
                bomBytes[1] == 0xFE)
            {
                if (bomBytes[2] == 0 && bomBytes[3] == 0)
                { return Encoding.UTF32; }
                return Encoding.Unicode;
            }
            
            // File may not contain BOM.
            return Encoding.Default;
        }

        private static bool ValidateDataHeaders(string[] data)
        {// Function to check first line of the csv file.
            if (data is null || data.Length == 0 || data[0] is null)
            { return false; }

            // Check whether first line of the
            // file contains any column name.
            if (data[0].Split(',').Length == 0)
            { return false; }

            // Check whether first line of the header is
            // the same as in source file weatherAUS.csv
            // from the dataset.
            if (!data[0].Equals(sourceFileHeader))
            { Console.WriteLine(fileColumnsWarning); }

            return true;
        }

        private static string GetCsvFormatFilePath()
        {// Function to read user input
         // from the console and check
         // whether user wrote extension
         // of the file correctly.
            string userInput = Console.ReadLine();

            if (userInput is null)
            { return string.Empty; }

            // In case user wrote only 
            // file name without extension.
            if (!userInput.EndsWith(".csv"))
            { userInput += ".csv"; }

            // If user provided only file
            // name without the full path.
            if (userInput.IndexOfAny(pathSeparators) == -1)
            { userInput = userInput.Insert(0, currentWorkingDirectory); }

            return userInput;
        }
    }
}
