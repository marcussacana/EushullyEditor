using System;
namespace VNX.EushullyEditor {
    //********************************************************************
    // Eusully Binary Editor Library 2.0                                 |
    // Created by Marcus-beta, VNX+ Fansub                               |
    // This Tool it's to any newwer dev make your own translation tool.  |
    // This is a FREE and OpenSource Tool                                |
    // http://www.github.com/marcussacana                                |
    //********************************************************************
    public class EushullyEditor {
        public String[] Strings = new String[0];
        public System.Text.Encoding SJISBase = System.Text.Encoding.GetEncoding(932);
        public string ScriptVersion { get; internal set; }
        public string Status { get; internal set; }
        internal int[] OffsetsIndexs;
        //StringTablePoint[0] = Start String Table Position; StringTablePoint[1] = End String Table Position.
        internal int[] StringTablePoint = new int[] { 0, 0 };
        internal FormatOptions Config;
        internal byte[] Script;
        internal byte[] AppendSig = new byte[] { 0x00, 0x45, 0x64, 0x69, 0x74, 0x65, 0x64, 0x20, 0x57, 0x69, 0x74, 0x68, 0x20, 0x45, 0x75, 0x73, 0x68, 0x75, 0x6C, 0x6C, 0x79, 0x54, 0x72, 0x61, 0x6E, 0x73, 0x6C, 0x61, 0x74, 0x6F, 0x72, 0x00 };

        /// <summary>
        /// Initialize the script editor
        /// </summary>
        /// <param name="script">Compiled Eussully Script</param>
        /// <param name="Format">Engine Binary Format Informaion</param>
        public EushullyEditor(byte[] script, FormatOptions Format) {
            if (!Tools.CompareAt(script, 0, new byte[] { 0x53, 0x59, 0x53 }))
                throw new Exception("Invalid Script");
            ScriptVersion = getVersion(script);
            Config = Format;
            Script = script;
            Status = "Initialized";
        }

        public void LoadScript() {
            int[] Offsets = new int[0];
            int[] OffsetsTypes = new int[0];
            object[] Entries = Config.StringEntries;
            int StringStart = Script.Length;
            int StringEnd = 0;
            //A make this method to allow edit script without perfect configuration of my tool, and i make the BruteValidator to confirm the format configuration
            if (Config.ClearedContent.Length != 4)
                throw new Exception("The op code to cleared string need have 4 bytes length");
            for (int position = Config.HeaderSize; position < StringStart; position += 4) {
                Status = string.Format("Finding Strings... ({0}%)", (position * 100) / StringStart);
                for (int index = 0; index < Entries.Length; index++) {
                    int[] offset;
                    int disc;
                    object Entry = Entries[index];
                    bool valid = MaskCheck(Entry, out disc, out offset, position);
                    if (valid) {
                        //Update String Table Start
                        foreach (int off in offset) {
                            int StringOffset = (Tools.GetDWOffset(Script, off) * 4) + Config.HeaderSize;
                            if (StringOffset < Config.HeaderSize || !CheckStr(StringOffset, Script))
                                goto ignore;
                            if (StringOffset < StringStart)
                                StringStart = StringOffset;
                            if (StringOffset > StringEnd)
                                StringEnd = StringOffset;
                        }
                        //Copy Offsets 
                        Offsets = AppendArray(Offsets, offset);
                        for (int i = 0; i < offset.Length; i++)
                            OffsetsTypes = AppendArray(OffsetsTypes, index);
                        position += ((object[])Entry).Length - 4 + disc;
                        break;
                    }
                ignore:
                    ;
                }
            }
            Status = "Working...";
            StringEnd++;
            bool Ended = false;
            while (!Ended || StringEnd % 4 != 0) {
                if (!Ended)
                    Ended = RXOR(Script[StringEnd - 1]) == 0x00 && RXOR(Script[StringEnd]) == 0x00;
                StringEnd++;
            }
            StringTablePoint = new int[] { StringStart, StringEnd };
            OffsetsIndexs = Offsets;
            Status = "Validating Format Configuration...";
            if (Config.BruteValidator)
                BruteValidator(Script);
            for (int index = 0; index < Offsets.Length; index++) {
                Status = string.Format("Reading Strings... {0}/{1} ({2}%)", index, Offsets.Length, (Offsets.Length / 100) * index);
                int pointer = (Tools.GetDWOffset(Script, Offsets[index]) * 4) + Config.HeaderSize;
                byte[] StringData = new byte[0];
                byte ENDSTR = WXOR(0x00);
                while (!(Script[pointer] == ENDSTR && Script[pointer + 1] == ENDSTR)) {
                    StringData = AppendArray(StringData, Script[pointer]);
                    pointer++;
                }
                StringData = RXOR(StringData);
                Strings = AppendArray(Strings, new String(SJISBase.GetString(StringData), Offsets[index]) { OpID = OffsetsTypes[index] });
            }
            Status = "Initialized";
        }

        private bool CheckStr(int stringOffset, byte[] script) {
            for (int i = stringOffset; i < stringOffset + 300 && i < script.Length; i++) {
                if (script[i] == 0x00)
                    return false;
                if (script[i] == RXOR(0x00) && script[i] == RXOR(0x00))
                    return true;
            }
            return false;
        }

        public byte[] Export() {
            byte[] Backup = new byte[Script.Length];
            Script.CopyTo(Backup, 0);
            byte[] StringTable = new byte[0];
            int[] TableTree = new int[0];
            int NewStartPosition = StringTablePoint[1];
            Status = "Generating String Table...";
            foreach (String str in Strings) {
                byte[] CompiledString = CompileString(str.STR);
                int position = StringTable.Length;
                StringTable = AppendArray(StringTable, CompiledString);
                TableTree = AppendArray(TableTree, position);
            }

            if (Config.ClearOldStrings || Config.SaveMethod == WriteMethod.AutoDetect) {
                Status = "Clearing old string table...";
                for (int i = 0; i < OffsetsIndexs.Length; i++) {
                    int Position = (Tools.GetDWOffset(Script, OffsetsIndexs[i]) * 4) + Config.HeaderSize;
                    int length = 0;
                    while (!(RXOR(Script[Position]) == 0x00 && RXOR(Script[Position + 1]) == 0x00)) {
                        length++;
                        Position++;
                    }
                    Position = (Tools.GetDWOffset(Script, OffsetsIndexs[i]) * 4) + Config.HeaderSize;
                    if (length % 4 == 0)
                        length += 4;
                    length += length % 4;
                    if (length == 0)
                        length = 4;
                    for (int pos = Position; pos < Position + length; pos += 4) {
                        Config.ClearedContent.CopyTo(Script, pos);
                    }
                }
                if (Config.SaveMethod == WriteMethod.AutoDetect) {
                    Status = "Finding new string table position...";
                    while (Script[NewStartPosition - 4] == Config.ClearedContent[0] && Script[NewStartPosition - 3] == Config.ClearedContent[1] && Script[NewStartPosition - 2] == Config.ClearedContent[2] && Script[NewStartPosition - 1] == Config.ClearedContent[3]) {
                        NewStartPosition -= 4;
                    }
                    int min = Script.Length;
                    foreach (String str in Strings)
                        if (((Tools.GetDWOffset(Script, str.OffsetPos) * 4) + Config.HeaderSize) < min)
                            min = (Tools.GetDWOffset(Script, str.OffsetPos) * 4) + Config.HeaderSize;
                    if (NewStartPosition < min)
                        NewStartPosition = min;
                }
            }
            byte[] OutScript = new byte[0];
            Status = "Generating Script...";
            switch (Config.SaveMethod) {
                case WriteMethod.Append:
                    NewStartPosition = Script.Length;
                    if (Tools.CompareAt(Script, Script.Length - AppendSig.Length, AppendSig)) {
                        NewStartPosition = StringTablePoint[0];
                    }
                    OutScript = new byte[NewStartPosition + StringTable.Length + AppendSig.Length];
                    Script.CopyTo(OutScript, 0);
                    StringTable.CopyTo(OutScript, NewStartPosition);
                    AppendSig.CopyTo(OutScript, NewStartPosition + StringTable.Length);
                    for (int i = 0; i < TableTree.Length; i++) {
                        int NewStrPos = TableTree[i] + NewStartPosition;
                        byte[] offset = Tools.GenDWOffet((NewStrPos - Config.HeaderSize) / 4);
                        offset.CopyTo(OutScript, Strings[i].OffsetPos);
                    }
                    break;

                case WriteMethod.Overwrite:
                    NewStartPosition = StringTablePoint[0];
                    goto case WriteMethod.AutoDetect;
                case WriteMethod.AutoDetect:
                    byte[] ScriptDump = new byte[NewStartPosition];
                    for (int i = 0; i < ScriptDump.Length; i++)
                        ScriptDump[i] = Script[i];
                    byte[] SufixDump = new byte[Script.Length - StringTablePoint[1]];
                    for (int i = 0; i < SufixDump.Length; i++)
                        SufixDump[i] = Script[i + StringTablePoint[1]];
                    OutScript = AppendArray(OutScript, ScriptDump);
                    OutScript = AppendArray(OutScript, StringTable);
                    OutScript = AppendArray(OutScript, SufixDump);
                    for (int i = 0; i < TableTree.Length; i++) {
                        int NewStrPos = TableTree[i] + NewStartPosition;
                        byte[] offset = Tools.GenDWOffet((NewStrPos - Config.HeaderSize) / 4);
                        offset.CopyTo(OutScript, Strings[i].OffsetPos);
                    }
                    for (int i = 0; i < Config.OffsetsToSeek.Length; i++) {
                        int off = (Tools.GetDWOffset(Script, Config.OffsetsToSeek[i]) * 4) + Config.HeaderSize;
                        int Diff = OutScript.Length - Script.Length;
                        if (off < ScriptDump.Length)
                            continue;//If the offset points to a position beyond the strings, then there was no change on the offset, so it's unnecessary to update it
                        off += Diff;
                        byte[] offset = Tools.GenDWOffet((off - Config.HeaderSize) / 4);
                        offset.CopyTo(OutScript, Config.OffsetsToSeek[i]);
                    }
                    break;
            }
            Script = Backup;
            return OutScript;
        }
        #region Algorithms

        #region ArrayTools
        private int[] AppendArray(int[] Original, int[] AppendData) {
            int[] ret = new int[Original.Length + AppendData.Length];
            Original.CopyTo(ret, 0);
            AppendData.CopyTo(ret, Original.Length);
            return ret;
        }
        private int[] AppendArray(int[] Original, int AppendData) {
            return AppendArray(Original, new int[] { AppendData });
        }
        private byte[] AppendArray(byte[] Original, byte[] AppendData) {
            byte[] ret = new byte[Original.Length + AppendData.Length];
            Original.CopyTo(ret, 0);
            AppendData.CopyTo(ret, Original.Length);
            return ret;
        }
        private byte[] AppendArray(byte[] Original, byte AppendData) {
            return AppendArray(Original, new byte[] { AppendData });
        }
        private String[] AppendArray(String[] Original, String[] AppendData) {
            String[] ret = new String[Original.Length + AppendData.Length];
            Original.CopyTo(ret, 0);
            AppendData.CopyTo(ret, Original.Length);
            return ret;
        }
        private String[] AppendArray(String[] Original, String AppendData) {
            return AppendArray(Original, new String[] { AppendData });
        }
        #endregion
        internal bool MaskCheck(object Mask, out int disc, out int[] offset, int position) {
            object[] Entry = (object[])Mask;
            offset = new int[0];
            disc = 0;
            for (int i = 0; i < Entry.Length; i++) {
                if (Entry[i] is Byte) {
                    if ((Byte)Entry[i] == Byte.any) {
                        continue;
                    }
                    else {
                        int[] tmp = new int[offset.Length + 1];
                        offset.CopyTo(tmp, 0);
                        tmp[offset.Length] = position + i + disc;
                        offset = tmp;
                        disc += 3;
                    }
                }
                else {
                    if ((byte)(int)Entry[i] != Script[position + i + disc]) {
                        return false;
                    }
                }
            }
            return true;
        }
        private string getVersion(byte[] script) {
            return string.Format("{0}.{1}.{2}.{3}", getByte(script[3]), getByte(script[4]), getByte(script[5]), getByte(script[6]));
        }

        private char getByte(byte b) {
            return char.ConvertFromUtf32(b)[0];
        }
        private byte[] CompileString(string str) {
            byte[] data = WXOR(SJISBase.GetBytes(str));
            byte[] tmp = new byte[data.Length + 2];
            data.CopyTo(tmp, 0);
            tmp[data.Length] = WXOR(0x00);
            tmp[data.Length + 1] = WXOR(0x00);
            data = tmp;
            while (data.Length % 4 != 0) {
                tmp = new byte[data.Length + 1];
                data.CopyTo(tmp, 0);
                tmp[data.Length] = WXOR(0x00);
                data = tmp;
            }
            return data;
        }
        private void BruteValidator(byte[] src) {
            byte[] data = new byte[src.Length];
            src.CopyTo(data, 0);
            int start = data.Length;
            int end = 0;
            bool InvalidFound = false;
            string log = "EushullyEditor - LOG\n";
            start = StringTablePoint[0];
            end = StringTablePoint[1];
            for (int i = 0; i < OffsetsIndexs.Length; i++) {
                int Position = (Tools.GetDWOffset(data, OffsetsIndexs[i]) * 4) + Config.HeaderSize; // offsets size are: (Value * 4) + 0x3C (Header Size)
                while (!(RXOR(data[Position - 1]) == 0x00 && RXOR(data[Position]) == 0x00)) {
                    data[Position] = WXOR(0x00);
                    Position++;
                }
            }
            for (int index = start; index < end; index++) {
                if (RXOR(data[index]) != 0x00) {
                    InvalidFound = true;
                    string back = log;
                    for (int pos = Config.HeaderSize; pos < start; pos += 4) {
                        if (Config.OffsetOPCode[0] == data[pos - 4] && Config.OffsetOPCode[1] == data[pos - 3] && Config.OffsetOPCode[2] == data[pos - 2] && Config.OffsetOPCode[3] == data[pos - 1]) {
                            if ((Tools.GetDWOffset(data, pos) * 4) + Config.HeaderSize == index) {
                                log += string.Format("An undetected string was found in {0} and probably it is called in {1}\n", index.ToString("X"), pos.ToString("X"));
                                int off = index;
                                while (RXOR(data[off]) != 0x00 && RXOR(data[off + 1]) != 0x00) {
                                    off++;
                                }
                                index = off;
                            }
                        }
                    }
                    if (back == log) {
                        log += "Undetected string found at 0x" + index.ToString("X") + "\n";
                        int off = index;
                        while (RXOR(data[off]) != 0x00) {
                            off++;
                        }
                        index = off;
                    }
                }
            }

            if (InvalidFound) {
                throw new Exception("FORMAT CONFIG ERROR:\n\n" + log);
            }
            data = src;
        }

        #region XOROPERATIONS
        private byte[] WXOR(byte[] b) {
            for (int pos = 0; pos < b.Length; pos++) {
                byte result = b[pos];
                for (int ind = Config.Key.Length - 1; ind > -1; ind--) {
                    result = (byte)(result ^ Config.Key[ind]);
                }
                b[pos] = result;
            }
            return b;
        }
        private byte[] RXOR(byte[] b) {
            for (int pos = 0; pos < b.Length; pos++) {
                byte result = b[pos];
                for (int ind = 0; ind < Config.Key.Length; ind++) {
                    result = (byte)(result ^ Config.Key[ind]);
                }
                b[pos] = result;
            }
            return b;
        }

        private byte WXOR(byte b) {
            byte result = b;
            for (int ind = Config.Key.Length - 1; ind > -1; ind--) {
                result = (byte)(result ^ Config.Key[ind]);
            }
            return result;
        }
        private byte RXOR(byte b) {
            byte result = b;
            foreach (byte key in Config.Key) {
                result = (byte)(result ^ key);
            }
            return result;
        }
        #endregion
        #endregion

    }


    public class FormatOptions {
        /// <summary>
        /// Header offsets do update after resize the script.
        /// </summary>
        public int[] OffsetsToSeek = new int[] { 0x28, 0x30, 0x38 };

        /// <summary>
        /// All offsets have a dword prefix, set this prefix here.
        /// </summary>
        public byte[] OffsetOPCode = new byte[] { 0x02, 0x00, 0x00, 0x00 };

        /// <summary>
        /// After Load the script remove all strings the program can find for don't removed strings,
        /// if found, he crash logging the reason (Recommended)
        /// </summary>
        public bool BruteValidator = false;

        /// <summary>
        /// Select how you like regenerate the script here.
        /// </summary>
        public WriteMethod SaveMethod = WriteMethod.Append;

        /// <summary>
        /// The Script Header Size
        /// </summary>
        public int HeaderSize = 0x3C;

        /// <summary>
        /// Use a Collection of byte array to represent a string OpCode in the Script,
        /// 0x00 at 0xFF it's valid values, (Byte.any = *) (Byte.offset position in op code)
        /// </summary>
        public object[] StringEntries = new object[] {
            new object[] //a text string entry
            { 0x6E, 0x00, 0x00, 0x00, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, 0x02, 0x00, 0x00, 0x00, Byte.offset},
            new object[] //a comment string entry
            { 0xA7, 0x01, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, Byte.offset },
            new object[] //furigana display string entry
            {0x96, 0x01, 0x00, 0x00, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, 0x02, 0x00, 0x00, 0x00, Byte.offset, 0x02, 0x00, 0x00, 0x00, Byte.offset},
            new object[] //Unknow function with string entry
            { 0x40, 0x01, 0x00, 0x00, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, 0x02, 0x00, 0x00, 0x00 , Byte.offset, 0x02, 0x00, 0x00, 0x00, Byte.offset , Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any },
            new object[] //set-string string entry
            { 0x92, 0x01, 0x00, 0x00, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, 0x02, 0x00, 0x00, 0x00, Byte.offset } };

        /// <summary>
        /// The XOR Strings key (to decrypt), Default key it's 0xFF (255)
        /// </summary>
        public byte[] Key = new byte[] { 0xFF };


        /// <summary>
        /// Replace old strings to a Custom Command (only make affect in decompilers)
        /// </summary>
        public bool ClearOldStrings = true;

        /// <summary>
        /// Set here the OpCode to replace on the old string location.
        /// </summary>
        public byte[] ClearedContent = new byte[] { 0x02, 0x00, 0x00, 0x00 }; //02 00 00 00 = Exit/Return Command       

    }
    public enum Byte {
        /// <summary>
        /// Represent any byte mask
        /// </summary>
        any,
        /// <summary>
        /// Represent a DWORD Offset
        /// </summary>
        offset
    }

    public class String {
        internal String(string content, int OffsetPosition) { STR = content; OffsetPos = OffsetPosition; initalized = true; }
        internal String() { initalized = true; }
        private bool initalized;


        public bool EndText = true;
        public bool EndLine = false;
        public bool Furigana { get { return OpID == FuriganaID; } private set { } }
        public bool IsString { get { return OpID == StringID; } private set { } }
        private int FuriganaID = 2;
        private int StringID = 0;

        internal int OffsetPos;
        internal string STR;
        internal int OpID;
        public string getString() { if (!initalized) throw new Exception("You Can't create Strings"); return STR; }
        public void setString(string Content) { if (!initalized) throw new Exception("You Can't create Strings"); STR = Content; }
    }
    public enum WriteMethod {
        /// <summary>
        /// This method works with a bad configuration of script format but generate a bigger script (recommended/default)
        /// </summary>
        Append,
        /// <summary>
        /// This Method rewrite the String table and update all offsets, need a full format configuration to works
        /// and this methhod generate a smaller script (not recommended)
        /// </summary>
        Overwrite,
        /// <summary>
        /// Rewrite script only after the last undetected string, recommended if you want a smaller script without full configuration
        /// (Need configure the Header offsets to works)
        /// </summary>
        AutoDetect
    }
}