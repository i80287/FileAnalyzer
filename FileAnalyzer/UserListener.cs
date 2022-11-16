using System;

namespace FileAnalyzer
{
    /// <summary>
    /// Enum for understandable
    /// meaning the numbers of
    /// the available commands
    /// </summary>
    internal enum UserCommand : int
    {// Enumerator with available commands.
        SydneyInfo = 1,
        OrderedByLocation = 2,
        SunshineDays = 3,
        Statistics = 4,
        LoadNewFile = 5,
        ClearConsole = 6,
        Exit = 7
    }

    /// <summary>
    /// Class for interacting with user
    /// and receiving commands.
    /// </summary>
    internal class UserListener
    {
        private const string commandsList =
            "\nThe list of the available commands:\n\n"
          + "1. Show weather information about Sydney from 2009\n"
          + "    to 2010 and save data to the file\n\n"
          + "2. Show observations ordered by location and amount\n"
          + "    of rainfall and save data to the file\n\n"
          + "3. Show observations when sunshine period was longer\n"
          + "    then 4 hours, date and temperature for day with\n"
          + "    longest sunshine perdion and save data to the file\n"
          + "    Filename is selected by the user\n\n"
          + "4. Show statistics about the observations:\n"
          + "    1) Amount of days suitable for fishing, when afternoon\n"
          + "        wind speed was less then 13\n"
          + "    2) Amount of groups of observations from same locations\n"
          + "        and amount of observations in each group\n"
          + "    3) Amount of rainy warm days, when temperature was\n"
          + "        equals to or greater then 20 °C\n"
          + "    4) Amount of days with normal atmospheric pressure,\n"
          + "        when pressure is from 1000 to 1007 kPa\n\n"
          + "5. Load new file\n\n"
          + "6. Clear console\n\n"
          + "7. Exit from the menu\n\n"
          + "Write number of the command\n> ";

        private const string inputErrorReport = "Incorrect input. Please type number of the command";

        internal UserCommand NextCommand()
        {// Function to get correct command from user.
            Console.Write(commandsList);
            string userInput = Console.ReadLine();
            int commandNumber = (int)UserCommand.Exit;

            while (!int.TryParse(userInput, out commandNumber) ||
                   !Enum.IsDefined(typeof(UserCommand), commandNumber))
            {
                Console.WriteLine(inputErrorReport);
                Console.Write(commandsList);
                userInput = Console.ReadLine();
            }

            // Move cursor to the next line just for more
            // accurate display of the following data.
            Console.WriteLine();
            return (UserCommand)commandNumber;
        }
    }
}
