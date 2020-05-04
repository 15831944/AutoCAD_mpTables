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
        private MainWindow _mainWindow;

        /// <summary>
        /// Start command
        /// </summary>
        [CommandMethod("ModPlus", "mpTables", CommandFlags.Modal)]
        public void Main()
        {
            Statistic.SendCommandStarting(new ModPlusConnector());
            
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
                var context = new MainViewModel(_mainWindow);
                context.FillData();
                _mainWindow.DataContext = context;
                _mainWindow.Closed += (sender, args) => _mainWindow = null;
            }

            if (_mainWindow.IsLoaded)
            {
                _mainWindow.Activate();
            }
            else
            {
                AcApp.ShowModelessWindow(AcApp.MainWindow.Handle, _mainWindow);
            }
        }
    }
}
