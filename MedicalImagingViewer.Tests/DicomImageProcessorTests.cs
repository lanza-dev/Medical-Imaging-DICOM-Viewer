using MedicalImagingViewer.Services;

namespace MedicalImagingViewer.Tests;

public class DicomImageProcessorTests
{
    [Fact]
    public void ConvertToModalityValues_AppliesSlopeAndIntercept()
    {
        // Arrange
        short[] storedPixels =
        {
            128,
            500,
            1024,
            2191
        };

        double slope = 1.0;
        double intercept = -1024.0;

        // Act
        double[] result =
            DicomImageProcessor.ConvertToModalityValues(
                storedPixels,
                slope,
                intercept);

        // Assert
        Assert.Equal(-896, result[0]);
        Assert.Equal(-524, result[1]);
        Assert.Equal(0, result[2]);
        Assert.Equal(1167, result[3]);
    }

    [Fact]
    public void ApplyWindowLevel_MapsBelowInsideAndAboveWindow()
    {
        // Arrange
        double[] huPixels =
        {
        -200,
        0,
        100,
        200,
        400
    };

        double windowCenter = 100;
        double windowWidth = 400;

        // Window:
        // -100 through 300

        // Act
        byte[] result =
            DicomImageProcessor.ApplyWindowLevel(
                huPixels,
                windowCenter,
                windowWidth);

        // Assert
        Assert.Equal(0, result[0]);       // below window
        Assert.True(result[1] > 0);       // inside window
        Assert.True(result[2] > result[1]);
        Assert.True(result[3] > result[2]);
        Assert.Equal(255, result[4]);     // above window
    }

    [Fact]
    public void ApplyWindowLevel_ThrowsWhenWindowWidthIsZero()
    {
        double[] pixels = { 0, 100, 200 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DicomImageProcessor.ApplyWindowLevel(
                pixels,
                100,
                0));
    }
}