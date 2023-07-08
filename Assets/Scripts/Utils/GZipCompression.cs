using System;
using System.IO;
using System.IO.Compression;
using System.Text;

public static class GZipCompression
{
    public static bool SaveDataString(string filename, string data, bool compress = true)
    {
        return SaveData(filename, Encoding.ASCII.GetBytes(data), compress);
    }

    // Returns true if saving went well.
    public static bool SaveData(string filename, byte[] data, bool compress = true)
    {
        if (data == null || data.Length == 0) return false;

        FileStream file = new FileStream(filename, FileMode.Create);
        if (file == null) return false;

        if (compress)
        {
            GZipStream gz = new GZipStream(file, CompressionMode.Compress);
            if (gz == null) return false;

            gz.Write(data, 0, data.Length);
            gz.Close();
        }
        else
        {
            file.Write(data, 0, data.Length);
        }
        file.Close();
        return true;
    }

    public static string LoadDataString(string filename)
    {
        return Encoding.ASCII.GetString(LoadData(filename));
    }

    public static byte[] LoadData(string filename)
    {
        byte[] data = null;
        FileStream file = new FileStream(filename, FileMode.Open);
        if (file == null) return null;

        GZipStream gz = null;
        BinaryReader br = null;

        byte[] header = new byte[3];
        file.Read(header, 0, 3);
        file.Position = 0;
        long realsize = file.Length;
        // Inspect the header for the magic numbers gzip files start with
        if (header[0] == 0x1f && header[1] == 0x8b && header[2] == 8)
        {
            // It's gzip, so the 5th to 8th byte are the size.
            // Yes, that's a 32-bit number, so file size is limited to around 2GB uncompressed.
            byte[] x = new byte[4];
            file.Seek(-4, SeekOrigin.End);
            file.Read(x, 0, 4);
            file.Seek(0, SeekOrigin.Begin);
            realsize = BitConverter.ToInt32(x, 0);
            gz = new GZipStream(file, CompressionMode.Decompress);
            br = new BinaryReader(gz);
        }
        else
        {
            // Not compressed, read it raw
            br = new BinaryReader(file);
        }
        if (br == null) return null;

        data = br.ReadBytes((int)realsize);
        br.Close();
        if (gz != null) gz.Close();
        file.Close();
        return data;
    }

    public static byte[] MemoryCompressing(byte[] data, CompressionMode mode)
    {
        if (mode == CompressionMode.Compress)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                using (GZipStream compressStream = new GZipStream(outStream, mode))
                using (MemoryStream mStream = new MemoryStream(data))
                    mStream.CopyTo(compressStream);

                return outStream.ToArray();
            }
        }

        if (mode == CompressionMode.Decompress)
        {
            using (MemoryStream dataStream = new MemoryStream(data))
            using (GZipStream decompressStream = new GZipStream(dataStream, mode))
            using (MemoryStream decompressStreamOut = new MemoryStream())
            {
                decompressStream.CopyTo(decompressStreamOut);
                return decompressStreamOut.ToArray();
            }
        }

        return null;
    }
}
