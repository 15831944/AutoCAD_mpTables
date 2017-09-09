using System.Diagnostics.CodeAnalysis;
using mpPInterface;

namespace mpTables
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Interface : IPluginInterface
    {
        private const string _Name = "mpTables";
        private const string _AvailCad = "2011";
        private const string _LName = "Таблицы";
        private const string _Description = "Функция вставки в чертеж таблиц AutoCad согласно ГОСТам или из указанного файла";
        private const string _Author = "Пекшев Александр aka Modis";
        private const string _Price = "0";
        public string Name => _Name;
        public string AvailCad => _AvailCad;
        public string LName => _LName;
        public string Description => _Description;
        public string Author => _Author;
        public string Price => _Price;
    }
    public class VersionData
    {
        public const string FuncVersion = "2011";
    }
}
