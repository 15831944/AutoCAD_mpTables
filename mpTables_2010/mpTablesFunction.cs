#if ac2010
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
#elif ac2013
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Autodesk.AutoCAD.Runtime;
using ModPlusAPI;

namespace mpTables
{
    public class mpTablesFunction
    {
        // Вызов функции
        private MpTables _mpTables;
        [CommandMethod("ModPlus", "mpTables", CommandFlags.Modal)]
        public void Main()
        {
            Statistic.SendCommandStarting(new Interface());
            
            if (_mpTables == null)
            {
                _mpTables = new MpTables();
                _mpTables.Closed += win_Closed;
            }

            if (_mpTables.IsLoaded)
                _mpTables.Activate();
            else
                AcApp.ShowModelessWindow(AcApp.MainWindow.Handle, _mpTables);
        }

        private void win_Closed(object sender, EventArgs e)
        {
            _mpTables = null;
        }
    }
    /// <summary>
    /// Класс описывает текущую базу таблиц (зависит от страны: Россия, Украина и т.п.)
    /// </summary>
    public class TablesBase
    {
        /// <summary>
        /// Инициализация класса
        /// </summary>
        /// <param name="documentsFor">Указание страны, согласно таблицы кодов стран </param>
        public TablesBase(string documentsFor)
        {
            // Загружаем данные по таблицам
            switch (documentsFor)
            {
                case "RU":
                    Tables = LoadTables(XElement.Parse(GetResourceTextFile("TablesBase_RU.xml")));
                    break;
                case "UA":
                    Tables = LoadTables(XElement.Parse(GetResourceTextFile("TablesBase_UA.xml")));
                    break;
                case "BY":
                    Tables = LoadTables(XElement.Parse(GetResourceTextFile("TablesBase_BY.xml")));
                    break;
            }
            // Получаем путь к файлу чертежа с таблицами (без проверки его существования)
            DwgFileName = GetDwgFileName();
        }
        /// <summary>Чтение текстового внедренного ресурса</summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static string GetResourceTextFile(string filename)
        {
            var result = string.Empty;
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("mpTables.Resources." + filename))
            {
                if (stream != null)
                    using (var sr = new StreamReader(stream))
                    {
                        result = sr.ReadToEnd();
                    }
            }
            return result;
        }
        /// <summary>
        /// Имя файла из которого брать таблицы
        /// </summary>
        public string DwgFileName;
        /// <summary>
        /// Получение пути к файлу с таблицами
        /// </summary>
        /// <returns></returns>
        private static string GetDwgFileName()
        {
            var fileName = string.Empty;
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("ModPlus"))
            {
                if (key == null) return fileName;
                // Директория расположения файла
                var dir = Path.Combine(key.GetValue("TopDir").ToString(), "Data", "Dwg");
                // Имя файла из которого берем таблицу
                //fileName = Path.Combine(dir, "Tables_" + documentFor + ".dwg");
                fileName = Path.Combine(dir, "Tables.dwg");
                // Проверка наличия файла и его распаковка происходят при работе функции (при попытке вставки таблицы)
            }
            return fileName;
        }
        /// <summary>
        /// Таблицы в базе
        /// </summary>
        public List<TableDocumentInBase> Tables;
        /// <summary>
        /// Получение списка таблиц из базы
        /// </summary>
        /// <param name="xmlFile">xml-файл базы из ресурсов</param>
        /// <returns></returns>
        private static List<TableDocumentInBase> LoadTables(XElement xmlFile)
        {
            var tables = new List<TableDocumentInBase>();
            foreach (var xElement in xmlFile.Elements("Table"))
            {
                int i;
                bool b;
                var newDocumentInBase = new TableDocumentInBase
                {
                    Name = xElement.Attribute("name")?.Value,
                    TableStyleName = xElement.Attribute("tablestylename")?.Value,
                    Document = xElement.Attribute("document")?.Value,
                    Description = xElement.Attribute("description")?.Value,
                    Img = xElement.Attribute("img")?.Value,
                    DataRow = int.TryParse(xElement.Attribute("DataRow")?.Value, out i) ? i : 2,
                    DynRow = !bool.TryParse(xElement.Attribute("DynRow")?.Value, out b) || b, // true
                    NameToHeader = bool.TryParse(xElement.Attribute("NameToHeader")?.Value, out b) && b // false
                };
                var o = xmlFile.Element("Documents");
                if (o != null)
                    foreach (var element in o.Elements("Document"))
                    {
                        var xAttribute = element.Attribute("document");
                        if ((xAttribute != null && xAttribute.Value.Equals(newDocumentInBase.Document)))
                            newDocumentInBase.DocumentName = element.Attribute("name")?.Value;
                    }
                tables.Add(newDocumentInBase);
            }
            return tables;
        }
    }
    /// <summary>
    /// Класс описывает документ в базе (то, что в xml-файле)
    /// </summary>
    public class TableDocumentInBase
    {
        /// <summary>
        /// Название таблицы
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Название табличного стиля
        /// </summary>
        public string TableStyleName { get; set; }
        /// <summary>
        /// Номер нормативного документа
        /// </summary>
        public string Document { get; set; }
        /// <summary>
        /// Название нормативного документа
        /// </summary>
        public string DocumentName { get; set; }
        /// <summary>
        /// Описание таблицы (ссылка на таблицу в нормативном документе)
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Имя файла изображения
        /// </summary>
        public string Img { get; set; }
        /// <summary>
        /// Номер строки с которой начинаются строчки данных
        /// </summary>
        public int DataRow { get; set; }
        /// <summary>
        /// Возможность динамической вставки строк
        /// </summary>
        public bool DynRow { get; set; }
        /// <summary>
        /// Добавление имени таблицы в шапку
        /// </summary>
        public bool NameToHeader { get; set; }
    }
}
