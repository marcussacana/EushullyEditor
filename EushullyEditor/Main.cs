using System;

namespace VNX.EushullyEditor
{
    //********************************************************************
    // Eusully Binary Editor Library 2.0                                 |
    // Created by Marcus-beta, VNX+ Fansub                               |
    // This Tool it's to any newwer dev make your own translation tool.  |
    // This is a FREE and OpenSource Tool                                |
    // http://www.github.com/marcussacana                                |
    //********************************************************************
    public class EushullyEditor
    {
        public String[] Strings = new String[0];
        public string ScriptVersion { get; internal set; }
        public string Status { get; internal set; }
        internal  int[] OffsetsIndexs;
        /// <summary>
        /// StringTablePoint[0] = Start String Table Position; StringTablePoint[1] = End String Table Position.
        /// </summary>
        internal int[] StringTablePoint = new int[] { 0, 0 };
        internal FormatOptions Config;
        internal byte[] Script;
        internal byte[] AppendSig = new byte[] { 0x00, 0x45, 0x64, 0x69, 0x74, 0x65, 0x64, 0x20, 0x57, 0x69, 0x74, 0x68, 0x20, 0x45, 0x75, 0x73, 0x68, 0x75, 0x6C, 0x6C, 0x79, 0x54, 0x72, 0x61, 0x6E, 0x73, 0x6C, 0x61, 0x74, 0x6F, 0x72, 0x00 };
        /// <summary>
        /// Initialize the script editor
        /// </summary>
        /// <param name="script">Compiled Eussully Script</param>
        /// <param name="Format">Engine Binary Format Informaion</param>
        public EushullyEditor(byte[] script, FormatOptions Format)
        {
            if (!Tools.CompareAt(script, 0, new byte[] { 0x53, 0x59, 0x53 }))
                throw new Exception("Invalid Script");
            ScriptVersion = getVersion(script);
            Config = Format;
            Script = script;
            Status = "Initialized";
        }

        public  void LoadScript()
        {
            int[] Offsets = new int[0];
            object[] Entries = Config.StringEntries;
            int StringStart = Script.Length;
            int StringEnd = 0;
            if (Config.BruteValidator && Config.SaveMethod == WriteMethod.Append)//A make this method to allow edit script without perfect configuration of my tool, and i make the BruteValidator to confirm the format configuration
                throw new Exception("You can't use the Script Validator in using Append Write Mode");
            if (Config.ClearedContent.Length != 4)
                throw new Exception("The op code to cleared string need have 4 bytes length");
            for (int position = Config.HeaderSize; position < StringStart; position += 4)
            {
                Status = string.Format("Finding Strings... ({0}%)", (position*100)/StringStart);
                for (int index = 0; index < Entries.Length; index++)
                {
                    int[] offset = new int[0];
                    int disc = 0;
                    bool valid = true;
                    object[] Entry = (object[])Entries[index];
                    for (int i = 0; i < Entry.Length; i++)
                    {
                        if (Entry[i] is Byte)
                        {
                            if ((Byte)Entry[i] == Byte.any)
                            {
                                continue;
                            }
                            else
                            {
                                int[] tmp = new int[offset.Length+1];
                                offset.CopyTo(tmp, 0);
                                tmp[offset.Length] = position + i + disc;
                                offset = tmp;
                                disc += 3;
                            }
                        }
                        else
                        {
                            if ((byte)(int)Entry[i] != Script[position + i + disc])
                            {
                                valid = false;
                                break;
                            }
                        }
                    }
                    if (valid)
                    {
                        //Update String Table Start
                        foreach(int off in offset)
                        {
                            int StringOffset = (Tools.GetDWOffset(Script, off)*4)+Config.HeaderSize;
                            if (StringOffset < StringStart)
                                StringStart = StringOffset;
                            if (StringOffset > StringEnd)
                                StringEnd = StringOffset;
                        }
                        //Copy Offsets 
                        int[] tmp = new int[Offsets.Length + offset.Length];
                        Offsets.CopyTo(tmp, 0);
                        offset.CopyTo(tmp, Offsets.Length);
                        Offsets = tmp;
                        position += Entry.Length-4+disc;
                        break;
                    }
                }
            }
            Status = "Working...";
            StringEnd++;
            bool Ended = false;
            while (!Ended || StringEnd % 4 != 0){
                if (!Ended)
                    Ended = RXOR(Script[StringEnd - 1]) == 0x00 && RXOR(Script[StringEnd]) == 0x00;
                StringEnd++;
            }
            StringTablePoint = new int[] { StringStart, StringEnd };
            OffsetsIndexs = Offsets;
            Status = "Validating Format Configuration...";
            if (Config.BruteValidator)
                BruteValidator(Script);            
            for (int index = 0; index < Offsets.Length; index++)
            {
                Status = string.Format("Reading Strings... {0}/{1} ({2}%)", index, Offsets.Length, (Offsets.Length/100)*index);
                int pointer = (Tools.GetDWOffset(Script, Offsets[index]) * 4) + Config.HeaderSize;
                byte[] StringData = new byte[0];
                byte ENDSTR = WXOR(0x00);
                while (!(Script[pointer] == ENDSTR && Script[pointer+1] == ENDSTR))
                {
                    byte[] temp = new byte[StringData.Length + 1];
                    StringData.CopyTo(temp, 0);
                    temp[StringData.Length] = Script[pointer];
                    StringData = temp;
                    pointer++;
                }
                StringData = RXOR(StringData);
                String[] tmp = new String[Strings.Length + 1];
                Strings.CopyTo(tmp, 0);
                tmp[Strings.Length] = new String(DataTools.SJByteArrayToString(StringData), Offsets[index]);
                Strings = tmp;
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
            foreach (String str in Strings)
            {
                byte[] CompiledString = CompileString(str.STR);
                byte[] tmp = new byte[StringTable.Length + CompiledString.Length];
                StringTable.CopyTo(tmp, 0);
                CompiledString.CopyTo(tmp, StringTable.Length);
                int position = StringTable.Length;
                StringTable = tmp;
                int[] temp = new int[TableTree.Length + 1];
                TableTree.CopyTo(temp, 0);
                temp[TableTree.Length] = position;
                TableTree = temp;
            }

            if (Config.ClearOldStrings || Config.SaveMethod == WriteMethod.AutoDetect)
            {
                Status = "Clearing old string table...";
                for (int i = 0; i < OffsetsIndexs.Length; i++)
                {
                    int Position = (Tools.GetDWOffset(Script, OffsetsIndexs[i]) * 4) + Config.HeaderSize;
                    int length = 0;
                    while (!(RXOR(Script[Position + 1]) == 0x00 && RXOR(Script[Position]) == 0x00))
                    {
                        length++;
                        Position++;
                    }
                    Position = (Tools.GetDWOffset(Script, OffsetsIndexs[i]) * 4) + Config.HeaderSize;
                    if (length % 4 == 0)
                        length += 4;
                    length += length % 4;
                    if (length == 0)
                        length = 4;
                    for (int pos = Position; pos < Position+length; pos += 4)
                    {
                        Config.ClearedContent.CopyTo(Script, pos);
                    }
                }
                if (Config.SaveMethod == WriteMethod.AutoDetect)
                {
                    Status = "Finding new string table position...";
                    while (Script[NewStartPosition - 4] == Config.ClearedContent[0] && Script[NewStartPosition - 3] == Config.ClearedContent[1] && Script[NewStartPosition - 2] == Config.ClearedContent[2] && Script[NewStartPosition - 1] == Config.ClearedContent[3])
                    {
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
            switch (Config.SaveMethod)
            {
                case WriteMethod.Append:
                    NewStartPosition = Script.Length;
                    if (Tools.CompareAt(Script, Script.Length - AppendSig.Length, AppendSig))
                    {
                       NewStartPosition = StringTablePoint[0];
                    }
                    OutScript = new byte[NewStartPosition + StringTable.Length + AppendSig.Length];
                    Script.CopyTo(OutScript, 0);
                    StringTable.CopyTo(OutScript, NewStartPosition);
                    AppendSig.CopyTo(OutScript, NewStartPosition + StringTable.Length);
                    for (int i = 0; i < TableTree.Length; i++)
                    {
                        int NewStrPos = TableTree[i] + NewStartPosition;
                        byte[] offset = Tools.GenDWOffet((NewStrPos - Config.HeaderSize) / 4);
                        offset.CopyTo(OutScript, Strings[i].OffsetPos);
                    }
                    break;
                case WriteMethod.AutoDetect:
                    byte[] ScriptDump = new byte[NewStartPosition];
                    for (int i = 0; i < ScriptDump.Length; i++)
                        ScriptDump[i] = Script[i];
                    byte[] SufixDump = new byte[Script.Length - StringTablePoint[1]];
                    for (int i = 0; i < SufixDump.Length; i++)
                        SufixDump[i] = Script[i + StringTablePoint[1]];
                    OutScript = new byte[ScriptDump.Length + StringTable.Length + SufixDump.Length];
                    ScriptDump.CopyTo(OutScript, 0);
                    StringTable.CopyTo(OutScript, ScriptDump.Length);
                    SufixDump.CopyTo(OutScript, ScriptDump.Length + StringTable.Length);
                    for (int i = 0; i < TableTree.Length; i++)
                    {
                        int NewStrPos = TableTree[i] + NewStartPosition;
                        byte[] offset = Tools.GenDWOffet((NewStrPos - Config.HeaderSize) / 4);
                        offset.CopyTo(OutScript, Strings[i].OffsetPos);
                    }
                    for (int i = 0; i < Config.OffsetsToSeek.Length; i++)
                    {
                        int off = (Tools.GetDWOffset(Script, Config.OffsetsToSeek[i]) * 4) + Config.HeaderSize;
                        int Diff = OutScript.Length - Script.Length;
                        if (off < ScriptDump.Length)
                            continue;//if the is a opcode pointer, don't need update...
                        off += Diff;
                        byte[] offset = Tools.GenDWOffet((off-Config.HeaderSize)/4);
                        offset.CopyTo(OutScript, Config.OffsetsToSeek[i]);
                    }
                    break;
                case WriteMethod.Overwrite:
                    NewStartPosition = StringTablePoint[0];
                    byte[] data = new byte[NewStartPosition];
                    for (int i = 0; i < data.Length; i++)
                        data[i] = Script[i];
                    byte[] Sufix = new byte[Script.Length - StringTablePoint[1]];
                    for (int i = 0; i < Sufix.Length; i++)
                        Sufix[i] = Script[i + StringTablePoint[1]];
                    OutScript = new byte[data.Length + StringTable.Length + Sufix.Length];
                    data.CopyTo(OutScript, 0);
                    StringTable.CopyTo(OutScript, data.Length);
                    Sufix.CopyTo(OutScript, data.Length + StringTable.Length);
                    for (int i = 0; i < TableTree.Length; i++)
                    {
                        int NewStrPos = TableTree[i] + NewStartPosition;
                        byte[] offset = Tools.GenDWOffet((NewStrPos - Config.HeaderSize) / 4);
                        offset.CopyTo(OutScript, Strings[i].OffsetPos);
                    }
                    for (int i = 0; i < Config.OffsetsToSeek.Length; i++)
                    {
                        int off = (Tools.GetDWOffset(Script, Config.OffsetsToSeek[i]) * 4) + Config.HeaderSize;
                        int Diff = OutScript.Length - Script.Length;
                        if (off < data.Length)
                            continue;//if the is a opcode pointer, don't need update...
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
        private string getVersion(byte[] script)
        {
            return string.Format("{0}.{1}.{2}.{3}", getByte(script[3]), getByte(script[4]), getByte(script[5]), getByte(script[6]));
        }

        private char getByte(byte b)
        {
            return char.ConvertFromUtf32(b)[0];
        }
        private byte[] CompileString(string str)
        {
            byte[] data = WXOR(DataTools.StringToByteArray(DataTools.SJStringToHex(str)));
            byte[] tmp = new byte[data.Length + 2];
            data.CopyTo(tmp, 0);
            tmp[data.Length] = WXOR(0x00);
            tmp[data.Length + 1] = WXOR(0x00);
            data = tmp;
            while (data.Length % 4 != 0)
            {
                tmp = new byte[data.Length + 1];
                data.CopyTo(tmp, 0);
                tmp[data.Length] = WXOR(0x00);
                data = tmp;
            }
            return data;
        }
        private void BruteValidator(byte[] src)
        {
            byte[] data = new byte[src.Length];
            src.CopyTo(data, 0);
            int start = data.Length;
            int end = 0;
            bool InvalidFound = false;
            string log = "EushullyEditor - LOG\n";
            start = StringTablePoint[0];
            end = StringTablePoint[1];
            for (int i = 0; i < OffsetsIndexs.Length; i++)
            {
                int Position = (Tools.GetDWOffset(data, OffsetsIndexs[i]) * 4) + Config.HeaderSize; // offsets size are: (Value * 4) + 0x3C (Header Size)
                while (!(RXOR(data[Position-1]) == 0x00 && RXOR(data[Position]) == 0x00))
                {
                    data[Position] = WXOR(0x00);
                    Position++;
                }
            }
            for (int index = start; index < end; index++)
            {
                if (RXOR(data[index]) != 0x00)
                {
                    InvalidFound = true;
                    string back = log;
                    for (int pos = Config.HeaderSize; pos < start; pos += 4)
                    {
                        if (Config.OffsetOPCode[0] == data[pos - 4] && Config.OffsetOPCode[1] == data[pos - 3] && Config.OffsetOPCode[2] == data[pos - 2] && Config.OffsetOPCode[3] == data[pos - 1])
                        {
                            if ((Tools.GetDWOffset(data, pos) * 4) + Config.HeaderSize == index)
                            {
                                log += string.Format("An undetected string was found in {0} and probably it is called in {1}\n", DataTools.IntToHex(index), DataTools.IntToHex(pos));
                                int off = index;
                                while (RXOR(data[off]) != 0x00 && RXOR(data[off + 1]) != 0x00)
                                {
                                    off++;
                                }
                                index = off;
                            }
                        }
                    }
                    if (back == log)
                    {
                        log += "Undetected string found at 0x" + DataTools.IntToHex(index) + "\n";
                        int off = index;
                        while (RXOR(data[off]) != 0x00)
                        {
                            off++;
                        }
                        index = off;
                    }
                }
            }

            if (InvalidFound)
            {
                throw new Exception("FORMAT CONFIG ERROR:\n\n" + log);
            }
            data = src; 
        }    

        #region XOROPERATIONS
        private byte[] WXOR(byte[] b)
        {
            for (int pos = 0; pos < b.Length; pos++)
            {
                byte result = b[pos];
                for (int ind = Config.Key.Length - 1; ind > -1; ind--)
                {
                    result = (byte)(result ^ Config.Key[ind]);
                }
                b[pos] = result;
            }
            return b;
        }
        private byte[] RXOR(byte[] b)
        {
            for (int pos = 0; pos < b.Length; pos++)
            {
                byte result = b[pos];
                for (int ind = 0; ind < Config.Key.Length; ind++)
                {
                    result = (byte)(result ^ Config.Key[ind]);
                }
                b[pos] = result;
            }
            return b;
        }

        private byte WXOR(byte b) {
            byte result = b;
            for (int ind = Config.Key.Length - 1; ind > -1; ind--)
            {
                result = (byte)(result ^ Config.Key[ind]);
            }
            return result;
        }
        private byte RXOR(byte b)
        {
            byte result = b;
            foreach (byte key in Config.Key)
            {
                result = (byte)(result ^ key);
            }
            return result;
        }
        #endregion
        #endregion

    }


    public class FormatOptions
    {
        /// <summary>
        /// Some scripts have a offset in the header, you can list offsets position and method to seek the new position,
        /// use "new int[] { OffsetIndex1, OffsetIndex2, OffsetIndex2};
        /// </summary>
        public int[] OffsetsToSeek = new int[] { 0x28, 0x30, 0x38 };

        /// <summary>
        /// All offsets have a prefix, this 
        /// </summary>
        public byte[] OffsetOPCode = new byte[] { 0x02, 0x00, 0x00, 0x00 };
        /// <summary>
        /// After Load the script remove all strings the program can find for don't removed strings,
        /// if found, he crash logging the reason (Recommended)
        /// </summary>
        public bool BruteValidator = false;
        /// <summary>
        /// Change the save method using this variable
        /// </summary>
        public WriteMethod SaveMethod = WriteMethod.Overwrite;
        /// <summary>
        /// The Script Header Size
        /// </summary>
        public int HeaderSize = 0x3C;
        /// <summary>
        /// Use a Collection of byte array to find at the file,
        /// use "new object[] {new object[] {}, new object[] {}, ...}"
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
        /// The XOR Strings key (to decrypt), Kamidori key it's 0xFF (255)
        /// </summary>
        public byte[] Key = new byte[] { 0xFF };


        /// <summary>
        /// On you use WriteMethod.Append, the old strings don't change, but for allow a more compressed scripts if you zip, you can null all strings data.
        /// </summary>
        public bool ClearOldStrings = true;
        public byte[] ClearedContent = new byte[] { 0x02, 0x00, 0x00, 0x00 };
        public byte[] TablePrefix = new byte[] { 0x05, 0x00, 0x00, 0x00 };

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

    public class String
    {
        internal String(string content, int OffsetPosition) { STR = content; OffsetPos = OffsetPosition; initalized = true; }
        internal String() { initalized = true; }

        private bool initalized;
        internal int OffsetPos;
        internal string STR;        
        public string getString() { if (!initalized) throw new Exception("You Can't create Strings"); return STR; }
        public void setString(string Content) { if (!initalized) throw new Exception("You Can't create Strings"); STR = Content; }
    }
    public enum WriteMethod
    {
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
    public class Resources
    {
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
        public string FakeBreakLine(string text)
        {
            if (!Monospaced)
            {
                if (font == null || TextArea == null)
                    throw new Exception("You need configure game text information before use this resource.");
                string[] lines = text.Split('\n');
                if (lines.Length == 1)
                    return text;
                for (int i = 0; i < lines.Length; i++)
                {
                    while (true)
                    {
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
            else
            {
                string[] lines = text.Split('\n');
                if (lines.Length == 1)
                    return text;
                for (int i = 0; i < lines.Length; i++)
                {
                    while (lines[i].Length < MonospacedLengthLimit)
                    {
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

        public string GetFakedBreakLineText(string text)
        {
            string[] lines = text.Split('\n');
            if (lines.Length > 1)
            {
                for (int i = 0; i < lines.Length; i++)
                {
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
            else
            {
                if (text.StartsWith(" ") || text.EndsWith(" "))//prevent problems
                    return text;
                while (text.Contains("  "))
                    text.Replace("  ", " ");
                return text;
            }

        }
        private System.Drawing.Size TextSize(string text)
        {
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                System.Drawing.SizeF size = g.MeasureString(text, font);
                return size.ToSize();
            }

        }
    }
}
