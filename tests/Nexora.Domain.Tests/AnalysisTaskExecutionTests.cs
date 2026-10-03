using Nexora.Domain.Entities;

namespace Nexora.Domain.Tests;

public sealed class AnalysisTaskExecutionTests
{
    [Fact]
    public void Interrupted_task_can_be_requeued_and_completed()
    {
        var task = new AnalysisTask(Guid.NewGuid(), AnalysisTask.MediaAnalysisType, 0, null, null);
        task.Queue();
        task.BeginProcessing();
        var firstAttempt = new AnalysisTaskAttempt(task.Id, 1, "worker-a");
        firstAttempt.Finish(AnalysisTaskAttempt.Failed, "worker_interrupted", "Worker stopped.");

        task.RequeueAfterFailure();
        Assert.Equal(AnalysisTask.Queued, task.Status);
        task.BeginProcessing();
        var secondAttempt = new AnalysisTaskAttempt(task.Id, 2, "worker-b");
        secondAttempt.Finish(AnalysisTaskAttempt.Succeeded);
        task.Complete(true);

        Assert.Equal(AnalysisTask.Succeeded, task.Status);
        Assert.Equal(AnalysisTaskAttempt.Failed, firstAttempt.Status);
        Assert.Equal(AnalysisTaskAttempt.Succeeded, secondAttempt.Status);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public void Cancellation_during_processing_finishes_as_cancelled()
    {
        var task = new AnalysisTask(Guid.NewGuid(), AnalysisTask.MediaAnalysisType, 0, null, null);
        task.Queue();
        task.BeginProcessing();
        task.RequestCancel();
        task.MarkCancelled();

        Assert.Equal(AnalysisTask.Cancelled, task.Status);
        Assert.NotNull(task.CancelRequestedAt);
    }
}
