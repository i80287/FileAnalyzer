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

        private string _workingDir;
        private Table _table;

        internal ProgramCore()
        {
            _workingDir = GetCurrentWorkingDir();
            string[] dataFromFile = RequestDataFromFile();
            _table = new Table(dataFromFile);
        }    

        internal void SydneyInfoCommand()
        {
            (StringBuilder tableData, StringBuilder fileStrBuilder) = _table.FetchByLocationEndYear("Sydney", 2009, 2010);

            string filePath = _workingDir + sydneyInfoFileName;
            bool resultOfWriting = WriteToFile(filePath, fileStrBuilder);
            
            PrintCommandExecutionResult(tableData, resultOfWriting, filePath);
        }

        internal void LocationRainfallCommand()
        {
            (StringBuilder tableData, StringBuilder fileStrBuilder) = _table.FetchSortedByLocation();

            string filePath = _workingDir + locationRainfallFileName;
            bool resultOfWriting = WriteToFile(filePath, fileStrBuilder);

            PrintCommandExecutionResult(tableData, resultOfWriting, filePath);
        }

        internal void SunShineAndMaxTempCommand()
        {
            (StringBuilder tableData, StringBuilder fileStrBuilder) = _table.FetchBySunShine();

            string filePath = RequestFilePathOrName();
            bool resultOfWriting = WriteToFile(filePath, fileStrBuilder);

            PrintCommandExecutionResult(tableData, resultOfWriting, filePath);
        }

        private void PrintCommandExecutionResult(StringBuilder tableData, bool resultOfWriting, string filePath)
        {// Function to write table data and result of writing data to the file.
            Print();
            Print(tableData, end: "\n\n");

            if (resultOfWriting)
            { Print(string.Format(fileSaveReport, filePath), end: "\n\n"); }
            else
            { Print(fileSaveErrorReport, end: "\n\n"); }
        }
    }
}
