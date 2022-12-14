namespace FileAnalyzer 
{
    /// <remarks>
    /// "Вариант 5. Отличный денёк для рыбалки в Австралии"
    /// 
    /// This work made by
    /// kormilitsyn vladimir
    /// from group БПИ226.
    /// </remarks>

    /// <summary>
    /// This class binds backend of
    /// the program (class ProgramCore)  
    /// and frondend of the program
    /// (class UserListener).
    /// </summary>
    public class Program
    {
        private ProgramCore _programCore;
        private UserListener _userListener;

        private static void Main(string[] args)
            => new Program().Run();

        public Program()
        {// Initialize backend part and frontend part classes.
            // Change locale to en-US 
            // to avoid errors with parsing
            // double numbers from the file.
            ChangeLocale();
            _programCore = new ProgramCore();
            _userListener = new UserListener();
        }

        public void Run()
        {
            do
            {// Main program loop.
                UserCommand userCommand = _userListener.NextCommand();
                switch (userCommand)
                {// Choose the command to execute based on user input.
                    case UserCommand.SydneyInfo:
                    { _programCore.SydneyInfoCommand(); }
                    break;

                    case UserCommand.OrderedByLocation:
                    { _programCore.LocationRainfallCommand(); }
                    break;

                    case UserCommand.SunshineDays:
                    { _programCore.SunShineAndMaxTempCommand(); }
                    break;

                    case UserCommand.Statistics:
                    { _programCore.StatisticCommand(); }
                    break;

                    case UserCommand.LoadNewFile:
                    { _programCore.LoadNewFile(); }
                    break;

                    case UserCommand.ClearConsole:
                    { _programCore.ClearUserConsole(); }
                    break;

                    case UserCommand.Exit:
                    { _programCore.PrintExitMessage(); }
                    return;
                }
            }
            while (true);
            
        }

        private void ChangeLocale()
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;
            System.Threading.Thread.CurrentThread.CurrentCulture 
                = new System.Globalization.CultureInfo("en-US", false);
            System.Threading.Thread.CurrentThread.CurrentUICulture 
                = new System.Globalization.CultureInfo("en-US", false);
        }
    }
}