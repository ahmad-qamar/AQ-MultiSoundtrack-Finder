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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Reflection;

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

            dateTimePicker2.Value = dateTimePicker2.Value.AddSeconds(10);
        }

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].GetType() == typeof(DataGridViewButtonColumn) && e.RowIndex >= 0)
            {
                Process.Start("explorer.exe", "/select, \"" + dataGridView1.Rows[e.RowIndex].Cells[0].Value + "\"");
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
            Task.Run(filterAndDisplaySongs);
        }

        Task<List<(string, string)>> findMatchingSongs(List<(string, TimeSpan)> songs, int[] currentIndex, List<int[]> selectedIndexes, int required, long requiredDuration, long allowedOffset, CancellationToken token)
        {
            var tracksFound = new List<(string, string)>();
            var trackBuffer = new List<(string, TimeSpan)>();

            long currentPos = 0;

            var maxIterations = (long)Math.Pow(songs.Count, required);

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

            while (!token.IsCancellationRequested & increment())
            {
                trackBuffer.Clear();
                trackBuffer.AddRange(currentIndex.Select(i => songs[i]));

                currentPos++;
                var pc = (int)Math.Round(((decimal)currentPos / maxIterations) * 100);

                progressBar1.Invoke((MethodInvoker)delegate
                {
                    if (pc > progressBar1.Value + 1 || pc < progressBar1.Value)
                    {
                        progressBar1.Value = pc;
                    }
                });


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
            return Task.FromResult(tracksFound);
        }

        CancellationTokenSource cancellationSource = new CancellationTokenSource();
        async Task filterAndDisplaySongs()
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

                if (!cancellationSource.IsCancellationRequested) cancellationSource.Cancel();
                cancellationSource = new CancellationTokenSource();
                tracksFound = await findMatchingSongs(sortedAudioFiles, trackIndexes, new List<int[]>(), tracksToFind,
                    requiredDuration, allowedOffset, cancellationSource.Token);

                dataGridView1.Invoke((MethodInvoker)delegate
                {
                    dataGridView1.Rows.Clear();

                    List<DataGridViewRow> rows = new List<DataGridViewRow>();
                    tracksFound.ForEach(t =>
                    {
                        var row = new DataGridViewRow();
                        row.CreateCells(dataGridView1);
                        row.Cells[0].Value = t.Item1;
                        row.Cells[1].Value = t.Item2;
                        rows.Add(row);
                    });
                    dataGridView1.Rows.AddRange(rows.ToArray());
                });

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
