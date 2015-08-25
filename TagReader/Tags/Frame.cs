using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagReader.Tags
{
    class Frame
    {
        protected String frame_id;
        public byte[] data;

        // Flags
        bool flag_tag_alter;
        bool flag_file_alter;
        bool flag_read_only;

        bool flag_group_id;
        bool flag_compression;
        bool flag_encryption;
        bool flag_unsynchronisation;
        bool flag_data_length;
        

        public Frame(byte[] full_data)
        {
            frame_id = System.Text.Encoding.ASCII.GetString(File.Subset(full_data, 0, 4));

            // Get Frame flags
            byte[] flags_b = { full_data[8], full_data[9] };
            BitArray flags = new BitArray(16);
            BitArray flags_a = new BitArray(flags_b);
            for (int i = 0; i < flags_a.Length; i++)
                flags[i] = flags_a[i];

            // Set Flags
            flag_tag_alter  = flags[1];
            flag_file_alter = flags[2];
            flag_read_only  = flags[3];

            flag_group_id          = flags[9];
            flag_compression       = flags[12];
            flag_encryption        = flags[13];
            flag_unsynchronisation = flags[14];
            flag_data_length       = flags[15];

            data = File.Subset(full_data, 10, full_data.Length - 10);   
        }
  
        public override string ToString() { return "Frame: " + this.frame_id; }

        // Info Retrevial/Setting

        public virtual string getValue() { return this.ToString(); }
        protected virtual void updateFrame() { return; }

        public virtual void writeValue(object[] args) { updateFrame(); }
        public byte[] getFrameBuffer()
        {
            byte[] buffer = new byte[10 + data.Length];
            getHeader().CopyTo(buffer, 0);
            data.CopyTo(buffer, 10);

            return buffer;
        }
        protected virtual byte[] getHeader()
        {
            // Assumes data array is new
            byte[] id_buf = Encoding.ASCII.GetBytes(frame_id);

            byte[] size_buf = BitConverter.GetBytes(File.toSynchsafe(data.Length));
            Array.Reverse(size_buf);

            BitArray flags = new BitArray(16);
            flags[1] = flag_tag_alter;
            flags[2] = flag_file_alter;
            flags[3] = flag_read_only;

            flags[9] = flag_group_id;
            flags[12] = flag_compression;
            flags[13] = flag_encryption;
            flags[14] = flag_unsynchronisation;
            flags[15] = flag_data_length;

            byte[] header = new byte[10];
            id_buf.CopyTo(header, 0);
            size_buf.CopyTo(header, 4);
            flags.CopyTo(header, 8);

            return header;
        }

    }

    class FrameUniqueFileIdentifier : Frame
    {
        String owner;
        byte[] indentifer;

        public FrameUniqueFileIdentifier(byte[] full_data)
            : base(full_data)
        {
            // Get owner string
            int pos = 0;
            while (data[pos] != 0x00)
                pos++;

            this.owner = Encoding.ASCII.GetString(File.Subset(data, 0, pos));
            this.indentifer = File.Subset(data, pos + 1, data.Length - pos - 1);
        }

        public override string ToString() { return base.ToString() + " :: Owner: " + this.owner; }
    }
    class FrameTextInformation : Frame
    {
        Encoding encode;
        String information;

        // For user defined text frames
        String[] text_tags = 
        { "TALB","TBPM","TCOM","TCON",
          "TCOP","TDEN","TDLY","TDOR",
          "TDRC","TDRL","TDTG","TENC",
          "TEXT","TFLT","TIPL","TIT1",
          "TIT2","TIT3","TKEY","TLAN",
          "TLEN","TMCL","TMED","TMOO",
          "TOAL","TOFN","TOLY","TOPE",
          "TORY","TOWN","TPE1","TPE2",
          "TPE3","TPE4","TPOS","TPRO",
          "TPUB","TRCK","TRDA","TRSN",
          "TRSO","TSOA","TSOP","TSOT",
          "TSRC","TSSE","TSST","TYER"
        };

        String description;
        String value;


        public FrameTextInformation(byte[] full_data)
            : base(full_data)
        {
            encode = FrameFuncs.getEncoding(data[0]);

            // Custom Text Tag
            if (!text_tags.Contains<String>(frame_id))
            {
                int pos = 1;
                while (pos < data.Length && data[pos] != 0x00)
                    pos++;

                description = encode.GetString(File.Subset(data, 1, pos));

                pos++;
                if (data[pos] == 0x00)
                    pos++;

                value = encode.GetString(File.Subset(data, pos, data.Length - pos - 1));

                information = value;

            } else // Normal Text Tag
                information = encode.GetString(File.Subset(data, 1, data.Length - 1));

        }

        public override string ToString() { return base.ToString() + " :: Info: " + this.information; }
        public override string getValue() { return this.information; }

        public override void writeValue(params object[] args)
        {
            if (args.Length == 1)
                this.information = (String)args[0];
            else if (args.Length == 2)
            {
                this.description = (string)args[0];
                this.value = (string)args[1];
            }

            base.writeValue(args);
        }
        protected override void updateFrame()
        {
            byte enc = data[0];
            if (description != null || value != null)
            {
                int desc_size = encode.GetByteCount(description);
                data = new byte[encode.GetByteCount(value) + desc_size + 2];
                encode.GetBytes(description).CopyTo(data, 1);
                encode.GetBytes(value).CopyTo(data, desc_size + 2);
                data[0] = enc;
                data[1 + desc_size] = 0x00;
            }
            else
            {
                data = new byte[encode.GetByteCount(information) + 1];
                data[0] = enc;
                encode.GetBytes(information).CopyTo(data, 1);
            }
        }
    }
    class FrameURLLink : Frame
    {
        String URL;

        public FrameURLLink(byte[] full_data)
            : base(full_data)
        {
            URL = Encoding.ASCII.GetString(data);
        }

        public override string ToString() { return base.ToString() + " :: URL: " + this.URL; }
    }
    class FrameMusicCDIdentifier : Frame
    {
        byte[] CD_TOC;

        public FrameMusicCDIdentifier(byte[] full_data)
            : base(full_data)
        {
            CD_TOC = data;
        }
    }
    class FrameEventTimingCodes : Frame
    {
        byte time_stamp_format;
        int time_stamp_size;
        Dictionary<String, byte[]> events;

        Dictionary<byte, String> event_types = new Dictionary<byte,String>
            {
                {0x00, "padding"},
                {0x01, "end of initial silence"},
                {0x02, "intro start"},
                {0x03, "main part start"},
                {0x04, "outro start"},
                {0x05, "outro end"},
                {0x06, "verse start"},
                {0x07, "refrain start"},
                {0x08, "interlude start"},
                {0x09, "theme start"},
                {0x0A, "variation start"},
                {0x0B, "key change"},
                {0x0C, "time change"},
                {0x0D, "momentary unwanted noise"},
                {0x0E, "sustained noise"},
                {0x0F, "sustained noise end"},
                {0x10, "intro end"},
                {0x11, "main part end"},
                {0x12, "verse end"},
                {0x13, "refrain end"},
                {0x14, "theme end"},
                {0x15, "profanity"},
                {0x16, "profanity end"},
                {0xFD, "audio end"},
                {0xFE, "audio file ends"},
            };

        public FrameEventTimingCodes(byte[] full_data)
            : base(full_data)
        {
            time_stamp_format = data[0];
            if (time_stamp_format == 0x01 || time_stamp_format == 0x02)
                time_stamp_size = 4;
            else
                time_stamp_size = 1;

            int pos = 1;

            while (pos < data.Length)
            {
                // Skip buffer
                while (data[pos] == 0xFF)
                    pos++;

                String type = event_types[data[pos]];
                pos++;

                byte[] timestamp = File.Subset(data, pos, time_stamp_size);

                events.Add(type, timestamp);

                pos += time_stamp_size;
            }
        }
    }
    class FrameMPEGLocationLookupTable : Frame
    {
        int frames;
        int bytes;
        int millsec;
        byte byte_deviation;
        byte millsec_deviation;
 
        public FrameMPEGLocationLookupTable(byte[] full_data)
            : base(full_data)
        {
            byte[] frames_b = File.Subset(data, 0, 2);
            Array.Reverse(frames_b);
            frames  = Convert.ToInt16(frames_b);

            byte[] bytes_b = File.Subset(data, 2, 3);
            Array.Reverse(bytes_b);
            bytes   = Convert.ToInt32(bytes_b);

            byte[] millsec_b = File.Subset(data, 5, 3);
            Array.Reverse(millsec_b);
            millsec = Convert.ToInt32(File.Subset(data, 5, 3));

            byte_deviation = data[8];
            millsec_deviation = data[9];
        }
    }
    class FrameSynchronizedTempoCodes : Frame
    {
        byte time_stamp_format;
        byte[] tempo_data;

        public FrameSynchronizedTempoCodes(byte[] full_data)
            : base(full_data)
        {
            time_stamp_format = data[0];

            tempo_data = File.Subset(data, 1, data.Length - 1);
        }
    }
    class FrameUnsynchronizedLyricsTranscription : Frame
    {
        Encoding encode;
        byte[] language;
        String descriptor;
        String lyrics;

        public FrameUnsynchronizedLyricsTranscription(byte[] full_data)
            : base(full_data)
        {
            encode = FrameFuncs.getEncoding(data[0]);
            language = File.Subset(data, 1, 3);

            int pos = 4;
            while (data[pos] != 0x00)
                pos++;

            descriptor = encode.GetString(File.Subset(data, 2, pos - 4));

            pos++;
            if (data[pos] == 0x00)
                pos++;

            lyrics = encode.GetString(File.Subset(data, pos, data.Length - pos));
        }

        public override string ToString() { return base.ToString() + " :: Descriptor: " + this.descriptor; }
    }
    class FrameSynchronizedLyrics : Frame
    {
        Encoding encode;
        byte[] language;
        byte time_stamp_format;
        String content_type;
        String descriptor;

        Dictionary<byte, String> types = new Dictionary<byte, String>
        {
            {0x00, "other"},
            {0x01, "lyrics"},
            {0x02, "text transcription"},
            {0x03, "movement/part name"},
            {0x04, "events"},
            {0x05, "chord"},
            {0x06, "trivia/'pop up' information"},
            {0x07, "URLs to webpages"},
            {0x08, "URLs to images"},
        };
            
        public FrameSynchronizedLyrics(byte[] full_data)
            : base(full_data)
        {
            encode = FrameFuncs.getEncoding(data[0]);

            language = File.Subset(data, 1, 3);

            time_stamp_format = data[4];

            try
            {
                content_type = types[data[5]];
            } 
            catch (KeyNotFoundException)
            {
                content_type = "Unknown key";
            }
            

            descriptor = encode.GetString(File.Subset(data, 6, data.Length - 6));
        }

        public override string ToString() { return base.ToString() + ":: Descriptor: " + this.descriptor; }
    }
    class FrameComments : Frame
    {
        Encoding encode;
        byte[] language;
        String description;
        String comment;

        public FrameComments(byte[] full_data)
            : base(full_data)
        {
            encode = FrameFuncs.getEncoding(data[0]);
            language = File.Subset(data, 1, 3);

            int pos = 4;

            while (data[pos] != 0x00)
                pos++;

            description = encode.GetString(File.Subset(data, 4, pos - 4));

            pos++;
            if (data[pos] == 0x00)
                pos++;

            comment = encode.GetString(File.Subset(data, pos, data.Length - pos));
        }

        public override string ToString() { return base.ToString() + " :: Descriptor: " + this.description; }
        public override string getValue() { return comment; }
    }
    class FrameRelativeVolumeAdjustment : Frame
    {
        String channel;
        int adjustment;
        byte peak;
        byte[] peak_volume;

        Dictionary<byte, String> channels = new Dictionary<byte, string>
        {
            {0x00, "Other"},
            {0x01, "Master volume"},
            {0x02, "Front right"},
            {0x03, "Front left"},
            {0x04, "Back right"},
            {0x05, "Back left"},
            {0x06, "Front centre"},
            {0x07, "Back centre"},
            {0x08, "Subwoofer"}
        };

        public FrameRelativeVolumeAdjustment(byte[] full_data)
            : base(full_data)
        {
            channel = channels[data[0]];

            adjustment = Convert.ToInt16(File.Subset<byte>(data, 1, 2));

            peak = data[3];

            peak_volume = File.Subset(data, 4, data.Length - 4);
        }
    }
    class FrameEqualisation : Frame
    {
        byte interpolation;
        String identification;
        int freq;
        int vol;

        public FrameEqualisation(byte[] full_data)
            : base(full_data)
        {
            interpolation = data[0];

            int pos = 1;
            while (data[pos] != 0x00)
                pos++;

            identification = Encoding.ASCII.GetString(File.Subset(data, 1, pos - 1));

            pos++;

            freq = Convert.ToInt16(File.Subset(data, pos, 2));

            pos += 2;

            vol = Convert.ToInt16(File.Subset(data, pos, 2));

        }

        public override string ToString() { return base.ToString() + ":: Identification : " + this.identification; }

    }

    static class FrameFuncs
    {
        public static Frame getFrame(String id, byte[] data)
        {

            if (id.StartsWith("T"))
                return new FrameTextInformation(data);

            else if (id.StartsWith("W") && id != "WXXX")
                return new FrameURLLink(data);

            switch (id)
            {
                case "UFID":
                    return new FrameUniqueFileIdentifier(data);

                case "MCDI":
                    return new FrameMusicCDIdentifier(data);

                case "ETCO":
                    return new FrameEventTimingCodes(data);

                case "MLLT":
                    return new FrameMPEGLocationLookupTable(data);

                case "SYTC":
                    return new FrameSynchronizedTempoCodes(data);

                case "USLT":
                    return new FrameUnsynchronizedLyricsTranscription(data);

                case "SYLT":
                    return new FrameSynchronizedLyrics(data);

                case "COMM":
                    return new FrameComments(data);

                case "RVA2":
                    return new FrameRelativeVolumeAdjustment(data);

                case "EQU2":
                    return new FrameEqualisation(data);
            }

            return null;
        }

        public static Encoding getEncoding(byte b)
        {
            if (b == 0x00)
                return System.Text.Encoding.GetEncoding("iso-8859-1");
            if (b == 0x01 || b == 0x02)
                return System.Text.Encoding.Unicode;
            if (b == 0x03)
                return System.Text.Encoding.UTF8;

            return null;
        }
    }
}

