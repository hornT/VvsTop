using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelDataReader;
using JetBrains.Annotations;
using NLog;
using NPOI.XSSF.UserModel;

namespace Top
{
    internal struct FileData
    {
        [NotNull]
        internal readonly Dictionary<string, double> Import;

        [NotNull]
        internal readonly Dictionary<string, double> Export;

        internal readonly DateTime Date;

        public FileData([NotNull]Dictionary<string, double> import, [NotNull]Dictionary<string, double> export, DateTime dt)
        {
            Import = import;
            Export = export;
            Date = dt;
        }
    }

    public sealed class TopResultSummary
    {
        [NotNull]
        private readonly Logger _logger;

        private const int FILES_COUNT = 6;
        private const int DATE_COLUMN_INDEX = 1;
        private const int CODE_COLUMN_INDEX = 3;
        private const int SUMM_COLUMN_INDEX = 5;
        //private const int TOP_GOODS_COUNT = 200;

        [NotNull]
        private readonly FileInfo[] _files;

        [NotNull]
        public readonly TopResult Import;

        [NotNull]
        public readonly TopResult Export;

        public TopResultSummary([NotNull]FileInfo[] files, [NotNull]string folder1Name, [NotNull]string folder2Name)
        {
            _logger = LogManager.GetCurrentClassLogger() ?? throw new Exception("logger is null");

            if (files.Length != FILES_COUNT)
            {
                throw new ArgumentException("Всего должно быть 6 файлов");
            }

            _files = files;

            Import = new TopResult(folder1Name, folder2Name);
            Export = new TopResult(folder1Name, folder2Name);
        }

        internal void Process()
        {
            FileData[] datas = new FileData[FILES_COUNT];

            Parallel.For(0, FILES_COUNT, i =>
            {
                FileInfo file = _files[i];
                if (file == null)
                    throw new Exception($"file {i} is null");

                datas[i] = ProcessFile(file.FullName);
            });

            for (int i = 0; i < FILES_COUNT; i++)
            {
                Import.AddData(datas[i].Import, i, datas[i].Date);
                Export.AddData(datas[i].Export, i, datas[i].Date);
            }
        }

        private FileData ProcessFile([NotNull] string filePath)
        {
            using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    using (var result = reader.AsDataSet())
                    {
                        if (result == null)
                        {
                            _logger.Error($"Файл {filePath} не прочитан");
                            throw new Exception($"Файл {filePath} не прочитан");
                        }

                        DataTable tbImport = result.Tables[0];
                        DataTable tbExport = result.Tables[1];

                        if (tbImport == null || tbExport == null)
                        {
                            _logger.Error($"Не прочитаны таблицы");
                            throw new Exception($"Файл {filePath} не прочитан");
                        }

                        Dictionary<string, double> import = GetDataFromSheet(tbImport);
                        Dictionary<string, double> export = GetDataFromSheet(tbExport);

                        string dStr = tbImport.Rows[1][DATE_COLUMN_INDEX] as string;
                        string[] tmpS = dStr.Split('/', '\\');
                        int.TryParse(tmpS[0], out int month);
                        int.TryParse(tmpS[1], out int year);
                        DateTime dt = new DateTime(year, month, 1);

                        return new FileData(import, export, dt);
                    } 
                }
            }
        }

        [NotNull]
        private Dictionary<string, double> GetDataFromSheet([NotNull] DataTable dt)
        {
            return dt
                .AsEnumerable()
                .Skip(1)
                .GroupBy(row => row.Field<string>(CODE_COLUMN_INDEX))
                .Select(gr => new { Code = gr.Key, Summ = gr.Sum(x => x.Field<double>(SUMM_COLUMN_INDEX)) })
                //.OrderByDescending(x => x.Summ)
                //.Take(TOP_GOODS_COUNT)
                .ToDictionary(x => x.Code, x => x.Summ);
        }

        public void ExportToExcel([NotNull] string filePath)
        {
            if (Export.Data == null || Import.Data == null)
            {
                _logger.Warn("Export.Data == null || Import.Data == null");
                return;
            }

            var wb = new XSSFWorkbook();
            Import.CreateSheet(wb, "Импорт");
            Export.CreateSheet(wb, "Экспорт");

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                try
                {
                    wb.Write(fs);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }                
            }
        }
    }
}
