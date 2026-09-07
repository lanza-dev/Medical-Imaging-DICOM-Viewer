using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;

namespace MedicalImagingViewer.Services;

public static class DicomNetworkService
{
    public static async Task<DicomStatus?> EchoAsync(
        string host,
        int port,
        string callingAeTitle,
        string calledAeTitle)
    {
        DicomStatus? responseStatus = null;

        var client = DicomClientFactory.Create(
            host,
            port,
            false,
            callingAeTitle,
            calledAeTitle);

        var request = new DicomCEchoRequest();

        request.OnResponseReceived = (echoRequest, echoResponse) =>
        {
            responseStatus = echoResponse.Status;
        };

        await client.AddRequestAsync(request);
        await client.SendAsync();

        return responseStatus;
    }

    public static async Task<List<DicomDataset>> FindStudiesAsync(
        string host,
        int port,
        string callingAeTitle,
        string calledAeTitle)
    {
        var studies = new List<DicomDataset>();

        var client = DicomClientFactory.Create(
            host,
            port,
            false,
            callingAeTitle,
            calledAeTitle);

        var request =
            DicomCFindRequest.CreateStudyQuery();

        request.OnResponseReceived = (findRequest, findResponse) =>
        {
            if (findResponse.Status == DicomStatus.Pending &&
                findResponse.Dataset != null)
            {
                studies.Add(findResponse.Dataset);
            }
        };

        await client.AddRequestAsync(request);
        await client.SendAsync();

        return studies;
    }

    public static async Task<DicomStatus?> StoreAsync(
        string host,
        int port,
        string callingAeTitle,
        string calledAeTitle,
        string filePath)
    {
        DicomStatus? responseStatus = null;

        var client = DicomClientFactory.Create(
            host,
            port,
            false,
            callingAeTitle,
            calledAeTitle);

        var request =
            new DicomCStoreRequest(filePath);

        request.OnResponseReceived =
            (storeRequest, storeResponse) =>
            {
                responseStatus = storeResponse.Status;
            };

        await client.AddRequestAsync(request);
        await client.SendAsync();

        return responseStatus;
    }
        public static async Task<DicomStatus?> MoveStudyAsync(
    string host,
    int port,
    string callingAeTitle,
    string calledAeTitle,
    string destinationAeTitle,
    string studyInstanceUid)
    {
        DicomStatus? responseStatus = null;

        var client = DicomClientFactory.Create(
            host,
            port,
            false,
            callingAeTitle,
            calledAeTitle);

        var request =
            new DicomCMoveRequest(
                destinationAeTitle,
                studyInstanceUid);

        request.OnResponseReceived =
            (moveRequest, moveResponse) =>
            {
                responseStatus = moveResponse.Status;
            };

        await client.AddRequestAsync(request);
        await client.SendAsync();

        return responseStatus;
    }
}