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
using System.Threading;

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
            var folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.SelectedPath = textBox1.Text;

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
                    AudioFiles.Add((file, TagLib.File.Create(file).Properties.Duration));
                }
                catch { }
            }
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            filterAndDisplaySongs();
        }

        List<(string, string)> findMatchingSongs(List<(string, TimeSpan)> songs, int[] currentIndex, List<int[]> selectedIndexes, int required, long requiredDuration, long allowedOffset)
        {
            var tracksFound = new List<(string, string)>();
            var trackBuffer = new List<(string, TimeSpan)>();

            bool increment()
            {
                int x = required - 1;
                while (currentIndex[x] == songs.Count - 1)
                {
                    currentIndex[x] = 0;
                    x--;

                    if (x == -1) return false;
                }
                do
                {
                    currentIndex[x]++;
                } while (currentIndex[x] == songs.Count - 2);

                return true;
            }

            while (increment())
            {
                trackBuffer.Clear();
                trackBuffer.AddRange(currentIndex.Select(i => songs[i]));

                var duration = trackBuffer.Sum(d => d.Item2.Ticks);
                //Debug.WriteLine($"{duration} -> {string.Join(":", currentIndex.Select(i => i.ToString()))}");
                if ((requiredDuration + allowedOffset >= duration) && (requiredDuration - allowedOffset <= duration))
                    selectedIndexes.Add(currentIndex.ToArray());
            }

            var orderedIndexes = selectedIndexes.Select(i => i.OrderBy(x => x)).Distinct();

            foreach (var index in orderedIndexes)
            {
                tracksFound.AddRange(index.Select(x => (songs[x].Item1, songs[x].Item2.ToString("mm\\:ss"))));
                tracksFound.Add(("", ""));
            }

            return tracksFound;
        }

        void filterAndDisplaySongs()
        {
            try
            {
                var allowedOffset = TimeSpan
                    .FromSeconds(dateTimePicker2.Value.Second + (dateTimePicker2.Value.Minute * 60)).Ticks;
                var requiredDuration = TimeSpan
                    .FromSeconds(dateTimePicker1.Value.Second + (dateTimePicker1.Value.Minute * 60)).Ticks;

                var tracksToFind = (int)numericUpDown1.Value;
                var trackIndexes = new int[tracksToFind];
                for (int i = 0; i < trackIndexes.Length; i++)
                {
                    trackIndexes[i] = i;
                }

                var tracksFound = new List<(string, string)>();
                var sortedAudioFiles = AudioFiles.OrderBy(a => a.Item2.Ticks).ToList();

                tracksFound = findMatchingSongs(sortedAudioFiles, trackIndexes, new List<int[]>(), tracksToFind,
                    requiredDuration, allowedOffset);

                dataGridView1.AutoGenerateColumns = false;
                dataGridView1.Rows.Clear();
                tracksFound.ForEach(t =>
                {
                    var index = dataGridView1.Rows.Add();
                    dataGridView1.Rows[index].Cells["TrackName"].Value = t.Item1;
                    dataGridView1.Rows[index].Cells["Duration"].Value = t.Item2;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
