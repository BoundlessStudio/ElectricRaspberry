using System.ClientModel.Primitives;

public class ExponentialRetryPipelinePolicy : PipelinePolicy
{
  private readonly int _maxRetryCount;
  private readonly TimeSpan _initialDelay;
  private readonly TimeSpan _maxDelay;

  public ExponentialRetryPipelinePolicy(int maxRetryCount, TimeSpan initialDelay, TimeSpan maxDelay)
  {
    _maxRetryCount = maxRetryCount;
    _initialDelay = initialDelay;
    _maxDelay = maxDelay;
  }


  public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
  {
    for (int retry = 0; retry < _maxRetryCount; retry++)
    {
      try
      {
        if (currentIndex < pipeline.Count - 1)
        {
          pipeline[currentIndex + 1].Process(message, pipeline, currentIndex + 1);
        }
        return;
      }
      catch (Exception)
      {
        if (retry == _maxRetryCount - 1)
        {
          throw;
        }

        var delay = TimeSpan.FromSeconds(Math.Min(_initialDelay.TotalSeconds * Math.Pow(2, retry), _maxDelay.TotalSeconds));
        Task.Delay(delay).Wait();
      }
    }
  }

  public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
  {
    for (int retry = 0; retry < _maxRetryCount; retry++)
    {
      try
      {
        if (currentIndex < pipeline.Count - 1)
        {
          await pipeline[currentIndex + 1].ProcessAsync(message, pipeline, currentIndex + 1);
        }
        return;
      }
      catch (Exception)
      {
        if (retry == _maxRetryCount - 1)
        {
          throw;
        }

        var delay = TimeSpan.FromSeconds(Math.Min(_initialDelay.TotalSeconds * Math.Pow(2, retry), _maxDelay.TotalSeconds));
        await Task.Delay(delay);
      }
    }
  }
}