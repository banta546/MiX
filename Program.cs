using System.Security.Cryptography;

namespace MiX
{
    class MiX
    {
        static private Feedback fb = new Feedback();

        private static int Main(string[] args)
        {
            if (args.Count() < 1) fb.StdOut(Feedback.Error.INVALID_ARGS);
            int success = 0;
            foreach (string path in args)
            {
                if (!File.Exists(path)) { fb.StdOut(Feedback.Error.FILE_NOT_FOUND, path); continue;}
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (!fs.CanRead) {fb.StdOut(Feedback.Error.FILE_ACCESS); continue;}
                }

                char delimiter = System.OperatingSystem.IsLinux() ? '/' : '\\';
                string outPath = args[0].Contains(delimiter) ? args[0].Substring(0, args[0].LastIndexOf(delimiter)+1) : "";

                Xmk? xmk = ParseExt(path);
                if (xmk == null) continue;
                if (xmk.ExportToFile(outPath) == 1) {fb.StdOut(Feedback.Error.NO_DATA); continue;}

                fb.StdOut(outPath);
                success++;
            }
            fb.StdOut(success, args.Count());
            return 0;
        }

        private static Xmk? ParseExt(string path)
        {
            string ext = path.Substring(path.LastIndexOf('.')+1, path.Length-(path.LastIndexOf('.')+1));

            switch (ext.ToUpper())
            {
                case "MID":
                    fb.StdOut("MID", path);
                    MidiParser mp = new MidiParser(path);
                    return mp.ParseMidi();
                
                case "CHART":
                    fb.StdOut("CHART", path);
                    ChartParser cp = new ChartParser(path);
                    return cp.ParseChart();
                
                default:
                    fb.StdOut(Feedback.Error.FILE_FORMAT);
                    return null;
            }
        }
    }
}