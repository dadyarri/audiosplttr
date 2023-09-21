using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using AudioSplttr.Models;
using FFmpeg.NET;

namespace AudioSplttr.Splitter;

public static partial class AudioSplitter
{
    public static async IAsyncEnumerable<int> Split(SplttrOptions options)
    {
        var ffmpeg = new Engine(options.FFmpegPath);
        var inputFile = new InputFile(options.InputFile);
        var files = Directory.GetFiles(options.OutputPath);

        int counter;

        if (options.Rewrite || files.Length == 0)
        {
            counter = 1;
        }
        else
        {
            counter = files
                .Order()
                .Select(item =>
                    int.Parse(Regex.Match(item, @"(\d+)").Groups[1].Value)
                )
                .Max() + 1;
        }

        var startPoint = 0.0;

        var findSilenceProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = options.FFmpegPath,
                Arguments =
                    $"""-i "{options.InputFile}" -af silencedetect=n={options.SilenceVolume}dB:d={options.PauseSize} -f null -""",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };

        var ranges = new List<SplttrSilenceRange>();

        findSilenceProcess.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null && args.Data.Contains("silence_start"))
            {
                var matchSilenceStart = SilenceStartRegex().Match(args.Data);
                var value = matchSilenceStart.Groups[1].Value;
                var replace = value.Replace(".", ",");
                ranges.Add(new SplttrSilenceRange
                    { Start = double.Parse(replace) });
            }

            if (args.Data != null && args.Data.Contains("silence_end"))
            {
                var matchSilenceEnd = SilenceEndRegex().Match(args.Data);
                ranges[^1].End = double.Parse(matchSilenceEnd.Groups[1].Value.Replace(".", ","));
            }
        };

        findSilenceProcess.Start();
        findSilenceProcess.BeginOutputReadLine();
        findSilenceProcess.BeginErrorReadLine();
        await findSilenceProcess.WaitForExitAsync();

        var amountOfChunks = ranges.Count;
        var oneChunkProgress = 100 / amountOfChunks;

        foreach (var range in ranges)
        {
            var conversionOptions = new ConversionOptions();
            var outputFile =
                new OutputFile(
                    $"{options.OutputPath}\\{options.NameTemplate}_{counter}.{options.OutputFormat.ToString().ToLower()}");
            conversionOptions.CutMedia(
                TimeSpan.FromSeconds(startPoint),
                TimeSpan.FromSeconds((range.Start + 0.1) - startPoint)
            );
            await ffmpeg.ConvertAsync(inputFile, outputFile, conversionOptions, default);
            startPoint = range.End;
            counter++;
            yield return counter * oneChunkProgress;
        }

        yield return 100;
    }

    [GeneratedRegex(@"silence_start: (0|\d+.\d+)")]
    private static partial Regex SilenceStartRegex();

    [GeneratedRegex("silence_end: (\\d+.\\d+)")]
    private static partial Regex SilenceEndRegex();
}