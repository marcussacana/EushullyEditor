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
            if (Config.BruteValidator && Config.SaveMethod == WriteMethod.Append)
                throw new Exception("You can't use the Script Validator in using Append Write Mode");
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
                Strings = AppendArray(Strings, new String(DataTools.SJByteArrayToString(StringData), Offsets[index]) { OpID = OffsetsTypes[index] });
            }
            Status = "Initialized";
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
                    while (!(RXOR(Script[Position + 1]) == 0x00 && RXOR(Script[Position]) == 0x00)) {
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
            byte[] data = WXOR(DataTools.StringToByteArray(DataTools.SJStringToHex(str)));
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
                                log += string.Format("An undetected string was found in {0} and probably it is called in {1}\n", DataTools.IntToHex(index), DataTools.IntToHex(pos));
                                int off = index;
                                while (RXOR(data[off]) != 0x00 && RXOR(data[off + 1]) != 0x00) {
                                    off++;
                                }
                                index = off;
                            }
                        }
                    }
                    if (back == log) {
                        log += "Undetected string found at 0x" + DataTools.IntToHex(index) + "\n";
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
        public WriteMethod SaveMethod = WriteMethod.Overwrite;

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


        internal bool EndText = true;
        internal bool EndLine = false;
        internal bool Furigana { get { return OpID == FuriganaID; } set { throw new Exception("Read-Only"); } }
        internal bool IsString { get { return OpID == StringID; } set { throw new Exception("Read-Only"); } }
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


    /// <summary>
    /// Opitional Resources to make a fake brekline using the text length...
    /// </summary>
    public class Resources {
        public System.Drawing.Font font;
        public System.Drawing.Size TextArea;
        /// <summary>
        /// if you set true, you can have problem to edit script after fake breaklines
        /// </summary>
        public bool RemoveBreakLine;

        /// <summary>
        /// If the game use monospaced characteres you can set this to make a breakline using the char count
        /// </summary>
        public bool Monospaced;
        public int MonospacedLengthLimit;
        public string FakeBreakLine(string text) {
            text = text.Replace("ー", "");
            if (!Monospaced) {
                if (font == null || TextArea == null)
                    throw new Exception("You need configure game text information before use this resource.");
                string[] lines = text.Split('\n');
                if (lines.Length == 1)
                    return text;
                for (int i = 0; i < lines.Length; i++) {
                    while (true) {
                        System.Drawing.Size size = TextSize(lines[i]);
                        if (size.Width < TextArea.Width)
                            lines[i] += " ";
                        else
                            break;
                    }

                }
                string Result = string.Empty;
                foreach (string line in lines)
                    if (RemoveBreakLine)
                        Result += line;
                    else
                        Result += line + "\n";
                if (Result.EndsWith("\n"))
                    Result = Result.Substring(0, Result.Length - 1);
                return Result;
            }
            else {
                string[] lines = text.Split('\n');
                if (lines.Length == 1)
                    return text;
                for (int i = 0; i < lines.Length; i++) {
                    while (lines[i].Length < MonospacedLengthLimit) {
                        lines[i] += " ";
                    }
                    if (!RemoveBreakLine)
                        lines[i] += "\n";
                }
                string Result = string.Empty;
                foreach (string line in lines)
                    Result += line;
                if (Result.EndsWith("\n"))
                    Result = Result.Substring(0, Result.Length - 1);
                while (Result.EndsWith(" "))
                    Result = Result.Substring(0, Result.Length - 1);
                return Result;
            }
        }

        public string GetFakedBreakLineText(string text) {
            text = text.Replace("", "ー");
            string[] lines = text.Split('\n');
            if (lines.Length > 1) {
                for (int i = 0; i < lines.Length; i++) {
                    string str = lines[i];
                    while (str.EndsWith(" "))
                        str = str.Substring(0, str.Length - 1);
                    lines[i] = str;
                }
                string Result = string.Empty;
                foreach (string line in lines)
                    if (RemoveBreakLine)
                        Result += line;
                    else
                        Result += line + "\n";
                if (Result.EndsWith("\n"))
                    Result = Result.Substring(0, Result.Length - 1);
                return Result;
            }
            else {
                if (text.StartsWith(" ") || text.EndsWith(" "))//prevent problems
                    return text;
                while (text.Contains("  "))
                    text.Replace("  ", " ");
                return text;
            }

        }
        private System.Drawing.Size TextSize(string text) {
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero)) {
                System.Drawing.SizeF size = g.MeasureString(text, font);
                return size.ToSize();
            }

        }

        /// <summary>
        /// The Bytes collection to represent a String ends (wait-for-input 0)
        /// </summary>
        public object _EndText = new object[] { 0x72, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        /// <summary>
        /// The Bytes collection to represent a String break line (end-text-line 0)
        /// </summary>
        public object _EndLine = new object[] { 0x6F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        //Return In-Game Text, but make the script Read-Only.
        public String[] MergeStrings(EushullyEditor WorkSpace) {
            String[] Input = WorkSpace.Strings;
            String[] Result = new String[Input.Length];
            Input.CopyTo(Result, 0);
            //Step 1 - Detect Op Codes
            for (int i = 0; i < Result.Length; i++) {
                if (Result[i].IsString) {
                    int NextEntry = FindEnd(WorkSpace, Input[i].OffsetPos, (object[])WorkSpace.Config.StringEntries[Input[i].OpID]);
                    int ig;
                    int[] ign;//ignore
                    Input[i].EndText = WorkSpace.MaskCheck(_EndText, out ig, out ign, NextEntry);
                    /*if (Input[i].EndText) //allow breakline after end string command
                        Input[i].EndLine = WorkSpace.MaskCheck(_EndLine, out ig, out ign, NextEntry + ((object[])_EndText).Length);
                    else*/
                    Input[i].EndLine = WorkSpace.MaskCheck(_EndLine, out ig, out ign, NextEntry);

                }
            }
            bool Ck = false;
            for (int main = 0, i = 1; i < Result.Length; i++) {
                String Main = Result[main];
                
                if (Main.IsString || Main.Furigana) {
                    String Next = Result[i];
                    if (Next.Furigana || (!Ck && Main.Furigana)) {
                        if (!Ck && Main.Furigana) {
                            Ck = true;
                            Main.STR = "[" + Main.STR + "/" + Next.STR + "]";
                            Next.STR = "";
                        }
                        else {
                            Main.STR += "[" + Next.STR + "/" + Result[i + 1].STR + "]";
                            Next.STR = "";
                            Result[i + 1].STR = "";
                            i++;
                        }
                        continue;
                    }
                    else {
                        Main.STR += Next.STR;
                        Next.STR = "";
                        if (Next.EndLine)
                            Main.STR += "\\n";
                        if (Next.EndText) {
                            main = i + 1;
                            Ck = false;
                            continue;
                        }
                    }
                } else { main++; Ck = false; }
            }
            return Result;
        }
        private int FindEnd(EushullyEditor WorkSpace, int At, object[] Mask) {
            byte[] script = WorkSpace.Script;
            int disc = 0;
            int StartOpCode = 0;
            for (int i = 0; i < Mask.Length; i++) {
                object entry = Mask[i];
                if (entry is Byte)
                    if ((Byte)entry == Byte.offset) {
                        disc += 3;
                        int ig;
                        int[] ign;
                        
                        if (WorkSpace.MaskCheck(Mask, out ig, out ign, At - i))
                            StartOpCode = At - i;
                    }
            }
            return StartOpCode + disc + Mask.Length;
        }

        public System.Windows.Forms.RichTextBox AutoLigth(System.Windows.Forms.RichTextBox tb) {
            int pointer = tb.SelectionStart;
            int len = tb.SelectionLength;
            tb.SelectAll();
            tb.SelectionColor = System.Drawing.SystemColors.ControlText;
            for (int i = 0; i < tb.Text.Length; i++) {
                if (tb.Text[i] == '[' || tb.Text[i] == ']') {
                    tb.Select(i, 1);
                    tb.SelectionColor = System.Drawing.Color.Blue;
                    tb.SelectionFont = new System.Drawing.Font(tb.Font.FontFamily, 10, System.Drawing.FontStyle.Bold);
                }
                if (tb.Text[i] == '/') {
                    tb.Select(i, 1);
                    tb.SelectionColor = System.Drawing.Color.Red;
                    tb.SelectionFont = new System.Drawing.Font(tb.Font.FontFamily, 10, System.Drawing.FontStyle.Bold);
                }
                if (tb.Text[i] == '\\' && tb.Text[i] == 'n') {
                    tb.Select(i, 2);
                    tb.SelectionColor = System.Drawing.Color.Red;
                    tb.SelectionFont = new System.Drawing.Font(tb.Font.FontFamily, 10, System.Drawing.FontStyle.Bold);
                }

            }
            tb.SelectionLength = len;
            tb.SelectionStart = pointer;
            return tb;
        }
    }
}