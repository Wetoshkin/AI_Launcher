namespace Launcher.Models.HuggingFace;

public static class ModelChoiceScorer
{
    public static ModelChoiceScore Score(HuggingFaceModelSummary model)
    {
        var score = 0;
        var reasons = new List<string>();

        if (model.Downloads >= 1_000_000)
        {
            score += 35;
            reasons.Add("HF popularity");
        }
        else if (model.Downloads >= 100_000)
        {
            score += 20;
            reasons.Add("moderate downloads");
        }

        if (model.Likes >= 500)
        {
            score += 20;
            reasons.Add("many likes");
        }

        if (model.IsCompatibleWithCurrentGpu)
        {
            score += 20;
            reasons.Add("fits current GPU");
        }

        if (model.HasPreferredQuant)
        {
            score += 15;
            reasons.Add("preferred quant available");
        }

        if (model.IsRuntimeCompatible)
        {
            score += 15;
            reasons.Add("runtime compatible");
        }

        return new ModelChoiceScore(Math.Min(100, score), reasons);
    }
}
