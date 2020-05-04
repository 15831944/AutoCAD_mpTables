namespace mpTables
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows.Input;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Jigs;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
    using SystemVariableChangedEventArgs = Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs;

    /// <summary>
    /// Главная модель представления
    /// </summary>
    public class MainViewModel : VmBase
    {
        private const string LangItem = "mpTables";
        private readonly string _favorite;
        private readonly string _all;
        private readonly string _current;
        private readonly MainWindow _parentWindow;

        // Текущий документ с таблицами
        private TablesBase _tablesBase;
        private readonly ObservableCollection<string> _favoriteTableStyles;

        private int _selectedTabIndex;
        private int _normativeSourceIndex = -1;
        private string _selectedFilter;
        private string _selectedScale;
        private TableDocumentInBase _selectedTable;

        private double _textHeight = 2.5;
        private int _rowHeight = 8;
        private string _selectedTextStyle;
        private string _selectedLayer;
        private bool _useDynamicRowInsertion;

        private bool _topLeftSystemInsertSnap = true;
        private bool _topRightSystemInsertSnap;
        private bool _bottomLeftSystemInsertSnap;
        private bool _bottomRightSystemInsertSnap;

        private bool _topLeftUserInsertSnap = true;
        private bool _topRightUserInsertSnap;
        private bool _bottomLeftUserInsertSnap;
        private bool _bottomRightUserInsertSnap;

        private string _userTableFile;
        private string _selectedUserTableName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="parentWindow">Parent window</param>
        public MainViewModel(MainWindow parentWindow)
        {
            _parentWindow = parentWindow;
            UserTableNames = new ObservableCollection<string>();
            _favorite = Language.GetItem(LangItem, "favorite");
            _all = Language.GetItem(LangItem, "all");
            _current = Language.GetItem(LangItem, "current");
            Filters = new ObservableCollection<string>();
            Tables = new ObservableCollection<TableDocumentInBase>();
            _favoriteTableStyles = new ObservableCollection<string>();

            AcApp.SystemVariableChanged += AcAppOnSystemVariableChanged;
        }

        #region Insert sanp

        /// <summary>
        /// Привязка вставки "Верх лево" для системных таблиц
        /// </summary>
        public bool TopLeftSystemInsertSnap
        {
            get => _topLeftSystemInsertSnap;
            set
            {
                if (_topLeftSystemInsertSnap == value)
                    return;
                _topLeftSystemInsertSnap = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(TopLeftSystemInsertSnap), value.ToString(), true);
                if (value)
                {
                    TopRightSystemInsertSnap = false;
                    BottomLeftSystemInsertSnap = false;
                    BottomRightSystemInsertSnap = false;
                }
            }
        }

        /// <summary>
        /// Привязка вставки "Верх право" для системных таблиц
        /// </summary>
        public bool TopRightSystemInsertSnap
        {
            get => _topRightSystemInsertSnap;
            set
            {
                if (_topRightSystemInsertSnap == value)
                    return;
                _topRightSystemInsertSnap = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(TopRightSystemInsertSnap), value.ToString(), true);
                if (value)
                {
                    TopLeftSystemInsertSnap = false;
                    BottomLeftSystemInsertSnap = false;
                    BottomRightSystemInsertSnap = false;
                }
            }
        }

        /// <summary>
        /// Привязка вставки "Низ лево" для системных таблиц
        /// </summary>
        public bool BottomLeftSystemInsertSnap
        {
            get => _bottomLeftSystemInsertSnap;
            set
            {
                if (_bottomLeftSystemInsertSnap == value)
                    return;
                _bottomLeftSystemInsertSnap = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(BottomLeftSystemInsertSnap), value.ToString(), true);
                if (value)
                {
                    TopLeftSystemInsertSnap = false;
                    TopRightSystemInsertSnap = false;
                    BottomRightSystemInsertSnap = false;
                }
            }
        }

        /// <summary>
        /// Привязка вставки "Низ право" для системных таблиц
        /// </summary>
        public bool BottomRightSystemInsertSnap
        {
            get => _bottomRightSystemInsertSnap;
            set
            {
                if (_bottomRightSystemInsertSnap == value)
                    return;
                _bottomRightSystemInsertSnap = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(BottomRightSystemInsertSnap), value.ToString(), true);
                if (value)
                {
                    TopLeftSystemInsertSnap = false;
                    TopRightSystemInsertSnap = false;
                    BottomLeftSystemInsertSnap = false;
                }
            }
        }

        /// <summary>
        /// Привязка вставки "Верх лево" для таблиц из файла
        /// </summary>
        public bool TopLeftUserInsertSnap
        {
            get => _topLeftUserInsertSnap;
            set
            {
                if (_topLeftUserInsertSnap == value)
                    return;
                _topLeftUserInsertSnap = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(TopLeftUserInsertSnap), value.ToString(), true);
            }
        }

        /// <summary>
        /// Привязка вставки "Верх право" для таблиц из файла
        /// </summary>
        public bool TopRightUserInsertSnap
        {
            get => _topRightUserInsertSnap;
            set
            {
                if (_topRightUserInsertSnap == value)
                    return;
                _topRightUserInsertSnap = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(TopRightUserInsertSnap), value.ToString(), true);
            }
        }

        /// <summary>
        /// Привязка вставки "Низ лево" для таблиц из файла
        /// </summary>
        public bool BottomLeftUserInsertSnap
        {
            get => _bottomLeftUserInsertSnap;
            set
            {
                if (_bottomLeftUserInsertSnap == value)
                    return;
                _bottomLeftUserInsertSnap = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(BottomLeftUserInsertSnap), value.ToString(), true);
            }
        }

        /// <summary>
        /// Привязка вставки "Низ право" для таблиц из файла
        /// </summary>
        public bool BottomRightUserInsertSnap
        {
            get => _bottomRightUserInsertSnap;
            set
            {
                if (_bottomRightUserInsertSnap == value)
                    return;
                _bottomRightUserInsertSnap = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(BottomRightUserInsertSnap), value.ToString(), true);
            }
        }

        #endregion

        /// <summary>
        /// Индекс выбранной вкладки
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex == value)
                    return;
                _selectedTabIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanUseDynamicRowInsert));
            }
        }

        /// <summary>
        /// Индекс источника нормативной базы таблиц:
        /// 0 - RU
        /// 1 - UA
        /// 2 - BY
        /// </summary>
        public int NormativeSourceIndex
        {
            get => _normativeSourceIndex;
            set
            {
                if (_normativeSourceIndex == value)
                    return;
                _normativeSourceIndex = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(NormativeSourceIndex), value.ToString(), true);
                InitTableBaseAndFillFilters();
            }
        }

        /// <summary>
        /// Фильтры таблиц
        /// </summary>
        public ObservableCollection<string> Filters { get; }

        /// <summary>
        /// Выбранный фильтр
        /// </summary>
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (_selectedFilter == value)
                    return;
                _selectedFilter = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(SelectedFilter), value, true);
                FillTablesByFilter();
            }
        }

        /// <summary>
        /// Коллекция таблиц текущему фильтру
        /// </summary>
        public ObservableCollection<TableDocumentInBase> Tables { get; }

        /// <summary>
        /// Выбранная таблица
        /// </summary>
        public TableDocumentInBase SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (_selectedTable == value)
                    return;
                _selectedTable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFavorite));
                OnPropertyChanged(nameof(IsEnabledIsFavoriteChange));
                OnPropertyChanged(nameof(CanUseDynamicRowInsert));
                OnPropertyChanged(nameof(InoperativeVisibility));
                if (value != null)
                {
                    UserConfigFile.SetValue(LangItem, nameof(SelectedTable), value.TableStyleName, true);
                    _parentWindow.ZoomImage();
                }
            }
        }

        /// <summary>
        /// Отмечена ли таблица как избранная
        /// </summary>
        public bool IsFavorite => SelectedTable != null && _favoriteTableStyles.Contains(SelectedTable.TableStyleName);

        /// <summary>
        /// Доступно ли изменения свойства "Избранное"
        /// </summary>
        public bool IsEnabledIsFavoriteChange => SelectedTable != null;

        /// <summary>
        /// Видимость флага "Недействующий"
        /// </summary>
        public System.Windows.Visibility InoperativeVisibility
        {
            get
            {
                if (SelectedTable != null && SelectedTable.TableStyleName.EndsWith("_old"))
                    return System.Windows.Visibility.Visible;
                return System.Windows.Visibility.Hidden;
            }
        }

        /// <summary>
        /// Коллекция масштабов аннотаций в текущем документе
        /// </summary>
        public List<string> Scales { get; private set; }

        /// <summary>
        /// Выбранный масштаб аннотаций
        /// </summary>
        public string SelectedScale
        {
            get => _selectedScale;
            set
            {
                if (_selectedScale == value)
                    return;
                _selectedScale = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(SelectedScale), value, true);
            }
        }

        /// <summary>
        /// Высота строк для системных таблиц
        /// </summary>
        public double TextHeight
        {
            get => _textHeight;
            set
            {
                if (Math.Abs(_textHeight - value) < 0.1)
                    return;
                _textHeight = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(TextHeight), value.ToString(CultureInfo.InvariantCulture), true);
            }
        }

        /// <summary>
        /// Высота строк
        /// </summary>
        public int RowHeight
        {
            get => _rowHeight;
            set
            {
                if (_rowHeight == value)
                    return;
                _rowHeight = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(RowHeight), value.ToString(), true);
            }
        }

        /// <summary>
        /// Текстовые стили в текущем документе
        /// </summary>
        public List<string> TextStyles { get; private set; }

        /// <summary>
        /// Выбранный текстовый стиль
        /// </summary>
        public string SelectedTextStyle
        {
            get => _selectedTextStyle;
            set
            {
                if (_selectedTextStyle == value)
                    return;
                _selectedTextStyle = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(SelectedTextStyle), value, true);
            }
        }

        /// <summary>
        /// Слои в текущем документе
        /// </summary>
        public List<string> Layers { get; private set; }

        /// <summary>
        /// Выбранный слой
        /// </summary>
        public string SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                if (_selectedLayer == value)
                    return;
                _selectedLayer = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLayerLocked));
                UserConfigFile.SetValue(LangItem, nameof(SelectedLayer), value, true);
            }
        }

        /// <summary>
        /// Является ли выбранный слой заблокированным
        /// </summary>
        public bool IsLayerLocked =>
            Utils.IsLockedLayer(SelectedLayer == _current ? Utils.GetCurrentLayerName() : SelectedLayer);

        /// <summary>
        /// Использовать динамическую вставку строк при вставке пользовательских таблицы
        /// </summary>
        public bool UseDynamicRowInsertion
        {
            get => _useDynamicRowInsertion;
            set
            {
                if (_useDynamicRowInsertion == value)
                    return;
                _useDynamicRowInsertion = value;
                OnPropertyChanged();
                UserConfigFile.SetValue(LangItem, nameof(UseDynamicRowInsertion), value.ToString(), true);
            }
        }

        /// <summary>
        /// Возможность вставлять таблицу с динамическим добавлением строк
        /// </summary>
        public bool CanUseDynamicRowInsert => SelectedTabIndex != 0 || SelectedTable == null || SelectedTable.DynRow;

        /// <summary>
        /// Файл пользовательских таблиц
        /// </summary>
        public string UserTableFile
        {
            get => _userTableFile;
            set
            {
                if (_userTableFile == value)
                    return;
                _userTableFile = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanInsertUserTable));
                UserConfigFile.SetValue(LangItem, "file", value, true);
            }
        }

        /// <summary>
        /// Пользовательские таблицы из указанного файла <see cref="UserTableFile"/>
        /// </summary>
        public ObservableCollection<string> UserTableNames { get; }

        /// <summary>
        /// Выбранная таблица из файла
        /// </summary>
        public string SelectedUserTableName
        {
            get => _selectedUserTableName;
            set
            {
                if (_selectedUserTableName == value)
                    return;
                _selectedUserTableName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanInsertUserTable));
            }
        }

        /// <summary>
        /// Можно ли вставить пользовательскую таблицу
        /// </summary>
        public bool CanInsertUserTable => File.Exists(UserTableFile) && !string.IsNullOrEmpty(SelectedUserTableName);

        /// <summary>
        /// Изменение свойства "Избранное" для текущей таблицы
        /// </summary>
        public ICommand ChangeIsFavoriteForCurrentTableCommand => new RelayCommand<bool>(b =>
        {
            if (SelectedTable == null)
                return;

            if (b)
            {
                _favoriteTableStyles.Add(SelectedTable.TableStyleName);
            }
            else
            {
                if (_favoriteTableStyles.Contains(SelectedTable.TableStyleName))
                    _favoriteTableStyles.Remove(SelectedTable.TableStyleName);

                if (SelectedFilter == _favorite)
                {
                    Tables.Remove(SelectedTable);
                    SelectedTable = Tables.FirstOrDefault();
                }
            }

            OnPropertyChanged(nameof(IsFavorite));
        });

        /// <summary>
        /// Команда очистки файла пользовательских таблиц
        /// </summary>
        public ICommand ClearUserTableFileCommand => new RelayCommandWithoutParameter(() =>
        {
            UserTableNames.Clear();
            UserTableFile = string.Empty;
            UserConfigFile.SetValue(LangItem, "file", string.Empty, true);
        });

        /// <summary>
        /// Команда открытия файла пользовательских таблиц
        /// </summary>
        public ICommand OpenUserTableFileCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                var ofd = new System.Windows.Forms.OpenFileDialog
                {
                    Filter = Language.GetItem(LangItem, "h33") + " (*.DWG)|*.DWG",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = false,
                    ShowHelp = false,
                    Title = Language.GetItem(LangItem, "h34")
                };

                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    UserTableFile = ofd.FileName;
                    LoadTablesFromFile();
                }
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        });

        /// <summary>
        /// Команда вставки пользовательской таблицы
        /// </summary>
        public ICommand InsertTableCommand => new RelayCommandWithoutParameter(() =>
        {
            if (SelectedTabIndex == 0)
            {
                InsertTable();
            }
            else
            {
                InsertUserTable();
            }
        });

        /// <summary>
        /// Заполнить данные
        /// </summary>
        public void FillData()
        {
            try
            {
                var doc = AcApp.DocumentManager.MdiActiveDocument;
                var db = doc.Database;

                // Заполнение списка масштабов
                Scales = new List<string>();
                var ocm = db.ObjectContextManager;
                var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                foreach (var ans in occ.Cast<AnnotationScale>())
                {
                    Scales.Add(ans.Name);
                }

                OnPropertyChanged(nameof(Scales));

                // Начальное значение масштаба
                var cans = occ.CurrentContext as AnnotationScale;
                SelectedScale = cans != null ? cans.Name : Scales.FirstOrDefault();

                FillLayers();

                var currentTextStyle = FillTextStyles();

                LoadFromSettings(currentTextStyle);

                LoadTablesFromFile();

                // Распаковка файла с таблицами
                Utils.ExtractTablesDwg();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void LoadFromSettings(string currentTextStyle)
        {
            if (bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(TopLeftSystemInsertSnap)), out var b))
                _topLeftSystemInsertSnap = b;
            if (bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(TopRightSystemInsertSnap)), out b))
                _topRightSystemInsertSnap = b;
            if (bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(BottomLeftSystemInsertSnap)), out b))
                _bottomLeftSystemInsertSnap = b;
            if (bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(BottomRightSystemInsertSnap)), out b))
                _bottomRightSystemInsertSnap = b;

            if (bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(TopLeftUserInsertSnap)), out b))
                _topLeftUserInsertSnap = b;
            if (bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(TopRightUserInsertSnap)), out b))
                _topRightUserInsertSnap = b;
            if (bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(BottomLeftUserInsertSnap)), out b))
                _bottomLeftUserInsertSnap = b;
            if (bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(BottomRightUserInsertSnap)), out b))
                _bottomRightUserInsertSnap = b;

            if (bool.TryParse(UserConfigFile.GetValue(LangItem, nameof(UseDynamicRowInsertion)), out b))
                _useDynamicRowInsertion = b;

            _userTableFile = UserConfigFile.GetValue(LangItem, "file");

            var scale = UserConfigFile.GetValue(LangItem, nameof(SelectedScale));
            if (Scales.Contains(scale))
                SelectedScale = scale;

            if (double.TryParse(UserConfigFile.GetValue(LangItem, nameof(TextHeight)), out var d))
                TextHeight = d;

            if (int.TryParse(UserConfigFile.GetValue(LangItem, nameof(RowHeight)), out var i))
                RowHeight = i;

            var savedTextStyle = UserConfigFile.GetValue(LangItem, nameof(SelectedTextStyle));
            if (TextStyles.Contains(savedTextStyle))
                SelectedTextStyle = savedTextStyle;
            else if (!string.IsNullOrEmpty(currentTextStyle) && TextStyles.Contains(currentTextStyle))
                SelectedTextStyle = currentTextStyle;
            else
                SelectedTextStyle = TextStyles.FirstOrDefault();

            var savedLayer = UserConfigFile.GetValue(LangItem, nameof(SelectedLayer));
            SelectedLayer = Layers.Contains(savedLayer) ? savedLayer : Layers.FirstOrDefault();

            OnPropertyChanged(nameof(TopLeftSystemInsertSnap));
            OnPropertyChanged(nameof(TopRightSystemInsertSnap));
            OnPropertyChanged(nameof(BottomLeftSystemInsertSnap));
            OnPropertyChanged(nameof(BottomRightSystemInsertSnap));

            OnPropertyChanged(nameof(TopLeftUserInsertSnap));
            OnPropertyChanged(nameof(TopRightUserInsertSnap));
            OnPropertyChanged(nameof(BottomLeftUserInsertSnap));
            OnPropertyChanged(nameof(BottomRightUserInsertSnap));

            OnPropertyChanged(nameof(UserTableFile));
            OnPropertyChanged(nameof(UseDynamicRowInsertion));

            NormativeSourceIndex =
                int.TryParse(UserConfigFile.GetValue(LangItem, nameof(NormativeSourceIndex)), out i) ? i : 0;

            var savedFavorite = UserConfigFile.GetValue(LangItem, "FavoriteTableStyles");
            if (!string.IsNullOrEmpty(savedFavorite))
            {
                foreach (var s in savedFavorite.Split('$'))
                {
                    _favoriteTableStyles.Add(s);
                }
            }

            _favoriteTableStyles.CollectionChanged += (sender, args) =>
            {
                UserConfigFile.SetValue(LangItem, "FavoriteTableStyles", string.Join("$", _favoriteTableStyles), true);
            };
        }

        private string FillTextStyles()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            TextStyles = new List<string>();

            string currentTextStyle = null;

            using (var tr = doc.TransactionManager.StartOpenCloseTransaction())
            {
                var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

                foreach (var id in tst)
                {
                    var textStyleTableRecord = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (!string.IsNullOrEmpty(textStyleTableRecord.Name))
                    {
                        TextStyles.Add(textStyleTableRecord.Name);
                    }
                }

                var styleTableRecord = (TextStyleTableRecord)tr.GetObject(db.Textstyle, OpenMode.ForRead);
                if (TextStyles.Contains(styleTableRecord.Name))
                {
                    currentTextStyle = styleTableRecord.Name;
                }

                tr.Abort();
            }

            OnPropertyChanged(nameof(TextStyles));
            return currentTextStyle;
        }

        private void FillLayers()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            Layers = new List<string>
            {
                _current
            };

            using (doc.LockDocument())
            {
                using (var tr = db.TransactionManager.StartOpenCloseTransaction())
                {
                    var lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt != null)
                    {
                        foreach (var layerId in lt)
                        {
                            var layer = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                            if (layer != null && !string.IsNullOrEmpty(layer.Name))
                            {
                                Layers.Add(layer.Name);
                            }
                        }
                    }

                    tr.Abort();
                }
            }

            OnPropertyChanged(nameof(Layers));
        }

        private void InitTableBaseAndFillFilters()
        {
            try
            {
                var documentFor = "RU";
                switch (NormativeSourceIndex)
                {
                    case 0:
                        documentFor = "RU";
                        break;
                    case 1:
                        documentFor = "UA";
                        break;
                    case 2:
                        documentFor = "BY";
                        break;
                }

                _tablesBase = new TablesBase(documentFor);

                Filters.Clear();
                Filters.Add(_all);
                Filters.Add(_favorite);

                if (_tablesBase?.Tables != null)
                {
                    foreach (var tableDocumentInBase in _tablesBase.Tables)
                    {
                        if (!Filters.Contains(tableDocumentInBase.Document) &
                            !string.IsNullOrEmpty(tableDocumentInBase.Document))
                        {
                            Filters.Add(tableDocumentInBase.Document);
                        }
                    }
                }

                var savedFilter = UserConfigFile.GetValue(LangItem, nameof(SelectedFilter));
                SelectedFilter = Filters.Contains(savedFilter) ? savedFilter : Filters.FirstOrDefault();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void FillTablesByFilter()
        {
            try
            {
                Tables.Clear();

                if (_tablesBase?.Tables != null)
                {
                    if (SelectedFilter == _all)
                    {
                        foreach (var table in _tablesBase.Tables)
                        {
                            Tables.Add(table);
                        }
                    }
                    else if (SelectedFilter == _favorite)
                    {
                        foreach (var table in _tablesBase.Tables.Where(t => _favoriteTableStyles.Contains(t.TableStyleName)))
                        {
                            Tables.Add(table);
                        }
                    }
                    else
                    {
                        foreach (var table in _tablesBase.Tables.Where(x => x.Document.Equals(SelectedFilter)))
                        {
                            Tables.Add(table);
                        }
                    }

                    {
                        var savedTableStyle = UserConfigFile.GetValue(LangItem, nameof(SelectedTable));
                        var table = Tables.FirstOrDefault(t => t.TableStyleName == savedTableStyle);
                        SelectedTable = table ?? Tables.FirstOrDefault();
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void LoadTablesFromFile()
        {
            try
            {
                UserTableNames.Clear();

                if (!File.Exists(UserTableFile))
                {
                    UserTableFile = string.Empty;
                    return;
                }

                using (var sourceDb = new Database(false, true))
                {
                    sourceDb.ReadDwgFile(UserTableFile, FileOpenMode.OpenForReadAndAllShare, true, string.Empty);

                    using (var tr = sourceDb.TransactionManager.StartTransaction())
                    {
                        var sourceBtr = (BlockTableRecord)tr.GetObject(sourceDb.CurrentSpaceId, OpenMode.ForWrite, false);

                        foreach (var obj in sourceBtr)
                        {
                            var ent = (Entity)tr.GetObject(obj, OpenMode.ForRead);
                            if (!(ent is Table))
                                continue;

                            var table = (Table)tr.GetObject(obj, OpenMode.ForRead);
                            UserTableNames.Add(table.TableStyleName);
                        }

                        tr.Abort();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Equals("eNotImplementedYet"))
                {
                    MessageBox.Show(Language.GetItem(LangItem, "msg3"), MessageBoxIcon.Alert);
                }
                else
                {
                    ExceptionBox.Show(ex);
                }
            }
        }

        private void InsertTable()
        {
            if (SelectedTable == null)
                return;
            InsertSnap insertSnap;
            if (TopLeftSystemInsertSnap)
                insertSnap = InsertSnap.TopLeft;
            else if (TopRightSystemInsertSnap)
                insertSnap = InsertSnap.TopRight;
            else if (BottomLeftSystemInsertSnap)
                insertSnap = InsertSnap.BottomLeft;
            else
                insertSnap = InsertSnap.BottomRight;

            try
            {
                _parentWindow.Hide();
                InsertTable(insertSnap);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                AcApp.ShowModelessWindow(AcApp.MainWindow.Handle, _parentWindow);
            }
        }

        private void InsertUserTable()
        {
            if (string.IsNullOrEmpty(SelectedUserTableName))
                return;
            InsertSnap insertSnap;
            if (TopLeftUserInsertSnap)
                insertSnap = InsertSnap.TopLeft;
            else if (TopRightUserInsertSnap)
                insertSnap = InsertSnap.TopRight;
            else if (BottomLeftUserInsertSnap)
                insertSnap = InsertSnap.BottomLeft;
            else
                insertSnap = InsertSnap.BottomRight;

            try
            {
                _parentWindow.Hide();
                InsertTableFromDwg(insertSnap);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                AcApp.ShowModelessWindow(AcApp.MainWindow.Handle, _parentWindow);
            }
        }

        private void InsertTable(InsertSnap insertSnap)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var scale = Utils.GetScale(SelectedScale);
            Table table;

            // Блокируем документ
            using (doc.LockDocument())
            {
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    // Копируем таблицу из файла ресурсов
                    table = GetTableFromSource(tr);
                    if (table == null)
                    {
                        // Не удалось скопировать таблицу
                        MessageBox.Show(Language.GetItem(LangItem, "msg2"), MessageBoxIcon.Close);
                        return;
                    }

                    table.Layer = SelectedLayer == _current ? Utils.GetCurrentLayerName() : SelectedLayer;

                    // Масштабируем до перемещения для правильного отображения
                    var mat = Matrix3d.Scaling(scale, table.Position);
                    table.TransformBy(mat);
                    table.SuppressRegenerateTable(true);

                    // Перемещаем с джигой
                    var jig = new TableDrag();
                    var jigResult = jig.StartJig(table, insertSnap);
                    if (jigResult.Status != PromptStatus.OK)
                    {
                        table.Erase();
                        return;
                    }

                    table.Position = jig.TablePositionPoint();
                    doc.TransactionManager.QueueForGraphicsFlush();

                    table.SuppressRegenerateTable(false);

                    // Динамическая вставка строк
                    if (SelectedTable.DynRow && UseDynamicRowInsertion)
                    {
                        table.SuppressRegenerateTable(true);
                        table.DowngradeOpen();
                        var tableAddCellsJig = new TableAddCellsJig(
                            table.GeometricExtents.MaxPoint.Y - table.GeometricExtents.MinPoint.Y,
                            SelectedTable.DataRow, RowHeight * scale, table.Position);
                        tableAddCellsJig.StartJig(table);
                        table.SuppressRegenerateTable(false);
                    }

                    tr.Commit();
                }
            }

            SetProperties(table.Id, doc, scale);
        }

        private void SetProperties(ObjectId tableId, Document doc, double scale)
        {
            var db = doc.Database;
            using (doc.LockDocument())
            {
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    var table = (Table)tr.GetObject(tableId, OpenMode.ForWrite);
                    var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

                    if (SelectedTable.NameToHeader)
                    {
                        table.Cells[0, 0].TextString = SelectedTable.Name;
                    }

                    table.Cells.TextStyleId = tst[SelectedTextStyle];

                    // Установка высоты текста для ячеек типа "Заголовок" и "Название"
                    if (SelectedTable.DataRow == 0)
                    {
                        for (var i = 0; i < table.Rows.Count; i++)
                        {
                            var rowType = table.RowType(i);
                            if (rowType == RowType.DataRow)
                            {
                                continue;
                            }

                            if (rowType == RowType.TitleRow)
                            {
                                table.Rows[i].TextHeight = 5 * scale;
                            }

                            if (rowType == RowType.HeaderRow)
                            {
                                table.Rows[i].TextHeight = 3 * scale;
                            }
                        }
                    }

                    if (SelectedTable.DataRow > 0)
                    {
                        for (var i = 0; i < SelectedTable.DataRow; i++)
                        {
                            if (i == 0)
                            {
                                table.Rows[i].TextHeight = 5 * scale;
                            }
                            else
                            {
                                table.Rows[i].TextHeight = 3 * scale;
                            }
                        }

                        for (var i = SelectedTable.DataRow; i < table.Rows.Count; i++)
                        {
                            table.Rows[i].TextStyleId = tst[SelectedTextStyle];
                            table.Rows[i].Height = RowHeight * scale;
                            table.Rows[i].TextHeight = TextHeight * scale;

                            // Копирование свойств с предыдущей ячейки
                            if (i != SelectedTable.DataRow & SelectedTable.DynRow)
                            {
                                for (var j = 0; j < table.Columns.Count; j++)
                                {
                                    var isMerged = table.Cells[i, j].IsMerged;
                                    if (isMerged != null && !isMerged.Value)
                                    {
                                        table.Cells[i, j].Style = table.Cells[i - 1, j].Style;
                                        table.Cells[i, j].DataFormat = table.Cells[i - 1, j].DataFormat;
                                        if (table.Cells[i - 1, j].Alignment != null)
                                        {
                                            table.Cells[i, j].Alignment = table.Cells[i - 1, j].Alignment;
                                        }

                                        if (table.Cells[i - 1, j].TextStyleId != null)
                                        {
                                            table.Cells[i, j].TextStyleId = table.Cells[i - 1, j].TextStyleId;
                                        }

                                        if (table.Cells[i - 1, j].TextHeight != null)
                                        {
                                            table.Cells[i, j].TextHeight = table.Cells[i - 1, j].TextHeight;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    tr.Commit();
                }
            }
        }

        private Table GetTableFromSource(Transaction tr)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var sourceDb = new Database(false, true);
            try
            {
                Table tbl = null;

                Utils.ExtractTablesDwg();

                // Read the DWG into a side database
                sourceDb.ReadDwgFile(_tablesBase.DwgFileName, FileOpenMode.OpenForReadAndAllShare, true, string.Empty);

                var tblIds = new ObjectIdCollection();

                using (var myT = sourceDb.TransactionManager.StartTransaction())
                {
                    var sourceBtr =
                        (BlockTableRecord)myT.GetObject(sourceDb.CurrentSpaceId, OpenMode.ForWrite, false);

                    foreach (var obj in sourceBtr)
                    {
                        var ent = (Entity)myT.GetObject(obj, OpenMode.ForRead);
                        if (ent is Table table)
                        {
                            if (table.TableStyleName.Equals(SelectedTable.TableStyleName))
                            {
                                tblIds.Add(table.ObjectId);
                                var im = new IdMapping();
                                sourceDb.WblockCloneObjects(tblIds, db.CurrentSpaceId, im, DuplicateRecordCloning.Ignore, false);
                                
                                tbl = (Table)tr.GetObject(im.Lookup(table.ObjectId).Value, OpenMode.ForWrite, false, true);
                                break;
                            }
                        }
                    }

                    myT.Commit();
                }

                return tbl?.ObjectId == ObjectId.Null ? null : tbl;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return null;
            }
        }

        private void InsertTableFromDwg(InsertSnap insertSnap)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            // Блокируем документ
            using (doc.LockDocument())
            {
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    Table tbl;
                    using (var sourceDb = new Database(false, true))
                    {
                        sourceDb.ReadDwgFile(UserTableFile, FileOpenMode.OpenForReadAndAllShare, true, string.Empty);

                        var objIds = new ObjectIdCollection();
                        var tblIds = new ObjectIdCollection();

                        // Создаем пустую таблицу
                        tbl = new Table();

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
                                    var table = (Table)myT.GetObject(obj, OpenMode.ForWrite);
                                    if (table.TableStyleName.Equals(SelectedUserTableName))
                                    {
                                        tblIds.Add(table.ObjectId);
                                        var im = new IdMapping();
                                        sourceDb.WblockCloneObjects(tblIds, db.CurrentSpaceId, im, DuplicateRecordCloning.Replace, false);
                                        tbl = (Table)tr.GetObject(im.Lookup(table.ObjectId).Value, OpenMode.ForWrite, false, true);
                                        break;
                                    }
                                }
                            }

                            myT.Commit();
                        }
                    }

                    tbl.Layer = SelectedLayer == _current ? Utils.GetCurrentLayerName() : SelectedLayer;

                    var mat = Matrix3d.Scaling(Utils.GetScale(SelectedScale), tbl.Position);
                    tbl.TransformBy(mat);

                    // Перемещаем с джигой
                    var jig = new TableDrag();
                    var startJig = jig.StartJig(tbl, insertSnap);
                    if (startJig.Status != PromptStatus.OK)
                    {
                        tbl.Erase();
                        return;
                    }

                    tbl.Position = jig.TablePositionPoint();
                    doc.TransactionManager.QueueForGraphicsFlush();

                    if (UseDynamicRowInsertion)
                    {
                        tbl.DowngradeOpen();
                        var rowHeight = tbl.Rows[tbl.Rows.Count - 1].Height;

                        var tableAddCellsJig = new TableAddCellsJig(
                            tbl.GeometricExtents.MaxPoint.Y - tbl.GeometricExtents.MinPoint.Y,
                            2, rowHeight, tbl.Position);

                        tableAddCellsJig.StartJig(tbl);
                        ed.Regen();
                    }

                    tr.Commit();
                }
            }
        }

        private void AcAppOnSystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            if (e.Name == "CLAYER" && SelectedLayer == _current)
                OnPropertyChanged(nameof(IsLayerLocked));
        }
    }
}
