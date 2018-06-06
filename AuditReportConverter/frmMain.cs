using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace AuditReportConverter
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private void buttonSelectSourceFile_Click(object sender, EventArgs e)
        {
            // *** Configure the dialog box ***
            openFileDialogSourceFile.Title = "Select the source file";
            openFileDialogSourceFile.Filter = "CSV files|*.csv|All files|*.*";
            openFileDialogSourceFile.FileName = "";

            DialogResult result = openFileDialogSourceFile.ShowDialog();

            if (result == DialogResult.OK)
            {
                labelSourceFile.Text = openFileDialogSourceFile.FileName;
                labelPass.Text = string.Empty;
            }
        }

        private void buttonDestinationDirectory_Click(object sender, EventArgs e)
        {
            // *** Configure dialog box ***

            DialogResult result = folderBrowserDialogFolder.ShowDialog();

            if (result == DialogResult.OK)
            {
                labelDestinationFolder.Text = folderBrowserDialogFolder.SelectedPath;
                labelPass.Text = string.Empty;
            }
        }

        private void buttonProcess_Click(object sender, EventArgs e)
        {
            char originalDelimiterCharacter = ',';
            char newDelimiterCharacter = ';';
            string sourceFile = labelSourceFile.Text;
            string destinationFolder = labelDestinationFolder.Text;

            // Verify sourceFile
            if (!File.Exists(sourceFile))
            {
                string msg = @"Please select a valid source file.";
                MessageBox.Show(msg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Verify destinationFolder
            if (!Directory.Exists(destinationFolder))
            {
                string msg = @"Please select a valid destination folder.";
                MessageBox.Show(msg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Verify destinationFolder is empty
            if (Directory.EnumerateFileSystemEntries(destinationFolder).Any())
            {
                string msg = @"The destination directory is not empty." + Environment.NewLine + Environment.NewLine + @"Proceeding will destroy all contents!!!";
                DialogResult result = MessageBox.Show(msg, "Do you want to proceed?", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                {
                    // *** Empty directory ***
                    DirectoryInfo di = new DirectoryInfo(destinationFolder);

                    foreach (FileInfo file in di.EnumerateFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.EnumerateDirectories())
                    {
                        dir.Delete(true);
                    }
                } else
                {
                    return;
                }
            }

            // *** Start Processing ***
            auditReportConverter.OriginalDelimiter = originalDelimiterCharacter;
            auditReportConverter.DestinationDelimiter = newDelimiterCharacter;
            auditReportConverter.SourceFile = sourceFile;
            auditReportConverter.DestinationFolder = destinationFolder;

            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.RunWorkerAsync();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            backgroundWorker.ReportProgress(0, "Pass 1 of 4");
            auditReportConverter.ProcessFile(backgroundWorker);
            backgroundWorker.ReportProgress(0, "Pass 2 of 4");
            auditReportConverter.TransformFile(backgroundWorker);
            backgroundWorker.ReportProgress(0, "Pass 3 of 4");
            auditReportConverter.SplitFile(backgroundWorker);
            backgroundWorker.ReportProgress(0, "Pass 4 of 4");
            auditReportConverter.ReportFile(backgroundWorker);
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            if (e.UserState != null)
            {
                labelPass.Text = e.UserState.ToString();
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Value = 0;
            labelPass.Text = "Completed!!!";
            textBoxResults.Font = new Font(FontFamily.GenericMonospace, textBoxResults.Font.Size);
            textBoxResults.Text = File.ReadAllText(auditReportConverter.DestinationFolder + @"//report.txt");
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form aboutBox = new AboutBox();

            aboutBox.ShowDialog();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


    }
}
