using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace FileAnalyzer
{
    /// <summary>
    /// Class for keeping 
    /// and processing 
    /// data from the file.
    /// </summary>
    public class Table
    {
        private const string averageRainfallReport = "\nAverage rainfall in the {0}: {1} mm\n";
        private const string noRainfallReport = "No rainfall measurements in the {0}\n";
        private const string sunshinePeriodReport = "Longest sunshine period was on {0}.\n"
                                                  + "It lasted for {1} hours.\n"
                                                  + "Max temperature on that date was {2} °C.\n";
        private const string fishingDaysReport = "Amount of days suitable for fishing: {0}\n";
        private const string fishingDaysWithDirReport = "Amount of days suitable for fishing when wind\n" +
                                                        "  has only W, WSW, SW, SSW and W directions: {0}\n";
        private const string groupsCountReport = "Amount of groups of observations\n" +
                                                 "  grouped by location name: {0}\n";
        private const string eachGroupSizeReport =  "Amount of observations in group\n" +
                                                    "  from the {0}: {1}\n";
        private const string rainyDayAmountReport = "Amount of rainy days when max temperature\n" +
                                                    " temperature was at least 20 °C: {0}\n";
        private const string normalPressDaysReport = "Amount of days with normal pressure\n" +
                                                     " from 1000 to 1007 kPa: {0}\n";

        // Table line in format ┌──...──┐
        private readonly string _startTableLine;

        // Table line in format ├──...──┤
        private readonly string _midTableLine;

        // Table line in format └──...──┘
        private readonly string _endTableLine;

        // Table line in format │ Name1 │ ... │ NameK │
        private readonly string _columnsNamesLine;

        // Amount of fields in the row.
        // Needed for the accuracy of data 
        // representation in the console.
        private readonly int _amountOfFields;

        // Array with widths for 
        // each column in the table.
        // E.g. column for locations
        // can be 12 symbols wide
        // and column for date can be
        // 10 symbols wide.
        private readonly int[] _columnsWidth;
        private readonly string[] _columnsNames;
        private readonly string _csvFormatColumnsNames;        

        private readonly List<ObservationData> _observations;

        public Table(string[] data)
        {
            if (data == null || data.Length == 0)
            { return; }

            _amountOfFields = data[0].Trim().Split(',').Length;
            _observations = ParseData(data);

            _csvFormatColumnsNames = new string(data[0]);
            _columnsWidth = CalculateColumnsWidth();
            _columnsNames = ParseColumnsNames();

            // Form table main lines used
            // to form text representation
            // of the table in the console.
            _startTableLine = BuildStartLine();
            _midTableLine = BuildMidLine();
            _endTableLine = BuildEndLine();
            _columnsNamesLine = BuildColumnNamesLine();
        }

        public (StringBuilder, StringBuilder) FetchByLocationEndYear(string location, params int[] years)
        {// Function to select data based on location and years.
         // Returns observations in console table and csv formats
            List<ObservationData> filteredObservs = _observations
                .Where(observData => 
                    observData.Location.Equals(location) &&
                     years.Contains(observData.Year))
                .ToList();
            
            StringBuilder strBuilder = FormTable(filteredObservs, 20);
            StringBuilder fileStrBuilder = FormCsvData(filteredObservs);
            
            return (strBuilder, fileStrBuilder);
        }

        public (StringBuilder, StringBuilder) FetchSortedByLocation()
        {// Function to group observation by locations and
         // calculate average rainfall for each observation.
         // Returns observations in console table and csv formats.
            List<string> _locationsNames = _observations
                    .Select(observData => observData.Location)
                    .ToHashSet()
                    .ToList();

            StringBuilder strBuilder = 
                new StringBuilder(_observations.Count + _locationsNames.Count);
            
            StringBuilder fileStrBuilder = new StringBuilder(_observations.Count);
            fileStrBuilder.AppendLine(_csvFormatColumnsNames);

            foreach (string locationName in _locationsNames)
            {
                // List with observations from the same location.
                List<ObservationData> sameLocationObservs = _observations
                    .Where(observ => locationName.Equals(observ.Location))
                    .ToList();
                
                sameLocationObservs.Sort(
                    (observ1, observ2) => observ1.CompareRainfall(observ2));
                
                IEnumerable<double> rainfalls = sameLocationObservs
                    .Where(observData => observData.RainfallAsStr != "NA")
                    .Select(observData => observData.Rainfall);

                // Add report about the average rainfall.
                if (rainfalls.Count() > 0)
                {
                    strBuilder.AppendFormat(
                        averageRainfallReport, 
                        locationName, 
                        rainfalls.Average()
                    );
                }
                else
                {// If all observations from location have "NA" rainfall.
                    strBuilder.AppendFormat(
                        noRainfallReport, 
                        locationName
                    );
                }
                
                strBuilder.Append(FormTable(sameLocationObservs, 10));

                // Add selected observations to the file StringBuilder.
                fileStrBuilder.AppendJoin<ObservationData>('\n', sameLocationObservs);
            }

            return (strBuilder, fileStrBuilder);
        }

        public (StringBuilder, StringBuilder) FetchBySunShine()
        {// Function to select observations based on sunshine period length
         // and to find date and max temperature when that period was the longest.
         // Returns observations in console table and csv formats.
            List<ObservationData> filteredObservs = _observations
                .Where(observData => observData.SunshineAsStr != "NA")
                .Where(observData => observData.Sunshine >= 4.0)
                .ToList();

            ObservationData longestPeriodObserv = filteredObservs
                .MaxBy(observData => observData.Sunshine);

            // Report about longest sunshine's date and max temperature on that date.
            string report = string.Format(
                sunshinePeriodReport,
                longestPeriodObserv.Date,
                longestPeriodObserv.Sunshine,
                longestPeriodObserv.MaxTempAsStr
            );
            StringBuilder strBuilder = new StringBuilder(report);
            strBuilder.Append(FormTable(filteredObservs, 20));

            StringBuilder fileStrBuilder = FormCsvData(filteredObservs);

            return (strBuilder, fileStrBuilder);
        }

        public StringBuilder ShowTableStatistic()
        {
            (int fishingDaysCount, int fishingDaysWithDirCount)
                = FishingDaysCount();
            Dictionary<string, int> locsNamesAndCounts = LocsInEachGroups();
            int rainyDaysCount = CountRainyDays();
            int normalPressureDaysCount = CountNormalPressDays();
            
            StringBuilder strBuilder = new StringBuilder();
            // Add amount of fishing days without 
            // and with wind direction filter.
            strBuilder.AppendLine(string.Format(fishingDaysReport, fishingDaysCount));
            strBuilder.AppendLine(string.Format(fishingDaysWithDirReport, fishingDaysWithDirCount));
            
            // Add total amount of groups.
            strBuilder.AppendLine(string.Format(groupsCountReport, locsNamesAndCounts.Count));
            foreach(string locName in locsNamesAndCounts.Keys)
            {// Add info about size of each group.
                strBuilder.AppendLine(
                    string.Format(eachGroupSizeReport, locName, locsNamesAndCounts[locName]));
            }

            strBuilder.AppendLine(string.Format(rainyDayAmountReport, rainyDaysCount));
            strBuilder.AppendLine(string.Format(normalPressDaysReport, normalPressureDaysCount));

            return strBuilder;
        }

        private StringBuilder FormTable(List<ObservationData> observations, int howMuchFromStartAndEnd = -1)
        {// Function to form given observations into a StringBuilder
         // contains data in console text table format with headers.
            StringBuilder strBuilder = new StringBuilder(observations.Count);

            // Form the header of the text table.
            strBuilder.AppendLine(_startTableLine);
            strBuilder.AppendLine(_columnsNamesLine);
            strBuilder.AppendLine(_midTableLine);

            // Form body of the text table.
            if (howMuchFromStartAndEnd <= 0 ||
                observations.Count / 2 <= howMuchFromStartAndEnd)
            {// Add all observations.
                foreach(ObservationData observData in observations)
                { strBuilder.Append(BuildLine(observData)); }
            }
            else
            {// Add only 2 * howMuchFromStartAndEnd observations
                // Add first howMuchFromStartAndEnd elements.
                foreach(ObservationData observData in observations.Take(howMuchFromStartAndEnd))
                { strBuilder.Append(BuildLine(observData)); }

                // Add "..." to each column to show skipped lines.
                for (int j = 0; j != 3; ++j)
                { strBuilder.Append(BuildLine(".")); }

                // Add last howMuchFromStartAndEnd elements.
                foreach(ObservationData observData in observations.TakeLast(howMuchFromStartAndEnd))
                { strBuilder.Append(BuildLine(observData)); }
            }
            
            strBuilder.AppendLine(_endTableLine);

            return strBuilder;
        }
        
        private StringBuilder FormCsvData(List<ObservationData> obserations)
        {// Function to form given observations into
         // a StringBuilder with csv format lines.
            StringBuilder fileStrBuilder = new StringBuilder(obserations.Count);
            fileStrBuilder.AppendLine(_csvFormatColumnsNames);

            foreach (ObservationData observData in obserations)
            {// Fill StringBuilder with data for the csv file.
                fileStrBuilder.AppendLine(observData.ToString());
            }

            return fileStrBuilder;
        }

        private (int, int) FishingDaysCount()
        {// Function to calculate amount of days suitable for 
         // fishing both without and with wind direction filter.
            int goodFishDaysCount = _observations
                .Where(observData => !observData.WindSpeedAsStr.Equals("NA") &&
                    observData.WindSpeed < 13.0)
                .Count();

            HashSet<string> availableDirections = 
                new HashSet<string> { "W", "WSW", "SW", "SSW", "S" };
            int goodFishDaysCountWithDir= _observations
                .Where(observData => !observData.WindSpeedAsStr.Equals("NA") &&
                    observData.WindSpeed < 13.0)
                .Where(observData => availableDirections
                    .Contains(observData.WindDir.ToUpper()))
                .Count();

            return (goodFishDaysCount, goodFishDaysCountWithDir);
        }

        private Dictionary<string, int> LocsInEachGroups()
        {// Function to calculate amount of locations in each group.
            HashSet<string> differentLocations =
                _observations
                .Select(observData => observData.Location)
                .ToHashSet();

            Dictionary<string, int> locsNamesAndCounts =
                differentLocations
                .ToDictionary(
                    location => location,
                    location => _observations
                        .Where(observData => observData
                            .Location.Equals(location))
                        .Count());

            return locsNamesAndCounts;
        }

        private int CountRainyDays()
        {// Function to calculate amount of rainy days when
         // max temperature was at least 20 degrees Celsius.
            return _observations
                .Where(observData => !observData.MaxTempAsStr.Equals("NA"))
                .Where(observData => observData.MaxTemp >= 20.0)
                .Where(observData => observData.RainTodayAsStr.Equals("Yes"))
                .Count();
        }
        
        private int CountNormalPressDays()
        {// Function to calculate amount of days when
         // pressure at 9am was from 1000 to 1007 kPa.
            return _observations
                .Where(observData => !observData.PressureAsStr.Equals("NA"))
                .Where(observData => observData.Pressure >= 1000.0 &&
                    observData.Pressure <= 1007.0)
                .Count();
        }

        private List<ObservationData> ParseData(string[] data)
        {// Function to parse data from the file
         // and add it to the List with prepared data.
            List<ObservationData> observations = new List<ObservationData>(data.Length);
            foreach (string rowData in data.Skip(1))
            {// Add observations to the table.
                ObservationData observData = new ObservationData(rowData, _amountOfFields);
                if (observData.IsParsedSuccessfully)
                {// Check is needed because not every row can be parsed successfully.
                    observations.Add(observData);
                }
            }
            return observations;
        }

        private int[] CalculateColumnsWidth()
        {// Function to calculate width of each column
         // based on max width of the fields in each column.
            int[] columnsWidth = new int[_amountOfFields];
            foreach (ObservationData observData in _observations)
            {
                for (int i = 0; i < _amountOfFields; ++i)
                {
                    if (observData.SplittedData[i].Length > columnsWidth[i])
                    {
                        columnsWidth[i] = observData.SplittedData[i].Length;
                    }
                }
            }
            return columnsWidth;
        }

        private string[] ParseColumnsNames()
        {// Function to parse first line of the
         // table into the names for columns.
            string[] columnsNames = new string[_amountOfFields];
            Array.Copy(_csvFormatColumnsNames.Trim().Split(','), columnsNames, _amountOfFields);
            // To be able to write table to the console
            // names of the columns should be reduced.
            for (int i = 0; i < _amountOfFields; ++i)
            {
                if (columnsNames[i].Length > _columnsWidth[i])
                {// If name of the column is longer then column width.
                    columnsNames[i] = columnsNames[i].Substring(0, _columnsWidth[i] - 1) + ".";
                }
            }
            return columnsNames;
        }

        private string BuildStartLine()
        {// Function to build first line of text
         // table representation in the console.
            // Init StringBuilder with ┌ symbol.
            StringBuilder strBuilder = new StringBuilder("\u250C");

            for (int i = 0; i < _columnsWidth.Length - 1; ++i)
            {// Add ───...───┬.
                strBuilder.Append(new string('\u2500', _columnsWidth[i]));
                strBuilder.Append('\u252C');
            }

            // Add ───...───┐.
            strBuilder.Append(new string('\u2500', _columnsWidth[^1]));
            strBuilder.Append('\u2510');

            return strBuilder.ToString();
        }

        private string BuildMidLine()
        {// Function to build middle line of text
         // table representation in the console.
         // (this line needed for separating data)
            // Init StringBuilder with ├ symbol.
            StringBuilder strBuilder = new StringBuilder("\u251C");

            for (int i = 0; i < _columnsWidth.Length - 1; ++i)
            {// Add ───...───┼.
                strBuilder.Append(new string('\u2500', _columnsWidth[i]));
                strBuilder.Append('\u253C');
            }

            // Add ───...───┤.
            strBuilder.Append(new string('\u2500', _columnsWidth[^1]));
            strBuilder.Append('\u2524');

            return strBuilder.ToString();
        }

        private string BuildEndLine()
        {// Function to build end line of text
         // table representation in the console.
            // Init StringBuilder with └ symbol.
            StringBuilder strBuilder = new StringBuilder("\u2514");

            for (int i = 0; i < _columnsWidth.Length - 1; ++i)
            {// Add ───...───┴.
                strBuilder.Append(new string('\u2500', _columnsWidth[i]));
                strBuilder.Append('\u2534');
            }

            // Add ───...───┘.
            strBuilder.Append(new string('\u2500', _columnsWidth[^1]));
            strBuilder.Append('\u2518');

            return strBuilder.ToString();
        }
        
        private string BuildColumnNamesLine()
        {// Function to build line with column names.
            // Init StringBuilder with │ symbol
            StringBuilder strBuilder = new StringBuilder("\u2502");
            for (int i = 0; i < _amountOfFields; ++i)
            {// Add    NameK   │
                strBuilder.Append(_columnsNames[i].PadRight(_columnsWidth[i], ' ') + "\u2502");
            }
            return strBuilder.ToString();
        }

        private StringBuilder BuildLine(string str)
        {// Function to build line in format │ str │ ... │ str │
            // Init StringBuilder with │ symbol.
            StringBuilder strBuilder = new StringBuilder("\u2502");
            for (int i = 0; i < _amountOfFields; ++i)
            {// Add    str   │
                strBuilder.Append(str.PadRight(_columnsWidth[i], ' ') + "\u2502");
            }
            // Add line terminator
            strBuilder.AppendLine();
            return strBuilder;
        }

        private StringBuilder BuildLine(ObservationData observData)
        {
            StringBuilder strBuilder = new StringBuilder("\u2502");
            for (int i = 0; i < _amountOfFields; ++i)
            {
                strBuilder.Append(observData.SplittedData[i].PadRight(_columnsWidth[i], ' ') + "\u2502");
            }
            strBuilder.AppendLine();
            return strBuilder;
        }
    }
}
