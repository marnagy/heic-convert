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
			int threadsAmount = args.Length == 3 ? int.Parse(args[2]) : 1;

			var dir = Directory.CreateDirectory(args[1]);
			FileInfo[] files = dir.GetFiles()
				.Where(f => Path.GetExtension(f.FullName).ToUpper() == $".{inFormat}")
				.ToArray();

			int totalTicks = files.Length;
			var pbarOptions = new ProgressBarOptions
			{
				DenseProgressBar = true,
				CollapseWhenFinished = true,
				ShowEstimatedDuration = true,
				 ForegroundColor = ConsoleColor.White
			};
			var pbar = new ProgressBar(totalTicks, "Converting files", options: pbarOptions);

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

			if (threadsAmount == 1)
			{
				while (taskQueue.Count > 0)
				{
					var entry = taskQueue.Dequeue();

					using (var image = new MagickImage(entry.file.FullName, MagickFormat.Heic))
					{
						image.Write(entry.outputFileName, format);
					}
					pbar.Tick();
				}
			}
			else
			{
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

							
							try
							{
								using (var image = new MagickImage(localItem.Item1, MagickFormat.Heic))
								{
									image.Write(localItem.Item2, format);
								}
							}
							catch
							{

							}
							finally
							{
								pbar.Tick();
							}
						}
					}));
					threads[i].Start();
				}

				foreach (var thread in threads)
				{
					thread.Join();
				}
			}

			Environment.Exit(0);
		}
		private static void CheckArgs(string[] args)
		{
			if ( args.Length < 2 || args.Length > 3)
			{
				Console.WriteLine("Invalid number of arguments.");
				Console.WriteLine("Needed arguments: output format [jpg, jpeg, png], directory path, thread amount (optional, default=1)");
				Environment.Exit(1);
			}

			List<string> possibleFormats = new List<string>( new[] { "png", "jpg", "jpeg"} );
			if ( possibleFormats.IndexOf( args[0].ToLower() ) == -1 )
			{
				Console.WriteLine($"Chosen format ({args[0]}) is not supported.");
				Console.WriteLine("Please, choose one of the following: jpg, jpeg, png");
				Environment.Exit(1);
			}

			if ( !Directory.Exists(args[1]))
			{
				Console.WriteLine($"Directory {args[1]} does not exist.");
				Console.WriteLine("Check the directory name and try again, please.");
				Environment.Exit(1);
			}

			if ( args.Length == 3)
			{
				if ( int.TryParse(args[2], out int t))
				{
					if ( t <= 0)
					{
						Console.WriteLine($"Invalid number of threads: {t}");
						Console.WriteLine("Please choose a positive integer");
						Environment.Exit(1);
					}
				}
				else
				{
					Console.WriteLine($"Invalid number of threads: {t}");
					Console.WriteLine("Please choose a positive integer");
					Environment.Exit(1);
				}
			} 
		}
	}
}
