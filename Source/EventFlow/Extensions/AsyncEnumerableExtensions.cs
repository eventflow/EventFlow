using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Extensions
{
    public static class AsyncEnumerableExtensions
    {
        // No existing overload taking a Func<..., Task>
        public static async Task ForEachAwaitAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, Task> action,
            CancellationToken cancellationToken)
        {
            var index = 0;
            var e = source.GetEnumerator();

            try
            {
                while (true)
                {
                    if (await e.MoveNext(cancellationToken).ConfigureAwait(false))
                    {
                        await action(e.Current, checked(index++)).ConfigureAwait(false);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            finally
            {
                e?.Dispose();
            }
        }
    }
}
