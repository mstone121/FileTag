using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagReader.Tags
{
    public class TagID3v2 : Tag
    {
        // Flags
        bool flag_unsynch;
        bool flag_ext_header;
        bool flag_ex_indic;
        bool flag_footer;

        // Ex Header
        bool flag_update;
        bool flag_crc;
        bool flag_restr;

        // Restrictions
        int  restr_frames;
        int  restr_tag_size;
        bool restr_text_encode;
        int  restr_text_field;
        bool restr_image_encoding;
        int  restr_image_size;

        byte[] tag_buffer;

        Dictionary<String, Frame> frames;

        // Constructors
        public TagID3v2(byte[] tag_buffer)
        {
            byte[] header = File.Subset(tag_buffer, 0, 10);

            BitArray flags = File.getBitArrayFromByte(header[5]);

            // Set Flags
            flag_unsynch    = flags[0];
            flag_ext_header = flags[1];
            flag_ex_indic   = flags[2];
            flag_footer     = flags[3];

            // Frame position
            int pos = 10;

            #region Ex Header
            if (flag_ext_header)
            {
                // Size bytes
                byte[] ex_h_size_b = File.Subset(tag_buffer, 10, 4);
                Array.Reverse(ex_h_size_b);
                int ex_h_size = File.getSynchsafe(BitConverter.ToInt32(ex_h_size_b, 0));

                // Get rest of header
                int flag_size = tag_buffer[14];
                byte[] ex_flags = File.Subset(tag_buffer, 15, flag_size);

                BitArray ex_flags_a = new BitArray(ex_flags);
                flag_update = ex_flags_a[1];
                flag_crc = ex_flags_a[2];
                flag_restr = ex_flags_a[3];

                pos = 15 + flag_size;
                // Is updated tag
                if (flag_update)
                    pos++;

                // CRC 32 code
                if (flag_crc)
                {
                    pos += 6;
                }

                #region Tag restrictions
                if (flag_restr)
                {
                    int res_size = tag_buffer[pos];
                    pos++;
                    byte[] res = File.Subset(tag_buffer, pos, res_size);
                    pos += res_size;

                    BitArray res_flags = new BitArray(res);

                    if (!res_flags[0] && !res_flags[1])
                    {
                        restr_frames = 128;
                        restr_tag_size = 1024;
                    }
                    else if (!res_flags[0] && res_flags[1])
                    {
                        restr_frames = 64;
                        restr_tag_size = 128;
                    }
                    else if (res_flags[0] && !res_flags[1])
                    {
                        restr_frames = 32;
                        restr_tag_size = 40;
                    }
                    else if (res_flags[0] && res_flags[1])
                    {
                        restr_frames = 32;
                        restr_tag_size = 4;
                    }

                    // Text encoding
                    restr_text_encode = res_flags[2];

                    // Text fields size
                    if (!res_flags[3] && !res_flags[4])
                        restr_text_field = 0;
                    else if (!res_flags[3] && res_flags[4])
                        restr_text_field = 1024;
                    else if (res_flags[3] && !res_flags[4])
                        restr_text_field = 128;
                    else if (res_flags[3] && res_flags[4])
                        restr_text_field = 30;

                    // Image encoding
                    restr_image_encoding = res_flags[5];

                    // Image size
                    if (!res_flags[6] && !res_flags[7])
                        restr_image_size = 0;
                    else if (!res_flags[6] && res_flags[7])
                        restr_image_size = 256;
                    else if (res_flags[6] && !res_flags[7])
                        restr_image_size = 64;
                    else if (res_flags[6] && res_flags[7])
                        restr_image_size = -1;
                }
                #endregion
            }
            #endregion

            this.tag_buffer = tag_buffer;

            ReadFrames(pos);

            // Output console info
            this.printTags();
            System.Console.Write("\n\n");
        }

        // Frame Reader
        private void ReadFrames(int pos)
        {
            frames = new Dictionary<string, Frame>();

            while (pos < tag_buffer.Length)
            {
                // Save location of tag for writing
                int loc = pos;

                // Get Frame ID
                String frame_id = System.Text.Encoding.ASCII.GetString(File.Subset(tag_buffer, pos, 4));
                pos += 4;

                // Get Frame Size
                byte[] size_b = File.Subset(tag_buffer, pos, 4);
                Array.Reverse(size_b);
                int size = BitConverter.ToInt32(size_b, 0) + 10;
                pos += 6;

                // Get Frame data
                byte[] data = File.Subset(tag_buffer, loc, size);

                // Add Frame
                Frame frame = FrameFuncs.getFrame(frame_id, data);
                if (frame != null)
                    frames.Add(frame_id, frame);

                pos += size - 10;

                // Skip Padding
                while (pos < tag_buffer.Length && tag_buffer[pos] == 0x00)
                    pos++;
            }
        }

        // Info Retrevial
        public String getProperty(String property)
        {
            switch (property)
            {
                case "Track":
                    return getFrameValue("TRCK");
                case "Title":
                    return getFrameValue(new String[3] { "TIT1", "TIT2", "TIT3" });     
                case "Album":
                    return getFrameValue("TALB");
                case "Artist":
                    return getFrameValue(new String[4] { "TPE1", "TPE2", "TPE3", "TPE4" });
                case "Year":
                    return getFrameValue("TDRC");
                case "Length":
                    return getFrameValue("TLEN");
                case "BPM":
                    return getFrameValue("TBPM");
                case "Discnumber":
                    return getFrameValue("TSOA");
                case "Cover":
                    return getFrameValue("APIC");
                case "Composer":
                    return getFrameValue("TCOM");
                case "Comment":
                    return getFrameValue("COMM");
            }

            throw new Exception("Property not found");
        }

        public String getFrameValue(String key)
        {
            if (frames.ContainsKey(key))
                return frames[key].getValue();
            else
                return "";
        }                
        public String getFrameValue(String[] keys)
        {
            foreach (String key in keys)
                if (frames.ContainsKey(key))
                    return frames[key].getValue();

            return "";
        }

        public void updateTagBuffer()
        {
            byte[] header = File.Subset(tag_buffer, 0, 10);
            byte[] ext_header = new byte[0];
            int ex_h_size = 0;
            if (flag_ext_header)
            {
                byte[] ex_h_size_b = File.Subset(tag_buffer, 10, 4);
                Array.Reverse(ex_h_size_b);
                ex_h_size = File.getSynchsafe(BitConverter.ToInt32(ex_h_size_b, 0));

                ext_header = File.Subset(tag_buffer, 10, ex_h_size);
            }

            int frame_size = 0;
            foreach (String id in frames.Keys)
                frame_size += frames[id].data.Length + 10;

            byte[] new_buffer = new byte[Math.Max(ex_h_size + 10 + frame_size, tag_buffer.Length)];
            header.CopyTo(new_buffer, 0);
            ext_header.CopyTo(new_buffer, 10);

            int pos = 10 + ex_h_size;
            foreach (String id in frames.Keys)
            {
                frames[id].getFrameBuffer().CopyTo(new_buffer, pos);
                pos += (frames[id].data.Length + 10);
            }

            this.tag_buffer = new_buffer;

            frames.Clear();
            ReadFrames(10 + ex_h_size);
        }
        public byte[] getTagBuffer() { return tag_buffer; }

        // Info Setting
        public void writeProperty(String property, params object[] args)
        {
            switch (property)
            {
                case "Track":
                    writeFrameValue("TRCK", args);
                    return;

                case "Title":
                    writeFrameValue(new String[3] { "TIT1", "TIT2", "TIT3" }, args);
                    return;

                case "Album":
                    writeFrameValue("TALB", args);
                    return;

                case "Artist":
                    writeFrameValue(new String[4] { "TPE1", "TPE2", "TPE3", "TPE4" }, args);
                    return;

                case "Year":
                    writeFrameValue("TDRC", args);
                    return;

                case "Length":
                    writeFrameValue("TLEN", args);
                    return;

                case "BPM":
                    writeFrameValue("TBPM", args);
                    return;

                case "Discnumber":
                    writeFrameValue("TSOA", args);
                    return;

                case "Cover":
                    writeFrameValue("APIC", args);
                    return;

                case "Composer":
                    writeFrameValue("TCOM", args);
                    return;

                case "Comment":
                    writeFrameValue("COMM", args);
                    return;
            }

            throw new Exception("Property not found");
        }

        public void writeFrameValue(String key, params object[] args)
        {
            if (frames.ContainsKey(key))
                frames[key].writeValue(args);
        }
        public void writeFrameValue(String[] keys, params object[] args)
        {
            foreach (String key in keys)
                if (frames.ContainsKey(key))
                {
                    frames[key].writeValue(args);
                    break;
                }
        }        

        // Utils
        public void printTags()
        {
            foreach (String key in frames.Keys)
                System.Console.Write(frames[key].ToString() + "\n");
        }
    }
}
