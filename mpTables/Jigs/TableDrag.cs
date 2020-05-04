namespace mpTables.Jigs
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.GraphicsInterface;
    using Models;
    using ModPlusAPI;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    public class TableDrag : DrawJig
    {
        private const string LangItem = "mpTables";
        private Point3d _prevPoint; 
        private Point3d _currentPoint;
        private Table _table;
        private InsertSnap _insertSnap;

        public PromptResult StartJig(Table table, InsertSnap insertSnap)
        {
            _table = table;
            _insertSnap = insertSnap;
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
                switch (_insertSnap)
                {
                    case InsertSnap.TopLeft:
                        mInsertPt = _currentPoint;
                        break;
                    case InsertSnap.TopRight:
                        mInsertPt = new Point3d(_currentPoint.X - _table.Width, _currentPoint.Y, _currentPoint.Z);
                        break;
                    case InsertSnap.BottomLeft:
                        mInsertPt = new Point3d(_currentPoint.X, _currentPoint.Y + _table.Height, _currentPoint.Z);
                        break;
                    case InsertSnap.BottomRight:
                        mInsertPt = new Point3d(_currentPoint.X - _table.Width, _currentPoint.Y + _table.Height, _currentPoint.Z);
                        break;
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
