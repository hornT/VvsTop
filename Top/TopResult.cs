using System;
using System.Collections.Generic;
using System.Data;
using JetBrains.Annotations;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Top
{
    internal sealed class TopItem
    {
        // 3 месяца для 2х лет
        private const int DATA_SIZE = 6;

        [NotNull]
        internal double[] Data = new double[DATA_SIZE];
    }

    public sealed class TopResult
    {
        // По 3 месяца + итог к каждому году
        private const int COLUMNS_COUNT = 8;

        [NotNull]
        private readonly Dictionary<string, TopItem> _records = new Dictionary<string, TopItem>();

        [NotNull]
        private readonly string[] _columnNames = new string[COLUMNS_COUNT];

        internal DataTable Data { get; private set; }

        public TopResult([NotNull]string folder1Name, [NotNull]string folder2Name)
        {
            _columnNames[3] = $"Итог {folder1Name}";
            _columnNames[7] = $"Итог {folder2Name}";
        }

        internal void AddData([NotNull]Dictionary<string, double> data, int index, DateTime dt)
        {
            if (index < 3)
                _columnNames[index] = dt.ToString("MM.yyyy");
            else
                _columnNames[index + 1] = dt.ToString("MM.yyyy");

            foreach (KeyValuePair<string, double> d in data)
            {
                if (_records.TryGetValue(d.Key, out TopItem item) && item != null)
                    item.Data[index] = d.Value;
                else
                {
                    item = new TopItem();
                    item.Data[index] = d.Value;
                    _records[d.Key] = item;
                }
            }
        }

        private const string CODE_COLUMN_NAME = "Код";
        private const string TOTAL_COLUMN_NAME = "Итог";

        [NotNull]
        public DataTable GetData()
        {
            Data = new DataTable();

            Data.Columns.Add(CODE_COLUMN_NAME, typeof(string));
            Data.Columns.Add(_columnNames[0], typeof(double));
            Data.Columns.Add(_columnNames[1], typeof(double));
            Data.Columns.Add(_columnNames[2], typeof(double));
            Data.Columns.Add(_columnNames[3], typeof(double));
            Data.Columns.Add(_columnNames[4], typeof(double));
            Data.Columns.Add(_columnNames[5], typeof(double));
            Data.Columns.Add(_columnNames[6], typeof(double));
            Data.Columns.Add(_columnNames[7], typeof(double));
            Data.Columns.Add(TOTAL_COLUMN_NAME, typeof(double));

            foreach (KeyValuePair<string, TopItem> record in _records)
            {
                var row = Data.NewRow();

                row.BeginEdit();
                row[CODE_COLUMN_NAME] = record.Key;

                row[_columnNames[0]] = Math.Round(record.Value.Data[0], 2, MidpointRounding.AwayFromZero);
                row[_columnNames[1]] = Math.Round(record.Value.Data[1], 2, MidpointRounding.AwayFromZero);
                row[_columnNames[2]] = Math.Round(record.Value.Data[2], 2, MidpointRounding.AwayFromZero);
                row[_columnNames[4]] = Math.Round(record.Value.Data[3], 2, MidpointRounding.AwayFromZero);
                row[_columnNames[5]] = Math.Round(record.Value.Data[4], 2, MidpointRounding.AwayFromZero);
                row[_columnNames[6]] = Math.Round(record.Value.Data[5], 2, MidpointRounding.AwayFromZero);

                double t1 = Math.Round(record.Value.Data[0] + record.Value.Data[1] + record.Value.Data[2], 2, MidpointRounding.AwayFromZero);
                double t2 = Math.Round(record.Value.Data[3] + record.Value.Data[4] + record.Value.Data[5], 2, MidpointRounding.AwayFromZero);

                row[_columnNames[3]] = t1;
                row[_columnNames[7]] = t2;

                // Итоговые проценты
                row[TOTAL_COLUMN_NAME] = Math.Round(t2/t1*100 - 100, 1, MidpointRounding.AwayFromZero);
                row.EndEdit();

                Data.Rows.Add(row);
            }

            return Data;
        }

        internal void CreateSheet([NotNull]XSSFWorkbook wb, [NotNull]string sheetName)
        {
            if(Data == null)
                throw new Exception("Data == null");

            var sh = (XSSFSheet)wb.CreateSheet(sheetName);

            // Заголовки
            IRow row = sh.CreateRow(0);
            for (int i = 0; i < Data.Columns.Count; i++)
            {
                ICell cell = row.CreateCell(i);
                cell.SetCellValue(Data.Columns[i].ColumnName);
            }

            for (int i = 0; i < Data.Rows.Count; i++)
            {
                row = sh.CreateRow(i + 1);
                for (int j = 0; j < Data.Columns.Count; j++)
                {
                    ICell cell = row.CreateCell(j);
                    cell.SetCellValue(Data.Rows[i][j].ToString());
                }
            }
        }
    }
}
