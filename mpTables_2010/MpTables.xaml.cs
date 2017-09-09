#if ac2010
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
#elif ac2013
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
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
using System.Xml.Linq;
using ModPlus;
using mpMsg;
using mpSettings;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Visibility = System.Windows.Visibility;

namespace mpTables
{
    /// <summary>
    /// Логика взаимодействия для MpTables.xaml
    /// </summary>
    public partial class MpTables
    {
        // Текущий документ с таблицами
        private TablesBase _tablesBase;

        public MpTables()
        {
            InitializeComponent();
            MpWindowHelpers.OnWindowStartUp(
                this,
                MpSettings.GetValue("Settings", "MainSet", "Theme"),
                MpSettings.GetValue("Settings", "MainSet", "AccentColor"),
                MpSettings.GetValue("Settings", "MainSet", "BordersType")
                );
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
            if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
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
            if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Released)
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
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            Focus();
        }
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
        private void TbTextHeight_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Запрет нажатия пробела
            if (e.Key == Key.Space)
                e.Handled = true;
        }
        private void _PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Ввод только цифр и точки
            short val;
            if (!short.TryParse(e.Text, out val) && !e.Text.Equals("."))
                e.Handled = true;
        }
        private void MpTables_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
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
                        File.WriteAllBytes(Path.Combine(dir, "Tables.dwg"),
                            Properties.Resources.Tables);
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
                        MpMsgWin.Show("Не удалось скопировать файл на диск!" +
                                      Environment.NewLine +
                                      "Возможно отсутствуют права администратора");
                    }
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
                TbTextHeight.Text = "2.5";
                // Начальное значение высоты строк
                TbRowHeight.Text = "8";

                string txtstname;
                using (var acTrans = doc.TransactionManager.StartTransaction())
                {
                    var tst = (TextStyleTable)acTrans.GetObject(db.TextStyleTableId, OpenMode.ForRead);

                    foreach (var id in tst)
                    {
                        var tstr = (TextStyleTableRecord)acTrans.GetObject(id, OpenMode.ForRead);
                        CbTextStyle.Items.Add(tstr.Name);
                    }
                    var curtxt = (TextStyleTableRecord)acTrans.GetObject(db.Textstyle, OpenMode.ForRead);
                    txtstname = curtxt.Name;
                    acTrans.Commit();
                }
                CbTextStyle.SelectedItem = txtstname;
            }
            catch (System.Exception ex)
            {
                MpExWin.Show(ex);
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
                int index;
                // Источник для базы (страна)
                CbDocumentsFor.SelectedIndex = int.TryParse(MpSettings.GetValue("Settings", "mpTables", "CbDocumentsFor"), out index) ? index : 0;

                // Масштаб
                var scale = MpSettings.GetValue("Settings", "mpTables", "CbScales");
                CbScales.SelectedIndex = CbScales.Items.Contains(scale)
                    ? CbScales.Items.IndexOf(scale)
                    : 0;
                // Текстовый стиль (меняем, если есть в настройках, а иначе оставляем текущий)
                var txtstl = MpSettings.GetValue("Settings", "mpTables", "CbTextStyle");
                if (CbTextStyle.Items.Contains(txtstl))
                    CbTextStyle.SelectedIndex = CbTextStyle.Items.IndexOf(txtstl);
                // Динамические строчки
                bool b;
                ChkDynRowsStandard.IsChecked = !bool.TryParse(MpSettings.GetValue("Settings", "mpTables", "ChkDynRowsStandard"), out b) || b;
                ChkDynRows.IsChecked = !bool.TryParse(MpSettings.GetValue("Settings", "mpTables", "ChkDynRows"), out b) || b;
                // Высота строк
                var rowH = MpSettings.GetValue("Settings", "mpTables", "TbRowHeight");
                if (!string.IsNullOrEmpty(rowH))
                    TbRowHeight.Text = rowH;
                // Высота текста
                var txtH = MpSettings.GetValue("Settings", "mpTables", "TbTextHeight");
                if (!string.IsNullOrEmpty(txtH))
                    TbTextHeight.Text = txtH;
                // Привязка
                RbBottomLeft.IsChecked =
                    bool.Parse(MpSettings.GetValue("Settings", "mpTables", "RbBottomLeft"));
                RbBottomRight.IsChecked =
                    bool.Parse(MpSettings.GetValue("Settings", "mpTables", "RbBottomRight"));
                RbMnlBottomLeft.IsChecked =
                    bool.Parse(MpSettings.GetValue("Settings", "mpTables", "RbMnlBottomLeft"));
                RbMnlBottomRight.IsChecked =
                    bool.Parse(MpSettings.GetValue("Settings", "mpTables", "RbMnlBottomRight"));
                RbMnlTopLeft.IsChecked =
                    bool.Parse(MpSettings.GetValue("Settings", "mpTables", "RbMnlTopLeft"));
                RbMnlTopRight.IsChecked =
                    bool.Parse(MpSettings.GetValue("Settings", "mpTables", "RbMnlTopRight"));
                RbTopLeft.IsChecked =
                    bool.Parse(MpSettings.GetValue("Settings", "mpTables", "RbTopLeft"));
                RbTopRight.IsChecked =
                    bool.Parse(MpSettings.GetValue("Settings", "mpTables", "RbTopRight"));
                ExpDetail.IsExpanded = bool.Parse(MpSettings.GetValue("Settings", "mpTables", "ExpDetail"));
            }
            catch
            {
                // ignored
            }
        }
        // Сохранение в файл настроек
        private void SaveToSettings()
        {
            // Источник для базы (страна)
            //MpSettings.SetValue("Settings", "mpTables", "CbDocumentsFor",
            //    CbDocumentsFor.SelectedIndex.ToString(CultureInfo.InvariantCulture), false);
            // Список отсеивания
            //MpSettings.SetValue("Settings", "mpTables", "CbDocWeed",
            //    CbDocWeed.SelectedIndex.ToString(CultureInfo.InvariantCulture), false);
            // Выбранный штамп
            //MpSettings.SetValue("Settings", "mpTables", "CbTables",
            //                        CbTables.SelectedIndex.ToString(CultureInfo.InvariantCulture), false);
            // Масштаб
            MpSettings.SetValue("Settings", "mpTables", "CbScales",
                                    CbScales.SelectedItem.ToString(), false);
            // Текстовый стиль
            MpSettings.SetValue("Settings", "mpTables", "CbTextStyle",
                                    CbTextStyle.SelectedItem.ToString(), false);
            // Динамические строчки
            MpSettings.SetValue("Settings", "mpTables", "ChkDynRowsStandard",
                                    (ChkDynRowsStandard.IsChecked != null && ChkDynRowsStandard.IsChecked.Value).ToString(), false);
            MpSettings.SetValue("Settings", "mpTables", "ChkDynRows",
                                    (ChkDynRows.IsChecked != null && ChkDynRows.IsChecked.Value).ToString(), false);

            // Высота строк
            MpSettings.SetValue("Settings", "mpTables", "TbRowHeight", TbRowHeight.Text, false);
            // Высота текста
            MpSettings.SetValue("Settings", "mpTables", "TbTextHeight", TbTextHeight.Text, false);
            // Привязка
            MpSettings.SetValue("Settings", "mpTables", "RbBottomLeft",
                                    (RbBottomLeft.IsChecked != null && RbBottomLeft.IsChecked.Value).ToString(), false);
            MpSettings.SetValue("Settings", "mpTables", "RbBottomRight",
                                    (RbBottomRight.IsChecked != null && RbBottomRight.IsChecked.Value).ToString(), false);
            MpSettings.SetValue("Settings", "mpTables", "RbMnlBottomLeft",
                                    (RbMnlBottomLeft.IsChecked != null && RbMnlBottomLeft.IsChecked.Value).ToString(), false);
            MpSettings.SetValue("Settings", "mpTables", "RbMnlBottomRight",
                                    (RbMnlBottomRight.IsChecked != null && RbMnlBottomRight.IsChecked.Value).ToString(), false);
            MpSettings.SetValue("Settings", "mpTables", "RbMnlTopLeft",
                                    (RbMnlTopLeft.IsChecked != null && RbMnlTopLeft.IsChecked.Value).ToString(), false);
            MpSettings.SetValue("Settings", "mpTables", "RbMnlTopRight",
                                    (RbMnlTopRight.IsChecked != null && RbMnlTopRight.IsChecked.Value).ToString(), false);
            MpSettings.SetValue("Settings", "mpTables", "RbTopLeft",
                                    (RbTopLeft.IsChecked != null && RbTopLeft.IsChecked.Value).ToString(), false);
            MpSettings.SetValue("Settings", "mpTables", "RbTopRight",
                                    (RbTopRight.IsChecked != null && RbTopRight.IsChecked.Value).ToString(), false);
            MpSettings.SetValue("Settings", "mpTables", "ExpDetail",
                                    ExpDetail.IsExpanded.ToString(), false);
            MpSettings.SaveFile();
        }
        // Выбор базы таблиц
        private void CbDocumentsFor_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                var comboBoxItem = cb?.SelectedItem as ComboBoxItem;
                if (cb != null && comboBoxItem != null && cb.SelectedIndex != -1)
                {
                    _tablesBase = new TablesBase(comboBoxItem.Tag.ToString());
                    MpSettings.SetValue("Settings", "mpTables", "CbDocumentsFor", cb.SelectedIndex.ToString(CultureInfo.InvariantCulture), true);
                }
                else _tablesBase = null;
                // Заполняем список отсеивания
                CbDocWeed.ItemsSource = null;
                if (_tablesBase?.Tables != null)
                {
                    var lst = new List<string> { "Все" };
                    foreach (var tableDocumentInBase in _tablesBase.Tables)
                    {
                        if (!lst.Contains(tableDocumentInBase.Document) &
                            !string.IsNullOrEmpty(tableDocumentInBase.Document))
                            lst.Add(tableDocumentInBase.Document);
                    }
                    CbDocWeed.ItemsSource = lst;
                    int index;
                    if (int.TryParse(MpSettings.GetValue("Settings", "mpTables", "CbDocWeed"), out index))
                    {
                        CbDocWeed.SelectedIndex = CbDocWeed.Items.Count >= index ? index : 0;
                    }
                    else CbDocWeed.SelectedIndex = 0;
                }
            }
            catch (System.Exception ex)
            {
                MpExWin.Show(ex);
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
                    int index;
                    if (int.TryParse(MpSettings.GetValue("Settings", "mpTables", "CbTables"), out index))
                    {
                        CbTables.SelectedIndex = CbTables.Items.Count >= index ? index : 0;
                    }
                    else CbTables.SelectedIndex = 0;

                    if (cb.SelectedIndex != -1)
                        MpSettings.SetValue("Settings", "mpTables", "CbDocWeed", cb.SelectedIndex.ToString(CultureInfo.InvariantCulture), true);
                }
            }
            catch (System.Exception ex)
            {
                MpExWin.Show(ex);
            }
        }
        // Выбор таблицы
        private void CbTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                if (cb != null && cb.Items.Count > 0)
                {
                    var selectedTable = cb.SelectedItem as TableDocumentInBase;
                    if (selectedTable != null)
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
                                VersionData.FuncVersion +
                                ";component/Resources/Images/" +
                                (CbDocumentsFor.SelectedItem as ComboBoxItem)?.Tag +
                                "/" + selectedTable.Img + ".png",
                                    UriKind.Absolute));
                        }
                        catch
                        {
                            img.Source = new BitmapImage(
                                new Uri(@"pack://application:,,,/mpTables_" + VersionData.FuncVersion +
                                        ";component/Resources/Images/NoImage.png",
                                    UriKind.Absolute));
                        }
                        // Если есть атрибут, запрещающий динамическую вставку строк
                        ChkDynRowsStandard.IsEnabled = selectedTable.DynRow;
                        // Полное название документа
                        TbDocumentName.Text = selectedTable.DocumentName;
                        // Inoperative
                        TbInoperative.Visibility = selectedTable.TableStyleName.EndsWith("_old") ? Visibility.Visible : Visibility.Collapsed;
                    }
                    else ClearTableInfo();

                    if (cb.SelectedIndex != -1)
                        MpSettings.SetValue("Settings", "mpTables", "CbTables", cb.SelectedIndex.ToString(CultureInfo.InvariantCulture), true);
                }
                else ClearTableInfo();
            }
            catch (System.Exception ex)
            {
                MpExWin.Show(ex);
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

        private static double Scale(string scaleName)
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
            var selectedTable = CbTables.SelectedItem as TableDocumentInBase;
            if (selectedTable == null) return;
            // привязка точки вставки
            var pointAligin = "TopLeft";
            if (RbBottomLeft.IsChecked != null && RbBottomLeft.IsChecked.Value) pointAligin = "BottomLeft";
            if (RbBottomRight.IsChecked != null && RbBottomRight.IsChecked.Value) pointAligin = "BottomRight";
            if (RbTopLeft.IsChecked != null && RbTopLeft.IsChecked.Value) pointAligin = "TopLeft";
            if (RbTopRight.IsChecked != null && RbTopRight.IsChecked.Value) pointAligin = "TopRight";
            Hide();
            try
            {
                InsertTable(pointAligin, selectedTable);
            }
            catch (System.Exception exception)
            {
                MpExWin.Show(exception);
            }
            finally
            {
                Show();
            }
        }
        private void InsertTable(string pointAligin, TableDocumentInBase selectedTableDocumentInBase)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            // Блокируем документ
            using (doc.LockDocument())
            {
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    // Копируем таблицу из файла ресурсов
                    var tbl = GetTableFromSource(tr, selectedTableDocumentInBase);
                    if (tbl == null)
                    {
                        MpMsgWin.Show("Не удалось скопировать таблицу :'(");
                        return;
                    }
                    // Масштабируем до перемещения для правильного отображения
                    var mat = Matrix3d.Scaling(Scale(CbScales.SelectedItem.ToString()), tbl.Position);
                    tbl.TransformBy(mat);
                    tbl.SuppressRegenerateTable(true);

                    // Перемещаем с джигой
                    var jig = new TableDrag();
                    var jigresult = jig.StartJig(tbl, pointAligin);
                    if (jigresult.Status != PromptStatus.OK)
                    {
                        tbl.Erase();
                        return;
                    }
                    tbl.Position = jig.TablePositionPoint();
                    doc.TransactionManager.QueueForGraphicsFlush();


                    tbl.SuppressRegenerateTable(false);
                    // Динамическая вставка строк
                    if (selectedTableDocumentInBase.DynRow)
                        if (ChkDynRowsStandard.IsChecked != null && ChkDynRowsStandard.IsChecked.Value)
                        {
                            tbl.SuppressRegenerateTable(true);
                            tbl.DowngradeOpen();
                            var addcelljig = new TableAddCellsJig
                            {
                                FPt = tbl.Position,
                                RowH = double.Parse(TbRowHeight.Text.Replace(',', '.')) * Scale(CbScales.SelectedItem.ToString()),
                                StopRows = selectedTableDocumentInBase.DataRow,
                                TbH = tbl.GeometricExtents.MaxPoint.Y - tbl.GeometricExtents.MinPoint.Y
                            };
                            addcelljig.StartJig(tbl);
                            tbl.SuppressRegenerateTable(false);
                        }

                    ////////////////////////////////////////////////
                    // Присваиваем свойства//
                    /////////////////////////
                    var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                    tbl.Cells.TextStyleId = tst[CbTextStyle.SelectedItem.ToString()];

                    if (selectedTableDocumentInBase.NameToHeader)
                        tbl.Cells[0, 0].TextString = selectedTableDocumentInBase.Name;
                    tbl.Cells.TextStyleId = tst[CbTextStyle.SelectedItem.ToString()];

                    if (selectedTableDocumentInBase.DataRow > 0)
                        for (var i = selectedTableDocumentInBase.DataRow; i < tbl.Rows.Count; i++)
                        {
                            tbl.Rows[i].TextStyleId = tst[CbTextStyle.SelectedItem.ToString()];
                            tbl.Rows[i].Height = double.Parse(TbRowHeight.Text.Replace(',', '.')) * Scale(CbScales.SelectedItem.ToString());
                            tbl.Rows[i].TextHeight = double.Parse(TbTextHeight.Text.Replace(',', '.')) * Scale(CbScales.SelectedItem.ToString());
                            // Копирование свойств с предыдущей ячейки
                            if (i != selectedTableDocumentInBase.DataRow & selectedTableDocumentInBase.DynRow)
                            {
                                for (var j = 0; j < tbl.Columns.Count; j++)
                                {
                                    var isMerged = tbl.Cells[i, j].IsMerged;
                                    if (isMerged != null && !isMerged.Value)
                                    {
                                        tbl.Cells[i, j].Style = tbl.Cells[i - 1, j].Style;
                                        tbl.Cells[i, j].DataFormat = tbl.Cells[i - 1, j].DataFormat;
                                        if (tbl.Cells[i - 1, j].Alignment != null)
                                            tbl.Cells[i, j].Alignment = tbl.Cells[i - 1, j].Alignment;
                                        if (tbl.Cells[i - 1, j].TextStyleId != null)
                                            tbl.Cells[i, j].TextStyleId = tbl.Cells[i - 1, j].TextStyleId;
                                        if (tbl.Cells[i - 1, j].TextHeight != null)
                                            tbl.Cells[i, j].TextHeight = tbl.Cells[i - 1, j].TextHeight;
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
                        if (!File.Exists(_tablesBase.DwgFileName))
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
                                if (ent is Table)
                                {
                                    var tblsty = (Table)myT.GetObject(obj, OpenMode.ForRead);

                                    if (tblsty.TableStyleName.Equals(selectedTableDocumentInBase.TableStyleName))
                                    {
                                        tblIds.Add(tblsty.ObjectId);
                                        var im = new IdMapping();
                                        sourceDb.WblockCloneObjects(tblIds, db.CurrentSpaceId, im, DuplicateRecordCloning.Ignore, false);
                                        tbl = (Table)tr.GetObject(im.Lookup(tblsty.ObjectId).Value, OpenMode.ForWrite);
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
            catch (System.Exception exception)
            {
                MpExWin.Show(exception);
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
                    Filter = @"Файл чертежа (*.DWG)|*.DWG",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = false,
                    ShowHelp = false,
                    Title = @"Выберите файл"
                };
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TbFileFrom.Text = ofd.FileName;
                    LoadTablesFromFile(ofd.FileName, LvTablesFromDwg);
                    MpSettings.SetValue("Settings", "mpTables", "file", ofd.FileName, true);
                }
            }
            catch (System.Exception ex)
            {
                MpExWin.Show(ex);
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
                TbFileFrom.Text = MpSettings.GetValue("Settings", "mpTables", "file");
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
            catch (System.Exception ex)
            {
                if (ex.Message.Equals("eNotImplementedYet"))
                    MpMsgWin.Show("В разделе \"Таблицы из файла\" указан файл, несовместимый с данной версией AutoCAD!");
                else MpExWin.Show(ex);
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
                    Transaction tr = doc.TransactionManager.StartTransaction();
                    using (tr)
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

                        var mat = Matrix3d.Scaling(Scale(CbScales.SelectedItem.ToString()), tbl.Position);
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
                                RowH =
                                                               double.Parse(TbRowHeight.Text) *
                                                               Scale(CbScales.SelectedItem.ToString()),
                                StopRows = 2,
                                TbH =
                                                               tbl.GeometricExtents.MaxPoint.Y -
                                                               tbl.GeometricExtents.MinPoint.Y
                            };
                            jigaddcell.StartJig(tbl);
                            ed.Regen();
                        }
                        tr.Commit();
                    }
                }
            }// try
            catch (System.Exception ex)
            {
                MpExWin.Show(ex);
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
            MpSettings.SetValue("Settings", "mpTables", "file", string.Empty, true);
        }

    }
}
