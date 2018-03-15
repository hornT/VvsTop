using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using NLog;

namespace Top
{
    /// <summary>
    /// Основной класс для обработки файлов
    /// </summary>
    public sealed class Top200Processor
    {
        [NotNull]
        private readonly Logger _logger;

        public Top200Processor()
        {
            _logger = LogManager.GetCurrentClassLogger() ?? throw new Exception("logger is null");
        }

        public TopResultSummary Process([NotNull] string path, out string message)
        {
            _logger.Info($"Начинаем обработку папки {path}");
            // В корневой папке должно быть 2 папки, в каждой из них по 3 файла эксель
            // Название каждой папки принимаем за название года

            DirectoryInfo di = new DirectoryInfo(path);
            DirectoryInfo[] dirs = di.GetDirectories().OrderBy(x => x.Name).ToArray();
            if (dirs.Length != 2)
            {
                _logger.Error($"Внутри {path} {dirs.Length} папки, должно быть 2");
                message = "Должно быть 2 папки";
                return null;
            }

            var f1 = dirs[0]?.GetFiles().OrderBy(x => x.Name).ToArray();
            var f2 = dirs[1]?.GetFiles().OrderBy(x => x.Name).ToArray();

            if (f1 == null || f2 == null)
            {
                _logger.Error($"Файлы не найдены. f1: {f1}, f2: {f2}");
                message = "Файлы не найдены";
                return null;
            }

            if (f1.Length != 3 || f2.Length != 3)
            {
                _logger.Error($"Внутри {dirs[0].Name} папки {f1.Length} файлов, внутри {dirs[1].Name} папки {f2.Length} файлов");
                message = "Должно быть 3 файла";
                return null;
            }

            FileInfo[] files = f1.Union(f2).ToArray();
            var result = new TopResultSummary(files, dirs[0].Name, dirs[1].Name);

            try
            {
                result.Process();
                message = null;
                return result;
            }
            catch (Exception e)
            {
                _logger.Error(e);
                message = "Произошла ошибка обработки. Подробности в логе";
                return null;
            }
        }
    }
}
