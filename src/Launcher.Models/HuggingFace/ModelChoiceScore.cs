namespace Launcher.Models.HuggingFace;

public sealed record ModelChoiceScore(int Value, IReadOnlyList<string> Reasons);
