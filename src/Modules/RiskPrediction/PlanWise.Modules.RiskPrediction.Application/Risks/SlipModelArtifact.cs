using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlanWise.Modules.RiskPrediction.Application.Risks;

// The fitted model, as produced by research/sprint-slip-model/export_model.py and embedded in this
// assembly. A logistic regression over five features is small enough to ship as its coefficients,
// which is why there is no ONNX runtime or native inference library anywhere in this solution.
//
// The choice of a linear model was measured, not assumed: on raw feature values logistic regression
// scores ROC AUC 0.510 (a coin flip) against gradient boosting's 0.569, but the relationships in the
// data are monotonic and sharply non-linear, and applying log1p first lifts the linear model to
// 0.588 — ahead of boosting on the same features. A linear model also yields exact per-feature
// attributions, which the explanation drawer needs and which a boosted model could only supply
// through TreeSHAP.
public sealed record SlipModelArtifact(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("modelName")] string ModelName,
    [property: JsonPropertyName("provenance")] SlipModelProvenance Provenance,
    [property: JsonPropertyName("features")] IReadOnlyList<SlipModelFeature> Features,
    [property: JsonPropertyName("intercept")] double Intercept,
    [property: JsonPropertyName("transform")] string Transform,
    [property: JsonPropertyName("calibration")] SlipModelCalibration Calibration)
{
    private const string ResourceName =
        "PlanWise.Modules.RiskPrediction.Application.Risks.Model.sprint-slip-model.json";

    private const int SupportedSchemaVersion = 1;
    private const string SupportedTransform = "log1p";

    private static readonly Lazy<SlipModelArtifact> Embedded = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    public static SlipModelArtifact Instance => Embedded.Value;

    private static SlipModelArtifact Load()
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"The trained slip model '{ResourceName}' is not embedded in this assembly.");
        }

        SlipModelArtifact artifact = JsonSerializer.Deserialize<SlipModelArtifact>(stream)
            ?? throw new InvalidOperationException($"The trained slip model '{ResourceName}' is empty.");

        // A newer exporter could change the maths (a different transform, an isotonic calibration
        // curve) while keeping the same field names. Failing loudly at startup beats serving
        // confident nonsense, so both the schema and the transform are checked rather than assumed.
        if (artifact.SchemaVersion != SupportedSchemaVersion)
        {
            throw new InvalidOperationException(
                $"The trained slip model is schema version {artifact.SchemaVersion}, but this build only understands version {SupportedSchemaVersion}.");
        }

        if (!string.Equals(artifact.Transform, SupportedTransform, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The trained slip model uses the '{artifact.Transform}' transform, but this build only implements '{SupportedTransform}'.");
        }

        return artifact;
    }
}

public sealed record SlipModelProvenance(
    [property: JsonPropertyName("dataset")] string Dataset,
    [property: JsonPropertyName("rows")] int Rows,
    [property: JsonPropertyName("projects")] int Projects,
    [property: JsonPropertyName("sprints")] int Sprints,
    [property: JsonPropertyName("baseRate")] double BaseRate,
    [property: JsonPropertyName("trainingWindowDays")] int TrainingWindowDays);

/// <param name="Median">Substituted when the project cannot supply the feature at all.</param>
/// <param name="Mean">Of the log1p-transformed column, not the raw one — the exporter standardises after transforming.</param>
public sealed record SlipModelFeature(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("median")] double Median,
    [property: JsonPropertyName("mean")] double Mean,
    [property: JsonPropertyName("scale")] double Scale,
    [property: JsonPropertyName("coefficient")] double Coefficient);

/// <summary>
/// Platt scaling over the model's own decision function: p = 1 / (1 + exp(a·f + b)). The sign
/// convention is scikit-learn's, where <paramref name="A"/> is normally negative.
/// </summary>
public sealed record SlipModelCalibration(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("a")] double A,
    [property: JsonPropertyName("b")] double B);
