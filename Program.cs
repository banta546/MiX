using System;

namespace MiX
{
    class MiX
    {
        static private Feedback fb = new Feedback();

        private static int Main(string[] args)
        {
            if (args.Count() < 1) { fb.StdOut(Feedback.Error.INVALID_ARGS); return 1; }
            if (!File.Exists(args[0])) { fb.StdOut(Feedback.Error.FILE_NOT_FOUND, args[0]); return 1; }
            using (FileStream fs = new FileStream(args[0], FileMode.Open, FileAccess.Read))
            {
                if (!fs.CanRead) { fb.StdOut(Feedback.Error.FILE_ACCESS); return 1; }
            }

            char delimiter = System.OperatingSystem.IsLinux() ? '/' : '\\';
            string outPath = args[0].Contains(delimiter) ? args[0].Substring(0, args[0].LastIndexOf(delimiter)+1) : "";

            if (args.Count() == 2 && Directory.Exists(args[1])) outPath = args[1];
            if (outPath.LastIndexOf(delimiter) != outPath.Count()-1) outPath += delimiter;

            Xmk? xmk = ParseExt(args[0]);
            if (xmk == null) return 1;
            if (xmk.ExportToFile(outPath) == 1) fb.StdOut(Feedback.Error.NO_DATA);

            fb.StdOut(outPath);
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