using System;

namespace EushullyEditor
{
    /// <summary>
    /// EusullyBinary Editor Library BETA
    /// Created by Marcus-beta, VNX+ Fansub
    /// This Tool it's to any newwer dev make your own translation tool.
    /// This is a FREE and OpenSource Tool
    /// http://www.github.com/marcussacana
    /// </summary>
    public class EushullyBinary
    {
        public string ScriptVersion { get; private set; } = "0.0.0.0";
        public FormatOptions Config = new FormatOptions();
        public DialogueScript[] DialogSripts = new DialogueScript[0];
        private byte[] Script = new byte[0];
        private byte[] ScriptSignature;
        private byte[] SSig = new byte[] { 0x00, 0x45, 0x64, 0x69, 0x74, 0x65, 0x64, 0x20, 0x57, 0x69, 0x74, 0x68, 0x20, 0x45, 0x75, 0x73, 0x68, 0x75, 0x6C, 0x6C, 0x79, 0x54, 0x72, 0x61, 0x6E, 0x73, 0x6C, 0x61, 0x74, 0x6F, 0x72, 0x00 };
        public void LoadScript(byte[] script)
        {
            DialogSripts = new DialogueScript[0];
            byte[] Header = new byte[] { 0x53, 0x59, 0x53 };
            byte[] FileHeader = new byte[] { script[0], script[1], script[2] };
            if (script[0] != Header[0] || script[1] != Header[1] || script[2] != Header[2])
            {
                throw new Exception("Invalid Script");
            }
            ScriptVersion =
                DataTools.SJHexToString(new string[] { DataTools.ByteArrayToString(new byte[] { script[3] }) }) + "." +
                DataTools.SJHexToString(new string[] { DataTools.ByteArrayToString(new byte[] { script[4] }) }) + "." +
                DataTools.SJHexToString(new string[] { DataTools.ByteArrayToString(new byte[] { script[5] }) }) + "." +
                DataTools.SJHexToString(new string[] { DataTools.ByteArrayToString(new byte[] { script[6] }) });

            for (int pos = 0; pos < script.Length; pos += 4)
            {
                object[] temp = HaveStringAt(pos, script);
                bool cont = true;
                while (cont)
                {
                    cont = false;
                    bool Havestring = (bool)temp[0];
                    if (Havestring)
                    {
                        int[] Ats = (int[])temp[1];
                        foreach (int SAt in Ats)
                        {
                            int At = SAt;
                            DialogueScript ds = new DialogueScript();
                            string str = "";
                            OffsetType after = OffsetType.text;

                            while (true)
                            {
                                if (after == OffsetType.linebreak)
                                {
                                    str = "\n";
                                }
                                else
                                {
                                    if (after == OffsetType.text)
                                    {
                                        if (Config.DefaultOffsetType == OffsetFormat.Seek)
                                        {
                                            str = getStringAt(Tools.GetDWOffset(script, At) * 4, script);
                                        }
                                        else
                                        {
                                            if (Config.DefaultOffsetType == OffsetFormat.FromStart)
                                            {
                                                str = getStringAt((Tools.GetDWOffset(script, At) * 4) + At, script);
                                            }
                                            else
                                            {
                                                str = getStringAt((Tools.GetDWOffset(script, At) * 4) + Config.HeaderSize, script);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                ds.OffsetsFormats = Config.DefaultOffsetType;
                                ds.setDialogue(ds.Dialogue + str);
                                object[] OffsetsPos = new object[ds.OffsetsPos.Length + 1];
                                ds.OffsetsPos.CopyTo(OffsetsPos, 0);
                                OffsetsPos[ds.OffsetsPos.Length] = new object[] { At, after };
                                ds.OffsetsPos = OffsetsPos;
                                if (after != OffsetType.linebreak)
                                {
                                    At += 4;
                                }
                                else
                                {
                                    At += Config.EndLine.Length;
                                }
                                bool bf = after == OffsetType.text;
                                after = isA(At, script);
                                if (after == OffsetType.text)
                                {
                                    if (bf)
                                    {
                                        ds.setDialogue(ds.Dialogue + "|");
                                    }
                                    pos = At;
                                    temp = HaveStringAt(At, script);
                                    Havestring = (bool)temp[0];
                                    if (((int[])temp[1]).Length > 1)
                                    {
                                        Ats = (int[])temp[1];
                                        cont = true;
                                        break;
                                    }
                                    object[] Mask = (object[])temp[2];
                                    pos += CountOffSize(Mask);
                                    At = pos;
                                }
                            }
                            object[] tmp = new DialogueScript[DialogSripts.Length + 1];
                            DialogSripts.CopyTo(tmp, 0);
                            tmp[DialogSripts.Length] = ds;
                            DialogSripts = (DialogueScript[])tmp;
                        }//foreach
                    }//if
                }//while
            }//for
            this.Script = script;
            if (Config.BruteValidator)
            {
                BruteValidator(script);
            }
        }
        public byte[] ExportScript() {
            ScriptSignature = new byte[0];
            ScriptSignature = Tools.AppendFile(ScriptSignature, Config.TablePrefix);
            ScriptSignature = Tools.AppendFile(ScriptSignature, SSig);
            byte[] outscript = new byte[0];
            if (Config.SaveMethod == WriteMethod.Append) {
                object temp = GetTablePos(Script);
                int StartTable = ((int[])temp)[0];
                int EndTable = ((int[])temp)[1];
                while (!(RXOR(Script[EndTable]) == 0x00 && RXOR(Script[EndTable]) == 0x00))
                {
                    EndTable++;
                }
                temp = EndTable % 4;
                for (int cnt = 0; cnt < 4-(int)temp; cnt++) { EndTable++; }
                for (int ind = 0; ind < StartTable; ind += 4) {
                    outscript = Tools.AppendFile(outscript, new byte[] { Script[ind], Script[ind + 1], Script[ind + 2], Script[ind + 3] });
                }
                temp = Tools.CompareAt(Script, StartTable-ScriptSignature.Length, ScriptSignature);
                if (!(bool)temp)
                {
                    if (Config.ClearOldStrings)
                    {
                        byte[] nulltable = new byte[EndTable - StartTable];
                        nulltable = NullData(nulltable, 0, nulltable.Length);
                        outscript = Tools.AppendFile(outscript, nulltable);
                        outscript.CopyTo(Script, 0);
                        for (int pos = EndTable; pos < Script.Length; pos++)
                        {
                            outscript = Tools.AppendFile(Script, new byte[] { Script[pos] });
                        }
                    }
                    else {
                        for (int ind = StartTable; ind < Script.Length; ind += 4)
                        {
                            outscript = Tools.AppendFile(outscript, new byte[] { Script[ind], Script[ind + 1], Script[ind + 2], Script[ind + 3] });
                        }
                    }
                    outscript = Tools.AppendFile(outscript, ScriptSignature);
                }
                else {
                    outscript = Tools.cutFile(outscript, StartTable);
                }
                temp = GenStrTable(outscript.Length);
                byte[] Table = (byte[])((object[])temp)[0];
                object[] Offsets = (object[])((object[])temp)[1];
                for (int ind = 0; ind < Offsets.Length; ind++)
                {
                    int OffPos = (int)((object[])Offsets[ind])[0];
                    byte[] OffValue = (byte[])((object[])Offsets[ind])[1];
                    outscript = Tools.OverWriteAt(outscript, OffValue, OffPos);
                }
                outscript = Tools.AppendFile(outscript, Table);
            }
            return outscript;
        }

        private object[] GenStrTable(int TablePos)
        {
            byte[] table = new byte[0];
            for (int cnt = 0; cnt < 4 - (TablePos%4); cnt++)
            { table = Tools.AppendFile(table, new byte[] { WXOR(0x00) }); }
            object[] NewOffsets = new object[0];//  new object[] {OffsetPos, OffsetValue};
            for (int ind = 0; ind < DialogSripts.Length; ind++) {
                DialogueScript DS = DialogSripts[ind];
                if (Config.SaveMethod == WriteMethod.Append) {
                    string[] EditedSplited = DS.Dialogue.Split(new char[] { '\n', '|'});
                    int OriginalLines = DS.OriginalDialogue.Split(new char[] { '\n', '|' }).Length;
                    int EditedLines = EditedSplited.Length;
                    if (EditedLines > OriginalLines)
                    {
                        throw new Exception("You can't add new linebreak using Append Write Mode.\nCrash in Dialogue: " + ind);
                    }
                    string[] strs = new string[OriginalLines];
                    for (int i = 0; i < strs.Length; i++) {
                        if (i >= EditedSplited.Length) {
                            strs[i] = " ";
                        } else {
                            strs[i] = EditedSplited[i];
                        }
                    }
                    int disc = 0;
                    for (int i = 0; i < ((object[])DS.OffsetsPos).Length; i++)
                    {
                        int OffPos = (int)((object[])DS.OffsetsPos[i])[0];
                        OffsetType OffType = (OffsetType)((object[])DS.OffsetsPos[i])[1];
                        if (OffType != OffsetType.text) {
                            disc++;
                            continue;
                        }
                        byte[] str = genString(strs[i-disc]);
                        str = Tools.AppendFile(str, new byte[] { WXOR(0x00), WXOR(0x00) });
                        int size = str.Length % 4;
                        for (int cnt = 0; cnt < 4-size; cnt++) { str = Tools.AppendFile(str, new byte[] { WXOR(0x00) }); }
                        int Pos = table.Length + TablePos;
                        byte[] OffValue;
                        if (DS.OffsetsFormats == OffsetFormat.FromStart)
                        {
                            OffValue = Tools.GenDWOffet((Pos-OffPos)/4);
                        } else {
                            if (DS.OffsetsFormats == OffsetFormat.Seek)
                            {
                                OffValue = Tools.GenDWOffet(Pos/4);
                            }
                            else
                            {
                                OffValue = Tools.GenDWOffet((Pos-Config.HeaderSize)/4);
                            }
                        }
                        object[] rst = new object[] { OffPos, OffValue };
                        object[] temp = new object[NewOffsets.Length+1];
                        NewOffsets.CopyTo(temp, 0);
                        temp[NewOffsets.Length] = rst;
                        NewOffsets = temp;
                        table = Tools.AppendFile(table, str);
                    }
                } else { throw new Exception("Fail"); }
            }
            return new object[] { table, NewOffsets };
        }

        private byte[] genString(string str)
        {
            return WXOR(DataTools.StringToByteArray(DataTools.SJStringToHex(str)));
        }

        private byte[] CorrectData(byte[] data) {
            int tot = data.Length % 4;
            for (int cnt = 0; cnt < tot - 4; cnt++)
            {
                byte[] tmp = new byte[data.Length + 1];
                data.CopyTo(tmp, 0);
                tmp[data.Length] = WXOR(0x00);
                data = tmp;
            }
            return data;
        }
        private byte[] NullData(byte[] src)
        {
            byte[] data = src;
            for (int ind = 0; ind < DialogSripts.Length; ind++)
            {
                DialogueScript DS = DialogSripts[ind];
                for (int index = 0; index < DS.OffsetsPos.Length; index++)
                {
                    int off = 0;
                    if (((OffsetType)((object[])DS.OffsetsPos[index])[1]) != OffsetType.text) { continue; }
                    if (DS.OffsetsFormats == OffsetFormat.Seek)
                    {
                        off = Tools.GetDWOffset(data, ((int)((object[])DS.OffsetsPos[index])[0])) * 4;
                    }
                    else
                    {
                        if (DS.OffsetsFormats == OffsetFormat.FromStart)
                        {
                            off = Tools.GetDWOffset(data, ((int)((object[])DS.OffsetsPos[index])[0])) * 4 + ((int[])DS.OffsetsPos[0])[index];
                        }
                        else
                        {
                            off = Tools.GetDWOffset(data, ((int)((object[])DS.OffsetsPos[index])[0])) * 4 + Config.HeaderSize;
                        }
                    }
                    while (!(RXOR(data[off]) == 0x00 && RXOR(data[off + 1]) == 0x00))
                    {
                        data[off] = WXOR(0x00);
                        off++;
                    }
                }
            }
            return data;
        }
        private byte[] NullData(byte[] script, int start, int end)
        {
            int jmp = Config.ClearedContent.Length;
            for (int pos = start; pos < end; pos += jmp)
            {
                for (int ind = 0; ind < jmp; ind++)
                {
                    script[pos + ind] = Config.ClearedContent[ind];
                }
            }
            return script;
        }
        private int[] GetTablePos(byte[] script) {
            int start = script.Length;
            int end = 0;
            for (int ind = 0; ind < DialogSripts.Length; ind++)
            {
                DialogueScript DS = DialogSripts[ind];
                for (int index = 0; index < DS.OffsetsPos.Length; index++)
                {
                    int off = 0;
                    int At = ((int)((object[])DS.OffsetsPos[index])[0]);
                    OffsetType type = ((OffsetType)((object[])DS.OffsetsPos[index])[1]);
                    if (type != OffsetType.text) { continue; }
                    if (DS.OffsetsFormats == OffsetFormat.Seek)
                    {
                        off = Tools.GetDWOffset(script, At) * 4;
                    }
                    else
                    {
                        if (DS.OffsetsFormats == OffsetFormat.FromStart)
                        {
                            off = Tools.GetDWOffset(script, At) * 4 + At;
                        }
                        else
                        {
                            off = Tools.GetDWOffset(script, At) * 4 + Config.HeaderSize;
                        }
                    }
                    if (off > end) { end = off; }
                    if (off < start)
                    { start = off; }
                }
            }
            return new int[] {start, end };
        }
        private void BruteValidator(byte[] src)
        {
            byte[] data = new byte[src.Length];
            src.CopyTo(data, 0);
            int start = data.Length;
            int end = 0;
            bool InvalidFound = false;
            string log = "EushullyEditor - LOG\n";
            int[] rst = GetTablePos(data);
            start = rst[0];
            end = rst[1];
            data = NullData(data);
            #region FindForContent
            for (int index = start; index < end; index++) {
                if (RXOR(data[index]) != 0x00) {
                    InvalidFound = true;
                    string back = log;
                    if (Config.DefaultOffsetType == OffsetFormat.FromStart)
                    {
                        for (int pos = Config.HeaderSize; pos < start; pos += 4)
                        {
                            if (Config.OffsetOPCode[0] == data[pos - 4] && Config.OffsetOPCode[1] == data[pos - 3] && Config.OffsetOPCode[2] == data[pos - 2] && Config.OffsetOPCode[3] == data[pos - 1])
                            {
                                if (Tools.GetDWOffset(data, pos) * 4 + pos == index)
                                {
                                    log += "Undetected string found at 0x" + DataTools.IntToHex(index) + " and probably is called in 0x" + DataTools.IntToHex(pos) + "\n";
                                    int off = index;
                                    while (RXOR(data[off]) != 0x00 && RXOR(data[off+1]) != 0x00)
                                    {
                                        off++;
                                    }
                                    index = off;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Config.DefaultOffsetType == OffsetFormat.Seek)
                        {
                            for (int pos = Config.HeaderSize; pos < start; pos += 4)
                            {
                                if (Config.OffsetOPCode[0] == data[pos - 4] && Config.OffsetOPCode[1] == data[pos - 3] && Config.OffsetOPCode[2] == data[pos - 2] && Config.OffsetOPCode[3] == data[pos - 1])
                                {
                                    if (Tools.GetDWOffset(data, pos) * 4 == index)
                                    {
                                        log += "Undetected string found at 0x" + DataTools.IntToHex(index) + " and probably is called in 0x" + DataTools.IntToHex(pos) + "\n";
                                        int off = index;
                                        while (RXOR(data[off]) != 0x00 && RXOR(data[off + 1]) != 0x00)
                                        {
                                            off++;
                                        }
                                        index = off;
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int pos = Config.HeaderSize; pos < start; pos += 4)
                            {
                                if (Config.OffsetOPCode[0] == data[pos - 4] && Config.OffsetOPCode[1] == data[pos - 3] && Config.OffsetOPCode[2] == data[pos - 2] && Config.OffsetOPCode[3] == data[pos - 1])
                                {
                                    if ((Tools.GetDWOffset(data, pos) * 4) + Config.HeaderSize == index)
                                    {
                                        log += "Undetected string found at 0x" + DataTools.IntToHex(index) + " and probably is called in 0x" + DataTools.IntToHex(pos) + "\n";
                                        int off = index;
                                        while (RXOR(data[off]) != 0x00 && RXOR(data[off + 1]) != 0x00)
                                        {
                                            off++;
                                        }
                                        index = off;
                                    }
                                }
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
            #endregion
            if (InvalidFound)
            {
                throw new Exception("FORMAT CONFIG ERROR:\n\n" + log);
            }
            data = src;
        }

        private int CountOffSize(object[] content) {
            try
            {
                int result = content.Length;
                foreach (object obj in content)
                {
                    if (obj is Byte)
                    {
                        if ((Byte)obj == Byte.off)
                        {
                            result -= 1;
                        }
                    }
                }
                return result;
            }
            catch { return 0;  }
            }
        private OffsetType isA(int pos, byte[] script)
        {
            if ((bool)HaveStringAt(pos, script)[0])
            {
                return OffsetType.text;
            }
            object[] Entrie = Config.EndLine;
            bool valid = true;
            for (int index = 0; index < Entrie.Length; index++)
            {
                if (Entrie[index] == null)
                {
                    continue;
                }
                if ((byte)((int)Entrie[index]) != script[index + pos])
                {
                    valid = false;
                    break;
                }
            }
            if (valid) { return OffsetType.linebreak; }
            Entrie = Config.EndText;
            valid = true;
            for (int index = 0; index < Entrie.Length; index++)
            {
                if (Entrie[index] == null)
                {
                    continue;
                }
                if ((byte)((int)Entrie[index]) != script[index + pos])
                {
                    valid = false;
                    break;
                }
            }
            if (valid) { return OffsetType.textend; } else { return OffsetType.other; }
        }

        private string getStringAt(int offset, byte[] script)
        {
            object str = new byte[0];
            int pos = offset;
            while (!(RXOR(script[pos]) == 0x00 && RXOR(script[pos + 1]) == 0x00))
            {
                byte[] temp = new byte[((byte[])str).Length + 1];
                ((byte[])str).CopyTo(temp, 0);
                temp[((byte[])str).Length] = RXOR(script[pos]);
                str = temp;
                pos++;
            }
            return DataTools.SJHexToString(DataTools.ByteArrayToString((byte[])str).Split('-'));
        }
        
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
        private object[] HaveStringAt(int pos, byte[] script)
        {
            object[] Entries = Config.StringEntries;
            for (int ind = 0; ind < Config.StringEntries.Length; ind++)
            {
                object[] Entrie = (object[])Config.StringEntries[ind];
                if (Entrie.Length + pos > script.Length)
                {
                    continue;
                }

                int[] Offsets = new int[0];
                bool valid = true;
                bool inOffset = false;
                for (int index = 0; index < Entrie.Length; index++)
                {
                    if (Entrie[index] is Byte)
                    {
                        if ((Byte)Entrie[index] == Byte.any)
                        {
                            inOffset = false;
                        }
                        if (!inOffset && (Byte)Entrie[index] == Byte.off)
                        {
                            inOffset = true;
                            int[] temp = new int[Offsets.Length + 1];
                            Offsets.CopyTo(temp, 0);
                            temp[Offsets.Length] = pos + index;
                            Offsets = temp;                           
                        }
                        continue;
                    } else { inOffset = false; }
                    if ((byte)((int)Entrie[index]) != script[index + pos])
                    {
                        inOffset = false;
                        valid = false;
                        break;
                    }
                }
                if (valid)
                {
                    return new object[] { true, Offsets, Entrie };
                }
            }
            return new object[] { false, 0, 0 };
        }
    }


    public class FormatOptions
    {
        /// <summary>
        /// Some scripts have a offset in the header, you can list offsets position and method to seek the new position,
        /// use "new object[] {new object[] {POS, OffFormat}, new object[] {POS, OffFormat}, ...}"
        /// </summary>
        public object[] ProtectOffsets = new object[] {
            new object[] { 0x28, OffsetFormat.SeekWithHeader },
            new object[] { 0x30, OffsetFormat.SeekWithHeader },
            new object[] { 0x38, OffsetFormat.SeekWithHeader } };
        public byte[] OffsetOPCode = new byte[] { 0x02, 0x00, 0x00, 0x00 };
        /// <summary>
        /// After Load the script remove all strings the program can find for don't removed strings,
        /// if found, he crash logging the reason (Recommended)
        /// </summary>
        public bool BruteValidator = true;
        /// <summary>
        /// Change the save method using this variable
        /// </summary>
        public WriteMethod SaveMethod = WriteMethod.Append;
        /// <summary>
        /// The Script Header size to append in SeekWithHeader Offsets
        /// </summary>
        public int HeaderSize = 0x3C;
        /// <summary>
        /// Use a Collection of byte array to find at the file,
        /// use "new object[] {new object[] {}, new object[] {}, ...}"
        /// 0x00 at 0xFF it's valid values, (Byte.any = *) (Byte.off = offset position in op code)
        /// </summary>
        public object[] StringEntries = new object[] {
            new object[] //a text string entry
            { 0x6E, 0x00, 0x00, 0x00, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, 0x02, 0x00, 0x00, 0x00, Byte.off, Byte.off, Byte.off, Byte.off},
            new object[] //a comment string entry
            { 0xA7, 0x01, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, Byte.off, Byte.off, Byte.off, Byte.off },
            new object[] //furigana display string entry
            {0x96, 0x01, 0x00, 0x00, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, 0x02, 0x00, 0x00, 0x00, Byte.off, Byte.off, Byte.off, Byte.off, 0x02, 0x00, 0x00, 0x00, Byte.off, Byte.off, Byte.off, Byte.off},
            new object[] //Unknow function with string entry
            { 0x40, 0x01, 0x00, 0x00, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, 0x02, 0x00, 0x00, 0x00 , Byte.off, Byte.off, Byte.off, Byte.off, 0x02, 0x00, 0x00, 0x00, Byte.off, Byte.off, Byte.off, Byte.off, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any },
            new object[] //set-string string entry
            { 0x92, 0x01, 0x00, 0x00, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, Byte.any, 0x02, 0x00, 0x00, 0x00, Byte.off, Byte.off, Byte.off, Byte.off } };
        /// <summary>
        /// The Bytes collection to represent a String ends (wait-for-input 0)
        /// </summary>
        public object[] EndText = new object[] { 0x72, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        /// <summary>
        /// The Bytes collection to represent a String break line (end-text-line 0)
        /// </summary>
        public object[] EndLine = new object[] { 0x6F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        /// <summary>
        /// some scripts can count that strings offsets from start of script,
        /// and others, can seek the string starting at offset position
        /// </summary>
        public OffsetFormat DefaultOffsetType = OffsetFormat.SeekWithHeader;

        /// <summary>
        /// The XOR Strings key, Kamidori key it's 0xFF (255)
        /// </summary>
        public byte[] Key = new byte[] { 0xFF };


        /// <summary>
        /// On you use WriteMethod.Append, the old strings don't change, but for make more compress scripts you can null all strings data.
        /// </summary>
        public bool ClearOldStrings = true;
        public byte[] ClearedContent = new byte[] { 0x02, 0x00, 0x00, 0x00 };
        public byte[] TablePrefix = new byte[] { 0x05, 0x00, 0x00, 0x00 };

    }
    public enum Byte { any, off }
    public class DialogueScript
    {
        public string Dialogue = "";

        public OffsetFormat OffsetsFormats;
        public string OriginalDialogue { get; private set; } = "";
        internal void setDialogue(string str)
        {
            Dialogue = str;
            OriginalDialogue = str;
        }
        internal object[] OffsetsPos = new object[0];
    }
    public enum WriteMethod
    {
        /// <summary>
        /// This method works with a bad configuration of script format but generate a bigger script (recommended/default)
        /// </summary>
        Append,
        /// <summary>
        /// This Method rewrite the String table and update all offsets, need a full format configuration to works
        /// and this methhod generate a smaller script (not recommended) - NOT SUPPORTED IN THIS VERSION
        /// </summary>
        Overwrite
    }
    public enum OffsetType
    {
        text, textend, linebreak, other
    }
    public enum OffsetFormat
    {
        /// <summary>
        /// The offset use this calc ((Offset*4)+OffsetPosition) to get strings position
        /// </summary>
        FromStart,
        /// <summary>
        /// The offset use this calc (Offset*4) to get strings position
        /// </summary>
        Seek,
        /// <summary>
        /// The offset use this calc ((Offset*4)+Headersize) to get strings position (Default)
        /// </summary>
        SeekWithHeader
    }
}
