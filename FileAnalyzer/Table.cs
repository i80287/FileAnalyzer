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
        private const string averageRainfallReport = "# Average rainfall in the {0}: {1} mm";
        private const string noRainfallReport = "# No rainfall measurements in the {0}";
        private const string sunshinePeriodReport = "Longest sunshine period was on {0}.\n"
                                                  + "It lasted for {1} hours.\n"
                                                  + "Max temperature on that date was {2} °C.\n\n";
        private const string fishingDaysReport =  "Amount of days suitable for fishing: {0}";
        private const string fishingDaysWithDirReport = "Amount of days suitable for fishing when wind\n" +
                                                        " has only w, wsw, sw, ssw and w directions: {0}";
        private const string groupsCountReport = "Amount of groups of observations " +
                                                 "grouped by location name: {0}";
        private const string eachGroupSizeReport =  "Amount of observations in group" +
                                                    "  from the {0}: {1}";
        private const string rainyDayAmountReport = "Amount of rainy days when max temperature\n" +
                                                    " temperature was at least 20 °C: {0}";
        private const string normalPressDaysReport = "Amount of days with normal pressure" +
                                                     " from 1000 to 1007 kPa: {0}";

        private List<ObservationData> _observations;

        // Amount of fields in the row.
        // Needed for the accuracy of data 
        // representation data in the console.
        private int _amountOfFields;
        // Array with widths for 
        // each column in the table.
        // E.g. column for locations
        // can be 12 symbols wide
        // and column for date can be
        // 10 symbols wide.
        private int[] _columnsWidth;
        private int _tableWidth;
        private string _csvFormatColumnsNames;
        private string[] _columnsNames;
        
        public Table(string[] data)
        {
            if (data == null || data.Length == 0)
            {// If empty data is provided.
                _observations = new List<ObservationData>(0);
                return;
            }

            _amountOfFields = data[0].Trim().Split(',').Length;

            _csvFormatColumnsNames = string.Copy(data[0]);
            _columnsNames = new string[_amountOfFields];
            Array.Copy(data[0].Trim().Split(','), _columnsNames, _amountOfFields);
            _columnsWidth = new int[_amountOfFields];

            _observations = new List<ObservationData>(data.Length);
            foreach(string rowData in data.Skip(1))
            {
                ObservationData observData = new ObservationData(rowData, _amountOfFields);

                if (observData.IsParsedSuccessfully)
                {// Check is needed because not every row can be parsed successfully.
                    _observations.Add(observData);
                }
            }
            
            PreProcessData();
        }

        private void PreProcessData()
        {// Function to pre calculate data needed
         // to execute user commands.         

            // Calculating max width for each column.
            foreach (ObservationData observData in _observations)
            {
                for (int i = 0; i < _amountOfFields; ++i)
                {
                    if (observData.SplittedData[i].Length > _columnsWidth[i])
                    {
                        _columnsWidth[i] = observData.SplittedData[i].Length;
                    }
                }
            }

            // To be able to write table to the console we should
            // reduce the names of the columns
            for (int i = 0; i < _columnsNames.Length; ++i)
            {
                if (_columnsNames[i].Length > _columnsWidth[i])
                {
                    _columnsNames[i] = _columnsNames[i].Substring(0, _columnsWidth[i] - 1);
                    _columnsNames[i] += ".";
                }
            }

            // Sum of width for each column
            // + amount of " # " separated symbols between columns
            // + 1 '#' symbol for the right border of the table
            _tableWidth = _columnsWidth.Sum() + 3 * _columnsWidth.Length + 1;
        }

        public (StringBuilder, StringBuilder) FetchByLocationEndYear(string location, params int[] years)
        {// Function to select data based on location and years.
         // Returns observations in console table and csv formats
            List<ObservationData> filteredObservs = _observations
                .Where(observData => observData.Location.Equals(location) 
                                     && years.Contains(observData.Year))
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

            StringBuilder strBuilder = new StringBuilder(_observations.Count + _locationsNames.Count);
            
            StringBuilder fileStrBuilder = new StringBuilder(_observations.Count);
            fileStrBuilder.AppendLine(_csvFormatColumnsNames);

            for (int i = 0; i < _locationsNames.Count; i++)
            {
                // List with observations from the same location.
                List<ObservationData> sameLocationObservs = _observations
                    .Where(observ => _locationsNames[i].Equals(observ.Location))
                    .ToList();

                sameLocationObservs.Sort((observ1, observ2) => observ1.CompareRainfall(observ2));
                
                IEnumerable<double> rainfalls = sameLocationObservs
                    .Where(observData => observData.RainfallAsStr != "NA")
                    .Select(observData => observData.Rainfall);

                // Add report about the average rainfall.
                if (rainfalls.Count() > 0)
                {
                    double averageRainfall = rainfalls.Average();
                    string report = string.Format(averageRainfallReport, _locationsNames[i], averageRainfall)
                        .PadRight(_tableWidth - 1, ' ') + "#";
                    strBuilder.AppendLine(report);
                }
                else
                {// If all observations from location have "NA" rainfall.
                    string report = string.Format(noRainfallReport, _locationsNames[i])
                        .PadRight(_tableWidth - 1, ' ') + "#";
                    strBuilder.AppendLine(report);
                }

                // Add separation line.
                strBuilder.AppendLine(new string('#', _tableWidth));

                strBuilder.Append(FormPartialTable(sameLocationObservs, 10));
                
                // Add end line.
                strBuilder.AppendLine(new string('#', _tableWidth));

                foreach (ObservationData observData in sameLocationObservs)
                {// Fill StringBuilder with data for the csv file.
                    fileStrBuilder.AppendLine(observData.ToString());
                }
            }

            return (FormTableFromContent(strBuilder, false), fileStrBuilder);
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

            StringBuilder strBuilder = FormTable(filteredObservs, 20);

            // Report about longest sunshine's date and max temperature on that date.
            string report = string.Format(
                sunshinePeriodReport,
                longestPeriodObserv.Date,
                longestPeriodObserv.Sunshine,
                longestPeriodObserv.MaxTempAsStr
            );
            strBuilder.Insert(0, report);

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

        private StringBuilder FormTable(List<ObservationData> obserations, int howMuchFromStartAndEnd = -1)
        {// Function to form given observations into a StringBuilder
         // contains data in console text table format with headers.
            StringBuilder strBuilder = new StringBuilder(obserations.Count);

            // Forming the header of the text table.
            strBuilder.AppendLine(new string('#', _tableWidth));
            strBuilder.Append("# ");
            for (int i = 0; i < _amountOfFields; ++i)
            {
                strBuilder.Append(_columnsNames[i].PadRight(_columnsWidth[i], ' ') + " # ");
            }
            strBuilder.Append('\n');
            strBuilder.AppendLine(new string('#', _tableWidth));

            // Form body of the text table.
            if (howMuchFromStartAndEnd <= 0
                || obserations.Count / 2 <= howMuchFromStartAndEnd)
            {// Add all observations.
                foreach (ObservationData observData in obserations)
                {
                    strBuilder.Append("# ");
                    for (int i = 0; i < _amountOfFields; ++i)
                    {
                        strBuilder.Append(observData.SplittedData[i].PadRight(_columnsWidth[i], ' ') + " # ");
                    }
                    strBuilder.Append('\n');
                }
            }
            else
            {// Add only 2 * howMuchFromStartAndEnd observations
                foreach (ObservationData observData in obserations.Take(howMuchFromStartAndEnd))
                {// Add first howMuchFromStartAndEnd elements.
                    strBuilder.Append("# ");
                    for (int i = 0; i < _amountOfFields; ++i)
                    {
                        strBuilder.Append(observData.SplittedData[i].PadRight(_columnsWidth[i], ' ') + " # ");
                    }
                    strBuilder.Append('\n');
                }

                for (int j = 0; j != 3; ++j)
                {// Add "..." to each column to show reduction.
                    strBuilder.Append("# ");
                    for (int i = 0; i < _amountOfFields; ++i)
                    {
                        strBuilder.Append(".".PadRight(_columnsWidth[i], ' ') + " # ");
                    }
                    strBuilder.Append('\n');
                }
                                
                foreach (ObservationData observData in obserations.Skip(obserations.Count - howMuchFromStartAndEnd))
                {// Add last howMuchFromStartAndEnd elements.
                    strBuilder.Append("# ");
                    for (int i = 0; i < _amountOfFields; ++i)
                    {
                        strBuilder.Append(observData.SplittedData[i].PadRight(_columnsWidth[i], ' ') + " # ");
                    }
                    strBuilder.Append('\n');
                }
            }

            // Add last line to the text table.
            strBuilder.AppendLine(new string('#', _tableWidth));

            return strBuilder;
        }

        private StringBuilder FormPartialTable(List<ObservationData> obserations, int howMuchFromStartAndEnd = - 1)
        {// Function to form given observations into a StringBuilder
         // contains data in console text table format without
         // headers and last line.
            StringBuilder strBuilder = new StringBuilder(obserations.Count);

            // Form body of the text table.
            if (howMuchFromStartAndEnd <= 0
                || obserations.Count / 2 <= howMuchFromStartAndEnd)
            {// Add all observations.
                foreach (ObservationData observData in obserations)
                {
                    strBuilder.Append("# ");
                    for (int i = 0; i < _amountOfFields; ++i)
                    {
                        strBuilder.Append(observData.SplittedData[i].PadRight(_columnsWidth[i], ' ') + " # ");
                    }
                    strBuilder.Append('\n');
                }
            }
            else
            {// Add only 2 * howMuchFromStartAndEnd observations
                foreach (ObservationData observData in obserations.Take(howMuchFromStartAndEnd))
                {// Add first howMuchFromStartAndEnd elements.
                    strBuilder.Append("# ");
                    for (int i = 0; i < _amountOfFields; ++i)
                    {
                        strBuilder.Append(observData.SplittedData[i].PadRight(_columnsWidth[i], ' ') + " # ");
                    }
                    strBuilder.Append('\n');
                }

                for (int j = 0; j != 3; ++j)
                {// Add "..." to each column to show reduction.
                    strBuilder.Append("# ");
                    for (int i = 0; i < _amountOfFields; ++i)
                    {
                        strBuilder.Append(".".PadRight(_columnsWidth[i], ' ') + " # ");
                    }
                    strBuilder.Append('\n');
                }

                foreach (ObservationData observData in obserations.Skip(obserations.Count - howMuchFromStartAndEnd))
                {// Add last howMuchFromStartAndEnd elements.
                    strBuilder.Append("# ");
                    for (int i = 0; i < _amountOfFields; ++i)
                    {
                        strBuilder.Append(observData.SplittedData[i].PadRight(_columnsWidth[i], ' ') + " # ");
                    }
                    strBuilder.Append('\n');
                }
            }

            return strBuilder;
        }

        private StringBuilder FormTableFromContent(StringBuilder content, bool addEndLine = true)
        {// Function to form given StringBuilder into
         // data in console text table format with headers.

            StringBuilder strBuilder = new StringBuilder();

            // Forming the header of the text table.
            strBuilder.AppendLine(new string('#', _tableWidth));
            strBuilder.Append("# ");
            for (int i = 0; i < _amountOfFields; ++i)
            {
                strBuilder.Append(_columnsNames[i].PadRight(_columnsWidth[i], ' ') + " # ");
            }
            strBuilder.Append('\n');
            strBuilder.AppendLine(new string('#', _tableWidth));

            // Add provided content to the body of the text table.
            strBuilder.Append(content);

            if (addEndLine)
            {// Last line of the text table.
                strBuilder.AppendLine(new string('#', _tableWidth));
            }

            return strBuilder;
        }

        private StringBuilder FormCsvData(List<ObservationData> obserations)
        {// Function to form given observations into a StringBuilder with csv format lines.
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
            Dictionary<string, int> locsNamesAndCounts = 
                _observations.ToDictionary(
                    observData => observData.Location, 
                    observData => _observations
                        .Where(observData => observData
                            .Equals(observData.Location))
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
    }
}
