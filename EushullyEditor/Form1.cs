using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using EushullyEditor;
using System.Windows.Forms;

namespace EushullyEditor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        EushullyBinary EB = new EushullyBinary();
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All Bin files | *.bin";
            DialogResult dr = fd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                EB.Config = new FormatOptions();
                EB.LoadScript(System.IO.File.ReadAllBytes(fd.FileName));
                listBox1.Items.Clear();
                foreach (DialogueScript DS in EB.DialogSripts)
                {
                    listBox1.Items.Add(DS.Dialogue.Replace("\n", "\\n"));
                }
            }
        }
        public int index;
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                index = listBox1.SelectedIndex;
                textBox1.Text = EB.DialogSripts[index].Dialogue.Replace("\n", "\\n");
            }
            catch { }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\n' || e.KeyChar == '\r')
            {
                EB.DialogSripts[index].Dialogue = textBox1.Text.Replace("\\n", "\n");
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
                System.IO.File.WriteAllBytes(fd.FileName, EB.ExportScript());
            }
        }
    }
}