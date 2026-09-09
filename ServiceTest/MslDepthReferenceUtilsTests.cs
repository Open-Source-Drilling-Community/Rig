using OSDC.Drilling.Rig.WebPages.Shared;

namespace OSDC.Drilling.Rig.ServiceTest;

public class MslDepthReferenceUtilsTests
{
    [Test]
    public void ToMeanSeaLevelDepthReference_UsesOppositeOfDatumWgs84Depth()
    {
        const double drillFloorWgs84Depth = -85.646;
        const double meanSeaLevelWgs84Depth = -43.446;

        double? reference = MslDepthReferenceUtils.ToMeanSeaLevelDepthReference(
            meanSeaLevelWgs84Depth);

        Assert.That(reference, Is.EqualTo(43.446).Within(1e-9));
        Assert.That(drillFloorWgs84Depth + reference, Is.EqualTo(-42.2).Within(1e-9));
    }

    [Test]
    public void ToMeanSeaLevelDepthReference_PreservesMissingDatum()
    {
        Assert.That(MslDepthReferenceUtils.ToMeanSeaLevelDepthReference(null), Is.Null);
    }
}
