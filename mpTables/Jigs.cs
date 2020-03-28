namespace mpTables
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.GraphicsInterface;
    using ModPlusAPI;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    /// <summary>
    /// Jig for interactive adding cells to table
    /// </summary>
    public class TableAddCellsJig : DrawJig
    {
        private const string LangItem = "mpTables";
        private Point3d _prevPoint; 
        private Point3d _currentPoint; 
        private Line _line;

        private Table _tb;
        public double TbH; // первоначальная высота таблицы
        public Point3d FPt; // точка вставки таблицы
        public int StopRows; // количество строк, меньше которого не удалять строки
        public double RowH;
        
        public PromptResult StartJig(Table table)
        {
            _tb = table;
            _prevPoint = new Point3d(0, 0, 0);
            _line = new Line { StartPoint = ModPlus.Helpers.AutocadHelpers.UcsToWcs(FPt) };
            return AcApp.DocumentManager.MdiActiveDocument.Editor.Drag(this);
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
            var oldCmdEcho = AcApp.GetSystemVariable("CMDECHO");
            AcApp.SetSystemVariable("CMDECHO", 0);

            draw.Geometry.Draw(_line);

            _tb.UpgradeOpen();

            // Длина по вертикали
            var lenVer = _line.StartPoint.Y - _line.EndPoint.Y;

            if (lenVer > TbH)
            {
                _tb.InsertRows(_tb.Rows.Count, RowH, 1);
            }

            if (lenVer < TbH)
            {
                if (_tb.Rows.Count - 1 > StopRows)
                {
                    _tb.DeleteRows(_tb.Rows.Count - 1, 1);
                }
            }

            draw.Geometry.Draw(_tb);
            TbH = _tb.GeometricExtents.MaxPoint.Y - _tb.GeometricExtents.MinPoint.Y;

            AcApp.SetSystemVariable("CMDECHO", oldCmdEcho);
            return true;
        }
    }

    public class TableDrag : DrawJig
    {
        private const string LangItem = "mpTables";
        private Point3d _prevPoint; 
        private Point3d _currentPoint;
        private Table _table;
        private string _pointAlign;

        public PromptResult StartJig(Table table, string ptAlign)
        {
            _table = table;
            _pointAlign = ptAlign;
            _prevPoint = new Point3d(0, 0, 0);

            return AcApp.DocumentManager.MdiActiveDocument.Editor.Drag(this);
        }

        public Point3d TablePositionPoint()
        {
            return _table.Position;
        }

        /// <inheritdoc />
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var jigPromptPointOptions = new JigPromptPointOptions("\n" + Language.GetItem(LangItem, "msg4") + ": ")
            {
                UserInputControls = 
                    UserInputControls.Accept3dCoordinates |
                    UserInputControls.NoZeroResponseAccepted |
                    UserInputControls.AcceptOtherInputString |
                    UserInputControls.NoNegativeResponseAccepted
            };
            var rs = prompts.AcquirePoint(jigPromptPointOptions);
            _currentPoint = rs.Value;
            if (rs.Status != PromptStatus.OK)
            {
                return SamplerStatus.Cancel;
            }

            if (CursorHasMoved())
            {
                _prevPoint = _currentPoint;
                return SamplerStatus.OK;
            }

            return SamplerStatus.NoChange;
        }

        private bool CursorHasMoved()
        {
            return _currentPoint.DistanceTo(_prevPoint) > 1e-3;
        }

        /// <inheritdoc />
        protected override bool WorldDraw(WorldDraw draw)
        {
            try
            {
                var mInsertPt = _currentPoint;
                if (_pointAlign.Equals("TopLeft"))
                {
                    mInsertPt = _currentPoint;
                }

                if (_pointAlign.Equals("TopRight"))
                {
                    mInsertPt = new Point3d(_currentPoint.X - _table.Width, _currentPoint.Y, _currentPoint.Z);
                }

                if (_pointAlign.Equals("BottomLeft"))
                {
                    mInsertPt = new Point3d(_currentPoint.X, _currentPoint.Y + _table.Height, _currentPoint.Z);
                }

                if (_pointAlign.Equals("BottomRight"))
                {
                    mInsertPt = new Point3d(_currentPoint.X - _table.Width, _currentPoint.Y + _table.Height, _currentPoint.Z);
                }

                _table.Position = mInsertPt;

                return draw.Geometry.Draw(_table);
            }
            catch
            {
                return false;
            }
        }
    }
}
