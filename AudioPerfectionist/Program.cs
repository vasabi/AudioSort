using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using TagLib.Id3v1;
using TagLib.Id3v2;

namespace MFLogParser
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.SetWindowSize(180, 60);
            }
            catch
            {
            }
            
            Console.WriteLine("В работе...");
            Console.WriteLine("{0}", Environment.NewLine);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var musicToSortDir = baseDir + "music_to_sort";
            if (!Directory.Exists(musicToSortDir))
                musicToSortDir = Directory.CreateDirectory(musicToSortDir).FullName;

            var musicSortedDir = baseDir + "music_sorted";
            if (!Directory.Exists(musicSortedDir))
                musicSortedDir = Directory.CreateDirectory(musicSortedDir).FullName;

            var inputAudioFiles = Directory.EnumerateFiles(musicToSortDir, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".mp3") || s.EndsWith(".ogg") || s.EndsWith(".flac"));
            var filesCount = inputAudioFiles.Count();

            var patternFileName = @"(.*) - (.*) - (.*)";

            var fileCounter = 0;

            //Parallel.ForEach(inputAudioFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (currentFile) =>
            foreach (var currentFile in inputAudioFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(currentFile);
                var ext = Path.GetExtension(currentFile);
                var artist = Regex.Replace(fileName, patternFileName, "$1");
                var album = Regex.Replace(fileName, patternFileName, "$2");
                var track = Regex.Replace(fileName, patternFileName, "$3");
                if (Path.GetDirectoryName(currentFile) == musicToSortDir + "\\_Lossless")
                    track = track + " (Lossless)";                

                var albumDir = Path.Combine(musicSortedDir, artist, album);
                if (!Directory.Exists(albumDir))
                    albumDir = Directory.CreateDirectory(albumDir).FullName;

                var trackFilePath = Path.Combine(albumDir, track + ext);

                if (!File.Exists(trackFilePath))
                {
                    File.Move(currentFile, trackFilePath);

                    FinalOutput(track, albumDir);
                }              

                else
                {
                    ChangeConsoleFColourRed();
                    Console.Write("В папке уже есть файл с таким именем: ");
                    ChangeConsoleFColourBlue();
                    Console.Write("{0}", track + ext);

                    FileInfo currentFileInfo = new FileInfo(currentFile);
                    FileInfo existingFileInfo = new FileInfo(trackFilePath);

                    Console.WriteLine("{0}", Environment.NewLine);
                    Console.Write("Существующий файл ");
                    ChangeConsoleColourReset();
                    Console.Write("|");
                    ChangeConsoleFColourYellow();
                    Console.Write(" Новый файл");
                    ChangeConsoleColourReset();
                    Console.WriteLine("{0}", Environment.NewLine);
                    Console.WriteLine("Размер: {0} Mb | {1} Mb", existingFileInfo.Length / 1024 / 1024, currentFileInfo.Length / 1024 / 1024);
                    Console.WriteLine("Файл создан: {0} | {1}", existingFileInfo.LastWriteTime, currentFileInfo.LastWriteTime);
                    Console.WriteLine("Битрейт: {0} | {1}", TagLib.File.Create(existingFileInfo.FullName).Properties.AudioBitrate,
                        TagLib.File.Create(currentFileInfo.FullName).Properties.AudioBitrate);
                    Console.WriteLine("{0}", Environment.NewLine);
                    ChangeConsoleColourReset();
                    Console.Write("1 - ");
                    ChangeConsoleFColourBlue();
                    Console.Write("сохранить старый файл");
                    ChangeConsoleColourReset();
                    Console.Write(", 2 - ");
                    ChangeConsoleFColourYellow();
                    Console.Write("сохранить новый файл");
                    ChangeConsoleColourReset();
                    Console.Write(", 3 - ");
                    ChangeConsoleFColourGreen();
                    Console.Write("сохранить оба фала");
                    ChangeConsoleColourReset();
                    Console.WriteLine("{0}", Environment.NewLine);

                    Boolean isInputCorrect;
                    do
                    {
                        ConsoleKeyInfo answer;
                        answer = Console.ReadKey();
                        isInputCorrect = true;
                        switch (answer.Key)
                        {
                            case ConsoleKey.D1:
                            case ConsoleKey.NumPad1:
                                Console.CursorLeft = 0;
                                Console.Write(" ");
                                Console.CursorLeft = 0;
                                File.Delete(currentFile);
                                break;
                            case ConsoleKey.D2:
                            case ConsoleKey.NumPad2:
                                Console.CursorLeft = 0;
                                File.Delete(trackFilePath);
                                File.Move(currentFile, trackFilePath);

                                FinalOutput(track, albumDir);
                                break;
                            case ConsoleKey.D3:
                            case ConsoleKey.NumPad3:
                                Console.CursorLeft = 0;
                                var trackNew = track;
                                var index = 0;
                                do
                                {                                    
                                    trackNew = track + " (" + index + ")";
                                    index++;
                                }
                                while (File.Exists(Path.Combine(albumDir, trackNew + ext)));
                                ChangeConsoleFColourGreen();
                                Console.WriteLine("Имя нового файла: {0}", trackNew);
                                Console.WriteLine("{0}", Environment.NewLine);
                                ChangeConsoleColourReset();

                                File.Move(currentFile, Path.Combine(albumDir, trackNew + ext));

                                FinalOutput(trackNew, albumDir);
                                break;
                            default:
                                isInputCorrect = false;
                                Console.CursorLeft = 0;
                                Console.Write("Либо 1, либо 2, либо 3, блеать!");
                                Console.CursorLeft = 0;
                                break;
                        }
                    }
                    while (isInputCorrect == false);
                }

                Interlocked.Increment(ref fileCounter);
                Console.Title = "Файлов найдено: " + filesCount +
                    "; Перемещено: " + fileCounter + " Текущий файл: " + fileName + "." + ext;
            }
            //});

            GC.Collect();

            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            ChangeConsoleFColourGreen();
            Console.WriteLine("{0}", Environment.NewLine);
            Console.WriteLine("Готово! Времени затрачено: {0}", ts);
            Console.WriteLine("{0}", Environment.NewLine);
            Console.WriteLine("Файлов перемещено: {0} из {1}", fileCounter, filesCount);

            ConsoleKeyInfo OnExit;
            do
            {
                Console.WriteLine("{0}", Environment.NewLine);
                Console.WriteLine("Press Esc to exit");
                OnExit = Console.ReadKey();
            }
            while (OnExit.Key != ConsoleKey.Escape);
        }

        static void ChangeConsoleColourReset()
        {
            Console.ResetColor();
        }

        static void ChangeConsoleFColourRed()
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }

        static void ChangeConsoleFColourGreen()
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }

        static void ChangeConsoleFColourBlue()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
        }

        static void ChangeConsoleFColourYellow()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        static void FinalOutput(string track, string albumDir)
        {
            Console.Write("Трек ");
            ChangeConsoleFColourBlue();
            Console.Write("{0} ", track);
            ChangeConsoleColourReset();
            Console.Write("перемещен сюда: ");
            ChangeConsoleFColourGreen();
            Console.Write("{0}", albumDir);
            ChangeConsoleColourReset();
            Console.WriteLine("{0}", Environment.NewLine);
        }
    }
}
