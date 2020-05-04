namespace mpTables
{
    using System.Diagnostics;
    using System.IO;
    using Autodesk.AutoCAD.ApplicationServices.Core;
    using Autodesk.AutoCAD.DatabaseServices;
    using ModPlusAPI.Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    /// <summary>
    /// Вспомогательные утилиты
    /// </summary>
    public static class Utils
    {
        private const string LangItem = "mpTables";

        /// <summary>
        /// Распаковка файла с таблицами из ресурсов на диск
        /// </summary>
        public static void ExtractTablesDwg()
        {
            var dir = Path.Combine(ModPlusAPI.Constants.AppDataDirectory, "Data", "Dwg");

            try
            {
                // Папки Dwg может и не быть!
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Все таблицы в одном файле
                ExtractEmbeddedResource(dir, "Tables.dwg", "mpTables.Resources");

                // Удаляю файл по старому пути
                var oldFile = Path.Combine(ModPlusAPI.Constants.CurrentDirectory, "Data", "Dwg", "Tables.dwg");
                if (File.Exists(oldFile))
                {
                    try
                    {
                        File.Delete(oldFile);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            catch
            {
                MessageBox.Show(ModPlusAPI.Language.GetItem(LangItem, "msg1"), MessageBoxIcon.Close);
            }
        }

        /// <summary>
        /// Возвращает масштаб по имени
        /// </summary>
        /// <param name="scaleName">Имя масштаба аннотаций</param>
        /// <returns></returns>
        public static double GetScale(string scaleName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ocm = db.ObjectContextManager;
            var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
            var annotationScale = occ.GetContext(scaleName) as AnnotationScale;
            Debug.Assert(annotationScale != null, nameof(annotationScale) + " != null");
            return annotationScale.DrawingUnits / annotationScale.PaperUnits;
        }

        /// <summary>
        /// Проверка не заблокирован ли слой
        /// </summary>
        /// <param name="layerName">Имя слоя</param>
        public static bool IsLockedLayer(string layerName)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            using (doc.LockDocument())
            {
                using (OpenCloseTransaction tr = db.TransactionManager.StartOpenCloseTransaction())
                {
                    var lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null)
                        return false;
                    foreach (var layerId in lt)
                    {
                        var layer = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                        if (layer != null && layer.Name.Equals(layerName))
                        {
                            return layer.IsLocked;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Возвращает имя текущего слоя
        /// </summary>
        public static string GetCurrentLayerName()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            using (doc.LockDocument())
            {
                using (OpenCloseTransaction tr = db.TransactionManager.StartOpenCloseTransaction())
                {
                    var lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt != null)
                    {
                        foreach (var layerId in lt)
                        {
                            if (layerId == db.Clayer)
                            {
                                var layer = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                                if (layer != null)
                                {
                                    return layer.Name;
                                }
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        private static void ExtractEmbeddedResource(string outputDir, string file, string resourceLocation)
        {
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceLocation + @"." + file))
            {
                using (var fileStream = new FileStream(Path.Combine(outputDir, file), FileMode.Create))
                {
                    for (var i = 0; i < stream?.Length; i++)
                    {
                        fileStream.WriteByte((byte)stream.ReadByte());
                    }

                    fileStream.Close();
                }
            }
        }
    }
}
