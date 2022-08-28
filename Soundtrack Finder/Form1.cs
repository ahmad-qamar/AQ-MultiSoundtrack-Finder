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
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Soundtrack_Finder
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            textBox1.Text = Environment.CurrentDirectory;
            if (File.Exists("last")) textBox1.Text = File.ReadAllText("last");

            readAudioFiles();

            DataGridViewButtonColumn buttonColumn = new DataGridViewButtonColumn();
            buttonColumn.HeaderText = "Open";
            buttonColumn.Width = 50;
            buttonColumn.Name = "btn";
            buttonColumn.Text = "📂";
            buttonColumn.UseColumnTextForButtonValue = true;

            dataGridView1.CellClick += DataGridView1_CellClick;

            dataGridView1.Columns.Add(buttonColumn);
        }

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].GetType() == typeof(DataGridViewButtonColumn) && e.RowIndex >= 0)
            {
                Process.Start("explorer.exe", "/select, " + dataGridView1.Rows[e.RowIndex].Cells[0].Value);
            }
        }

        List<(string, TimeSpan)> AudioFiles = new List<(string, TimeSpan)>();
        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                try { File.WriteAllText("last", folderBrowserDialog1.SelectedPath); } catch { }

                textBox1.Text = folderBrowserDialog1.SelectedPath;

                readAudioFiles();
            }
        }

        private void readAudioFiles()
        {
            AudioFiles.Clear();

            foreach (string file in Directory.EnumerateFiles(textBox1.Text, "*.mp3", SearchOption.AllDirectories))
            {
                try
                {
                    /*var metadata = MetadataExtractor.Formats.Mpeg.Mp3MetadataReader
                        .ReadMetadata(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));*/

                    AudioFiles.Add((file, TagLib.File.Create(file).Properties.Duration));
                }
                catch { }
            }
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            filterAndDisplaySongs();
        }

        void filterAndDisplaySongs()
        {
            try
            {
                int iterations = 0;
                var allowedOffset = TimeSpan.FromSeconds(dateTimePicker2.Value.Second + (dateTimePicker2.Value.Minute * 60)).Ticks;
                var requiredDuration = TimeSpan.FromSeconds(dateTimePicker1.Value.Second + (dateTimePicker1.Value.Minute * 60)).Ticks;

                var tracksToFind = (int)numericUpDown1.Value;
                var trackIndexes = new int[tracksToFind];

                var tracksFound = new List<(string, string)>();
                var trackBuffer = new List<(string, TimeSpan)>();

                var sortedAudioFiles = AudioFiles.OrderBy(a => a.Item2.Ticks).ToList();

                for (int i = 0; i < sortedAudioFiles.Count; i++)
                {
                    trackBuffer.Clear();
                    for (int j = 0; j < tracksToFind; j++)
                    {
                        if (j + i >= sortedAudioFiles.Count) break;
                        trackBuffer.Add(sortedAudioFiles[i + j]);
                    }
                    try
                    {
                        var duration = trackBuffer.Sum(d => d.Item2.Ticks);
                        if (((requiredDuration + allowedOffset >= duration) && (requiredDuration - allowedOffset <= duration)))
                        {
                            tracksFound.AddRange(trackBuffer.Select(d => (d.Item1, d.Item2.ToString("mm\\:ss"))));
                            tracksFound.Add(("", ""));
                        }
                    }
                    catch
                    { }
                }

                /*
                  trackIndexes[x] = t;

                            trackBuffer.Clear();
                            trackBuffer = trackIndexes.Select(d => AudioFiles[d]).ToList();
                            var duration = trackBuffer.Sum(d => d.Item2.Ticks);

                            Console.WriteLine($"[{string.Join(", ", trackIndexes)}]");

                            if (trackIndexes.Length != trackIndexes.Distinct().Count()) continue;

                            if (((requiredDuration + allowedOffset >= duration) && (requiredDuration - allowedOffset <= duration)))
                            {
                                tracksFound.AddRange(trackBuffer.Select(d => (d.Item1, d.Item2.ToString("mm\\:ss"))));
                                tracksFound.Add(("", ""));
                            }
                */

                dataGridView1.Rows.Clear();
                dataGridView1.Rows.AddRange(tracksFound.Select(t =>
                {
                    var index = dataGridView1.Rows.Add();
                    dataGridView1.Rows[index].Cells["TrackName"].Value = t.Item1;
                    dataGridView1.Rows[index].Cells["Duration"].Value = t.Item2;

                    return dataGridView1.Rows[index];
                }).ToArray());
            }
            catch
            {

            }
        }
    }
}
