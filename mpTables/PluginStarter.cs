namespace mpTables
{
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    /// <summary>
    /// Main command class
    /// </summary>
    public class PluginStarter
    {
        // Вызов функции
        private MpTables _mpTables;

        /// <summary>
        /// Start command
        /// </summary>
        [CommandMethod("ModPlus", "mpTables", CommandFlags.Modal)]
        public void Main()
        {
            Statistic.SendCommandStarting(new ModPlusConnector());
            
            if (_mpTables == null)
            {
                _mpTables = new MpTables();
                _mpTables.Closed += (sender, args) => _mpTables = null;
            }

            if (_mpTables.IsLoaded)
            {
                _mpTables.Activate();
            }
            else
            {
                AcApp.ShowModelessWindow(AcApp.MainWindow.Handle, _mpTables);
            }
        }
    }
}
