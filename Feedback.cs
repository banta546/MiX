using System;

namespace MiX
{
    public class Feedback
    {
        private bool debug;
        private Dictionary<Error,string> error = new Dictionary<Error, string>()
        {
            {Error.INVALID_ARGS, "Error 54: Invalid argument(s)"},
            {Error.FILE_NOT_FOUND, "Error 55: File not found"},
            {Error.FILE_FORMAT, "Error 56: Invalid format"},
            {Error.FILE_ACCESS, "Error 57: File Inaccessible"},
            {Error.NO_DATA, "Error 58: No data"},
            {Error.FILENAME, "Error 59: Filename conflict"}
        };
        public enum Error
        {
            INVALID_ARGS, FILE_NOT_FOUND, FILE_FORMAT, FILE_ACCESS, NO_DATA, FILENAME
        }
        public enum Update
        {
            FILE_WRITTEN, OP_COMPLETE
        }
        public Feedback(bool _debug=false)
        {
            debug = _debug;
        }

        public void DbgOut(string s)
        {
            if (!debug) return;
            Console.WriteLine(s);
        }

        public void StdOut(string ext, string path)
        {
            // Beginning Op
            Console.WriteLine($"Parsing {ext} file (\"{path}\")");
        }

        public void StdOut(string path)
        {
            // File Written
            Console.WriteLine($"Update: File(s) written to \"{path}\"");
        }

        public void StdOut(int x, int y)
        {
            // Ops Complete
            Console.WriteLine($"{x}/{y} operations completed successfully");
        }

        public void StdOut(Error e, string path="")
        {
            // Error
            Console.WriteLine(error[e] + (path.Length!=0 ? $" (\"{path}\")" : path));
        }
    }
}