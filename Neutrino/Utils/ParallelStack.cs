using System.Threading.Channels;

namespace Neutrino.Utils;

public class ParallelStack<T>
{
    public Channel<T> SharedChannel = Channel.CreateUnbounded<T>();
    public int WaitingThreads { get; private set; }
    public CancellationToken Token => _cts.Token;
    public int QueuedSize { get; private set; }
    private int _threads;
    private int _threadId;
    private int _stackCapacity;
    private Stack<T>[] _threadLocal;
    private CancellationTokenSource _cts;
    public bool IsClosed { get; private set; } = false;

    public ParallelStack(int threads, int stackCapacity, CancellationToken ct)
    {
        _threads = threads;
        _stackCapacity = stackCapacity;
        _threadLocal = new Stack<T>[threads];
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, new CancellationToken());
    }

    public StackAccessor AssignThread()
    {
        if (_threadId >= _threads)
        {
            throw new InvalidOperationException(
                "The number of assigned threads exceeds the parallel stack thread count.");
        }
        int cid = _threadId++;
        _threadLocal[cid] = new Stack<T>(_stackCapacity);
        return new StackAccessor(this, cid);
    }

    public struct StackAccessor
    {

        internal StackAccessor(ParallelStack<T> stack, int threadId)
        {
            Stack = stack;
            ThreadId = threadId;
            stack._cts.Token.Register(() => { stack.IsClosed = true; });
        }

        public ParallelStack<T> Stack { get; }

        public int ThreadId { get; }

        public ValueTask<T> Pop()
        {
            if (Stack._threadLocal[ThreadId].Count == 0)
            {
                ValueTask<T> readTask;
                bool hasWaited = false;
                lock (Stack)
                {
                    if (Stack.SharedChannel.Reader.Count == 0)
                    {
                        hasWaited = true;
                        Stack.WaitingThreads++;
                        if (Stack.WaitingThreads >= Stack._threads)
                        {
                            Stack._cts.Cancel();
                        }
                    }
                }

                readTask = Stack.SharedChannel.Reader.ReadAsync(Stack._cts.Token);

                async ValueTask<T> QueueRead(ParallelStack<T> inst)
                {
                    var v = await readTask;
                    if (hasWaited)
                    {
                        inst.WaitingThreads--;
                    }

                    inst.QueuedSize--;
                    return v;
                }

                return QueueRead(Stack);
            }

            Stack.QueuedSize--;
            return ValueTask.FromResult(Stack._threadLocal[ThreadId].Pop());
        }

        public ValueTask Push(T value)
        {
            if (Stack._threadLocal[ThreadId].Count >= Stack._stackCapacity)
            {
                var writeTask = Stack.SharedChannel.Writer.WriteAsync(value, Stack._cts.Token);
                async ValueTask QueueWrite(ParallelStack<T> inst)
                {
                    await writeTask;
                    inst.QueuedSize++;
                }
                return QueueWrite(Stack);
            }
            Stack.QueuedSize++;
            Stack._threadLocal[ThreadId].Push(value);
            return ValueTask.CompletedTask;
        }

    }
}