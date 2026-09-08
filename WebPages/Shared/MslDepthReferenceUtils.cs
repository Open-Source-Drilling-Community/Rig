using System.Net.Http.Json;
using System.Text.Json;
using RigModel = OSDC.Drilling.Rig.ModelShared;

namespace OSDC.Drilling.Rig.WebPages.Shared;

public static class MslDepthReferenceUtils
{
    public static Task<double?> ResolveMeanSeaLevelDepthReferenceAsync(IRigAPIUtils api, RigModel.Cluster? cluster)
    {
        return CalculateMeanSeaLevelDepthReferenceAsync(
            api,
            cluster?.ReferencePoint?.Latitude,
            cluster?.ReferencePoint?.Longitude);
    }

    private static async Task<double?> CalculateMeanSeaLevelDepthReferenceAsync(IRigAPIUtils api, double? latitude, double? longitude)
    {
        if (latitude == null || longitude == null)
        {
            return null;
        }

        using HttpClient httpClient = api.CreateHttpClient(api.HostNameVerticalDatum, api.HostBasePathVerticalDatum);
        RigModel.Client client = new(httpClient.BaseAddress!.ToString(), httpClient);
        RigModel.MeanSeaLevelToWgs84Request request = new()
        {
            Positions =
            [
                new RigModel.EarthVerticalDatumPosition
                {
                    Latitude = latitude.Value,
                    Longitude = longitude.Value,
                    MeanSeaLevelDepth = 0
                }
            ]
        };
        RigModel.MeanSeaLevelToWgs84Response response =
            await client.ConvertMeanSeaLevelToWgs84Async(request);
        return response.Samples?.FirstOrDefault()?.Wgs84EllipsoidalDepth;
    }
}
