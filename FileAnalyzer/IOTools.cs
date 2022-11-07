using System;
using System.IO;
using System.Linq;
using System.Text;

namespace FileAnalyzer {
    /// <summary>
    /// This class contains methods to work 
    /// with file system and input-output
    /// interface (console).
    /// </summary>
    internal static class IOTools {
        private const string filePathRequestReport = "Type file name or full path to the file\n"
                                                   + "By default file will be searched in {0}\n> ";
        private const string badPathReport = "Invalid name or path format";
        private const string missingFileReport = "File not found";
        private const string badEncodingReport = "File is corrupted or data is represented in unsupported encoding";
        private const string fileReadErrorReport = "An error occured while reading the file. Please type file name or path again";
        private const string fileHeadersErrorReport = "Structure of the csv file is incorrect. Program can't parse that file";
        private const string fileColumnsWarning = "Structure of the provided csv file doesn't match the\n"
                                                     + "structure of weatherAUS.csv source file from the dataset\n"
                                                     + "Data may be read incorrectly\n";
        private const string fileNameRequestReport = "Type file name or full path to the file for saving data in it\n> ";

        // First line of the source file
        // weatherAUS.csv from the dataset
        private const string sourceFileHeader = "Date,Location,MinTemp,MaxTemp,Rainfall,Evaporation,Sunshine,"
                                              + "WindGustDir,WindGustSpeed,WindDir9am,WindDir3pm,WindSpeed9am,"
                                              + "WindSpeed3pm,Humidity9am,Humidity3pm,Pressure9am,Pressure3pm,"
                                              + "Cloud9am,Cloud3pm,Temp9am,Temp3pm,RainToday,RainTomorrow";

        private static readonly char[] invalidPathChars = Path.GetInvalidPathChars();
        private static readonly char[] invalidNameChars = Path.GetInvalidFileNameChars();

        private static readonly char[] pathSeparators = new char[2] { '/', '\\' };

        private static readonly string currentWorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;

        internal static void Print(char symbol = ' ', string end = "\n")
        {// Function to write char in the console.
            end ??= "\n";
            Console.Write(symbol);
            Console.Write(end);
        }

        internal static void Print(object data, string end = "\n")
        {// Function to write string representation of the object in the console.
            if (data is null) { return; }
            end ??= "\n";
            Console.Write(data.ToString());
            Console.Write(end);
        }

        internal static void Print(object[] data, string separator = " ", string end = "\n")
        {// Function to write array of objects in the console.
            if (data is null) { return; }
            separator ??= " ";
            end ??= "\n";
            Console.Write(string
                .Join(separator, data
                .Where(elem => !(elem is null))));
            Console.Write(end);
        }

        internal static void Print(StringBuilder data, string end = "\n")
        {// Function to write data from the StringBuilder in the console.
            if (data is null) { return; }
            end ??= "\n";
            Console.Write(data.ToString());
            Console.Write(end);
        }

        internal static string GetCurrentWorkingDir()
                => currentWorkingDirectory;

        internal static string[] RequestDataFromFile() 
        {
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
        {// Function to write data to the file. If file exists it will be overwritten
            if (!ValidatePathAndName(path)) { return false; }
            try { File.WriteAllText(path, content.ToString()); } 
            catch { return false; }
            return true;
        }

        internal static string RequestFilePathOrName()
        {
            Console.Write(fileNameRequestReport);
            string userInput = Console.ReadLine();

            while (!ValidatePathAndName(userInput))
            {
                Console.WriteLine(badPathReport);
                Console.Write(fileNameRequestReport);
                userInput = Console.ReadLine();
            }

            return userInput;
        }

        private static string RequestExistingFilePath()
        {
            Console.Write(filePathRequestReport, currentWorkingDirectory);
            string userInput = Console.ReadLine();
            if (!(userInput is null) && !userInput.EndsWith(".csv"))
            {// In case user wrote only file name without extension.
                userInput += ".csv";
            }

            while (!ValidatePathAndName(userInput) || !File.Exists(userInput)) 
            {
                if (!ValidatePathAndName(userInput)) 
                { Console.WriteLine(badPathReport); }
                else 
                { Console.WriteLine(missingFileReport); }
                Console.Write(filePathRequestReport, currentWorkingDirectory);

                userInput = Console.ReadLine();
                if (!(userInput is null) && !userInput.EndsWith(".csv"))
                {// In case user wrote only file name without extension.
                    userInput += ".csv";
                }
            }

            return userInput;
        }

        private static bool ValidatePathAndName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            { return false; }
            
            // Get file name from the path.
            string fileName = string.Empty;
            string pathToFile = string.Empty;
            string[] splittedPath = path.Trim().Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (!(splittedPath is null) && splittedPath.Length > 0)
            {
                fileName = splittedPath[^1];
                int lastSepIndex = path.LastIndexOfAny(pathSeparators);
                if (lastSepIndex != -1)
                {// If path to the file is provived;
                    pathToFile = path[..lastSepIndex];
                }
            }

            return !string.IsNullOrWhiteSpace(fileName)
                && pathToFile.IndexOfAny(invalidPathChars) == -1
                && fileName.IndexOfAny(invalidNameChars) == -1;
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
            if (bomBytes[0] == 0xFE && bomBytes[1] == 0xFF)
            {// UTF-16 BE starts with FE FF.
                return Encoding.BigEndianUnicode;
            }
            if (bomBytes[0] == 0xEF
                && bomBytes[1] == 0xBB
                && bomBytes[2] == 0xBF)
            {// UTF-8 starts with EF BB BF.
                return Encoding.UTF8;
            }
            if (bomBytes[0] == 0x2B
                && bomBytes[1] == 0x2F
                && bomBytes[2] == 0x76)
            {// UTF-7 starts with 2B 2F 76.
                return Encoding.UTF7;
            }
            if (bomBytes[0] == 0xFF
                && bomBytes[1] == 0xFE)
            {// both UTF-16 LE and UTF-32 LE start with FF FE
                if (bomBytes[2] == 0 && bomBytes[3] == 0)
                {
                    return Encoding.UTF32;
                }
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
    }
}
