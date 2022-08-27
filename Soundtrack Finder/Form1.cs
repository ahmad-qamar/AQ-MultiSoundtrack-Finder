using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Soundtrack_Finder
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            textBox1.Text = Environment.CurrentDirectory;

            dataGridView1.ColumnCount = 2;
            dataGridView1.Columns[0].Name = "Location";
            dataGridView1.Columns[1].Name = "Duration";

            DataGridViewButtonColumn buttonColumn = new DataGridViewButtonColumn();
            buttonColumn.HeaderText = "File Explorer";
            buttonColumn.Name = "btn";
            buttonColumn.Text = "📂";
            buttonColumn.UseColumnTextForButtonValue = true;
            dataGridView1.Columns.Add(buttonColumn);
        }
        List<(string, TimeSpan)> AudioFiles = new List<(string, TimeSpan)>();
        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;

                AudioFiles.Clear();

                foreach (string file in Directory.EnumerateFiles(textBox1.Text, "*.mp3", SearchOption.AllDirectories))
                {
                    /*var metadata = MetadataExtractor.Formats.Mpeg.Mp3MetadataReader
                        .ReadMetadata(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));*/
                    Mp3FileReader reader = new Mp3FileReader(file);
                    TimeSpan duration = reader.TotalTime;

                    AudioFiles.Add((file, duration));
                }
            }
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}
