using System;
using System.IO;
using ImageMagick.Formats;
using ImageMagick;
using System.Text;
using System.Linq;
using ShellProgressBar;
using System.Threading;
using System.Diagnostics.Tracing;
using System.Collections.Generic;

namespace HeicConvert
{
	class Program
	{
		static void Main(string[] args)
		{
			CheckArgs(args);

			const string inFormat = "HEIC";
			string outFormat = args[0].ToUpper();
			int threadsAmount = 4;

			var dir = Directory.CreateDirectory(args[1]);
			FileInfo[] files = dir.GetFiles()
				.Where(f => Path.GetExtension(f.FullName).ToUpper() == $".{inFormat}")
				.ToArray();

			int totalTicks = files.Length;
			var pbarSettings = new ProgressBarOptions
			{
				DenseProgressBar = true,
				CollapseWhenFinished = true,
				ShowEstimatedDuration = true
			};
			var pbar = new ProgressBar(totalTicks, "Converting files");

			var taskQueue = new Queue<(FileInfo file, string outputFileName)>(files.Length);

			for (int i = 0; i < files.Length; i++)
			{
				FileInfo file = files[i];

				string fullpath = file.FullName;
				string extension = file.Extension;
				var sb = new StringBuilder( fullpath );
				sb.Remove(fullpath.Length - extension.Length, extension.Length);
				string outFileName = $"{sb}.{outFormat}";

				taskQueue.Enqueue( (file, outFileName) );
			}
			Thread[] threads = new Thread[threadsAmount];
			for (int i = 0; i < threadsAmount; i++)
			{
				threads[i] = new Thread(new ThreadStart( () => {
					while (true)
					{
						(FileInfo, string) localItem;
						lock (taskQueue)
						{
							if (taskQueue.Count == 0)
								break;
							else
							{
								localItem = taskQueue.Dequeue();
							}
						}

						MagickFormat format;
						switch (outFormat)
						{
							case "JPEG": 
								format = MagickFormat.Jpeg;
								break;
							case "PNG": 
								format = MagickFormat.Png00;
								break;
							case "JPG": 
							default:
								format = MagickFormat.Jpg;
								break;
						}

						using (var image = new MagickImage(localItem.Item1, MagickFormat.Heic))
						{
							image.Write(new FileInfo(localItem.Item2), format);
						}
						pbar.Tick();
					}
				}));
				threads[i].Start();
			}

			foreach (var thread in threads)
			{
				thread.Join();
			}

			Environment.Exit(0);
		}
		private static void CheckArgs(string[] args)
		{
			if ( args.Length != 2)
			{
				Console.WriteLine("Invalid number of arguments.");
				Console.WriteLine("Needed arguments: output format [jpg (default), jpeg, png], directory path");
				Environment.Exit(1);
			}

			List<string> possibleFormats = new List<string>( new[] { "png", "jpg", "jpeg"} );
			if ( possibleFormats.IndexOf( args[0].ToLower() ) == -1 )
			{
				Console.WriteLine($"Chosen format ({args[0]}) is not supported.");
				Console.WriteLine("Please, choose one of the following: jpg (default), jpeg, png");
				Environment.Exit(1);
			}

			if ( !Directory.Exists(args[1]))
			{
				Console.WriteLine($"Directory {args[1]} does not exist.");
				Console.WriteLine("Check the directory name and try again, please.");
				Environment.Exit(1);
			}
		}
	}
}
