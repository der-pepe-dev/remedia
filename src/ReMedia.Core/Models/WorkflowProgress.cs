namespace ReMedia.Core.Models;

/// <summary>
/// Reports progress from a multi-step workflow operation.
/// Used by <see cref="IProgress{WorkflowProgress}"/> to drive
/// progress bars and status text in UI hosts.
/// </summary>
public sealed record WorkflowProgress(
    string StepName,
    int CurrentStep,
    int TotalSteps)
{
    public double Percentage => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;
}
