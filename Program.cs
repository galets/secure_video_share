using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using CommandLine;
using Models.FFProbe;
using System.Text;
using Newtonsoft.Json;

namespace svisha
{
    class Program
    {

        private static async Task<FFProbeResult> ProbeVideo(string inputPath)
        {
            var p = await Runner.ExecWithStdoutRedirect("ffprobe", new[]
            {
                "-show_format", "-show_streams", "-print_format", "json", "-loglevel", "quiet", inputPath
            });

            if (p.ExitCode != 0)
            {
                throw new Exception("could not probe video format");
            }

            var probeResult = p.StandardOutput.ReadToEnd();
            var result = FFProbeResult.FromJson(probeResult);
            return result;
        }

        private static MediaStream FindVideoStrean(FFProbeResult probeResult)
        {
            var ms = probeResult.Streams.Where(s => s.CodecType == "video").First();
            return ms;
        }

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        private static readonly string DefaultStoragePath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "svisha");

        [Verb("encode", HelpText = "Encode source video")]
        public class EncodeOptions
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('c', "codec", Required = false, Default = Encode.OutputCodec.h264, HelpText = "Codec to use: h264 or h265.")]
            public Encode.OutputCodec Codec { get; set; }

            [Option('o', "overwrite", Required = false, HelpText = "When source video already encoded, overwrite")]
            public bool Overwtite { get; set; }

            [Option('p', "output_path", Required = false, HelpText = "Specify which path should be used to store videos. Default: ~/Videos/svisha")]
            public string OutputPath { get; set; }

            [Value(0, Required = true, HelpText = "Path to the source video")]
            public string InputPath { get; set; }
        }

        [Verb("list", HelpText = "List all existing videos")]
        public class ListOptions
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('p', "output_path", Required = false, HelpText = "Specify which path should be used to store videos. Default: ~/Videos/svisha")]
            public string OutputPath { get; set; }
        }

        [Verb("uri", HelpText = "Generate a URI for playing back video")]
        public class UriOptions
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('p', "output_path", Required = false, HelpText = "Specify which path should be used to store videos. Default: ~/Videos/svisha")]
            public string OutputPath { get; set; }

            [Value(0, Required = true, HelpText = "Video ID or path to the source video")]
            public string VideoIdOrInputPath { get; set; }
        }

        static async Task DoEncode(EncodeOptions o)
        {
            o.OutputPath ??= DefaultStoragePath;

            if (!File.Exists(o.InputPath))
            {
                throw new FileNotFoundException("File not found", o.InputPath);
            }

            var dbPath = Path.Combine(o.OutputPath, "database.json");
            var db = Models.Database.Database.Load(dbPath);
            var existingVideo = db.Entries.Where(e => e.SourcePath == Path.GetFullPath(o.InputPath)).FirstOrDefault();
            string videoId = existingVideo?.Id ?? Guid.NewGuid().ToString("N");
            if (existingVideo != null && !o.Overwtite)
            {
                throw new Exception("This video is already encoded. Specify --overwrite parameter to remove existing video");
            }

            var probeResult = await ProbeVideo(o.InputPath);
            Console.WriteLine($"Video probed as {probeResult.Format.FormatLongName}");

            var videoStream = FindVideoStrean(probeResult);

            string title = probeResult.Format?.Tags?.Title ?? Path.GetFileNameWithoutExtension(o.InputPath);
            DateTimeOffset timestamp = probeResult.Format?.Tags?.CreationTime ?? DateTimeOffset.Now;
            var enc = new Encode(videoId, o.InputPath, videoStream.Width, videoStream.Height, title, JsonConvert.ToString(timestamp), null, o.OutputPath, o.Codec);
            await enc.Run();

            db = Models.Database.Database.Load(dbPath);
            db.Entries.RemoveAll(e => e.Id == videoId);
            db.Entries.Add(new Models.Database.Entry
            {
                Id = enc.VideoId,
                Codec = enc.Codec.ToString(),
                Date = timestamp,
                Key = ByteArrayToString(enc.Key),
                Thumbnail = "",
                Title = title,
                SourcePath = Path.GetFullPath(o.InputPath)
            });
            db.Save(dbPath);

            Console.WriteLine("Encoding complete. Load using:");
            Console.WriteLine("    cd {0}; http-server -p 8080 -a 127.0.0.1 -c 5", o.OutputPath);
            Console.WriteLine("    firefox http://127.0.0.1:8080/{0}/index.html#{1}", enc.VideoId, ByteArrayToString(enc.Key));
        }

        static int DoList(ListOptions o)
        {
            return 0;
        }

        static int DoUri(UriOptions o)
        {
            o.OutputPath ??= DefaultStoragePath;

            var dbPath = Path.Combine(o.OutputPath, "database.json");
            var db = Models.Database.Database.Load(dbPath);
            var existingVideo = db.Entries
                .Where(e => e.SourcePath == Path.GetFullPath(o.VideoIdOrInputPath) || e.Id == o.VideoIdOrInputPath)
                .FirstOrDefault();

            if (existingVideo == null)
            {
                throw new FileNotFoundException($"Not found: o.VideoIdOrInputPath");
            }

            Console.WriteLine("{0}/index.html#{1}", existingVideo.Id, existingVideo.Key);

            return 0;
        }


        static int Main(string[] args)
        {
            try
            {
                return Parser.Default.ParseArguments<EncodeOptions, ListOptions, UriOptions>(args)
                    .MapResult(
                        (EncodeOptions o) => { DoEncode(o).Wait(); return 0; },
                        (ListOptions o) => DoList(o),
                        (UriOptions o) => DoUri(o),
                        errs => 1
                    );
            }
            catch(Exception ex)
            {
                if (ex is System.AggregateException)
                {
                    ex = ex.InnerException;
                }
                Console.Error.WriteLine("Error: {0}", ex.Message);
                return 1;
            }
        }
    }
}
