using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

class ConfigurationManager
{
    static void Main()
    {
        string logDirectory = @"C:\Logs";
        int zipAfterDays = GetZipAfterDays();
        int zipDeleteAfterDays = GetDeleteAfterDays();
        string[] validExtensions = GetValidExtensions();

        ZipAndDeleteLogs(logDirectory, zipAfterDays, zipDeleteAfterDays, validExtensions);

        // daily timer to run the task
        System.Timers.Timer timer = new System.Timers.Timer();
        timer.Interval = 24 * 60 * 60 * 1000; // 24 hours in milliseconds
        timer.Elapsed += (sender, e) => ZipAndDeleteLogs(logDirectory, zipAfterDays, zipDeleteAfterDays, validExtensions);
        timer.Start();

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();

        timer.Stop();
        timer.Dispose();
    }

    static void ZipAndDeleteLogs(string logDirectory, int zipAfterDays, int zipDeleteAfterDays, string[] validExtensions)
    {
        try
        {
            IEnumerable<string> logFiles = Directory.GetFiles(logDirectory)
                                                    .Where(file => IsValidLogFile(file, zipAfterDays, validExtensions));

            foreach (string file in logFiles)
            {
                string zipFilePath = Path.ChangeExtension(file, ".zip");

                if (!File.Exists(zipFilePath))
                {
                    using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                    {
                        archive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }

                    Console.WriteLine($"file '{file}' zipped to '{zipFilePath}'.");
                    File.Delete(file);
                }
                else
                {
                    if (File.Exists(file))
                    {
                        File.Delete(zipFilePath);
                        using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                        {
                            archive.CreateEntryFromFile(file, Path.GetFileName(file));
                        }

                        Console.WriteLine($"Re-zipped file '{zipFilePath}'.");
                        File.Delete(file);
                    }
                }
            }      
            string[] zipFiles = Directory.GetFiles(logDirectory, "*.zip");
            foreach (string zipFile in zipFiles)
            {
                FileInfo fileInfo = new FileInfo(zipFile);
                if (DateTime.Now - fileInfo.CreationTime > TimeSpan.FromDays(zipDeleteAfterDays))
                {
                    File.Delete(zipFile);
                    Console.WriteLine($"Deleted old zip file '{zipFile}' after {zipDeleteAfterDays} days.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static bool IsValidLogFile(string filePath, int maxDaysOld, string[] validExtensions)
    {
        bool isOldEnough = (DateTime.Now - File.GetCreationTime(filePath)).TotalDays >= maxDaysOld;
        bool hasValidExtension = validExtensions.Any(ext => filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

        return isOldEnough && hasValidExtension;
    }

    static int GetZipAfterDays()
    {
        try
        {
            string appSettingsPath = Path.Combine(Environment.CurrentDirectory, "AppSettings.json");
            if (File.Exists(appSettingsPath))
            {
                string json = File.ReadAllText(appSettingsPath);
                dynamic settings = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                return settings?.ZipAfterDays ?? 7; 
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading AppSettings.json: {ex.Message}");
        }

        return 7;
    }

    static int GetDeleteAfterDays()
    {
        try
        {
            string appSettingsPath = Path.Combine(Environment.CurrentDirectory, "AppSettings.json");
            if (File.Exists(appSettingsPath))
            {
                string json = File.ReadAllText(appSettingsPath);
                dynamic settings = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                return settings?.DeleteAfterDays ?? 30;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading AppSettings.json: {ex.Message}");
        }

        return 30; 
    }

    static string[] GetValidExtensions()
    {
        try
        {
            string validationFilePath = Path.Combine(Environment.CurrentDirectory, "validation.json");
            if (File.Exists(validationFilePath))
            {
                string json = File.ReadAllText(validationFilePath);
                JObject settings = JObject.Parse(json);
                JArray extensionArray = (JArray)settings["extension"];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading validation.json: {ex.Message}");
        }
        return new string[] { ".log", ".txt" };
    }
}

