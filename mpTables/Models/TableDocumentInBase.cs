namespace mpTables.Models
{
    using System;
    using System.Windows.Media.Imaging;

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

        /// <summary>
        /// Текстовый индекс нормативного документ
        /// </summary>
        public string NormativeDocumentIndex { get; set; }
    }
}