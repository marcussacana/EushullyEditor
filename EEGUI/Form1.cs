using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using EushullyEditor;
using System.Windows.Forms;

namespace EEGUI
{
    public partial class Form1 : Form
    {
        public Form1() {
            //Kamidori Configuration
            Resources.RemoveBreakLine = false;
            Resources.Monospaced = true;
            Resources.MonospacedLengthLimit = 63;

            InitializeComponent();

        }
        BinEditor Editor;
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All Bin files | *.bin";
            DialogResult dr = fd.ShowDialog();

            if (dr == DialogResult.OK)
            {
                //EE = new EushullyEditor(System.IO.File.ReadAllBytes(fd.FileName), new FormatOptions()); //Initializate with default configuration
                Editor = new BinEditor(System.IO.File.ReadAllBytes(fd.FileName), new FormatOptions() {
                    ClearOldStrings = true,
                    BruteValidator = true
                });

                Text = "Eusshuly Script - v" + Editor.ScriptVersion;

                listBox1.Items.Clear();
                foreach (string str in Editor.Import())
                    listBox1.Items.Add(str);

            }
        }
        public int index;
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                index = listBox1.SelectedIndex;
                //GET TEXT WITH FAKE BREAK LINE
                textBox1.Text = Resources.GetFakedBreakLineText(listBox1.Items[index].ToString().Replace("\\n", "\n")).Replace("\n", "\\n");
                textBox1 = Resources.AutoLigth(textBox1);
            }
            catch { }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\n' || e.KeyChar == '\r')
            {
                //SAVE TEXT WITH FAKE BREAK LINE
                Editor.StringsInfo[index].Content = (Resources.FakeBreakLine(textBox1.Text.Replace("\\n", "\n")));
                listBox1.Items[index] = textBox1.Text;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "All Bin files | *.bin";
            DialogResult dr = fd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                System.IO.File.WriteAllBytes(fd.FileName, Editor.Export());
            }
        }

        private void openReadOnlyToolStripMenuItem_Click(object sender, EventArgs e) {
            
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All Bin files | *.bin";
            DialogResult dr = fd.ShowDialog();

            if (dr == DialogResult.OK) {
                MessageBox.Show("You are using Read-Only Mode", "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Editor = new BinEditor(System.IO.File.ReadAllBytes(fd.FileName), new FormatOptions()); //Initializate with default configuration
                Editor.Import();
                listBox1.Items.Clear();
                Editor.StringsInfo = Resources.MergeStrings(ref Editor, true);
                foreach (EushullyEditor.String str in Editor.StringsInfo)
                    listBox1.Items.Add(str.Content);

            }
        }
    }
}