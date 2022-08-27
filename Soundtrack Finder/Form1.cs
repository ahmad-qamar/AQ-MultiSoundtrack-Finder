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
            if (File.Exists("last")) textBox1.Text = File.ReadAllText("last");

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
                try { File.WriteAllText("last", folderBrowserDialog1.SelectedPath); } catch { }

                textBox1.Text = folderBrowserDialog1.SelectedPath;

                AudioFiles.Clear();

                foreach (string file in Directory.EnumerateFiles(textBox1.Text, "*.mp3", SearchOption.AllDirectories))
                {
                    try
                    {
                        /*var metadata = MetadataExtractor.Formats.Mpeg.Mp3MetadataReader
                            .ReadMetadata(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));*/
                        Mp3FileReader reader = new Mp3FileReader(file);
                        TimeSpan duration = reader.TotalTime;

                        AudioFiles.Add((file, duration));
                    }
                    catch { }
                }
            }
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            filterAndDisplaySongs();
        }

        void filterAndDisplaySongs()
        {
            var allowedOffset = TimeSpan.FromSeconds(dateTimePicker2.Value.Second + (dateTimePicker2.Value.Minute * 60)).Ticks;
            var requiredDuration = TimeSpan.FromSeconds(dateTimePicker1.Value.Second + (dateTimePicker1.Value.Minute * 60)).Ticks;

            var tracksToFind = (int)numericUpDown1.Value;
            var trackBuffer = new (string, TimeSpan)[tracksToFind];
            var trackIndexes = new int[tracksToFind];

            var tracksFound = new List<(string, string)>();

            for (int i = 0; i < tracksToFind; i++)
            {
            rec:
                if (trackBuffer.Length == tracksToFind)
                {
                    var duration = trackBuffer.Sum(t => t.Item2.Ticks);

                    if ((requiredDuration + allowedOffset >= duration) && (requiredDuration - allowedOffset <= duration))
                    {
                        tracksFound.AddRange(trackBuffer.Select(t => (t.Item1, t.Item2.ToString("mm\\:ss"))));
                        tracksFound.Add(("", ""));
                    }
                }

                for (int t = trackIndexes[i]; t < AudioFiles.Count; t++)
                {
                    if (trackBuffer.Length <= tracksToFind)
                    {
                        trackBuffer[trackBuffer.Length] = AudioFiles[t];
                        trackIndexes[i]++;

                        goto rec;
                    }
                }
            }
        }
    }
}
