using mpPInterface;

namespace mpTables
{
    public class Interface : IPluginInterface
    {
        public string Name => "mpTables";
        public string AvailCad => "2018";
        public string LName => "Таблицы";
        public string Description => "Функция вставки в чертеж таблиц AutoCad согласно ГОСТам или из указанного файла";
        public string Author => "Пекшев Александр aka Modis";
        public string Price => "0";
    }
    public class VersionData
    {
        public const string FuncVersion = "2018";
    }
}
