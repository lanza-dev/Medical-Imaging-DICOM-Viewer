using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FellowOakDicom.Imaging;
using System.Linq;

using FellowOakDicom;
using Microsoft.Win32;

using MedicalImagingViewer.Services;
using FellowOakDicom.Network;

namespace MedicalImagingViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    private double[]? _huPixels;
    private int _imageRows;
    private int _imageColumns;
    private IDicomServer? _dicomStoreServer;
    public MainWindow()
    {
        InitializeComponent();

        StartDicomStoreScp();
    }

    private async void OpenDicomFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open DICOM File",
            Filter = "DICOM files (*.dcm)|*.dcm|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var dicomFile = await DicomFile.OpenAsync(dialog.FileName);

            var dataset = dicomFile.Dataset;

            PatientText.Text =
                dataset.GetSingleValueOrDefault(
                    DicomTag.PatientName,
                    "Unknown");

            StudyUidText.Text =
                dataset.GetSingleValueOrDefault(
                    DicomTag.StudyInstanceUID,
                    "Unknown");

            SeriesUidText.Text =
                dataset.GetSingleValueOrDefault(
                    DicomTag.SeriesInstanceUID,
                    "Unknown");

            ModalityText.Text =
                dataset.GetSingleValueOrDefault(
                    DicomTag.Modality,
                    "Unknown");

            var rows =
                dataset.GetSingleValueOrDefault(
                    DicomTag.Rows,
                    0);

            var columns =
                dataset.GetSingleValueOrDefault(
                    DicomTag.Columns,
                    0);

            DimensionsText.Text = $"{columns} x {rows}";            

            if (dataset.TryGetSingleValue(DicomTag.WindowCenter, out double windowCenter))
            {
                WindowCenterText.Text = windowCenter.ToString();
            }
            else
            {
                WindowCenterText.Text = "Not present";
            }

            if (dataset.TryGetSingleValue(DicomTag.WindowWidth, out double windowWidth))
            {
                WindowWidthText.Text = windowWidth.ToString();
            }
            else
            {
                WindowWidthText.Text = "Not present";
            }

            var bitsAllocated =
    dataset.GetSingleValueOrDefault(DicomTag.BitsAllocated, (ushort)0);

            var bitsStored =
                dataset.GetSingleValueOrDefault(DicomTag.BitsStored, (ushort)0);

            var highBit =
                dataset.GetSingleValueOrDefault(DicomTag.HighBit, (ushort)0);

            var pixelRepresentation =
                dataset.GetSingleValueOrDefault(DicomTag.PixelRepresentation, (ushort)0);

            var photometricInterpretation =
                dataset.GetSingleValueOrDefault(
                    DicomTag.PhotometricInterpretation, "Unknown");

            var rescaleSlope =
                dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);

            var rescaleIntercept =
                dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);

            BitsAllocatedText.Text = bitsAllocated.ToString();
            BitsStoredText.Text = bitsStored.ToString();
            HighBitText.Text = highBit.ToString();

            PixelRepresentationText.Text =
                pixelRepresentation == 0 ? "Unsigned" : "Signed";

            PhotometricInterpretationText.Text =
                photometricInterpretation;

            RescaleSlopeText.Text = rescaleSlope.ToString();
            RescaleInterceptText.Text = rescaleIntercept.ToString();                                 

            var pixelData = DicomPixelData.Create(dataset);

            var frame = pixelData.GetFrame(0);
            var pixelBytes = frame.Data;

            var pixelCount = rows * columns;

            short[] storedPixels = new short[pixelCount];

            Buffer.BlockCopy(
                pixelBytes,
                0,
                storedPixels,
                0,
                pixelCount * sizeof(short));            

            double[] huPixels =
            DicomImageProcessor.ConvertToModalityValues(
            storedPixels,
            rescaleSlope,
            rescaleIntercept);

            double minHu = huPixels.Min();
            double maxHu = huPixels.Max();

            _huPixels = huPixels;
            _imageRows = rows;
            _imageColumns = columns;

            double displayWindowCenter;
            double displayWindowWidth;

            if (dataset.TryGetSingleValue(
                    DicomTag.WindowCenter,
                    out double dicomWindowCenter) &&
                dataset.TryGetSingleValue(
                    DicomTag.WindowWidth,
                    out double dicomWindowWidth) &&
                dicomWindowWidth > 0)
            {
                displayWindowCenter = dicomWindowCenter;
                displayWindowWidth = dicomWindowWidth;
            }
            else
            {
                // Fallback for images such as CT_small.dcm that do not
                // provide Window Center / Window Width.
                displayWindowCenter = (minHu + maxHu) / 2.0;
                displayWindowWidth = maxHu - minHu;
            }

            WindowCenterInput.Text =
    displayWindowCenter.ToString("F1");

            WindowWidthInput.Text =
                displayWindowWidth.ToString("F1");

            byte[] grayscalePixels = new byte[huPixels.Length];

            double windowMin =
                displayWindowCenter - displayWindowWidth / 2.0;

            double windowMax =
                displayWindowCenter + displayWindowWidth / 2.0;

            for (int i = 0; i < huPixels.Length; i++)
            {
                double hu = huPixels[i];

                if (hu <= windowMin)
                {
                    grayscalePixels[i] = 0;
                }
                else if (hu >= windowMax)
                {
                    grayscalePixels[i] = 255;
                }
                else
                {
                    double normalized =
                        (hu - windowMin) / (windowMax - windowMin);

                    grayscalePixels[i] =
                        (byte)(normalized * 255.0);
                }
            }

            int stride = columns;

            BitmapSource bitmap = BitmapSource.Create(
                columns,
                rows,
                96,
                96,
                PixelFormats.Gray8,
                null,
                grayscalePixels,
                stride);

            DicomImage.Source = bitmap;            
        }

        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Unable to open DICOM file",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RenderImage(
    double windowCenter,
    double windowWidth)
    {
        if (_huPixels == null)
            return;

        DicomImage.Source =
            DicomImageRenderer.Render(
                _huPixels,
                _imageRows,
                _imageColumns,
                windowCenter,
                windowWidth);
    }
    private void ApplyWindow_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (_huPixels == null)
        {
            MessageBox.Show(
                "Open a DICOM image first.",
                "Window / Level");
            return;
        }

        if (!double.TryParse(
                WindowCenterInput.Text,
                out double windowCenter))
        {
            MessageBox.Show(
                "Enter a valid Window Center.",
                "Window / Level");
            return;
        }

        if (!double.TryParse(
                WindowWidthInput.Text,
                out double windowWidth) ||
            windowWidth <= 0)
        {
            MessageBox.Show(
                "Window Width must be greater than zero.",
                "Window / Level");
            return;
        }

        RenderImage(windowCenter, windowWidth);
    }

    private async void EchoPacs_Click(
    object sender,
    RoutedEventArgs e)
    {
        string host = PacsHostInput.Text.Trim();
        string callingAe = CallingAeInput.Text.Trim();
        string calledAe = CalledAeInput.Text.Trim();

        if (!int.TryParse(
                PacsPortInput.Text,
                out int port))
        {
            MessageBox.Show(
                "Enter a valid PACS port.",
                "C-ECHO");
            return;
        }

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(callingAe) ||
            string.IsNullOrWhiteSpace(calledAe))
        {
            MessageBox.Show(
                "Host and AE Titles are required.",
                "C-ECHO");
            return;
        }

        try
        {
            var status =
                await DicomNetworkService.EchoAsync(
                    host,
                    port,
                    callingAe,
                    calledAe);

            if (status == null)
            {
                MessageBox.Show(
                    "No C-ECHO response was received.",
                    "C-ECHO");
                return;
            }

            MessageBox.Show(
                $"C-ECHO response: {status}",
                "DICOM Verification");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "C-ECHO failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void FindStudies_Click(
    object sender,
    RoutedEventArgs e)
    {
        string host = PacsHostInput.Text.Trim();
        string callingAe = CallingAeInput.Text.Trim();
        string calledAe = CalledAeInput.Text.Trim();

        if (!int.TryParse(
                PacsPortInput.Text,
                out int port))
        {
            MessageBox.Show(
                "Enter a valid PACS port.",
                "C-FIND");
            return;
        }

        try
        {
            var studies =
                await DicomNetworkService.FindStudiesAsync(
                    host,
                    port,
                    callingAe,
                    calledAe);

            if (studies.Count == 0)
            {
                MessageBox.Show(
                    "C-FIND completed successfully, but no studies were returned.",
                    "C-FIND");
                return;
            }

            var lines = new List<string>();

            foreach (var study in studies)
            {
                string patientName =
                    study.GetSingleValueOrDefault(
                        DicomTag.PatientName,
                        "Unknown");

                string patientId =
                    study.GetSingleValueOrDefault(
                        DicomTag.PatientID,
                        "Unknown");

                string studyDate =
                    study.GetSingleValueOrDefault(
                        DicomTag.StudyDate,
                        "Unknown");

                string studyUid =
                    study.GetSingleValueOrDefault(
                        DicomTag.StudyInstanceUID,
                        "Unknown");

                string modalities =
                    study.GetSingleValueOrDefault(
                        DicomTag.ModalitiesInStudy,
                        "Unknown");

                lines.Add(
                    $"Patient: {patientName}\n" +
                    $"Patient ID: {patientId}\n" +
                    $"Study Date: {studyDate}\n" +
                    $"Modality: {modalities}\n" +
                    $"Study UID: {studyUid}");
            }

            MessageBox.Show(
                string.Join(
                    "\n\n--------------------\n\n",
                    lines),
                $"C-FIND Results ({studies.Count} study)");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "C-FIND failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void StoreDicom_Click(
    object sender,
    RoutedEventArgs e)
    {
        string host = PacsHostInput.Text.Trim();
        string callingAe = CallingAeInput.Text.Trim();
        string calledAe = CalledAeInput.Text.Trim();

        if (!int.TryParse(
                PacsPortInput.Text,
                out int port))
        {
            MessageBox.Show(
                "Enter a valid PACS port.",
                "C-STORE");
            return;
        }

        var dialog =
            new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select DICOM File to Send",
                Filter =
                    "DICOM files (*.dcm)|*.dcm|All files (*.*)|*.*"
            };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var status =
                await DicomNetworkService.StoreAsync(
                    host,
                    port,
                    callingAe,
                    calledAe,
                    dialog.FileName);

            if (status == null)
            {
                MessageBox.Show(
                    "No C-STORE response was received.",
                    "C-STORE");
                return;
            }

            MessageBox.Show(
                $"C-STORE response: {status}",
                "DICOM Storage");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "C-STORE failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private void StartDicomStoreScp()
    {
        if (_dicomStoreServer != null)
            return;

        _dicomStoreServer =
            DicomServerFactory.Create<DicomStoreScp>(
                11112);
    }

    private async void MoveStudy_Click(
    object sender,
    RoutedEventArgs e)
    {
        string host = PacsHostInput.Text.Trim();
        string callingAe = CallingAeInput.Text.Trim();
        string calledAe = CalledAeInput.Text.Trim();

        if (!int.TryParse(
                PacsPortInput.Text,
                out int port))
        {
            MessageBox.Show(
                "Enter a valid PACS port.",
                "C-MOVE");
            return;
        }

        if (string.IsNullOrWhiteSpace(StudyUidText.Text))
        {
            MessageBox.Show(
                "Open a DICOM file first so the Study UID is available.",
                "C-MOVE");
            return;
        }

        string studyInstanceUid =
            StudyUidText.Text.Trim();

        try
        {
            var status =
                await DicomNetworkService.MoveStudyAsync(
                    host,
                    port,
                    callingAe,
                    calledAe,
                    "MEDVIEWER",
                    studyInstanceUid);

            if (status == null)
            {
                MessageBox.Show(
                    "No C-MOVE response was received.",
                    "C-MOVE");
                return;
            }

            MessageBox.Show(
                $"C-MOVE response: {status}",
                "DICOM Retrieve");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "C-MOVE failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}