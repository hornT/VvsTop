using System;
using System.Data;
using System.Windows.Forms;
using JetBrains.Annotations;
using NLog;
using Top;

namespace VvsTop
{
    public partial class MainForm : Form
    {
        [NotNull]
        private readonly Logger _logger;

        [NotNull]
        private readonly Top200Processor _processor = new Top200Processor();

        private TopResultSummary _result;

        public MainForm()
        {
            InitializeComponent();

            _logger = LogManager.GetCurrentClassLogger() ?? throw new Exception("logger is null");
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog == null || folderBrowserDialog.ShowDialog() != DialogResult.OK)
                return;

            Cursor = Cursors.WaitCursor;

            _result = _processor.Process(folderBrowserDialog.SelectedPath ?? string.Empty, out string message);
            Cursor = Cursors.Arrow;

            if (message != null || _result == null)
            {
                MessageBox.Show(message, "Произошла ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            dgvImport.DataSource = _result.Import.GetData();
            dgvExport.DataSource = _result.Export.GetData();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_result == null)
            {
                _logger.Warn("_result == null");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "xlsx files (*.xlsx)|*.xlsx";
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            if (string.IsNullOrEmpty(saveFileDialog.FileName) == true)
            {
                _logger.Warn("saveFileDialog.FileName empty");
                return;
            }

            _result.ExportToExcel(saveFileDialog.FileName);

            MessageBox.Show("Выполнено!", "Выгрузка", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
