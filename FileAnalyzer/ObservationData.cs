using System;
using System.Linq;
using System.Collections.Generic;


namespace FileAnalyzer
{
    /// <summary>
    /// Class represents data parsed
    /// from one row of table.
    /// </summary>
    internal class ObservationData
    {
        /// <remarks>
        /// To sort and filter data we need double 
        /// variables for Temperature, Rainfall, etc.
        /// 
        /// _someVariableAsString variables needed to keep "NA" values
        /// (we can't keep them in double because double can't be null)
        /// </remarks>
        private DateTime _date;
        private string _location;
        private double _maxTemp;
        private string _maxTempAsString;
        private double _rainfall;
        private string _rainfallAsString;
        private double _sunshine;
        private string _sunshineAsString;
        private string _windDir3pm;
        private double _windSpeed3pm;
        private string _windSpeed3pmAsString;
        private double _pressure9am;
        private string _pressure9amAsString;
        private bool _rainToday;
        private string _rainTodayAsString;

        // data in csv format
        private readonly string _rawData;

        // data in splitted format to write on the screen
        public string[] SplittedData { get; }

        public string Location { get => _location; }

        public int Year { get => _date.Year; }

        public string Date { get => $"{_date.Year}-{_date.Month}-{_date.Day}"; }

        public double Rainfall { get => _rainfall; }

        public string RainfallAsStr { get => _rainfallAsString; }

        public double Sunshine { get => _sunshine; }

        public string SunshineAsStr { get => _sunshineAsString; }
        
        public double MaxTemp { get => _maxTemp; }

        public string MaxTempAsStr { get => _maxTempAsString; }
        
        public double WindSpeed { get => _windSpeed3pm; }

        public string WindSpeedAsStr { get => _windSpeed3pmAsString; }

        public string WindDir { get => _windDir3pm; }
                
        public string RainTodayAsStr { get => _rainTodayAsString; }

        public double Pressure { get => _pressure9am; }

        public string PressureAsStr { get => _pressure9amAsString; }

        public bool IsParsedSuccessfully { get; }

        public ObservationData(string fileRow, int amountOfFields)
        {
            if (string.IsNullOrWhiteSpace(fileRow))
            {// If row is empty we shouldn't add it to the table with parsed data.
                IsParsedSuccessfully = false;
                return;
            }

            List<string> splittedData = fileRow.Trim().Split(',').ToList();
            while (splittedData.Count < amountOfFields)
            {// Auto fill the list in order not to check it's length when parsing.
                splittedData.Add(string.Empty);
            }

            _rawData = new string(fileRow);
            SplittedData = new string[amountOfFields];
            Array.Copy(splittedData.ToArray(), SplittedData, amountOfFields);

            ParseData(splittedData);
            IsParsedSuccessfully = true;
        }

        internal int CompareRainfall(ObservationData otherObservData)
        {
            if (_rainfallAsString == "NA" && otherObservData.RainfallAsStr == "NA")
            { return 0; }
            if (_rainfallAsString == "NA")
            { return 1; }
            if (otherObservData.RainfallAsStr == "NA")
            { return -1; }
            if (_rainfall < otherObservData.Rainfall)
            { return 1; }
            if (_rainfall == otherObservData.Rainfall)
            { return 0; }
            return -1;
        }
        
        private void ParseData(List<string> splittedData)
        {// function to parse needed data from the list 
            DateTime.TryParse(splittedData[0], out _date);
            
            _location = splittedData[1];

            double.TryParse(splittedData[3], out _maxTemp);
            _maxTempAsString = splittedData[3];

            double.TryParse(splittedData[4], out _rainfall);
            _rainfallAsString = splittedData[4];

            double.TryParse(splittedData[6], out _sunshine);
            _sunshineAsString = splittedData[6];

            _windDir3pm = splittedData[10];

            double.TryParse(splittedData[12], out _windSpeed3pm);
            _windSpeed3pmAsString = splittedData[12];

            double.TryParse(splittedData[15], out _pressure9am);
            _pressure9amAsString = splittedData[15];

            if (splittedData[21] == "Yes") { _rainToday = true; }
            _rainTodayAsString = splittedData[21];
        }

        public override string ToString()
            => _rawData;
    }
}
