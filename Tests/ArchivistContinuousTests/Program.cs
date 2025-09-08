using ContinuousTests;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Archivist Continuous-Test-Runner.");

        var runner = new ContinuousTestRunner(args, Cancellation.Cts.Token);

        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("Stopping...");
            e.Cancel = true;

            Cancellation.Cts.Cancel();
        };

        try
        {
            runner.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        Console.WriteLine("Done.");
    }
}

public static class Cancellation
{
    static Cancellation()
    {
        Cts = new CancellationTokenSource();
    }

    public static CancellationTokenSource Cts { get; }
}
