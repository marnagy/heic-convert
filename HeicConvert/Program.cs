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

			//ThreadPool.SetMaxThreads(workerThreads: 2, completionPortThreads: 2);

			//CountdownEvent countdown = new CountdownEvent(files.Length);
			for (int i = 0; i < files.Length; i++)
			{
				FileInfo file = files[i];

				//ThreadPool.QueueUserWorkItem(_ =>
				//{
				string fullpath = file.FullName;
				string extension = file.Extension;
				var sb = new StringBuilder( fullpath );
				sb.Remove(fullpath.Length - extension.Length, extension.Length);
				string outFilename = $"{sb}.{outFormat}";

				using (var image = new MagickImage(fullpath, MagickFormat.Heic))
				{
					image.Write(outFilename);
				}

				pbar.Tick();

			}
			//countdown.Wait();
			Environment.Exit(0);
		}
		private static void CheckArgs(string[] args)
		{
			if ( args.Length != 2)
			{
				Console.WriteLine("Invalid number of arguments.");
				Console.WriteLine("Needed arguments: output format [png, jpg, jpeg], directory path");
				Environment.Exit(1);
			}

			List<string> possibleFormats = new List<string>( new[] { "png", "jpg", "jpeg"} );
			if ( possibleFormats.IndexOf( args[0] ) == -1 )
			{
				Console.WriteLine($"Chosen format ({args[0]}) is not supported.");
				Console.WriteLine("Please, choose one of the following: png, jpg, jpeg");
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
