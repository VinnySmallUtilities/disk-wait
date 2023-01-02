/*
dotnet publish --output build/ -c Release
cp build/* /G/disk-wait/
/G/disk-wait/disk-wait 3
*/

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

        Int64 needCnt = 1;
        try
        {
            if (args.Length > 0)
                needCnt = Int64.Parse(args[0]);
        }
        catch
        {
            Console.Error.WriteLine("Illegal argument");
        }


        int r;
        Process? pi = null;
        Int64 cnt = 0;
        try
        {
            var psi = new ProcessStartInfo("dstat", $"--noheaders -d");
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput  = true;

            pi  = Process.Start(psi);
            if (pi == null)
                return -1;

            using var std = pi.StandardOutput;
            // Пропускаем строки заголовка
            // std.ReadLine();
            // std.ReadLine();

            do
            {
                // Thread.Sleep(100);
                r = getStatistics(std);
                if (r > 0) // Если жёсткий диск нагружен
                {
                    cnt = 0;
                }
                else
                if (r < 0)
                {
                    cnt++;
                }
            }
            while (!cancelled && cnt < needCnt);
        }
        finally
        {
            pi?.Kill();
        }

        return 0;
    }

    /// <summary></summary>
    /// <param name="std">Стандартный поток вывода dstat</param>
    /// <returns>Значение больше 0, если проводятся операции с жёстким диском. Меньше - нет операций. 0 - неопределено</returns>
    public static int getStatistics(StreamReader std)
    {
        var line = std.ReadLine()?.Trim();
        if (line == null || line.Length <= 0)
            return -1;

        // Пропускаем заголовок dstat - он его периодически выводит
        if (line.Contains("dsk/total") || line.Contains("read"))
            return 0;

        // Console.WriteLine(line);
        // read write
        // 826k  327k

        var splitted = line.Split(new string[] {" ", "\t"}, StringSplitOptions.RemoveEmptyEntries);
        if (splitted.Length < 2)
        {
            Console.Error.WriteLine("Incorrect format of dstat");
            return -1 - 1;
        }

        // Если есть операции с диском - выдаём об этом информацию
        if (
            haveOperations(splitted[0].Trim()) ||
            haveOperations(splitted[1].Trim())
            )
            return 1;

        // Если операции с диском есть, но скорее случайные - то даём неопределённый вывод
        if (haveUnknown(splitted[0].Trim()) || haveUnknown(splitted[1].Trim()))
            return 0;

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

    public static bool haveUnknown(string str)
    {
        if (str.EndsWith("k") && str.Length <= 3)
            return true;

        return false;
    }
}
