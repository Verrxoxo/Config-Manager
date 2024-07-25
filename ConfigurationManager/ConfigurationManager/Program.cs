using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Formatting.Json;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;

class LogFileManager
{
    private static string SourceDirectory;
    private static int zipAfterDays;
    private static int zipDeleteAfterDays;
    private static string[] ValidExtensions;

    static void Main()
    {
        GetZipSettings();
        SourceDirectory = GetSourceDirectory();
        ValidExtensions = GetValidExtensions();

        if (string.IsNullOrEmpty(SourceDirectory))
        {
            Console.WriteLine("SourceDirectory cannot be null or empty.");
            return;
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(SourceDirectory, "all.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Starting Log File Manager");

        ZipAndDeleteLogs(SourceDirectory, zipAfterDays, zipDeleteAfterDays, ValidExtensions);

        System.Timers.Timer timer = new System.Timers.Timer(24 * 60 * 60 * 1000);
        timer.Elapsed += Timer_Elapsed;
        timer.Start();

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();

        timer.Stop();
        timer.Dispose();
        Log.CloseAndFlush();
    }
    private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        ZipAndDeleteLogs(SourceDirectory, zipAfterDays, zipDeleteAfterDays, ValidExtensions);
    }

    static void ZipAndDeleteLogs(string logDirectory, int zipAfterDays, int zipDeleteAfterDays, string[] validExtensions)
    {
        try
        {
            var files = Directory.GetFiles(logDirectory)
                        .Where(file => IsValidLogFile(file, zipAfterDays, validExtensions))
                        .GroupBy(file => File.GetCreationTime(file).Date)
                        .ToList();

            foreach (var group in files)
            {
                string zipFileName = $"Logs_{group.Key:yyyyMMdd}.zip";
                string zipFilePath = Path.Combine(logDirectory, zipFileName);

                using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    foreach (var file in group)
                    {
                        archive.CreateEntryFromFile(file, Path.GetFileName(file));
                        Log.Information($"Added '{file}' to '{zipFilePath}'.");
                        File.Delete(file);
                    }
                }
            }

            var zipFiles = Directory.GetFiles(logDirectory, "*.zip");
            foreach (string zipFile in zipFiles)
            {
                FileInfo fileInfo = new FileInfo(zipFile);
                if ((DateTime.Now - fileInfo.CreationTime).TotalDays > zipDeleteAfterDays)
                {
                    File.Delete(zipFile);
                    Log.Information($"Deleted old zip file '{zipFile}' after {zipDeleteAfterDays} days.");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to zip and delete logs.");
        }
    }

    static bool IsValidLogFile(string filePath, int maxDaysOld, string[] ValidExtensions)
    {
        bool isOldEnough = (DateTime.Now - File.GetCreationTime(filePath)).TotalDays >= maxDaysOld;
        bool hasValidExtension = ValidExtensions.Any(ext => filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

        return isOldEnough && hasValidExtension;
    }

    static void GetZipSettings()
    {
        string appSettingsPath = Path.Combine(Environment.CurrentDirectory, "AppSettings.json");
        if (File.Exists(appSettingsPath))
        {
            try
            {
                string json = File.ReadAllText(appSettingsPath);
                JObject settings = JObject.Parse(json);
                zipAfterDays = settings["ZipAfterDays"]?.Value<int>() ?? 7;
                zipDeleteAfterDays = settings["DeleteAfterDays"]?.Value<int>() ?? 30;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing AppSettings.json. Using default values.");
                zipAfterDays = 7;
                zipDeleteAfterDays = 30;
            }
        }
        else
        {
            Log.Warning("AppSettings.json not found. Using default values.");
            zipAfterDays = 7;
            zipDeleteAfterDays = 30;
        }
    }

    static string GetSourceDirectory()
    {
        string appSettingsPath = Path.Combine(Environment.CurrentDirectory, "AppSettings.json");
        string defaultDirectory = @"C:\Logs";
        if (File.Exists(appSettingsPath))
        {
            try
            {
                String json = File.ReadAllText(appSettingsPath);
                JObject settings = JObject.Parse(json);
                return settings["SourceDirectory"]?.ToString() ?? defaultDirectory;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing AppSettings.json. Using default directory.");
                return defaultDirectory;
            }
        }
        else
        {
            Log.Warning("AppSettings.json not found. Using default directory.");
            return defaultDirectory;
        }
    }
    static string[] GetValidExtensions()
    {
        string appSettingsPath = Path.Combine(Environment.CurrentDirectory, "AppSettings.json");
        if (File.Exists(appSettingsPath))
        {
            try
            {
                string json = File.ReadAllText(appSettingsPath);
                JObject settings = JObject.Parse(json);
                JArray extensionsArray = settings["ValidExtensions"] == null ? new JArray(new JObject(new JProperty("ValidExtensions", "[\".log\", \".txt\"]"))) : (JArray)settings["ValidExtensions"];
                return extensionsArray?.Select(ext => ext.ToString()).ToArray() ?? new string[] { ".log", ".txt" };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing AppSettings.json for valid extensions. Using default extensions.");
                return new string[] { ".log", ".txt" };
            }
        }
        else
        {
            Log.Warning("AppSettings.json not found. Using default extensions.");
            return new string[] { ".log", ".txt" };
        }
    }
}