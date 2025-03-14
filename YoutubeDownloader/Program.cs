// ///////////////////////////////////////////////////////////////////////////////////////////////////////
//
//
// All rights reserved by OneBanc
//
//
// (c) Copyright 2024 OneBanc Technologies Pvt. Ltd.
//
//
// ////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

class Program
{

	private static string logFilePath = "../logs/log.log";
	private static double totalDuration;
	static async Task Main()
	{
		Console.OutputEncoding = Encoding.UTF8; // Ensure Unicode support

		Console.Write("\U0001F517 Enter YouTube video URL: "); // 🔗
		string videoUrl = Console.ReadLine();

		YoutubeClient youtube = new YoutubeClient();

		try
		{
			Video video = await youtube.Videos.GetAsync(videoUrl);
			totalDuration = ((TimeSpan)video.Duration).TotalSeconds;

			string safeTitle = Regex.Replace(video.Title, @"[^a-zA-Z0-9\s]", "").Trim().Replace(" ", "_");
			logFilePath = Path.Combine("../logs", $"log_{safeTitle}.log");

			string logDir = Path.GetDirectoryName(logFilePath);
			if (!Directory.Exists(logDir))
			{
				Directory.CreateDirectory(logDir);
			}
			Console.WriteLine($"\n\U0001F3A5 Downloading: {video.Title}"); // 🎬
			Log($"Downloading video: {video.Title}");

			StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);


			IList<MuxedStreamInfo> muxedStreams = streamManifest.GetMuxedStreams().OrderByDescending(s => s.VideoQuality).ToList();

			if (muxedStreams != null && muxedStreams.Count > 0)
			{
				// Show quality options
				Console.WriteLine("\nAvailable video qualities:");
				for (int i = 0; i < muxedStreams.Count; i++)
				{
					Console.WriteLine($"[{i + 1}] \t{muxedStreams[i].VideoQuality.Label}");
				}

				Console.Write("\n\U00002753 Choose quality (Enter number): "); // ❓
				if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > muxedStreams.Count)
				{
					Console.WriteLine("\U0000274C Invalid choice. Selecting highest available quality."); // ❌
					choice = 0; // Default to highest quality
				}

				MuxedStreamInfo muxedStream = muxedStreams[choice - 1];

				string filePath = Path.Combine("../videos", $"{safeTitle}_{muxedStream.VideoQuality.Label}.mp4");

				Console.WriteLine($"\n\U000023F3 Downloading Video + Audio in {muxedStream.VideoQuality.Label}..."); // ⏳
				await DownloadWithProgress(youtube, muxedStream, filePath);

				Console.WriteLine($"\n\U00002705 Download complete: {filePath}"); // ✅
				Log($"Download complete: {filePath}");
			}
			else
			{
				IList<IVideoStreamInfo> videoStreams = streamManifest.GetVideoStreams().OrderByDescending(s => s.VideoQuality).ToList();

				if (videoStreams.Count == 0)
				{
					Console.WriteLine("\U0000274C No video stream found!"); // ❌
					return;
				}

				IAudioStreamInfo audioStream = streamManifest.GetAudioStreams().OrderByDescending(s => s.Bitrate).FirstOrDefault();
				// Show quality options
				Console.WriteLine("\nAvailable video qualities:");
				int i = 0;
				foreach (IVideoStreamInfo stream in videoStreams)
				{
					Console.WriteLine($"[{(++i).ToString().PadLeft(2)}] \t{stream.VideoQuality.Label.PadRight(7)} \t[{(stream.Size.MegaBytes + audioStream?.Size.MegaBytes):F2} MB]");

				}

				Console.Write("\n\U00002753 Choose quality (Enter number): "); // ❓
				if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > videoStreams.Count)
				{
					Console.WriteLine("\U0000274C Invalid choice. Selecting highest available quality."); // ❌
					choice = 0; // Default to highest quality
				}

				IVideoStreamInfo videoStream = videoStreams[choice - 1];

				if (videoStream == null || audioStream == null)
				{
					Console.WriteLine("\U0000274C No suitable video or audio stream found!"); // ❌
					Log("No suitable video or audio stream found!");
					return;
				}

				string videoPath = Path.Combine(Directory.GetCurrentDirectory(), $"{safeTitle}_video.mp4");
				string audioPath = Path.Combine(Directory.GetCurrentDirectory(), $"{safeTitle}_audio.mp3");
				string outputPath = Path.Combine("../videos", $"{safeTitle}_{videoStream.VideoQuality.Label}.mkv");

				Console.WriteLine($"\n\U0001F39E Downloading Video in {videoStream.VideoQuality.Label}..."); // 📽️
				await DownloadWithProgress(youtube, videoStream, videoPath);

				Console.WriteLine("\n\U0001F3B5 Downloading Audio..."); // 🎵
				await DownloadWithProgress(youtube, audioStream, audioPath);

				Console.WriteLine("\n\U0001F504 Merging video and audio..."); // 🔄
				MergeVideoAndAudio(videoPath, audioPath, outputPath);

				Console.WriteLine("\n\U0001F3C1 Downloaded Successfully");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"\U0000274C Error: {ex.Message}"); // ❌
			Log($"Error: {ex.Message}");
		}

		Console.Write("Press Enter to exit!!");
		Console.Read();
	}

	static async Task DownloadWithProgress(YoutubeClient youtube, IStreamInfo stream, string filePath)
	{
		IProgress<double> progress = new Progress<double>(p =>
		{
			int barWidth = 30;
			int filledBars = (int)(p * barWidth);
			string progressBar = $"[{new string('\u2588', filledBars)}{new string('-', barWidth - filledBars)}] {(p * 100):0.0}%";
			// █ (U+2588) - Full block
			Console.Write($"\r{progressBar}");
		});

		await youtube.Videos.Streams.DownloadAsync(stream, filePath, progress);

		// ✅ Force 100% when download is done
		Console.CursorLeft = 0;
		Console.WriteLine("\r[██████████████████████████████] 100.0% ");
		Log($"Download completed: {filePath}");
	}

	static void MergeVideoAndAudio(string videoPath, string audioPath, string outputPath)
	{
		try
		{
			if (!File.Exists("ffmpeg.exe"))
			{
				Console.WriteLine("\u274C Error: ffmpeg.exe not found."); // ❌
				return;
			}

			if (!File.Exists(videoPath) || !File.Exists(audioPath))
			{
				Console.WriteLine("\u274C Error: Video or Audio file missing."); // ❌
				return;
			}

			if (totalDuration == 0)
			{
				Console.WriteLine("\u274C Error: Could not determine video duration."); // ❌
				return;
			}

			string outputDir = Path.GetDirectoryName(outputPath);
			if (!Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			string ffmpegCmd = $"-i \"{videoPath}\" -i \"{audioPath}\" -y -c copy \"{outputPath}\"";

			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = "ffmpeg.exe",
				Arguments = ffmpegCmd,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using (Process process = new Process { StartInfo = startInfo })
			{
				process.ErrorDataReceived += (sender, args) =>
				{
					if (args.Data != null)
					{
						UpdateProgress(args.Data, totalDuration);
						Log($"Merge Logs: {args.Data}");
					}
				};

				process.Start();
				process.BeginErrorReadLine();

				Thread spinnerThread = new Thread(ShowSpinner);
				spinnerThread.Start();

				process.WaitForExit();
				spinnerThread.Interrupt();

				// Ensure full progress bar before exit
				Console.CursorLeft = 0;
				Console.WriteLine("\r[██████████████████████████████] 100.0% ✅");

				Console.WriteLine("\n\u2705 Merge complete!"); // ✅
				Log("Merge completed successfully!\n");

				File.Delete(videoPath);
				File.Delete(audioPath);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"\u274C Unexpected error: {ex.Message}"); // ❌
			Log($"Unexpected error: {ex.Message}\n");
		}
	}

	static void UpdateProgress(string ffmpegOutput, double totalDuration)
	{
		Match match = Regex.Match(ffmpegOutput, @"time=(\d+):(\d+):(\d+\.\d+)");
		if (match.Success)
		{
			double hours = double.Parse(match.Groups[1].Value);
			double minutes = double.Parse(match.Groups[2].Value);
			double seconds = double.Parse(match.Groups[3].Value);
			double currentTime = (hours * 3600) + (minutes * 60) + seconds;

			int progress = (int)((currentTime / totalDuration) * 100);
			progress = Math.Min(progress, 100); // ✅ Prevent going over 100%

			int barWidth = 30;
			int filledBars = (progress * barWidth) / 100;
			string progressBar = $"[{new string('\u2588', filledBars)}{new string('-', barWidth - filledBars)}] {progress}%";

			Console.CursorLeft = 0;
			Console.Write($"\r{progressBar}");
		}
	}

	static void ShowSpinner()
	{
		char[] spinnerChars = { '|', '/', '-', '\\' };
		int index = 0;
		try
		{
			while (true)
			{
				Console.CursorLeft = 60;
				Console.Write(spinnerChars[index]);
				Thread.Sleep(100);
				index = (index + 1) % spinnerChars.Length;
			}
		}
		catch (ThreadInterruptedException)
		{
			Console.CursorLeft = 60;
			Console.Write(" ");
		}
	}

	static void Log(string message)
	{
		string logMessage = $"{DateTime.Now}: {message}";

		using (StreamWriter writer = new StreamWriter(logFilePath, true))
		{
			writer.WriteLine(logMessage);
		}
	}
}
