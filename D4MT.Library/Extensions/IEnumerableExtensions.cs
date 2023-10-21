using System.Runtime.CompilerServices;

namespace D4MT.Library.Extensions;

public static class IEnumerableExtensions {
    public static IEnumerable<Task<TReturn>> SelectAsync<T1, TReturn>(
        this IEnumerable<T1> enumerable,
        Func<T1, CancellationToken, Task<TReturn>> selectFn,
        CancellationToken cancellationToken
    ) {
        foreach (T1 item in enumerable) {
            yield return selectFn(item, cancellationToken);
        }
    }

    public static async IAsyncEnumerable<T1> AsAsyncEnumerable<T1>(
        this IEnumerable<Task<T1>> enumerable,
        [EnumeratorCancellation] CancellationToken cancellationToken
    ) {
        foreach (Task<T1> item in enumerable) {
            if (cancellationToken.IsCancellationRequested) {
                break;
            }

            yield return await item;
        }
    }
}
