namespace LogCreator
{
    internal class LogCreator
    {
        const string LogDirectory = @"C:\Logs";
        public static void Main(string[] args)
        {
            try
            {
                Console.Write("Enter the number of log files to create: ");
                string userInput = Console.ReadLine();
                int number;

                while (!int.TryParse(userInput, out number))
                {
                    Console.WriteLine("Invalid input. Please enter a number: ");
                    userInput = Console.ReadLine();
                }

                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                    Console.WriteLine($"Directory created at: {LogDirectory}");
                }
                else
                {
                    Console.WriteLine("Directory already exists.");
                }

                for (int i = 0; i < number; i++)
                {

                    DateTime logDate = DateTime.Now.Date.AddDays(-i);
                    string logFileName = @$"{LogDirectory}\app_{logDate:yyyy-MM-dd}.log";
                    bool fileExists = File.Exists(logFileName);


                    if (!fileExists)
                    {
                        File.Create(logFileName);

                        DateTime fileDate = DateTime.Now.AddDays(-i);
                        File.SetCreationTime(logFileName, fileDate);
                    }
                }
                Console.WriteLine("Log entries have been written to all files.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
            }
        }
    }
}
