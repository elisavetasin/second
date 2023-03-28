using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Timers;
using System.Windows;

namespace _10A
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DriveInfo drive = new DriveInfo(@"C:\");
            if (drive.IsReady) path_textBox.Text = drive.Name;
        }

        BackgroundWorker worker;
        readonly Timer timer = new Timer(10000);

        void GetSubfolders(string path, string pattern)
        {
            try
            {
                GetFiles(path, pattern);

                string[] dirs = Directory.GetDirectories(path);

                foreach (string dir in dirs)
                {
                    if (worker.CancellationPending) return;
                    GetSubfolders(dir, pattern);
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        void GetFiles(string path, string pattern)
        {
            string[] files = Directory.GetFiles(path, pattern);

            foreach (string file in files)
            {
                if (worker.CancellationPending) return;
                worker.ReportProgress(0, file);
            }
        }

        private void browse_button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) path_textBox.Text = folderBrowserDialog.SelectedPath;
        }

        private void search_button_Click(object sender, RoutedEventArgs e)
        {
            if (worker != null && worker.IsBusy) return;

            listBox.Items.Clear();

            string path = path_textBox.Text;
            string pattern = name_textBox.Text.Contains(".") ? name_textBox.Text : name_textBox.Text + ".*";
            bool sub = (bool)subfolders_checkBox.IsChecked;

            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.DoWork += (s, ev) => worker_DoWork(ev, path, pattern, sub);
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;

            timer.Interval = 10000;
            timer.AutoReset = false;
            timer.Elapsed += timer_Elapsed;
            timer.Start();

            worker.RunWorkerAsync();
        }

        void worker_DoWork(DoWorkEventArgs e, string path, string pattern, bool sub)
        {
            if (sub) GetSubfolders(path, pattern);
            else GetFiles(path, pattern);

            if (worker.CancellationPending)
            {
                e.Cancel = true;
                MessageBox.Show("Процесс остановлен, так как файлов по пути слишком много.",
                    "Сообщение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            listBox.Items.Add(e.UserState.ToString());
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && listBox.Items.Count == 0) MessageBox.Show("Файлы не найдены.",
                "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            worker.CancelAsync();
        }

        private void open_button_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedItem == null) return;

            string path = listBox.SelectedItem.ToString();
            if (Path.GetExtension(path) != ".txt") return;

            using (FileStream fstream = File.OpenRead(path))
            {
                byte[] buffer = new byte[fstream.Length];
                fstream.Read(buffer, 0, buffer.Length);
                string textFromFile = Encoding.Default.GetString(buffer);

                TextFromFile window = new TextFromFile() { Text = textFromFile };
                window.ShowDialog();
            }
        }

        private void compress_button_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedItem == null) return;

            string path = listBox.SelectedItem.ToString();
            string dir = Path.GetDirectoryName(path);
            string archivePath = Path.Combine(dir, Path.GetFileNameWithoutExtension(path) + ".zip");

            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
            {
                archive.CreateEntryFromFile(path, Path.GetFileName(path));
            }

            Process.Start(dir);
        }
    }
}