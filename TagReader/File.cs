using System;
using System.IO;            // For FileStream
using System.Threading;     // For Sleep
using System.Collections;   // For BitArray
using System.Collections.Generic;

using TagReader.Tags;

namespace TagReader
{
    public class File
    {
        // Headers
        byte[] ID3_v2_h = { 0x49, 0x44, 0x33 };
        byte[] ID3_v1_h = { 0x54, 0x41, 0x47 };

        // For file reading
        FileStream fileStream;

        // Tag
        public Tag tag;
        long tag_location;
        int tag_size;

        // File Info
        public Dictionary<String, String> file_info;

        String filename;
        FileInfo win_file_info;


        // Constructor
        public File(String filename)
        {
            this.filename = filename;
            win_file_info = new FileInfo(filename);

            // Set up file for reading
            fileStream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite);
            byte[] buffer = new byte[10];

            // Check for tag at beginning of file
            fileStream.Read(buffer, 0, 10);
            if (checkForTag(buffer))
            {
                tag_location = 0;
                return;
            }
            
            // Otherwise check at end of file
            fileStream.Seek(-10, SeekOrigin.End);
            fileStream.Read(buffer, 0, 10);
            if (checkForTag(buffer))
            {
                tag_location = fileStream.Length - 10;
                return;
            }

            throw new Exception("Could not find tag in file: " + filename);
        }

        private bool checkForTag(byte[] buffer)
        {
            // Make sure buffer is large enough
            if (buffer.Length < 10) {
                return false;
            }

            if (buffer[0] == ID3_v2_h[0] &&
                buffer[1] == ID3_v2_h[1] &&
                buffer[2] == ID3_v2_h[2]) // This is an ID3v2 tag
            {
                // Size bytes
                byte[] size_b = { buffer[6], buffer[7], buffer[8], buffer[9] };
                Array.Reverse(size_b);
                tag_size = getSynchsafe(BitConverter.ToInt32(size_b, 0)) + 10;

                // Get tag as byte buffer
                fileStream.Seek(-10, SeekOrigin.Current);
                byte[] tag_buffer = new byte[tag_size];
                fileStream.Read(tag_buffer, 0, tag_size);

                // Create tag
                tag = new TagID3v2(tag_buffer);
                file_info = getFileInfo();
                return true;
            }

            if (buffer[0] == ID3_v1_h[0] &&
                buffer[1] == ID3_v1_h[1] &&
                buffer[2] == ID3_v1_h[2]) // This is an ID3v1 tag
            {
                //tag = new TagID3v1();
                //loadTag();
                file_info = getFileInfo();
                return true;
            }

            return false;
        }

        // File Info Retrevial
        private Dictionary<String, String> getFileInfo()
        {
            Dictionary<String, String> info = new Dictionary<string, string>();
            String[] properties = new String[9] { "Track", "Title",
                                                    "Album", "Artist",
                                                    "Year", "BPM",
                                                    "Discnumber", 
                                                    "Composer", 
                                                    "Comment"};

            // Tag Info
            foreach (String property in properties)
                info.Add(property, tag.getProperty(property));

            // Track Length
            String length_str = tag.getProperty("Length");
            if (length_str != "")
            {
                double length = Convert.ToDouble(length_str);
                TimeSpan len_ts = TimeSpan.FromMilliseconds(length);
                info.Add("Length", string.Format("{0}:{1:D2}", (len_ts.Minutes + 60 * len_ts.Hours + 24 * 60 * len_ts.Days), len_ts.Seconds));
            }
            else 
                info.Add("Length", "");            

            // Filename and Path
            info.Add("Path", filename);

            String[] filename_a = filename.Split('\\');
            String filename_end = filename_a[filename_a.Length - 1];
            info.Add("Filename", filename_end);

            // File Info
            info.Add("Size", BytesToString(fileStream.Length));
            info.Add("Modified", win_file_info.LastWriteTime.ToString());

            return info;
        }

        public void updateFileInfo()
        {
            file_info = getFileInfo();
        }

        public void writeTag()
        {
            tag.updateTagBuffer();
            byte[] tag_buffer = tag.getTagBuffer();
            fileStream.Seek(tag_location, SeekOrigin.Begin);

            if (tag_buffer.Length > tag_size)
            {
                fileStream.Write(tag_buffer, 0, tag_size);
                fileStream.WriteAsync(tag_buffer, tag_size, tag_buffer.Length - tag_size);
            }
            else
            {
                fileStream.Write(tag_buffer, 0, tag_buffer.Length);
                fileStream.Write(new byte[tag_size - tag_buffer.Length], 0, tag_size - tag_buffer.Length);
            }

            tag_size = tag_buffer.Length;
            file_info = getFileInfo();
        }

        public void closeFile()
        {
            fileStream.Close();
        }

        // Utils
        private static String BytesToString(long byteCount)
        {
            // From Stackoverflow - users: deepee1, Erik Schierboom
            string[] suf = { "B", "KB", "MB", "GB", "TB" };
            if (byteCount == 0)
                return "0B";
            int place = Convert.ToInt32(Math.Floor(Math.Log(byteCount, 1024)));
            double num = Math.Round(byteCount / Math.Pow(1024, place), 2);
            return num.ToString() + " " + suf[place];
        }

        public static BitArray getBitArrayFromByte(byte b)
        {
            byte[] b_b = new byte[b];
            BitArray b_a = new BitArray(b_b);
            BitArray to_return = new BitArray(8);

            for (int i = 0; i < b_a.Length; i++)
                to_return.Set(i, b_a[i]);

            return to_return;
        }
        public static int getSynchsafe(int _in)
        {
            int _out = 0;
            int mask = 0x7F000000;
            
            while(mask != 0)
            {
                _out >>= 1;
                _out |= _in & mask;
                mask >>= 8;
            }

            return _out;
        }
        public static int toSynchsafe(int _in)
        {
            int _out = 0;
            int mask = 0x7F;

            while ((mask ^ 0x7FFFFFFF) != 0)
            {
                _out = _in & ~mask;
                _out <<= 1;
                _out |= _in & mask;
                mask = ((mask + 1) << 8) - 1;
                _in = _out;
            }

            return _out;
        }
        public static T[] Subset<T>(T[] array, int start, int length)
        {
            T[] subset = new T[length];
            Array.Copy(array, start, subset, 0, length);
            return subset;
        }
        
    }
}
