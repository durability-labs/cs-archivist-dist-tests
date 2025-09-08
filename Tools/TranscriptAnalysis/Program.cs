using ArchivistPlugin.OverwatchSupport;
using Logging;
using OverwatchTranscript;
using TranscriptAnalysis;

public static class Program
{
    private static readonly ILog log = new ConsoleLog();

    public static void Main(string[] args)
    {
        Log("Transcript Analysis");
        if (!args.Any())
        {
            Log("Please pass a .owts file");
            Console.ReadLine();
            return;
        }

        if (!File.Exists(args[0]))
        {
            Log("File doesn't exist: " + args[0]);
            Console.ReadLine();
            return;
        }

        var reader = OpenReader(args[0]);
        AppDomain.CurrentDomain.ProcessExit += (e, s) =>
        {
            CloseReader(reader);
        };

        var header = reader.GetHeader<OverwatchArchivistHeader>("cdx_h");
        var receivers = new ReceiverSet(args[0], log, reader, header);
        receivers.InitAll();

        var processor = new Processor(log, reader);
        processor.RunAll();

        receivers.FinishAll();

        CloseReader(reader);
        Log("Done.");
        Console.ReadLine();
    }

    private static ITranscriptReader OpenReader(string filepath)
    {
        try
        {
            Log($"Opening: '{filepath}'");
            return Transcript.NewReader(filepath);
        }
        catch (Exception ex)
        {
            Log("Failed to open file for reading: " + ex);
            Console.ReadLine();
            Environment.Exit(1);
            return null;
        }
    }

    private static void CloseReader(ITranscriptReader reader)
    {
        try
        {
            Log("Closing...");
            reader.Close();
        }
        catch (Exception ex)
        {
            Log("Failed to close reader: " + ex);
        }
    }

    private static void Log(string msg)
    {
        log.Log(msg);
    }
}
