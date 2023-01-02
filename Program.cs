using System.Diagnostics;

namespace disk_wait;
class Program
{
    static int Main(string[] args)
    {
        var cancelled = false;

        Console.CancelKeyPress += (s, e) =>
        {
            cancelled = true;
        };

        int r;
        Process? pi = null;
        try
        {
            var psi = new ProcessStartInfo("dstat", $"-d");
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput  = true;

            pi  = Process.Start(psi);
            if (pi == null)
                return -1;

            using var std = pi.StandardOutput;
            // Пропускаем строки заголовка
            std.ReadLine();
            std.ReadLine();

            do
            {
                Thread.Sleep(100);
                r = getStatistics(std);            
            }
            while (!cancelled && r >= 0);
        }
        finally
        {
            pi?.Kill();
        }

        return 0;
    }

    public static int getStatistics(StreamReader std)
    {

        var line = std.ReadLine()?.Trim();
        if (line == null || line.Length <= 0)
            return -1;

        // Console.WriteLine(line);
        // read write
        // 826k  327k

        var splitted = line.Split(new string[] {" ", "\t"}, StringSplitOptions.RemoveEmptyEntries);
        if (splitted.Length < 2)
        {
            Console.Error.WriteLine("Incorrect format of dstat");
            return -1 - 1;
        }

        if (
            haveOperations(splitted[0].Trim()) ||
            haveOperations(splitted[1].Trim())
            )
            return 1;

        return -1;
    }

    public static bool haveOperations(string str)
    {
        // Console.Write(str + "\t");

        if (str == "0")
            return false;
        if (str.EndsWith("B"))
            return false;
        if (str.EndsWith("k") && str.Length <= 3)
            return false;

        return true;
    }
}
