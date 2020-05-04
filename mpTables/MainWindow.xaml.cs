namespace mpTables
{
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>
    /// <see cref="MainWindow"/> code behind
    /// </summary>
    public partial class MainWindow
    {
        private const string LangItem = "mpTables";
        private Point _origin;
        private Point _start;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(LangItem, "h1");

            // Zooming and panning image
            MouseWheel += MainWindow_MouseWheel;
            PreviewTableImage.MouseDown += PreviewTableImageMouseDown;
            PreviewTableImage.MouseUp += PreviewTableImageMouseUp;
            PreviewTableImage.MouseMove += PreviewTableImageMouseMove;
        }

        /// <summary>
        /// Вписать изображение
        /// </summary>
        public void ZoomImage()
        {
            var m = PreviewTableImage.RenderTransform.Value;
            m.SetIdentity();
            PreviewTableImage.RenderTransform = new MatrixTransform(m);
        }
        
        #region Zooming and panning image
        
        private void PreviewTableImageMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                if (PreviewTableImage.IsMouseCaptured)
                {
                    return;
                }

                Cursor = Cursors.Hand;
                PreviewTableImage.CaptureMouse();

                _start = e.GetPosition(PreviewImageBorder);
                _origin.X = PreviewTableImage.RenderTransform.Value.OffsetX;
                _origin.Y = PreviewTableImage.RenderTransform.Value.OffsetY;
            }
        }

        private void PreviewTableImageMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Released)
            {
                PreviewTableImage.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
            }
        }

        private void PreviewTableImageMouseMove(object sender, MouseEventArgs e)
        {
            if (!PreviewTableImage.IsMouseCaptured)
            {
                return;
            }

            var p = e.MouseDevice.GetPosition(PreviewImageBorder);

            var m = PreviewTableImage.RenderTransform.Value;
            m.OffsetX = _origin.X + (p.X - _start.X);
            m.OffsetY = _origin.Y + (p.Y - _start.Y);

            PreviewTableImage.RenderTransform = new MatrixTransform(m);
        }

        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var p = e.MouseDevice.GetPosition(PreviewTableImage);

            var m = PreviewTableImage.RenderTransform.Value;
            if (e.Delta > 0)
            {
                m.ScaleAtPrepend(1.2, 1.2, p.X, p.Y);
            }
            else
            {
                m.ScaleAtPrepend(1 / 1.2, 1 / 1.2, p.X, p.Y);
            }

            PreviewTableImage.RenderTransform = new MatrixTransform(m);
        }

        private void BtImageSmall_OnClick(object sender, RoutedEventArgs e)
        {
            var m = PreviewTableImage.RenderTransform.Value;
            m.ScalePrepend(1 / 1.2, 1 / 1.2);
            PreviewTableImage.RenderTransform = new MatrixTransform(m);
        }

        private void BtImageBig_OnClick(object sender, RoutedEventArgs e)
        {
            var m = PreviewTableImage.RenderTransform.Value;
            m.ScalePrepend(1.2, 1.2);
            PreviewTableImage.RenderTransform = new MatrixTransform(m);
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
    }
}