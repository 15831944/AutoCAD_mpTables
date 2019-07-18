namespace mpTables
{
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.DatabaseServices;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Visibility = System.Windows.Visibility;

    public partial class MpTables
    {
        private const string LangItem = "mpTables";
        // Текущий документ с таблицами
        private TablesBase _tablesBase;

        public MpTables()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(LangItem, "h1");
            // Zooming and panning image
            MouseWheel += MainWindow_MouseWheel;
            img.MouseDown += img_MouseDown;
            img.MouseUp += img_MouseUp;
            img.MouseMove += image_MouseMove;

            //////////////////////////////////////////
            RbTopLeft.IsChecked = true;
            RbMnlTopLeft.IsChecked = true;
            LoadFileNameFromSettings();
            // Распаковка файла с таблицами
            ExtractTablesDwg();
        }

        #region Работа окна

        #region Zooming and panning image
        private Point _origin;
        private Point _start;

        private void img_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                if (img.IsMouseCaptured) return;
                Cursor = Cursors.Hand;
                img.CaptureMouse();

                _start = e.GetPosition(imageBorder);
                _origin.X = img.RenderTransform.Value.OffsetX;
                _origin.Y = img.RenderTransform.Value.OffsetY;

            }
        }
        private void img_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Released)
            {
                img.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
            }
        }

        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!img.IsMouseCaptured) return;
            var p = e.MouseDevice.GetPosition(imageBorder);

            var m = img.RenderTransform.Value;
            m.OffsetX = _origin.X + (p.X - _start.X);
            m.OffsetY = _origin.Y + (p.Y - _start.Y);

            img.RenderTransform = new MatrixTransform(m);
        }

        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var p = e.MouseDevice.GetPosition(img);

            var m = img.RenderTransform.Value;
            if (e.Delta > 0)
                m.ScaleAtPrepend(1.2, 1.2, p.X, p.Y);
            else
                m.ScaleAtPrepend(1 / 1.2, 1 / 1.2, p.X, p.Y);

            img.RenderTransform = new MatrixTransform(m);
        }
        private void BtImageSmall_OnClick(object sender, RoutedEventArgs e)
        {
            var m = img.RenderTransform.Value;
            m.ScalePrepend(1 / 1.2, 1 / 1.2);
            img.RenderTransform = new MatrixTransform(m);
        }

        private void BtImageBig_OnClick(object sender, RoutedEventArgs e)
        {
            var m = img.RenderTransform.Value;
            m.ScalePrepend(1.2, 1.2);
            img.RenderTransform = new MatrixTransform(m);
        }
        #endregion

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            Focus();
        }
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
        }

        #endregion

        // Распаковка файла с таблицами
        private static void ExtractTablesDwg()
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("ModPlus");
            using (key)
            {
                if (key != null)
                {
                    // Директория расположения файла
                    var dir = Path.Combine(key.GetValue("TopDir").ToString(), "Data", "Dwg");
                    try
                    {
                        // Папки Dwg может и не быть!
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        // Все таблицы в одном файле
                        ExtractEmbeddedResource(dir, "Tables.dwg", "mpTables.Resources");
                        // Вариант с несколькими таблицами мне не понравился, поэтому удаляю все файлы
                        var filesToDelete = new List<string> { "Tables_RU.dwg", "Tables_UA.dwg", "Tables_BY.dwg" };
                        foreach (var file in filesToDelete)
                        {
                            var oldFile = Path.Combine(dir, file);
                            if (File.Exists(oldFile))
                                File.Delete(oldFile);
                        }
                    }
                    catch
                    {
                        ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(LangItem, "msg1"),
                                      MessageBoxIcon.Close);
                    }
                }
            }
        }

        private static void ExtractEmbeddedResource(string outputDir, string file, string resourceLocation)
        {
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceLocation + @"." + file))
            {
                using (var fileStream = new FileStream(Path.Combine(outputDir, file), FileMode.Create))
                {
                    for (int i = 0; i < stream?.Length; i++)
                    {
                        fileStream.WriteByte((byte)stream.ReadByte());
                    }
                    fileStream.Close();
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            try
            {
                // Заполнение списка масштабов
                var ocm = db.ObjectContextManager;
                var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                foreach (var ans in occ.Cast<AnnotationScale>())
                {
                    CbScales.Items.Add(ans.Name);
                }
                // Начальное значение масштаба
                var cans = occ.CurrentContext as AnnotationScale;
                if (cans != null) CbScales.SelectedItem = cans.Name;
                // Начальное значение высоты текста
                TbTextHeight.Value = 2.5;
                // Начальное значение высоты строк
                TbRowHeight.Value = 8;

                string txtstname;
                using (var acTrans = doc.TransactionManager.StartTransaction())
                {
                    var tst = (TextStyleTable)acTrans.GetObject(db.TextStyleTableId, OpenMode.ForRead);

                    foreach (var id in tst)
                    {
                        var tstr = (TextStyleTableRecord)acTrans.GetObject(id, OpenMode.ForRead);
                        if (!string.IsNullOrEmpty(tstr.Name))
                            CbTextStyle.Items.Add(tstr.Name);
                    }
                    var curtxt = (TextStyleTableRecord)acTrans.GetObject(db.Textstyle, OpenMode.ForRead);
                    if (CbTextStyle.Items.Contains(curtxt.Name))
                        txtstname = curtxt.Name;
                    else txtstname = CbTextStyle.Items[0].ToString();
                    acTrans.Commit();
                }
                CbTextStyle.SelectedItem = txtstname;
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
            // Загружаем настройки
            LoadFromSettings();
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            SaveToSettings();
        }

        // Загрузка значений из файла настроек
        private void LoadFromSettings()
        {
            // Так как при первом запуске их может не быть,
            // то просто оборачиваем в try{}
            try
            {
                // Источник для базы (страна)
                CbDocumentsFor.SelectedIndex = int.TryParse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "CbDocumentsFor"), out int index) ? index : 0;

                // Масштаб
                var scale = UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "CbScales");
                CbScales.SelectedIndex = CbScales.Items.Contains(scale)
                    ? CbScales.Items.IndexOf(scale)
                    : 0;
                // Текстовый стиль (меняем, если есть в настройках, а иначе оставляем текущий)
                var txtstl = UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "CbTextStyle");
                if (CbTextStyle.Items.Contains(txtstl))
                    CbTextStyle.SelectedIndex = CbTextStyle.Items.IndexOf(txtstl);
                // Динамические строчки
                ChkDynRowsStandard.IsChecked = !bool.TryParse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "ChkDynRowsStandard"), out var b) || b;
                ChkDynRows.IsChecked = !bool.TryParse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "ChkDynRows"), out b) || b;
                // Высота строк
                if (double.TryParse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "TbRowHeight"), out var i))
                    TbRowHeight.Value = i;
                // Высота текста
                if (double.TryParse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "TbTextHeight"), out i))
                    TbTextHeight.Value = i;
                // Привязка
                RbBottomLeft.IsChecked =
                    bool.Parse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbBottomLeft"));
                RbBottomRight.IsChecked =
                    bool.Parse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbBottomRight"));
                RbMnlBottomLeft.IsChecked =
                    bool.Parse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbMnlBottomLeft"));
                RbMnlBottomRight.IsChecked =
                    bool.Parse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbMnlBottomRight"));
                RbMnlTopLeft.IsChecked =
                    bool.Parse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbMnlTopLeft"));
                RbMnlTopRight.IsChecked =
                    bool.Parse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbMnlTopRight"));
                RbTopLeft.IsChecked =
                    bool.Parse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbTopLeft"));
                RbTopRight.IsChecked =
                    bool.Parse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbTopRight"));
            }
            catch
            {
                // ignored
            }
        }

        // Сохранение в файл настроек
        private void SaveToSettings()
        {
            // Масштаб
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "CbScales",
                                    CbScales.SelectedItem.ToString(), false);
            // Текстовый стиль
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "CbTextStyle",
                                    CbTextStyle.SelectedItem.ToString(), false);
            // Динамические строчки
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "ChkDynRowsStandard",
                                    (ChkDynRowsStandard.IsChecked != null && ChkDynRowsStandard.IsChecked.Value).ToString(), false);
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "ChkDynRows",
                                    (ChkDynRows.IsChecked != null && ChkDynRows.IsChecked.Value).ToString(), false);

            // Высота строк
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "TbRowHeight", TbRowHeight.Value.ToString(), false);
            // Высота текста
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "TbTextHeight", TbTextHeight.Value.ToString(), false);
            // Привязка
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbBottomLeft",
                                    (RbBottomLeft.IsChecked != null && RbBottomLeft.IsChecked.Value).ToString(), false);
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbBottomRight",
                                    (RbBottomRight.IsChecked != null && RbBottomRight.IsChecked.Value).ToString(), false);
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbMnlBottomLeft",
                                    (RbMnlBottomLeft.IsChecked != null && RbMnlBottomLeft.IsChecked.Value).ToString(), false);
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbMnlBottomRight",
                                    (RbMnlBottomRight.IsChecked != null && RbMnlBottomRight.IsChecked.Value).ToString(), false);
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbMnlTopLeft",
                                    (RbMnlTopLeft.IsChecked != null && RbMnlTopLeft.IsChecked.Value).ToString(), false);
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbMnlTopRight",
                                    (RbMnlTopRight.IsChecked != null && RbMnlTopRight.IsChecked.Value).ToString(), false);
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbTopLeft",
                                    (RbTopLeft.IsChecked != null && RbTopLeft.IsChecked.Value).ToString(), false);
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "RbTopRight",
                                    (RbTopRight.IsChecked != null && RbTopRight.IsChecked.Value).ToString(), false);
            UserConfigFile.SaveConfigFile();
        }

        // Выбор базы таблиц
        private void CbDocumentsFor_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem comboBoxItem && cb.SelectedIndex != -1)
                {
                    _tablesBase = new TablesBase(comboBoxItem.Tag.ToString());
                    UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "CbDocumentsFor", cb.SelectedIndex.ToString(CultureInfo.InvariantCulture), true);
                }
                else _tablesBase = null;
                // Заполняем список отсеивания
                CbDocWeed.ItemsSource = null;
                if (_tablesBase?.Tables != null)
                {
                    var lst = new List<string> { ModPlusAPI.Language.GetItem(LangItem, "all") };
                    foreach (var tableDocumentInBase in _tablesBase.Tables)
                    {
                        if (!lst.Contains(tableDocumentInBase.Document) &
                            !string.IsNullOrEmpty(tableDocumentInBase.Document))
                            lst.Add(tableDocumentInBase.Document);
                    }
                    CbDocWeed.ItemsSource = lst;
                    if (int.TryParse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "CbDocWeed"), out var index))
                    {
                        CbDocWeed.SelectedIndex = CbDocWeed.Items.Count >= index ? index : 0;
                    }
                    else CbDocWeed.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        }

        // Выбор нормативного документа
        private void CbDocWeed_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                CbTables.ItemsSource = null;
                if (_tablesBase?.Tables != null && cb != null)
                {
                    if (cb.SelectedIndex == 0) // all
                    {
                        CbTables.ItemsSource = _tablesBase.Tables;
                    }
                    else if (cb.SelectedIndex > 0)
                    {
                        var selectedDocWeed = cb.SelectedItem.ToString();
                        CbTables.ItemsSource = _tablesBase.Tables.Where(x => x.Document.Equals(selectedDocWeed));
                    }
                    if (int.TryParse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "CbTables"), out var index))
                    {
                        CbTables.SelectedIndex = CbTables.Items.Count >= index ? index : 0;
                    }
                    else CbTables.SelectedIndex = 0;

                    if (cb.SelectedIndex != -1)
                        UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "CbDocWeed", cb.SelectedIndex.ToString(CultureInfo.InvariantCulture), true);
                }
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        }

        // Выбор таблицы
        private void CbTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.Items.Count > 0)
                {
                    if (cb.SelectedItem is TableDocumentInBase selectedTable)
                    {
                        // Нормативный документ
                        LiDocument.Text = selectedTable.Document;
                        // Краткое описание
                        TbDescription.Text = selectedTable.Description;
                        // Изображение
                        try
                        {
                            img.Source = new BitmapImage(
                                new Uri(@"pack://application:,,,/mpTables_" +
                                new ModPlusConnector().AvailProductExternalVersion +
                                ";component/Resources/Images/" +
                                (CbDocumentsFor.SelectedItem as ComboBoxItem)?.Tag +
                                "/" + selectedTable.Img + ".png",
                                    UriKind.Absolute));
                        }
                        catch
                        {
                            img.Source = new BitmapImage(
                                new Uri(@"pack://application:,,,/mpTables_" + new ModPlusConnector().AvailProductExternalVersion +
                                        ";component/Resources/Images/NoImage.png",
                                    UriKind.Absolute));
                        }
                        // Вписываю изображение
                        var m = img.RenderTransform.Value;
                        m.SetIdentity();
                        img.RenderTransform = new MatrixTransform(m);

                        // Если есть атрибут, запрещающий динамическую вставку строк
                        ChkDynRowsStandard.IsEnabled = selectedTable.DynRow;
                        // Полное название документа
                        TbDocumentName.Text = selectedTable.DocumentName;
                        // Inoperative
                        TbInoperative.Visibility = selectedTable.TableStyleName.EndsWith("_old") ? Visibility.Visible : Visibility.Collapsed;
                    }
                    else ClearTableInfo();

                    if (cb.SelectedIndex != -1)
                        UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "CbTables", cb.SelectedIndex.ToString(CultureInfo.InvariantCulture), true);
                }
                else ClearTableInfo();
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        }

        // Очистка элементов, описывающих таблицу
        private void ClearTableInfo()
        {
            // Нормативный документ
            LiDocument.Text = string.Empty;
            // Краткое описание
            TbDescription.Text = string.Empty;
            // Изображение
            img.Source = null;
            // Если есть атрибут, запрещающий динамическую вставку строк
            ChkDynRows.IsEnabled = false;
            // Полное название документа
            TbDocumentName.Text = string.Empty;
        }

        private static double GetScale(string scaleName)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ocm = db.ObjectContextManager;
            var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
            var ansc = occ.GetContext(scaleName) as AnnotationScale;
            Debug.Assert(ansc != null, "ansc != null");
            return (ansc.DrawingUnits / ansc.PaperUnits);
        }

        // Вставка 
        private void BtAddTable_Click(object sender, RoutedEventArgs e)
        {
            if (!(CbTables.SelectedItem is TableDocumentInBase selectedTable)) return;
            // привязка точки вставки
            var pointAlign = "TopLeft";
            if (RbBottomLeft.IsChecked != null && RbBottomLeft.IsChecked.Value) pointAlign = "BottomLeft";
            if (RbBottomRight.IsChecked != null && RbBottomRight.IsChecked.Value) pointAlign = "BottomRight";
            if (RbTopLeft.IsChecked != null && RbTopLeft.IsChecked.Value) pointAlign = "TopLeft";
            if (RbTopRight.IsChecked != null && RbTopRight.IsChecked.Value) pointAlign = "TopRight";
            Hide();
            try
            {
                InsertTable(pointAlign, selectedTable);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                Show();
            }
        }

        private void InsertTable(string pointAlign, TableDocumentInBase selectedTableDocumentInBase)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var scale = GetScale(CbScales.SelectedItem.ToString());
            Table table;
            // Блокируем документ
            using (doc.LockDocument())
            {
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    // Копируем таблицу из файла ресурсов
                    table = GetTableFromSource(tr, selectedTableDocumentInBase);
                    if (table == null)
                    {
                        ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(LangItem, "msg2"), MessageBoxIcon.Close);
                        return;
                    }
                    // Масштабируем до перемещения для правильного отображения

                    var mat = Matrix3d.Scaling(scale, table.Position);
                    table.TransformBy(mat);
                    table.SuppressRegenerateTable(true);

                    // Перемещаем с джигой
                    var jig = new TableDrag();
                    var jigResult = jig.StartJig(table, pointAlign);
                    if (jigResult.Status != PromptStatus.OK)
                    {
                        table.Erase();
                        return;
                    }
                    table.Position = jig.TablePositionPoint();
                    doc.TransactionManager.QueueForGraphicsFlush();

                    table.SuppressRegenerateTable(false);

                    // Динамическая вставка строк
                    if (selectedTableDocumentInBase.DynRow)
                    {
                        if (ChkDynRowsStandard.IsChecked != null && ChkDynRowsStandard.IsChecked.Value)
                        {
                            table.SuppressRegenerateTable(true);
                            table.DowngradeOpen();
                            var tableAddCellsJig = new TableAddCellsJig
                            {
                                FPt = table.Position,
                                RowH = TbRowHeight.Value * scale ?? 8 * scale,
                                StopRows = selectedTableDocumentInBase.DataRow,
                                TbH = table.GeometricExtents.MaxPoint.Y - table.GeometricExtents.MinPoint.Y
                            };
                            tableAddCellsJig.StartJig(table);
                            table.SuppressRegenerateTable(false);
                        }
                    }

                    tr.Commit();
                }
            }

            SetProperties(table.Id, selectedTableDocumentInBase, doc, scale);
        }

        private void SetProperties(ObjectId tableId, TableDocumentInBase selectedTableDocumentInBase, Document doc, double scale)
        {
            var db = doc.Database;
            using (doc.LockDocument())
            {
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    var table = (Table)tr.GetObject(tableId, OpenMode.ForWrite);
                    var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                    table.Cells.TextStyleId = tst[CbTextStyle.SelectedItem.ToString()];

                    if (selectedTableDocumentInBase.NameToHeader)
                        table.Cells[0, 0].TextString = selectedTableDocumentInBase.Name;
                    table.Cells.TextStyleId = tst[CbTextStyle.SelectedItem.ToString()];

                    // Установка высоты текста для ячеек типа "Заголовок" и "Название"
                    if (selectedTableDocumentInBase.DataRow == 0)
                    {
                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            RowType rowType = table.RowType(i);
                            if (rowType == RowType.DataRow)
                                continue;
                            if (rowType == RowType.TitleRow)
                                table.Rows[i].TextHeight = 5 * scale;
                            if (rowType == RowType.HeaderRow)
                                table.Rows[i].TextHeight = 3 * scale;
                        }
                    }

                    if (selectedTableDocumentInBase.DataRow > 0)
                    {
                        for (int i = 0; i < selectedTableDocumentInBase.DataRow; i++)
                        {
                            if (i == 0)
                                table.Rows[i].TextHeight = 5 * scale;
                            else table.Rows[i].TextHeight = 3 * scale;
                        }
                        for (var i = selectedTableDocumentInBase.DataRow; i < table.Rows.Count; i++)
                        {
                            table.Rows[i].TextStyleId = tst[CbTextStyle.SelectedItem.ToString()];
                            table.Rows[i].Height = TbRowHeight.Value * scale ?? 8 * scale;
                            table.Rows[i].TextHeight = TbTextHeight.Value * scale ?? 2.5 * scale;
                            // Копирование свойств с предыдущей ячейки
                            if (i != selectedTableDocumentInBase.DataRow & selectedTableDocumentInBase.DynRow)
                            {
                                for (var j = 0; j < table.Columns.Count; j++)
                                {
                                    var isMerged = table.Cells[i, j].IsMerged;
                                    if (isMerged != null && !isMerged.Value)
                                    {
                                        table.Cells[i, j].Style = table.Cells[i - 1, j].Style;
                                        table.Cells[i, j].DataFormat = table.Cells[i - 1, j].DataFormat;
                                        if (table.Cells[i - 1, j].Alignment != null)
                                            table.Cells[i, j].Alignment = table.Cells[i - 1, j].Alignment;
                                        if (table.Cells[i - 1, j].TextStyleId != null)
                                            table.Cells[i, j].TextStyleId = table.Cells[i - 1, j].TextStyleId;
                                        if (table.Cells[i - 1, j].TextHeight != null)
                                            table.Cells[i, j].TextHeight = table.Cells[i - 1, j].TextHeight;
                                    }
                                }
                            }
                        }
                    }

                    tr.Commit();
                }
            }
        }

        private Table GetTableFromSource(Transaction tr, TableDocumentInBase selectedTableDocumentInBase)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var sourceDb = new Database(false, true);
            try
            {
                // Создаем пустую таблицу
                Table tbl = null;
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("ModPlus"))
                {
                    if (key != null)
                    {
                        // Имя файла из которого берем таблицу
                        ExtractTablesDwg();

                        // Read the DWG into a side database
                        sourceDb.ReadDwgFile(_tablesBase.DwgFileName, FileShare.Read, true, "");

                        var tblIds = new ObjectIdCollection();

                        using (var myT = sourceDb.TransactionManager.StartTransaction())
                        {
                            var sourceBtr = (BlockTableRecord)myT.GetObject(sourceDb.CurrentSpaceId, OpenMode.ForWrite, false);

                            foreach (var obj in sourceBtr)
                            {
                                var ent = (Entity)myT.GetObject(obj, OpenMode.ForRead);
                                if (ent is Table table)
                                {
                                    if (table.TableStyleName.Equals(selectedTableDocumentInBase.TableStyleName))
                                    {
                                        tblIds.Add(table.ObjectId);
                                        var im = new IdMapping();
                                        sourceDb.WblockCloneObjects(tblIds, db.CurrentSpaceId, im, DuplicateRecordCloning.Ignore, false);
                                        tbl = (Table)tr.GetObject(im.Lookup(table.ObjectId).Value, OpenMode.ForWrite);
                                        break;
                                    }
                                }
                            }
                            myT.Commit();
                        }
                    }
                }
                return tbl?.ObjectId == ObjectId.Null ? null : tbl;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return null;
            }
        }


        #region From File

        private void BtDwgFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new System.Windows.Forms.OpenFileDialog
                {
                    Filter = ModPlusAPI.Language.GetItem(LangItem, "h33") + " (*.DWG)|*.DWG",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = false,
                    ShowHelp = false,
                    Title = ModPlusAPI.Language.GetItem(LangItem, "h34")
                };
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TbFileFrom.Text = ofd.FileName;
                    LoadTablesFromFile(ofd.FileName, LvTablesFromDwg);
                    UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "file", ofd.FileName, true);
                }
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        }

        private void BtMnlAddTable_Click(object sender, RoutedEventArgs e)
        {
            if (LvTablesFromDwg.SelectedIndex == -1) return;
            if (string.IsNullOrEmpty(TbFileFrom.Text)) return;
            // привязка точки вставки
            var pointAligin = "TopLeft";
            if (RbMnlBottomLeft.IsChecked != null && RbMnlBottomLeft.IsChecked.Value) pointAligin = "BottomLeft";
            if (RbMnlBottomRight.IsChecked != null && RbMnlBottomRight.IsChecked.Value) pointAligin = "BottomRight";
            if (RbMnlTopLeft.IsChecked != null && RbMnlTopLeft.IsChecked.Value) pointAligin = "TopLeft";
            if (RbMnlTopRight.IsChecked != null && RbMnlTopRight.IsChecked.Value) pointAligin = "TopRight";

            AddTableFromDwg(
                TbFileFrom.Text,
                LvTablesFromDwg.SelectedItem.ToString(),
                pointAligin,
                ChkDynRows.IsChecked != null && ChkDynRows.IsChecked.Value);
        }

        private void LoadFileNameFromSettings()
        {
            try
            {
                TbFileFrom.Text = UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "file");
                LoadTablesFromFile(TbFileFrom.Text, LvTablesFromDwg);
            }
            catch
            {
                TbFileFrom.Text = string.Empty;
            }
        }

        /// <summary>
        /// Загрузить имена таблиц из файла в ListView
        /// </summary>
        /// <param name="file">Файл</param>
        /// <param name="lv">ListView</param>
        private void LoadTablesFromFile(string file, ItemsControl lv)
        {
            try
            {
                if (!File.Exists(file))
                {
                    TbFileFrom.Text = string.Empty;
                    return;
                }
                lv.ItemsSource = null;
                var sourceDb = new Database(false, true);
                sourceDb.ReadDwgFile(file, FileShare.Read, true, string.Empty);
                IList<string> tbls = new List<string>();
                using (var tm = sourceDb.TransactionManager.StartTransaction())
                {
                    var sourceBtr = (BlockTableRecord)tm.GetObject(sourceDb.CurrentSpaceId, OpenMode.ForWrite, false);

                    foreach (var obj in sourceBtr)
                    {
                        var ent = (Entity)tm.GetObject(obj, OpenMode.ForRead);
                        if (ent is Table)
                        {
                            var tblsty = (Table)tm.GetObject(obj, OpenMode.ForRead);
                            if (!tbls.Contains(tblsty.TableStyleName))
                                tbls.Add(tblsty.TableStyleName);
                        }
                    }
                    tm.Commit();
                }
                lv.ItemsSource = tbls;
            }
            catch (Exception ex)
            {
                if (ex.Message.Equals("eNotImplementedYet"))
                    ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(LangItem, "msg3"),
                        MessageBoxIcon.Alert);
                else ExceptionBox.Show(ex);
            }
        }

        private void AddTableFromDwg(string file, string tblStlNme, string pointAligin, bool dynrows)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var sourceDb = new Database(false, true);
            var ed = doc.Editor;
            // Закрываем форму
            Hide();
            try
            {
                // Блокируем документ
                using (doc.LockDocument())
                {
                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        // Read the DWG into a side database
                        sourceDb.ReadDwgFile(file, FileShare.Read, true, string.Empty);

                        var objIds = new ObjectIdCollection();
                        var tblIds = new ObjectIdCollection();
                        // Создаем пустую таблицу
                        var tbl = new Table();

                        var tm = sourceDb.TransactionManager;

                        using (var myT = tm.StartTransaction())
                        {
                            var sourceBtr = (BlockTableRecord)myT.GetObject(sourceDb.CurrentSpaceId, OpenMode.ForWrite, false);

                            var mapping = new IdMapping();
                            sourceDb.WblockCloneObjects(objIds, db.TableStyleDictionaryId, mapping, DuplicateRecordCloning.Replace, false);

                            foreach (var obj in sourceBtr)
                            {
                                var ent = (Entity)myT.GetObject(obj, OpenMode.ForWrite);
                                if (ent is Table)
                                {
                                    var tblsty = (Table)myT.GetObject(obj, OpenMode.ForWrite);
                                    if (tblsty.TableStyleName.Equals(tblStlNme))
                                    {
                                        tblIds.Add(tblsty.ObjectId);
                                        var im = new IdMapping();
                                        sourceDb.WblockCloneObjects(tblIds, db.CurrentSpaceId, im, DuplicateRecordCloning.Replace, false);
                                        tbl = (Table)tr.GetObject(im.Lookup(tblsty.ObjectId).Value, OpenMode.ForWrite);
                                        break;
                                    }
                                }
                            }
                            myT.Commit();
                        }

                        var mat = Matrix3d.Scaling(GetScale(CbScales.SelectedItem.ToString()), tbl.Position);
                        tbl.TransformBy(mat);

                        // Перемещаем с джигой
                        var jig = new TableDrag();
                        var jigresult = jig.StartJig(tbl, pointAligin);
                        if (jigresult.Status != PromptStatus.OK)
                        {
                            tbl.Erase(); return;
                        }
                        tbl.Position = jig.TablePositionPoint();
                        doc.TransactionManager.QueueForGraphicsFlush();
                        ////////////////////////////////////////////
                        if (dynrows)
                        {
                            tbl.DowngradeOpen();
                            var jigaddcell = new TableAddCellsJig
                            {
                                FPt = tbl.Position,
                                RowH = TbRowHeight.Value ?? 8 * GetScale(CbScales.SelectedItem.ToString()),
                                StopRows = 2,
                                TbH = tbl.GeometricExtents.MaxPoint.Y - tbl.GeometricExtents.MinPoint.Y
                            };
                            jigaddcell.StartJig(tbl);
                            ed.Regen();
                        }
                        tr.Commit();
                    }
                }
            }// try
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
            finally
            {
                Show();
            }
        }

        #endregion

        // Очистить путь к файлу
        private void BtClearFileFrom_OnClick(object sender, RoutedEventArgs e)
        {
            LvTablesFromDwg.ItemsSource = null;
            TbFileFrom.Text = string.Empty;
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "mpTables", "file", string.Empty, true);
        }

        private void RbTopLeft_OnChecked(object sender, RoutedEventArgs e)
        {
            RbBottomLeft.IsChecked = false;
            RbBottomRight.IsChecked = false;
            RbTopRight.IsChecked = false;
        }

        private void RbTopRight_OnChecked(object sender, RoutedEventArgs e)
        {
            RbTopLeft.IsChecked = false;
            RbBottomLeft.IsChecked = false;
            RbBottomRight.IsChecked = false;
        }

        private void RbBottomLeft_OnChecked(object sender, RoutedEventArgs e)
        {
            RbTopRight.IsChecked = false;
            RbTopLeft.IsChecked = false;
            RbBottomRight.IsChecked = false;
        }

        private void RbBottomRight_OnChecked(object sender, RoutedEventArgs e)
        {
            RbTopRight.IsChecked = false;
            RbTopLeft.IsChecked = false;
            RbBottomLeft.IsChecked = false;
        }
    }
}