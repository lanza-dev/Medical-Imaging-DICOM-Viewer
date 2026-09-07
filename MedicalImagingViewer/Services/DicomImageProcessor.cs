namespace MedicalImagingViewer.Services;

public static class DicomImageProcessor
{
    public static double[] ConvertToModalityValues(
        short[] storedPixels,
        double rescaleSlope,
        double rescaleIntercept)
    {
        ArgumentNullException.ThrowIfNull(storedPixels);

        double[] modalityPixels =
            new double[storedPixels.Length];

        for (int i = 0; i < storedPixels.Length; i++)
        {
            modalityPixels[i] =
                storedPixels[i] * rescaleSlope
                + rescaleIntercept;
        }

        return modalityPixels;
    }

    public static byte[] ApplyWindowLevel(
        double[] modalityPixels,
        double windowCenter,
        double windowWidth)
    {
        ArgumentNullException.ThrowIfNull(modalityPixels);

        if (windowWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(windowWidth),
                "Window Width must be greater than zero.");
        }

        byte[] grayscalePixels =
            new byte[modalityPixels.Length];

        double windowMin =
            windowCenter - windowWidth / 2.0;

        double windowMax =
            windowCenter + windowWidth / 2.0;

        for (int i = 0; i < modalityPixels.Length; i++)
        {
            double value = modalityPixels[i];

            if (value <= windowMin)
            {
                grayscalePixels[i] = 0;
            }
            else if (value >= windowMax)
            {
                grayscalePixels[i] = 255;
            }
            else
            {
                double normalized =
                    (value - windowMin) /
                    (windowMax - windowMin);

                grayscalePixels[i] =
                    (byte)(normalized * 255.0);
            }
        }

        return grayscalePixels;
    }
}