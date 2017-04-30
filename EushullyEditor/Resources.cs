using System;

namespace VNX.EushullyEditor {

    /// <summary>
    /// Opitional Resources to make a fake brekline using the text length...
    /// </summary>
    public static class Resources {
        public static System.Drawing.Font font;
        public static System.Drawing.Size TextArea;
        /// <summary>
        /// if you set true, you can have problem to edit script after fake breaklines
        /// </summary>
        public static bool RemoveBreakLine;

        /// <summary>
        /// If the game use monospaced characteres you can set this to make a breakline using the char count
        /// </summary>
        public static bool Monospaced;
        public static int MonospacedLengthLimit;
        public static string FakeBreakLine(string text) {
            text = text.Replace("-----", "");
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

        public static string GetFakedBreakLineText(string text) {
            text = text.Replace("", "-----");
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
        private static System.Drawing.Size TextSize(string text) {
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero)) {
                System.Drawing.SizeF size = g.MeasureString(text, font);
                return size.ToSize();
            }

        }

        /// <summary>
        /// The Bytes collection to represent a String ends (wait-for-input 0)
        /// </summary>
        public static object _EndText = new object[] { 0x72, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        /// <summary>
        /// The Bytes collection to represent a String break line (end-text-line 0)
        /// </summary>
        public static object _EndLine = new object[] { 0x6F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        //Return In-Game Text, but make the script Read-Only.
        public static String[] MergeStrings(ref EushullyEditor WorkSpace, bool DetectOnly) {
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
            WorkSpace.Strings = Input;
            bool Ck = false;
            if (!DetectOnly)
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
                    }
                    else { main++; Ck = false; }
                }
            return Result;
        }
        private static int FindEnd(EushullyEditor WorkSpace, int At, object[] Mask) {
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

        public static System.Windows.Forms.RichTextBox AutoLigth(System.Windows.Forms.RichTextBox tb) {
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
