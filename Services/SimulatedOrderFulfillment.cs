using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Builds immediate fake results for the simulated order cart.
/// Values look clinical but are invented for demo use only.
/// </summary>
public sealed record SimulatedOrderSignResult(int LabResultCount, int ImagingStudyCount);

public static class SimulatedOrderFulfillment
{
    public static List<LabResult> BuildLabResults(
        int patientId,
        string panelName,
        IReadOnlyList<string> testNames,
        DateTime when)
    {
        var results = new List<LabResult>(testNames.Count);
        foreach (var name in testNames)
        {
            var sample = SampleFor(name, patientId);
            results.Add(new LabResult
            {
                PatientId = patientId,
                TestName = name,
                PanelName = panelName,
                Value = sample.Value,
                Units = sample.Units,
                ReferenceRange = sample.Range,
                Status = LabResultStatus.Final,
                CollectedAt = when,
                ResultedAt = when,
                IsAbnormal = sample.Abnormal,
                IsCritical = sample.Critical
            });
        }

        return results;
    }

    public static ImagingStudy BuildImagingStudy(
        int patientId,
        SimulatedOrderSpec spec,
        string provider,
        DateTime when)
    {
        return new ImagingStudy
        {
            PatientId = patientId,
            StudyDate = when,
            Modality = spec.Modality ?? "Imaging",
            BodyPart = spec.BodyPart ?? string.Empty,
            Description = spec.Name,
            Impression = ImpressionFor(spec, patientId),
            OrderingProvider = provider,
            Radiologist = spec.Type == OrderType.Ekg ? "Simulated ECG Read" : "Simulated Radiology Read",
            Status = ImagingStatus.Completed
        };
    }

    public static string SummarizeLabs(IEnumerable<LabResult> results)
    {
        var list = results.ToList();
        if (list.Count == 0) return "No results";
        var flagged = list.Where(r => r.IsAbnormal || r.IsCritical).ToList();
        if (flagged.Count == 0)
            return $"{list.Count} result(s), all within range";

        return string.Join("; ", flagged.Select(r =>
            $"{r.TestName} {r.Value}{(r.IsCritical ? " critical" : " high/low")}"));
    }

    private static (string Value, string? Units, string? Range, bool Abnormal, bool Critical) SampleFor(
        string testName,
        int patientId)
    {
        var key = Normalize(testName);
        var variant = Math.Abs(HashCode.Combine(patientId, key)) % 5;

        return key switch
        {
            "glucose" or "fingerstick glucose" => variant == 0
                ? ("142", "mg/dL", "70-99", true, false)
                : ("92", "mg/dL", "70-99", false, false),
            "bun" => ("16", "mg/dL", "7-20", false, false),
            "creatinine" => variant == 1
                ? ("1.5", "mg/dL", "0.7-1.3", true, false)
                : ("0.9", "mg/dL", "0.7-1.3", false, false),
            "sodium" => ("139", "mEq/L", "136-145", false, false),
            "potassium" => variant == 0
                ? ("5.3", "mEq/L", "3.5-5.0", true, false)
                : ("4.2", "mEq/L", "3.5-5.0", false, false),
            "chloride" => ("102", "mEq/L", "98-107", false, false),
            "co2" => ("24", "mEq/L", "22-29", false, false),
            "calcium" => ("9.4", "mg/dL", "8.6-10.2", false, false),
            "total protein" => ("7.1", "g/dL", "6.4-8.3", false, false),
            "albumin" => ("4.2", "g/dL", "3.5-5.0", false, false),
            "bilirubin" or "total bilirubin" => ("0.6", "mg/dL", "0.1-1.2", false, false),
            "direct bilirubin" => ("0.2", "mg/dL", "0.0-0.3", false, false),
            "alt" => ("28", "U/L", "7-56", false, false),
            "ast" => ("24", "U/L", "10-40", false, false),
            "alkaline phosphatase" => ("72", "U/L", "44-147", false, false),
            "wbc" => ("6.8", "x10^3/uL", "4.5-11.0", false, false),
            "rbc" => ("4.7", "x10^6/uL", "4.2-5.4", false, false),
            "hemoglobin" => ("14.1", "g/dL", "12.0-16.0", false, false),
            "hematocrit" => ("42", "%", "36-46", false, false),
            "platelets" => ("248", "x10^3/uL", "150-400", false, false),
            "mcv" => ("90", "fL", "80-100", false, false),
            "mch" => ("30", "pg", "27-33", false, false),
            "mchc" => ("33", "g/dL", "32-36", false, false),
            "total cholesterol" => variant == 2
                ? ("212", "mg/dL", "<200", true, false)
                : ("178", "mg/dL", "<200", false, false),
            "hdl" => ("54", "mg/dL", ">40", false, false),
            "ldl" => variant == 2
                ? ("138", "mg/dL", "<100", true, false)
                : ("96", "mg/dL", "<100", false, false),
            "triglycerides" => ("122", "mg/dL", "<150", false, false),
            "hba1c" or "hemoglobin a1c" or "point-of-care a1c" => variant == 0
                ? ("6.4", "%", "<5.7", true, false)
                : ("5.5", "%", "<5.7", false, false),
            "tsh" => ("2.1", "uIU/mL", "0.4-4.0", false, false),
            "free t4" => ("1.2", "ng/dL", "0.8-1.8", false, false),
            "free t3" => ("3.1", "pg/mL", "2.3-4.2", false, false),
            "ph" => ("6.0", null, "5.0-8.0", false, false),
            "specific gravity" => ("1.015", null, "1.005-1.030", false, false),
            "protein" => ("Negative", null, "Negative", false, false),
            "ketones" => ("Negative", null, "Negative", false, false),
            "blood" => ("Negative", null, "Negative", false, false),
            "leukocyte esterase" => ("Negative", null, "Negative", false, false),
            "nitrites" => ("Negative", null, "Negative", false, false),
            "prothrombin time" => ("12.1", "sec", "11.0-13.5", false, false),
            "inr" or "point-of-care inr" => variant == 3
                ? ("2.4", null, "2.0-3.0 (therapeutic)", false, false)
                : ("1.0", null, "0.8-1.1", false, false),
            "amphetamines" or "barbiturates" or "benzodiazepines" or "cannabinoids"
                or "cocaine" or "opiates" or "pcp" => ("Negative", null, "Negative", false, false),
            "troponin" => ("<0.01", "ng/mL", "<0.04", false, false),
            "bnp" => ("42", "pg/mL", "<100", false, false),
            "rapid strep" => variant == 1 ? ("Positive", null, "Negative", true, false) : ("Negative", null, "Negative", false, false),
            "rapid flu" or "influenza a/b" => ("Negative", null, "Negative", false, false),
            "rapid covid" or "sars-cov-2" => ("Negative", null, "Negative", false, false),
            "urine hcg" or "hcg" => ("Negative", null, "Negative", false, false),
            "urine dipstick" => ("Normal", null, "Normal", false, false),
            _ => ("Normal", null, "See comment", false, false)
        };
    }

    private static string ImpressionFor(SimulatedOrderSpec spec, int patientId)
    {
        var modality = (spec.Modality ?? spec.Name).ToLowerInvariant();
        var variant = Math.Abs(HashCode.Combine(patientId, spec.Name)) % 3;

        if (modality.Contains("ekg") || modality.Contains("electrocardiogram") || modality.Contains("rhythm"))
        {
            return variant == 1
                ? "Sinus bradycardia at 56 bpm. Normal axis. No acute ST-T wave changes. Simulated read."
                : "Normal sinus rhythm. Rate 72. Normal axis. No acute ischemic changes. Simulated read.";
        }

        if (modality.Contains("echo"))
        {
            return "Normal LV size and systolic function. Estimated EF 60%. No significant valvular disease. Simulated read.";
        }

        if (modality.Contains("x-ray") && (spec.BodyPart?.Contains("Chest", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return "No acute cardiopulmonary process. Heart size normal. Simulated read.";
        }

        if (modality.Contains("ct"))
        {
            return "No acute finding. Recommend clinical correlation. Simulated read.";
        }

        if (modality.Contains("mri"))
        {
            return "No acute osseous or soft-tissue abnormality. Simulated read.";
        }

        if (modality.Contains("ultrasound"))
        {
            return "Study technically adequate. No acute abnormality identified. Simulated read.";
        }

        return $"{spec.Name}: no acute finding. Simulated read.";
    }

    private static string Normalize(string name) =>
        name.Trim().ToLowerInvariant()
            .Replace("(cbc)", "", StringComparison.Ordinal)
            .Replace("(cmp)", "", StringComparison.Ordinal)
            .Replace("(bmp)", "", StringComparison.Ordinal)
            .Replace("  ", " ");
}
