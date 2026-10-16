namespace Shared.Application.Pipelines;

internal sealed class CommandExecutionState
{
    public object? RetryCommand { get; set; }
    public bool TransactionActive { get; set; }
    public bool CommitAttempted { get; set; }
    public bool RollbackFailed { get; set; }
}
