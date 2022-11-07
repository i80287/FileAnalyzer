namespace FileAnalyzer 
{
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
            _programCore = new ProgramCore();
            _userListener = new UserListener();
            // Change locale to en-US 
            // to avoid errors with parsing
            // double numbers from the file.
            ChangeLocale();
        }

        public void Run()
        {
            bool manuallyExit = false;
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

                    case UserCommand.Exit:
                    { manuallyExit = true; }
                    break;
                }
            }
            while (!manuallyExit);
        }

        private void ChangeLocale()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
        }
    }
}