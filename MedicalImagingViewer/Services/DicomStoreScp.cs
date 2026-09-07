using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;

namespace MedicalImagingViewer.Services;

public class DicomStoreScp :
    DicomService,
    IDicomServiceProvider,
    IDicomCStoreProvider
{
    private static readonly DicomTransferSyntax[] AcceptedTransferSyntaxes =
    {
        DicomTransferSyntax.ExplicitVRLittleEndian,
        DicomTransferSyntax.ExplicitVRBigEndian,
        DicomTransferSyntax.ImplicitVRLittleEndian
    };

    private static readonly DicomTransferSyntax[] AcceptedImageTransferSyntaxes =
    {
        DicomTransferSyntax.JPEGLSLossless,
        DicomTransferSyntax.JPEG2000Lossless,
        DicomTransferSyntax.JPEGProcess14SV1,
        DicomTransferSyntax.JPEGProcess14,
        DicomTransferSyntax.RLELossless,

        DicomTransferSyntax.JPEGLSNearLossless,
        DicomTransferSyntax.JPEG2000Lossy,
        DicomTransferSyntax.JPEGProcess1,
        DicomTransferSyntax.JPEGProcess2_4,

        DicomTransferSyntax.ExplicitVRLittleEndian,
        DicomTransferSyntax.ExplicitVRBigEndian,
        DicomTransferSyntax.ImplicitVRLittleEndian
    };

    public DicomStoreScp(
        INetworkStream stream,
        Encoding fallbackEncoding,
        ILogger logger,
        DicomServiceDependencies dependencies)
        : base(
            stream,
            fallbackEncoding,
            logger,
            dependencies)
    {
    }

    public Task OnReceiveAssociationRequestAsync(
        DicomAssociation association)
    {
        if (association.CalledAE != "MEDVIEWER")
        {
            return SendAssociationRejectAsync(
                DicomRejectResult.Permanent,
                DicomRejectSource.ServiceUser,
                DicomRejectReason.CalledAENotRecognized);
        }

        foreach (var pc in association.PresentationContexts)
        {
            if (pc.AbstractSyntax ==
                DicomUID.Verification)
            {
                pc.AcceptTransferSyntaxes(
                    AcceptedTransferSyntaxes);
            }
            else if (
                pc.AbstractSyntax.StorageCategory !=
                DicomStorageCategory.None)
            {
                pc.AcceptTransferSyntaxes(
                    AcceptedImageTransferSyntaxes);
            }
        }

        return SendAssociationAcceptAsync(
            association);
    }

    public Task OnReceiveAssociationReleaseRequestAsync()
    {
        return SendAssociationReleaseResponseAsync();
    }

    public void OnReceiveAbort(
        DicomAbortSource source,
        DicomAbortReason reason)
    {
    }

    public void OnConnectionClosed(
        Exception exception)
    {
    }

    public async Task<DicomCStoreResponse>
        OnCStoreRequestAsync(
            DicomCStoreRequest request)
    {
        string receiveFolder =
            Path.Combine(
                AppContext.BaseDirectory,
                "ReceivedDicom");

        Directory.CreateDirectory(
            receiveFolder);

        string sopInstanceUid =
            request.SOPInstanceUID.UID;

        string filePath =
            Path.Combine(
                receiveFolder,
                $"{sopInstanceUid}.dcm");

        await request.File.SaveAsync(
            filePath);

        return new DicomCStoreResponse(
            request,
            DicomStatus.Success);
    }

    public Task OnCStoreRequestExceptionAsync(
        string tempFileName,
        Exception e)
    {
        return Task.CompletedTask;
    }
}