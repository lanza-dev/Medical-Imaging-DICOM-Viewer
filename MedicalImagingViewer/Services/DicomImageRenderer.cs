using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MedicalImagingViewer.Services;

public static class DicomImageRenderer
{
    public static BitmapSource Render(
        double[] huPixels,
        int rows,
        int columns,
        double windowCenter,
        double windowWidth)
    {
        ArgumentNullException.ThrowIfNull(huPixels);

        byte[] grayscalePixels =
            DicomImageProcessor.ApplyWindowLevel(
                huPixels,
                windowCenter,
                windowWidth);

        int stride = columns;

        return BitmapSource.Create(
            columns,
            rows,
            96,
            96,
            PixelFormats.Gray8,
            null,
            grayscalePixels,
            stride);
    }
}