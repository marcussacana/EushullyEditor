using System;
using System.Text;

namespace EushullyEditor {
    public class BinEditor {
        public String[] StringsInfo = new String[0];
        public Encoding SJISBase = System.Text.Encoding.GetEncoding(932);
        public string ScriptVersion { get; internal set; }
        public string Status { get; internal set; }
        internal int[] OffsetsIndexs;
        //StringTablePoint[0] = Start String Table Position; StringTablePoint[1] = End String Table Position.
        internal int[] StringTablePoint = new int[] { 0, 0 };
        internal FormatOptions Config;
        internal byte[] Script;
        internal byte[] AppendSig = new byte[] { 0x00, 0x45, 0x64, 0x69, 0x74, 0x65, 0x64, 0x20, 0x57, 0x69, 0x74, 0x68, 0x20, 0x45, 0x75, 0x73, 0x68, 0x75, 0x6C, 0x6C, 0x79, 0x54, 0x72, 0x61, 0x6E, 0x73, 0x6C, 0x61, 0x74, 0x6F, 0x72, 0x00 };


        public BinEditor(byte[] Script, FormatOptions Format) {
            if (!Tools.CompareAt(Script, 0, new byte[] { 0x53, 0x59, 0x53 }))
                throw new Exception("Invalid Script");
            ScriptVersion = Version(Script);
            Config = Format;
            this.Script = Script;
            Status = "Initialized";
        }
        public BinEditor(byte[] Script) {
            if (!Tools.CompareAt(Script, 0, new byte[] { 0x53, 0x59, 0x53 }))
                throw new Exception("Invalid Script");
            Config = new FormatOptions();
            ScriptVersion = Version(Script);
            this.Script = Script;
            Status = "Initialized";
        }


        public string[] Import() {
            int[] Offsets = new int[0];
            int[] OffsetsTypes = new int[0];
            object[] Entries = Config.StringEntries;
            int StringStart = Script.Length;
            int StringEnd = 0;
            if (Config.ClearedContent.Length != 4)
                throw new Exception("The op code to cleared string need have 4 bytes length");

            for (int Pos = Config.HeaderSize; Pos < StringStart; Pos += 4) {
                Status = string.Format("Finding Strings... ({0}%)", (Pos * 100) / StringStart);
                for (int index = 0; index < Entries.Length; index++) {
                    int[] offset;
                    int disc;
                    object Entry = Entries[index];
                    bool valid = MaskCheck(Entry, out disc, out offset, Pos);
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
                        Pos += ((object[])Entry).Length - 4 + disc;
                        break;
                    }
                ignore:
                    ;
                }
            }

            Status = "Working...";
            if (StringStart == Script.Length)
                return new string[0];

            StringEnd++;
            bool Ended = false;
            while (!Ended || StringEnd % 4 != 0) {
                if (!Ended)
                    Ended = EqualsAt(Script, new byte[] { 0xFF, 0xFF}, StringEnd);
                StringEnd++;
            }
            StringTablePoint = new int[] { StringStart, StringEnd };
            OffsetsIndexs = Offsets;

            Status = "Validating Format Configuration...";
            if (Config.BruteValidator)
                BruteValidator(Script);

            for (int index = 0; index < Offsets.Length; index++) {
                Status = string.Format("Reading Strings... {0}/{1} ({2}%)", index, Offsets.Length, (Offsets.Length / 100) * index);
                int Off = (Tools.GetDWOffset(Script, Offsets[index]) * 4) + Config.HeaderSize;
                byte[] StringData = new byte[0];
                while (!EqualsAt(Script, new byte[] { 0xFF, 0xFF }, Off)) {
                    StringData = AppendArray(StringData, Script[Off++]);
                }
                StringData = XOR(StringData);

                StringsInfo = AppendArray(StringsInfo, new String() {
                    Content = SJISBase.GetString(StringData),
                    OffsetPos = Offsets[index], OpID = OffsetsTypes[index]
                });
            }
            Status = "Initialized";
            string[] Strs = new string[StringsInfo.Length];
            for (int i = 0; i < Strs.Length; i++)
                Strs[i] = StringsInfo[i].Content;
            return Strs;
        }

        private bool CheckStr(int stringOffset, byte[] script) {
            for (int i = stringOffset; i < stringOffset + 300 && i < script.Length; i++) {
                if (script[i] == 0x00)
                    return false;
                if (EqualsAt(script, new byte[] { 0xFF, 0xFF }, i))
                    return true;
            }
            return false;
        }

        public byte[] Export(string[] Strings = null) {
            if (Strings?.Length == StringsInfo.Length)
                for (int i = 0; i < Strings.Length; i++)
                    StringsInfo[i].Content = Strings[i];

            byte[] Backup = new byte[Script.Length];
            Script.CopyTo(Backup, 0);

            byte[] StringTable = new byte[0];
            int[] TableTree = new int[0];
            int NewStartPosition = StringTablePoint[1];
            Status = "Generating String Table...";
            foreach (String str in StringsInfo) {
                byte[] CompiledString = CompileString(str.Content);
                int position = StringTable.Length;
                StringTable = AppendArray(StringTable, CompiledString);
                TableTree = AppendArray(TableTree, position);
            }

            if (Config.ClearOldStrings || Config.SaveMethod == WriteMethod.AutoDetect) {
                Status = "Clearing old string table...";
                for (int i = 0; i < OffsetsIndexs.Length; i++) {
                    int Off = (Tools.GetDWOffset(Script, OffsetsIndexs[i]) * 4) + Config.HeaderSize;
                    int Length = 0;
                    while (!EqualsAt(Script, new byte[] { 0xFF, 0xFF }, Off + Length))
                        Length++;

                    if (Length % 4 == 0)
                        Length += 4;
                    Length += Length % 4;

                    if (Length == 0)
                        Length = 4;
                    for (int x = Off; x < Off + Length; x += 4)
                        Config.ClearedContent.CopyTo(Script, x);
                }
                if (Config.SaveMethod == WriteMethod.AutoDetect) {
                    Status = "Finding new string table position...";
                    while (EqualsAt(Script, Config.ClearedContent, NewStartPosition))
                        NewStartPosition -= 4;

                    int min = Script.Length;
                    foreach (String str in StringsInfo)
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
                    if (Tools.CompareAt(Script, Script.Length - AppendSig.Length, AppendSig))
                        NewStartPosition = StringTablePoint[0];

                    OutScript = new byte[0];
                    OutScript = AppendArray(OutScript, Script);
                    OutScript = AppendArray(OutScript, StringTable);
                    OutScript = AppendArray(OutScript, AppendSig);

                    for (int i = 0; i < TableTree.Length; i++) {
                        int NewStrPos = TableTree[i] + NewStartPosition;
                        byte[] offset = Tools.GenDWOffet((NewStrPos - Config.HeaderSize) / 4);
                        offset.CopyTo(OutScript, StringsInfo[i].OffsetPos);
                    }
                    break;

                case WriteMethod.Overwrite:
                    NewStartPosition = StringTablePoint[0];
                    goto case WriteMethod.AutoDetect;
                case WriteMethod.AutoDetect:
                    byte[] ScriptDump = new byte[NewStartPosition];
                    Array.Copy(Script, 0, ScriptDump, 0, ScriptDump.Length);

                    byte[] SufixDump = new byte[Script.Length - StringTablePoint[1]];
                    Array.Copy(Script, StringTablePoint[1], SufixDump, 0, SufixDump.Length);

                    OutScript = AppendArray(OutScript, ScriptDump);
                    OutScript = AppendArray(OutScript, StringTable);
                    OutScript = AppendArray(OutScript, SufixDump);
                    for (int i = 0; i < TableTree.Length; i++) {
                        int NewStrPos = TableTree[i] + NewStartPosition;
                        byte[] offset = Tools.GenDWOffet((NewStrPos - Config.HeaderSize) / 4);
                        offset.CopyTo(OutScript, StringsInfo[i].OffsetPos);
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
        
        private T[] AppendArray<T>(T[] Original, T[] AppendData) {
            T[] ret = new T[Original.Length + AppendData.Length];
            Original.CopyTo(ret, 0);
            AppendData.CopyTo(ret, Original.Length);
            return ret;
        }
        private T[] AppendArray<T>(T[] Original, T AppendData) => AppendArray(Original, new T[] { AppendData });

        internal bool MaskCheck(object Mask, out int disc, out int[] offset, int position) {
            object[] Entry = (object[])Mask;
            offset = new int[0];
            disc = 0;//discount
            for (int i = 0; i < Entry.Length; i++) {
                if (Entry[i] is Byte) {
                    if ((Byte)Entry[i] == Byte.Any)
                        continue;
                    else {
                        offset = AppendArray(offset, position + i + disc);
                        disc += 3;
                    }
                }
                else {
                    int pos = position + i + disc;
                    if (pos >= Script.Length)
                        return false;
                    if ((byte)(int)Entry[i] != Script[pos]) {
                        return false;
                    }
                }
            }
            return true;
        }
        private string Version(byte[] script) {
            return string.Format("{0}.{1}.{2}.{3}", getByte(script[3]), getByte(script[4]), getByte(script[5]), getByte(script[6]));
        }

        private char getByte(byte b) {
            return char.ConvertFromUtf32(b)[0];
        }
        private byte[] CompileString(string str) {
            byte[] Data = AppendArray(XOR(SJISBase.GetBytes(str)), new byte[] { 0xFF, 0xFF });

            while (Data.Length % 4 != 0)
                Data = AppendArray(Data, (byte)0xFF);
            return Data;
        }
        private void BruteValidator(byte[] src) {
            byte[] Data = new byte[src.Length];
            src.CopyTo(Data, 0);
            int Start = Data.Length;
            int End = 0;
            bool InvalidFound = false;
            string Log = "EushullyEditor - LOG\n";
            Start = StringTablePoint[0];
            End = StringTablePoint[1];
            for (int i = 0; i < OffsetsIndexs.Length; i++) {
                int Offset = (Tools.GetDWOffset(Data, OffsetsIndexs[i]) * 4) + Config.HeaderSize; // offsets size are: (Value * 4) + 0x3C (Header Size)
                while (!EqualsAt(Data, new byte[] { 0xFF, 0xFF }, Offset))
                    Data[Offset++] = 0xFF;
            }
            for (int i = Start; i < End; i++) {
                if (XOR(Data[i]) != 0x00) {
                    InvalidFound = true;
                    bool Found = false;
                    for (int x = Config.HeaderSize; x < Start; x += 4) {
                        if (EqualsAt(Data, Config.OffsetOPCode, x))
                            if ((Tools.GetDWOffset(Data, x) * 4) + Config.HeaderSize == i) {
                                Found = true;
                                Log += string.Format("An undetected string was found in 0x{0} and probably it is called in 0x{1}\n", i.ToString("X8"), x.ToString("X8"));
                                while (!EqualsAt(Data, new byte[] { 0xFF, 0xFF }, i))
                                    i++;
                            }
                    }
                    if (!Found) {
                        Log += "Undetected string found at 0x" + i.ToString("X8") + "\n";
                        while (XOR(Data[i]) != 0x00)
                            i++;
                    }
                }
            }

            if (InvalidFound)
                throw new Exception("FORMAT CONFIG ERROR:\n\n" + Log);
        }

        private bool EqualsAt(byte[] Arr, byte[] ToComp, int At) {
            if (ToComp.Length + At >= Arr.Length)
                return false;
            for (int i = 0; i < ToComp.Length; i++)
                if (Arr[i + At] != ToComp[i])
                    return false;
            return true;
        }
        private byte[] XOR(byte[] b) {
            byte[] rst = new byte[b.Length];
            for (int i = 0; i < rst.Length; i++)
                rst[i] = XOR(b[i]);
            return rst;
        }
        
        private byte XOR(byte b) => (byte)(b ^ 0xFF);
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
            { 0x6E, 0x00, 0x00, 0x00, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, 0x02, 0x00, 0x00, 0x00, Byte.Offset},
            new object[] //a comment string entry
            { 0xA7, 0x01, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, Byte.Offset },
            new object[] //furigana display string entry
            {0x96, 0x01, 0x00, 0x00, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, 0x02, 0x00, 0x00, 0x00, Byte.Offset, 0x02, 0x00, 0x00, 0x00, Byte.Offset},
            new object[] //Unknow function with string entry
            { 0x40, 0x01, 0x00, 0x00, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, 0x02, 0x00, 0x00, 0x00 , Byte.Offset, 0x02, 0x00, 0x00, 0x00, Byte.Offset , Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any },
            new object[] //set-string string entry
            { 0x92, 0x01, 0x00, 0x00, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, Byte.Any, 0x02, 0x00, 0x00, 0x00, Byte.Offset },
            new object[] //Unk string entry
            { 0xFE, 0x07, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, Byte.Offset } };

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
        Any,
        /// <summary>
        /// Represent a DWORD Offset
        /// </summary>
        Offset
    }

    public class String{
        public bool EndText = true;
        public bool EndLine = false;
        public string Content;
        public bool Furigana { get { return OpID == FuriganaID; } private set { } }
        public bool IsString { get { return OpID == StringID; } private set { } }
        private int FuriganaID = 2;
        private int StringID = 0;

        internal int OffsetPos;
        internal int OpID;
        
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