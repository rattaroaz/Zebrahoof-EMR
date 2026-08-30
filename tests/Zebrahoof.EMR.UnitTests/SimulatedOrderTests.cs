using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class SimulatedOrderTests
{
    [Fact]
    public void BuildLabResults_CreatesFinalValuesForEachTest()
    {
        var when = new DateTime(2026, 8, 28, 10, 0, 0);
        var results = SimulatedOrderFulfillment.BuildLabResults(
            patientId: 1,
            panelName: "Complete Blood Count (CBC)",
            testNames: ["WBC", "Hemoglobin", "Platelets"],
            when);

        Assert.Equal(3, results.Count);
        Assert.All(results, r =>
        {
            Assert.Equal(1, r.PatientId);
            Assert.Equal("Complete Blood Count (CBC)", r.PanelName);
            Assert.Equal(LabResultStatus.Final, r.Status);
            Assert.Equal(when, r.CollectedAt);
            Assert.False(string.IsNullOrWhiteSpace(r.Value));
        });
    }

    [Fact]
    public void BuildImagingStudy_MarksCompletedWithImpression()
    {
        var spec = new SimulatedOrderSpec
        {
            Type = OrderType.Ekg,
            Name = "12-Lead Electrocardiogram",
            Modality = "EKG",
            BodyPart = "Heart"
        };

        var study = SimulatedOrderFulfillment.BuildImagingStudy(3, spec, "Dr. Sarah Smith", DateTime.Today);

        Assert.Equal(ImagingStatus.Completed, study.Status);
        Assert.Equal("EKG", study.Modality);
        Assert.Contains("Simulated", study.Impression, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Dr. Sarah Smith", study.OrderingProvider);
    }

    [Fact]
    public async Task SignSimulatedCart_PostsLabAndImagingResults()
    {
        var clinical = new MockClinicalDataService();
        var cart = new List<OrderCartItem>
        {
            new()
            {
                Type = OrderType.Lab,
                DisplayName = "Complete Blood Count (CBC)",
                Priority = "STAT",
                OrderData = new SimulatedOrderSpec
                {
                    Type = OrderType.Lab,
                    Name = "Complete Blood Count (CBC)",
                    Category = "Hematology",
                    Destination = "Labs",
                    IncludedTests = ["WBC", "Hemoglobin"]
                }
            },
            new()
            {
                Type = OrderType.Ekg,
                DisplayName = "12-Lead Electrocardiogram",
                Priority = "Routine",
                OrderData = new SimulatedOrderSpec
                {
                    Type = OrderType.Ekg,
                    Name = "12-Lead Electrocardiogram",
                    Category = "EKG",
                    Destination = "Imaging",
                    Modality = "EKG",
                    BodyPart = "Heart"
                }
            },
            new()
            {
                Type = OrderType.Procedure,
                DisplayName = "Rapid Strep",
                Priority = "Routine",
                OrderData = new SimulatedOrderSpec
                {
                    Type = OrderType.Procedure,
                    Name = "Rapid Strep",
                    Category = "Point of Care",
                    Destination = "Labs",
                    IncludedTests = ["Rapid Strep"]
                }
            }
        };

        var changed = 0;
        clinical.PatientDataChanged += _ => changed++;

        var result = await clinical.SignSimulatedCartAsync(1, "John Doe", "Dr. Sarah Smith", cart);

        Assert.Equal(3, result.LabResultCount);
        Assert.Equal(1, result.ImagingStudyCount);
        Assert.Equal(1, changed);

        var labs = await clinical.GetLabResultsByPatientAsync(1);
        Assert.Contains(labs, l => l.TestName == "WBC" && l.Status == LabResultStatus.Final);
        Assert.Contains(labs, l => l.PanelName == "Point of Care" && l.TestName == "Rapid Strep");

        var imaging = await clinical.GetImagingByPatientAsync(1);
        Assert.Contains(imaging, i => i.Modality == "EKG" && i.Status == ImagingStatus.Completed);

        var orders = await clinical.GetSimulatedOrdersByPatientAsync(1);
        Assert.Contains(orders, o => o.Type == OrderType.Lab && o.Status == OrderStatus.Completed);
        Assert.Contains(orders, o => o.Type == OrderType.Ekg);
        Assert.Contains(orders, o => o.Type == OrderType.Procedure);
    }

    [Fact]
    public void Catalog_IncludesLabsImagingEkgEchoAndPoc()
    {
        var clinical = new MockClinicalDataService();
        var all = clinical.GetSimulatedOrderCatalog();

        Assert.Contains(all, c => c.Type == OrderType.Lab && c.Name.Contains("CBC"));
        Assert.Contains(all, c => c.Modality == "X-Ray");
        Assert.Contains(all, c => c.Type == OrderType.Ekg);
        Assert.Contains(all, c => c.Name.Contains("Echocardiogram"));
        Assert.Contains(all, c => c.Type == OrderType.Procedure && c.Name.Contains("Glucose"));

        var ekgOnly = clinical.GetSimulatedOrderCatalog(OrderCatalogFilter.Ekg);
        Assert.All(ekgOnly, c => Assert.Equal(OrderType.Ekg, c.Type));
    }
}
