using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using VNX.EushullyEditor;
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
        EushullyEditor EE;
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All Bin files | *.bin";
            DialogResult dr = fd.ShowDialog();

            if (dr == DialogResult.OK)
            {
                EE = new EushullyEditor(System.IO.File.ReadAllBytes(fd.FileName), new FormatOptions()); //Initializate with default configuration
                EE.LoadScript();
                listBox1.Items.Clear();
                foreach (VNX.EushullyEditor.String str in EE.Strings)
                    listBox1.Items.Add(str.getString());

            }
        }
        public int index;
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                index = listBox1.SelectedIndex;
                //                     GET TEXT WITH FAKE BREAK LINE
                textBox1.Text = Resources.GetFakedBreakLineText(listBox1.Items[index].ToString().Replace("\\n", "\n")).Replace("\n", "\\n");
                textBox1 = Resources.AutoLigth(textBox1);
            }
            catch { }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\n' || e.KeyChar == '\r')
            {
                //                               SAVE TEXT WUTG FAKE BREAK LINE
                EE.Strings[index].setString(Resources.FakeBreakLine(textBox1.Text.Replace("\\n", "\n")));
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
                System.IO.File.WriteAllBytes(fd.FileName, EE.Export());
            }
        }

        private void openReadOnlyToolStripMenuItem_Click(object sender, EventArgs e) {
            
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All Bin files | *.bin";
            DialogResult dr = fd.ShowDialog();

            if (dr == DialogResult.OK) {
                MessageBox.Show("You are using Read-Only Mode", "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                EE = new EushullyEditor(System.IO.File.ReadAllBytes(fd.FileName), new FormatOptions()); //Initializate with default configuration
                EE.LoadScript();
                listBox1.Items.Clear();
                EE.Strings = Resources.MergeStrings(ref EE, true);
                foreach (VNX.EushullyEditor.String str in EE.Strings)
                    listBox1.Items.Add(str.getString());

            }
        }
    }
}