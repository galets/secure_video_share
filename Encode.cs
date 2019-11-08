using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace svisha
{
    public class Encode
    {
        public enum OutputRatio
        {
            R16x9, R4x3, R9x16
        }

        public enum OutputCodec
        {
            h264, h265
        }

        class StreamRate
        {
            public string Name { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int BitRate { get; set; }
            public int AudioBitRate { get; set; }

            public StreamRate(string name, int w, int h, int br, int abr) => (Name, Width, Height, BitRate, AudioBitRate) = (name, w, h, br, abr);
        }

        readonly static StreamRate[] StreamH264Wide = 
        {
            new StreamRate("144p", 256, 144, 75, 32),
            new StreamRate("234p", 416, 234, 145, 32),
            new StreamRate("360p", 640, 360, 365, 48),
            new StreamRate("432pl", 768, 432, 730, 48),
            new StreamRate("432ph", 768, 432, 1100, 48),
            new StreamRate("540p", 960, 540, 2000, 64),
            new StreamRate("720pl", 1280, 720, 3000, 64),
            new StreamRate("720ph", 1280, 720, 4500, 72),
            new StreamRate("1080pl", 1920, 1080, 6000, 96),
            new StreamRate("1080ph", 1920, 1080, 7800, 128),
        };

        readonly static StreamRate[] StreamH264Square = 
        {
            new StreamRate("144p", 192, 144, 65, 32),
            new StreamRate("234p", 312, 234, 109, 32),
            new StreamRate("360p", 480, 360, 274, 48),
            new StreamRate("432pl", 576, 432, 548, 48),
            new StreamRate("432ph", 576, 432, 825, 48),
            new StreamRate("540p", 720, 540, 1500, 64),
            new StreamRate("720pl", 960, 720, 2251, 64),
            new StreamRate("720ph", 960, 720, 3376, 72),
            new StreamRate("1080pl", 1440, 1080, 4501, 96),
            new StreamRate("1080ph", 1440, 1080, 5851, 128),
        };

        readonly static StreamRate[] StreamH265Wide = 
        {
            new StreamRate("234p", 416, 234, 75, 32),
            new StreamRate("360p", 640, 360, 145, 32),
            new StreamRate("432p", 768, 432, 300, 48),
            new StreamRate("540pl", 960, 540, 600, 48),
            new StreamRate("540pm", 960, 540, 900, 48),
            new StreamRate("540ph", 960, 540, 1600, 64),
            new StreamRate("720pl", 1280, 720, 2400, 64),
            new StreamRate("720ph", 1280, 720, 3400, 72),
            new StreamRate("1080pl", 1920, 1080, 4500, 72),
            new StreamRate("1080ph", 1920, 1080, 5800, 96),
            new StreamRate("1440p", 2560, 1440, 8100, 96),
            new StreamRate("2160pl", 3840, 2160, 11600, 128),
            new StreamRate("2160ph", 3840, 2160, 16800, 128),
        };

        readonly static StreamRate[] StreamH265Square = 
        {
            new StreamRate("234p", 312, 234, 65, 32),
            new StreamRate("360p", 480, 360, 109, 32),
            new StreamRate("432p", 576, 432, 225, 48),
            new StreamRate("540pl", 720, 540, 450, 48),
            new StreamRate("540pm", 720, 540, 675, 48),
            new StreamRate("540ph", 720, 540, 1200, 64),
            new StreamRate("720pl", 960, 720, 1800, 64),
            new StreamRate("720ph", 960, 720, 2551, 72),
            new StreamRate("1080pl", 1440, 1080, 3376, 72),
            new StreamRate("1080ph", 1440, 1080, 4351, 96),
            new StreamRate("1440p", 1920, 1440, 6077, 96),
            new StreamRate("2160pl", 2880, 2160, 8702, 128),
            new StreamRate("2160ph", 2880, 2160, 12603, 128),
        };

        private static OutputRatio GetOutputRatio(long w, long h)
        {
            const double R16x9 = 16.0/9.0;
            const double R4x3 = 4.0/3.0;
            const double R9x16 = 9.0/16.0;
            double ratio = 1.0 * w / h;
            if (ratio >= R16x9 * 0.95 && ratio <= R16x9 * 1.05)
            {
                return OutputRatio.R16x9;
            }
            if (ratio >= R4x3 * 0.95 && ratio <= R4x3 * 1.05)
            {
                return OutputRatio.R4x3;
            }
            if (ratio >= R9x16 * 0.95 && ratio <= R9x16 * 1.05)
            {
                return OutputRatio.R9x16;
            }
            throw new Exception($"Output size ratio can not be determined based on dimensions {w}x{h}");
        }

        IEnumerable<StreamRate> GetStreamRates()
        {
            switch (Ratio, Codec)
            {
                case (OutputRatio.R16x9, OutputCodec.h264): return StreamH264Wide;
                case (OutputRatio.R4x3, OutputCodec.h264): return StreamH264Square;
                case (OutputRatio.R16x9, OutputCodec.h265): return StreamH265Wide;
                case (OutputRatio.R4x3, OutputCodec.h265): return StreamH265Square;
                case (OutputRatio.R9x16, OutputCodec.h264): return StreamH264Wide.Select(s => new StreamRate(s.Name, s.Height, s.Width, s.BitRate, s.AudioBitRate));
                case (OutputRatio.R9x16, OutputCodec.h265): return StreamH265Wide.Select(s => new StreamRate(s.Name, s.Height, s.Width, s.BitRate, s.AudioBitRate));
                default: throw new NotSupportedException($"Invalid combination of ratio {Ratio} and codec {Codec}");
            }
        }

        private static bool IsOutputRatioApplicable(long w, long h, StreamRate sr)
        {
            return (w * 120) >= (sr.Width * 100) || (h * 120) >= (sr.Height * 100);
        }

        static private RandomNumberGenerator rng = RandomNumberGenerator.Create();

        public readonly OutputRatio Ratio;
        public readonly OutputCodec Codec;
        public readonly long InputWidth;
        public readonly long InputHeight;
        public readonly string VideoId;
        public readonly string InputPath;
        public readonly string OutputPath;
        public readonly byte[] Key;
        private string? KeyPath;
        private string? KeyInfoPath;
        
        public Encode(string videoId, string inputPath, long? inputWidth, long? inputHeight, string outputPath, OutputCodec codec)
        {
            this.VideoId = videoId ?? throw new ArgumentNullException("videoId");
            this.InputPath = inputPath ?? throw new ArgumentNullException("inputPath");
            this.InputWidth = inputWidth ?? throw new ArgumentNullException("inputWidth");
            this.InputHeight = inputHeight ?? throw new ArgumentNullException("inputHeight");
            this.Ratio = GetOutputRatio(this.InputWidth, this.InputHeight);
            this.OutputPath = outputPath ?? throw new ArgumentNullException("outputPath");;
            this.Codec = codec;

            this.Key = new byte[16];
            rng.GetBytes(this.Key);
        }

        public async Task Run()
        {
            string? outputPathFinal = null;
            try
            {
                outputPathFinal = Path.Combine(OutputPath, VideoId);
                if (Directory.Exists(outputPathFinal))
                {
                    Directory.Delete(outputPathFinal, true);
                }
                Directory.CreateDirectory(outputPathFinal);

                KeyPath = Path.GetTempFileName();
                using (var f = new FileStream(KeyPath, FileMode.Create))
                {
                    f.Write(Key, 0, Key.Length);
                }

                KeyInfoPath = Path.GetTempFileName();
                using (var f = new StreamWriter(KeyInfoPath))
                {
                    f.WriteLine("http://127.0.0.1/playlist.key");
                    f.WriteLine(KeyPath);

                    var iv = new byte[16];
                    rng.GetBytes(iv);
                    foreach (var b in iv)
                    {
                        f.Write("{0:x2}", b);
                    }
                    f.WriteLine();
                }

                var playlist = new StringBuilder();
                playlist.AppendLine("#EXTM3U");
                playlist.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");

                var ffargs = new List<string>();
                ffargs.Add("-y");
                ffargs.Add("-hide_banner");
                ffargs.Add("-loglevel");
                ffargs.Add("info");
                ffargs.Add("-i");
                ffargs.Add(InputPath);

                var streamRates = GetStreamRates()
                    .Where(sr => IsOutputRatioApplicable(InputWidth, InputHeight, sr))
                    .OrderBy(sr => sr.Name == "360p" ? 0 : 1)
                    .ToArray();
                foreach (var sr in streamRates)
                {
                    ffargs.Add("-vcodec");
                    ffargs.Add(Codec == OutputCodec.h264 ? "libx264" : "libx265");
                    ffargs.Add("-vf");
                    ffargs.Add($"scale={sr.Width}:{sr.Height}");
                    ffargs.Add("-b:v");
                    ffargs.Add($"{sr.BitRate}k");

                    if (Codec == OutputCodec.h264)
                    {
                        ffargs.Add("-profile:v");
                        ffargs.Add("main");
                        ffargs.Add("-level");
                        ffargs.Add("3.1");
                    }

                    ffargs.Add("-acodec");
                    ffargs.Add("aac");
                    ffargs.Add("-b:a");
                    ffargs.Add($"{sr.BitRate}k");

                    ffargs.Add("-g");
                    ffargs.Add("60");
                    ffargs.Add("-hls_time");
                    ffargs.Add("6");
                    ffargs.Add("-hls_list_size");
                    ffargs.Add("0");
                    // ffargs.Add("-hls_segment_type");
                    // ffargs.Add("fmp4");
                    ffargs.Add("-hls_playlist_type");
                    ffargs.Add("vod");
                    ffargs.Add("-start_number");
                    ffargs.Add("10000");
                    ffargs.Add("-hls_key_info_file");
                    ffargs.Add(KeyInfoPath);

                    var streamFile = $"video_{sr.Name}.m3u8";
                    ffargs.Add(Path.Combine(outputPathFinal, streamFile));

                    playlist.AppendFormat("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH={0},RESOLUTION={1}x{2}\n", (sr.BitRate + sr.AudioBitRate) * 1024, sr.Width, sr.Height);
                    playlist.AppendLine(streamFile);

                }

                await Runner.Exec("ffmpeg", ffargs);

                playlist.AppendLine("#EXT-X-ENDLIST");
                File.WriteAllText(Path.Combine(outputPathFinal, "playlist.m3u8"), playlist.ToString());

                File.WriteAllText(Path.Combine(outputPathFinal, "index.html"), Resources.R["template_hls.html"]);
                File.WriteAllText(Path.Combine(outputPathFinal, "hls.light.min.js"), Resources.R["hls.light.min.js"]);
            }
            catch
            {
                if (outputPathFinal != null && Directory.Exists(outputPathFinal))
                {
                    Directory.Delete(outputPathFinal, true);
                }
            }
            finally
            {
                if (null != KeyPath)
                {
                    File.Delete(KeyPath);
                }

                if (null != KeyInfoPath)
                {
                    File.Delete(KeyInfoPath);
                }
            }
        }

    }

}