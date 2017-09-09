#if ac2010
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
#elif ac2013
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
using ModPlus;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.DatabaseServices;

namespace mpTables
{
    // jig
    public class TableAddCellsJig : DrawJig
    {
        private Point3d _prevPoint; // Предыдущая точка
        private Point3d _currPoint; // Нинешняя точка
        private Line _line;

        private Table _tb;
        public double TbH; // первоночальная высота таблицы
        public Point3d FPt; // точка вставки таблицы
        public int StopRows; // количество строк, меньше которого не удалять строки
        public double RowH;

        readonly Document _doc = AcApp.DocumentManager.MdiActiveDocument;

        public PromptResult StartJig(Table table // таблица            
            )
        {
            _tb = table;
            _prevPoint = new Point3d(0, 0, 0);
            _line = new Line { StartPoint = MpCadHelpers.UcsToWcs(FPt) };
            return AcApp.DocumentManager.MdiActiveDocument.Editor.Drag(this);
        }// public AcEd.PromptResult StartJig(string str)
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var jppo = new JigPromptPointOptions("\nУкажите вторую точку: ")
            {
                BasePoint = _line.StartPoint,
                UseBasePoint = true,
                UserInputControls = (UserInputControls.Accept3dCoordinates
                | UserInputControls.NoZeroResponseAccepted
                | UserInputControls.AcceptOtherInputString
                | UserInputControls.NoNegativeResponseAccepted)
            };
            var rs = prompts.AcquirePoint(jppo);
            _currPoint = rs.Value;
            if (rs.Status != PromptStatus.OK) return SamplerStatus.Cancel;
            if (CursorHasMoved())
            {
                _line.EndPoint = _currPoint;
                _prevPoint = _currPoint;
                return SamplerStatus.OK;
            }
            return SamplerStatus.NoChange;
        }
        private bool CursorHasMoved()
        {
            return _currPoint.DistanceTo(_prevPoint) > 1e-16;
        }
        protected override bool WorldDraw(WorldDraw draw)
        {
            var oldCmdEcho = AcApp.GetSystemVariable("CMDECHO");
            AcApp.SetSystemVariable("CMDECHO", 0);

            draw.Geometry.Draw(_line);

            _tb.UpgradeOpen();
            // Длина по вертикали
            var lenVer = _line.StartPoint.Y - _line.EndPoint.Y;

            if (lenVer > TbH)
                _tb.InsertRows(_tb.Rows.Count, RowH, 1);
            if (lenVer < TbH)
                if (_tb.Rows.Count - 1 > StopRows)
                    _tb.DeleteRows(_tb.Rows.Count - 1, 1);

            draw.Geometry.Draw(_tb);
            TbH = _tb.GeometricExtents.MaxPoint.Y - _tb.GeometricExtents.MinPoint.Y;

            AcApp.SetSystemVariable("CMDECHO", oldCmdEcho);
            return true;
        }
    }

    public class TableDrag : DrawJig
    {
        private Point3d _prevPoint; // Предыдущая точка
        private Point3d _currPoint; // Нинешняя точка
        private Table _table;
        private string _pointAligin;

        public PromptResult StartJig(Table table, string ptAlign)
        {
            _table = table;
            _pointAligin = ptAlign;
            _prevPoint = new Point3d(0, 0, 0);

            return AcApp.DocumentManager.MdiActiveDocument.Editor.Drag(this);
        }

        public Point3d TablePositionPoint()
        {
            return _table.Position;
        }
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var jppo = new JigPromptPointOptions("\nТочка вставки: ")
            {
                UserInputControls = (UserInputControls.Accept3dCoordinates
                                         | UserInputControls.NoZeroResponseAccepted
                                         | UserInputControls.AcceptOtherInputString
                                         | UserInputControls.NoNegativeResponseAccepted)
            };
            var rs = prompts.AcquirePoint(jppo);
            _currPoint = rs.Value;
            if (rs.Status != PromptStatus.OK) return SamplerStatus.Cancel;
            if (CursorHasMoved())
            {
                _prevPoint = _currPoint;
                return SamplerStatus.OK;
            }
            return SamplerStatus.NoChange;
        }
        private bool CursorHasMoved()
        {
            return _currPoint.DistanceTo(_prevPoint) > 1e-3;
        }
        protected override bool WorldDraw(WorldDraw draw)
        {
            try
            {
                var mInsertPt = _currPoint;
                if (_pointAligin.Equals("TopLeft")) mInsertPt = _currPoint;
                if (_pointAligin.Equals("TopRight"))
                    mInsertPt = new Point3d(_currPoint.X - _table.Width, _currPoint.Y, _currPoint.Z);
                if (_pointAligin.Equals("BottomLeft"))
                    mInsertPt = new Point3d(_currPoint.X, _currPoint.Y + _table.Height, _currPoint.Z);
                if (_pointAligin.Equals("BottomRight"))
                    mInsertPt = new Point3d(_currPoint.X - _table.Width, _currPoint.Y + _table.Height, _currPoint.Z);

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
