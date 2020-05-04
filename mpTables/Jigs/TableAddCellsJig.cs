namespace mpTables.Jigs
{
    using Autodesk.AutoCAD.ApplicationServices.Core;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.GraphicsInterface;
    using ModPlusAPI;

    /// <summary>
    /// Jig for interactive adding cells to table
    /// </summary>
    public class TableAddCellsJig : DrawJig
    {
        private const string LangItem = "mpTables";
        private Point3d _prevPoint; 
        private Point3d _currentPoint; 
        private Line _line;
        private double _originTableHeight; // первоначальная высота таблицы
        private readonly Point3d _tablePositionPoint; // точка вставки таблицы
        private readonly int _stopRows; // количество строк, меньше которого не удалять строки
        private readonly double _rowHeight;
        private Table _tb;

        public TableAddCellsJig(double originTableHeight, int stopRows, double rowHeight, Point3d tablePositionPoint)
        {
            _originTableHeight = originTableHeight;
            _stopRows = stopRows;
            _rowHeight = rowHeight;
            _tablePositionPoint = tablePositionPoint;
        }

        public PromptResult StartJig(Table table)
        {
            _tb = table;
            _prevPoint = new Point3d(0, 0, 0);
            _line = new Line
            {
                StartPoint = ModPlus.Helpers.AutocadHelpers.UcsToWcs(_tablePositionPoint)
            };
            return Application.DocumentManager.MdiActiveDocument.Editor.Drag(this);
        }

        /// <inheritdoc />
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var jigPromptPointOptions = new JigPromptPointOptions("\n" + Language.GetItem(LangItem, "msg5") + ": ")
            {
                BasePoint = _line.StartPoint,
                UseBasePoint = true,
                UserInputControls = UserInputControls.Accept3dCoordinates
                                    | UserInputControls.NoZeroResponseAccepted
                                    | UserInputControls.AcceptOtherInputString
                                    | UserInputControls.NoNegativeResponseAccepted
            };
            var rs = prompts.AcquirePoint(jigPromptPointOptions);
            _currentPoint = rs.Value;
            if (rs.Status != PromptStatus.OK)
            {
                return SamplerStatus.Cancel;
            }

            if (CursorHasMoved())
            {
                _line.EndPoint = _currentPoint;
                _prevPoint = _currentPoint;
                return SamplerStatus.OK;
            }

            return SamplerStatus.NoChange;
        }

        private bool CursorHasMoved()
        {
            return _currentPoint.DistanceTo(_prevPoint) > 1e-16;
        }

        /// <inheritdoc />
        protected override bool WorldDraw(WorldDraw draw)
        {
            var oldCmdEcho = Application.GetSystemVariable("CMDECHO");
            Application.SetSystemVariable("CMDECHO", 0);

            draw.Geometry.Draw(_line);

            _tb.UpgradeOpen();

            // Длина по вертикали
            var lenVer = _line.StartPoint.Y - _line.EndPoint.Y;

            if (lenVer > _originTableHeight)
            {
                _tb.InsertRows(_tb.Rows.Count, _rowHeight, 1);
            }

            if (lenVer < _originTableHeight)
            {
                if (_tb.Rows.Count - 1 > _stopRows)
                {
                    _tb.DeleteRows(_tb.Rows.Count - 1, 1);
                }
            }

            draw.Geometry.Draw(_tb);
            _originTableHeight = _tb.GeometricExtents.MaxPoint.Y - _tb.GeometricExtents.MinPoint.Y;

            Application.SetSystemVariable("CMDECHO", oldCmdEcho);
            return true;
        }
    }
}