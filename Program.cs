using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter
{
    internal class Program
    {
        private const FileOptions FileFlagNoBuffering = (FileOptions)0x20000000;
        private static Stopwatch stopwatch;
        private static FileStream file;
        private static int bufferSize;
        private static byte[] buffer;
        static void Main(string[] args)
        {
            if (args.Length < 4 || args.Length > 5)
            {
                Console.WriteLine($"Dexter.exe READ|WRITE|BOTH FileName SizeInGb BufferSizeInMb [CYCLES]");
                return;
            }

            var runType = args[0].ToUpper();
            var fileName = args[1];

            if (Path.GetExtension(fileName) == string.Empty)
            {
                fileName += ".dexter";
            }

            if (!int.TryParse(args[2], out int fileSizeInGb))
            {
                Console.WriteLine($"Unable to parse {args[2]} as a size.");
                Console.WriteLine($"Dexter.exe READ|WRITE|BOTH FileName SizeInGb BufferSizeInMb [CYCLES]");
                return;

            }

            if (!int.TryParse(args[3], out int bufferSizeInMb))
            {
                Console.WriteLine($"Unable to parse {args[2]} as a size.");
                Console.WriteLine($"Dexter.exe READ|WRITE|BOTH FileName SizeInGb BufferSizeInMb [CYCLES]");
                return;
            }

            bufferSize = bufferSizeInMb * 1024 * 1024;
            buffer = new byte[bufferSize];

            int cycles;

            if (args.Length == 5)
            {
                if (!int.TryParse(args[4], out cycles))
                {
                    cycles = 1;
                }
            }
            else
            {
                cycles = 1;
            }

            var cycleTime = new TimeSpan[cycles];

            try
            {
                switch (runType)
                {
                    case "READ":
                        {
                            Console.WriteLine($"Running {cycles} read cycle(s) of {fileSizeInGb}GB against '{fileName}'");
                            if (!File.Exists(fileName))
                            {
                                file = File.OpenWrite(fileName);
                                Console.WriteLine($"Generating test file '{fileName}'");
                                GenerateRandomData();
                                WriteData(fileSizeInGb * 1024, bufferSizeInMb);
                                file.Close();
                            }


                            file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None, bufferSizeInMb * 1024 * 1024, FileFlagNoBuffering | FileOptions.SequentialScan);

                            stopwatch = new Stopwatch();
                            stopwatch.Start();

                            for (int i = 0; i < cycles; i++)
                            {
                                Console.WriteLine($"******** Cycle {i + 1} start. ********");
                                var startTime = stopwatch.ElapsedMilliseconds;
                                ReadData(fileSizeInGb * 1024, bufferSizeInMb);

                                var endTime = stopwatch.ElapsedMilliseconds;
                                var elapsed = TimeSpan.FromMilliseconds(endTime - startTime);
                                cycleTime[i] = elapsed;

                                Console.WriteLine($"Cycle time: {elapsed}");
                                Console.WriteLine($"******** Cycle {i + 1} end. ********");
                                Console.WriteLine();
                            };

                            file.Close();
                            break;
                        }

                    case "WRITE":
                        {
                            Console.WriteLine($"Running {cycles} write cycle(s) of {fileSizeInGb}GB against '{fileName}'");

                            file = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSizeInMb * 1024 * 1024, FileFlagNoBuffering | FileOptions.WriteThrough);

                            stopwatch = new Stopwatch();
                            stopwatch.Start();

                            for (int i = 0; i < cycles; i++)
                            {
                                Console.WriteLine($"******** Cycle {i + 1} start. ********");
                                GenerateRandomData();
                                var startTime = stopwatch.ElapsedMilliseconds;
                                WriteData(fileSizeInGb * 1024, bufferSizeInMb);

                                var endTime = stopwatch.ElapsedMilliseconds;
                                var elapsed = TimeSpan.FromMilliseconds(endTime - startTime);
                                cycleTime[i] = elapsed;

                                Console.WriteLine($"Cycle time: {elapsed}");
                                Console.WriteLine($"******** Cycle {i + 1} end. ********");
                                Console.WriteLine();

                            };

                            file.Close();
                            break;
                        }

                    case "BOTH":
                        {
                            Console.WriteLine($"Running {cycles} read/write cycle(s) of {fileSizeInGb}GB against '{fileName}'");

                            file = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSizeInMb * 1024 * 1024, FileFlagNoBuffering | FileOptions.WriteThrough);

                            stopwatch = new Stopwatch();
                            stopwatch.Start();

                            for (int i = 0; i < cycles; i++)
                            {
                                Console.WriteLine($"******** Cycle {i + 1} start. ********");
                                GenerateRandomData();
                                var startTime = stopwatch.ElapsedMilliseconds;
                                
                                WriteData(fileSizeInGb * 1024, bufferSizeInMb);
                                ReadData(fileSizeInGb * 1024, bufferSizeInMb);
                                
                                var endTime = stopwatch.ElapsedMilliseconds;
                                var elapsed = TimeSpan.FromMilliseconds(endTime - startTime);
                                cycleTime[i] = elapsed;

                                Console.WriteLine($"Cycle time: {elapsed}");
                                Console.WriteLine($"******** Cycle {i + 1} end. ********");
                                Console.WriteLine();

                            };

                            file.Close();
                            break;
                        }

                    default:
                        {
                            Console.WriteLine($"Dexter.exe READ|WRITE|BOTH FileName SizeInMB [CYCLES]");
                            return;
                        }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in dexter: {e}");
                return;
            }

            long min = long.MaxValue;
            long max = 0;
            long total = 0;

            foreach (var time in cycleTime)
            {
                var ms = (int)time.TotalMilliseconds;
                min = Math.Min(min, ms);
                max = Math.Max(max, ms);
                total += ms;
            }

            var minTimeSpan = TimeSpan.FromMilliseconds(min);
            var maxTimeSpan = TimeSpan.FromMilliseconds(max);
            var totalTimeSpan = TimeSpan.FromMilliseconds(total);
            var averageTimeSpan = TimeSpan.FromMilliseconds(total / cycles);

            Console.WriteLine($"Minimum time: {minTimeSpan}");
            Console.WriteLine($"Maximum time: {maxTimeSpan}");
            Console.WriteLine($"Total time: {totalTimeSpan}");
            Console.WriteLine($"Average time: {averageTimeSpan}");
        }

        static void GenerateRandomData()
        {
            var random = new Random();

            Console.WriteLine("Generating random data");
            for (int i = 0; i < bufferSize; i++)
            {
                buffer[i] = (byte)random.Next();
            }
        }

        static void WriteData(int sizeInMb, int bufferSizeInMb)
        {
            file.Seek(0, SeekOrigin.Begin);
            var loops = Math.Ceiling((double)(sizeInMb / bufferSizeInMb));
            Console.WriteLine($"Writting {sizeInMb}MB in {bufferSizeInMb}MB sized blocks ({loops})");

            {
                for (int i = 0; i < loops; i++)
                {
                    file.Write(buffer, 0, bufferSize);
                    file.Flush();
                }

                Console.WriteLine($"Wrote {file.Length} bytes");
            }
        }

        static void ReadData(int sizeInMb, int bufferSizeInMb)
        {
            file.Seek(0, SeekOrigin.Begin);
            var loops = Math.Ceiling((double)(sizeInMb / bufferSizeInMb));
            Console.WriteLine($"Reading {sizeInMb}MB in {bufferSizeInMb}MB sized blocks ({loops})");

            {
                for (int i = 0; i < loops; i++)
                {
                    file.Read(buffer, 0, bufferSize);
                }

                Console.WriteLine($"Read {file.Length} bytes");
            }
        }
    }
}
