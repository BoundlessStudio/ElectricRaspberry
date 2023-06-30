
using System.Threading.Channels;

public interface ICommentTaskQueue
{
    ValueTask QueueAsync(PromptModel workItem);

    ValueTask<PromptModel> DequeueAsync(CancellationToken cancellationToken);
}

public sealed class CommentTaskQueue : ICommentTaskQueue
{
    private readonly Channel<PromptModel> _queue;

    public CommentTaskQueue(int capacity)
    {
        BoundedChannelOptions options = new(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<PromptModel>(options);
    }

    public async ValueTask QueueAsync(PromptModel workItem)
    {
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<PromptModel> DequeueAsync(CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);
        return workItem;
    }
}