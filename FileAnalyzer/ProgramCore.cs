using System;
using System.IO;
using System.Text;
using static FileAnalyzer.IOTools;

namespace FileAnalyzer
{   
    internal class ProgramCore
    {
        private const string sydneyInfoFileName = "Sydney_2009_2010_weatherAUS.csv";
        private const string locationRainfallFileName = "average_rain_weatherAUS.csv";
        private const string fileSaveReport = "Data was saved in the file: {0}";
        private const string fileSaveErrorReport = "An error occured while writing data to the file";

        private const string exitMessage = "Thanks for using the program. Press any key to exit";

        private readonly string _workingDir;
        private Table _table;

        internal ProgramCore()
        {
            _workingDir = GetCurrentWorkingDir();
            LoadNewFile();
        }    

        internal void SydneyInfoCommand()
        {// Function to select data
         // from the table based on
         // second criteria.
            (StringBuilder tableData, StringBuilder fileStrBuilder) = _table.FetchByLocationEndYear("Sydney", 2009, 2010);
            Print(tableData, end: "\n\n");

            string filePath = _workingDir + sydneyInfoFileName;
            SaveDataToFile(fileStrBuilder, filePath);
        }

        internal void LocationRainfallCommand()
        {// Function to select data
         // from the table based on
         // third criteria.
            (StringBuilder tableData, StringBuilder fileStrBuilder) = _table.FetchSortedByLocation();
            Print(tableData, end: "\n\n");

            string filePath = _workingDir + locationRainfallFileName;
            SaveDataToFile(fileStrBuilder, filePath);
        }

        internal void SunShineAndMaxTempCommand()
        {// Function to select data
         // from the table based on
         // fourth criteria.
            (StringBuilder tableData, StringBuilder fileStrBuilder) = _table.FetchBySunShine();
            Print(tableData, end: "\n\n");

            string filePath = RequestFilePathOrName();
            SaveDataToFile(fileStrBuilder, filePath);            
        }

        internal void StatisticCommand()
        {// Function to show statistic
         // about the data from the table.
            StringBuilder tableData = _table.ShowTableStatistic();
            Print(tableData, end: "\n\n");
        }

        internal void LoadNewFile()
        {// Function to load data from
         // the file in the new table.
         // File is selected by user.
            string[] dataFromFile = RequestDataFromFile();
            _table = new Table(dataFromFile);
        }

        internal void PrintExitMessage()
        {// Function to print exit message
         // before program will shutdown.
            ClearUserConsole();
            Print(exitMessage);
            Console.ReadKey(true);
        }

        internal void ClearUserConsole()
        {// Function to clear user's console.
            ClearConsole();
        }

        private void SaveDataToFile(StringBuilder fileStrBuilder, string filePath)
        {// Function to write data selected
         // from the table into the file.
            bool resultOfWriting = WriteToFile(filePath, fileStrBuilder);
            
            if (resultOfWriting)
            {// Show full path to the created file to the user.
                try
                {// Maybe file will be created with the
                 // wrong rights so program will not
                 // have access to it's full path.
                    FileInfo createdFileInfo = new FileInfo(filePath);
                    string fullPath = createdFileInfo.FullName;
                    Print(string.Format(fileSaveReport, fullPath), end: "\n\n");
                }
                catch
                { Print(string.Format(fileSaveReport, filePath), end: "\n\n"); }
            }
            else
            { Print(fileSaveErrorReport, end: "\n\n"); }
        }
    }
}
