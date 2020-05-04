namespace mpTables.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;
    using Models;

    /// <summary>
    /// Конвертирует текущую выбранную таблицу в источник данных для изображения
    /// </summary>
    public class SelectedTablePreviewImageConverter : IValueConverter
    {
        private static readonly BitmapImage NoPreviewImage;

        static SelectedTablePreviewImageConverter()
        {
            NoPreviewImage = new BitmapImage(
                new Uri(
                    $@"pack://application:,,,/mpTables_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/Images/NoImage.png",
                    UriKind.Absolute));
        }

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is TableDocumentInBase table)
                {
                    return new BitmapImage(
                        new Uri(
                            $@"pack://application:,,,/mpTables_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/Images/{table.NormativeDocumentIndex}/{table.Img}.png",
                            UriKind.Absolute));
                }
            }
            catch
            {
                // ignore
            }

            return NoPreviewImage;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
