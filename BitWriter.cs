using System.IO;

namespace MiX
{
    public class BitWriter
    {
        private FileStream fs;
        private bool reverseBytes;
        public BitWriter(string path)
        {
            fs = new FileStream(path, FileMode.Create);
            reverseBytes = BitConverter.IsLittleEndian;
        }

        public UInt32 GetPosition()
        {
            return (UInt32)fs.Position;
        }

        public void WriteInt8(int i)
        {
            WriteByte((byte)i);
        }

        public void WriteInt16(ushort i)
        {
            byte[] bytes = BitConverter.GetBytes(i).ToArray();
            if (reverseBytes) Array.Reverse(bytes);
            foreach (byte b in bytes) WriteByte(b);
        }

        public void WriteInt32(int i)
        {
            byte[] bytes = BitConverter.GetBytes(i).ToArray();
            if (reverseBytes) Array.Reverse(bytes);
            foreach (byte b in bytes) WriteByte(b);
        }

        public void WriteUint32(uint i)
        {
            byte[] bytes = BitConverter.GetBytes(i).ToArray();
            if (reverseBytes) Array.Reverse(bytes);
            foreach (byte b in bytes) WriteByte(b);
        }

        public void WriteFloat(float f)
        {
            byte[] bytes = BitConverter.GetBytes(f).ToArray();
            if (reverseBytes) Array.Reverse(bytes);
            foreach (byte b in bytes) WriteByte(b);
        }

        public void WriteString(string s)
        {
            byte[] bytes = new byte[s.Length];
            for (int i = 0; i < s.Length; i++) bytes[i] = Convert.ToByte(s[i]);
            foreach (byte b in bytes) WriteByte(b);
        }

        public void Close()
        {
            fs.Close();
        }

        private void WriteByte(byte b)
        {
            fs.WriteByte(b);
        }
    }
}