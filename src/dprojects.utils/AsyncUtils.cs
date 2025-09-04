using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Reflection;

namespace DProjects.Utils {


    public static class AsyncUtils {

        //variables
        private static readonly TaskFactory mMyTaskFactory = new(CancellationToken.None,
            TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);


        //methods
        public static TResult RunSync<TResult>(Func<Task<TResult>> func) {
            var cultureUi = CultureInfo.CurrentUICulture;
            var culture = CultureInfo.CurrentCulture;
            return mMyTaskFactory.StartNew(() => {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = cultureUi;
                return func();
            }).Unwrap().GetAwaiter().GetResult();
        }

        public static void RunSync(Func<Task> func) {
            var cultureUi = CultureInfo.CurrentUICulture;
            var culture = CultureInfo.CurrentCulture;
            mMyTaskFactory.StartNew(() => {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = cultureUi;
                return func();
            }).Unwrap().GetAwaiter().GetResult();
        }


        //utils
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> collection) {
            var result = new List<T>();
            await foreach (var item in collection) {
                result.Add(item);
            }
            return result;
        }
        public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> collection) {
            return [.. (await collection.ToListAsync())];
        }

        //utils
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            foreach (var item in enumerable) {
                yield return await Task.FromResult(item);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        public static IEnumerable<T> ToEnumerable<T>(this IAsyncEnumerable<T> iAsyncEnumerable) {
            var e = iAsyncEnumerable.GetAsyncEnumerator(default);
            try {
                while (true) {
                    if (!Wait(e.MoveNextAsync()))
                        break;

                    yield return e.Current;
                }
            } finally {
                Wait(e.DisposeAsync());
            }
        }
        private static void Wait(ValueTask task) {
            var waiter = task.GetAwaiter();
            if (!waiter.IsCompleted) {
                task.AsTask().GetAwaiter().GetResult();
                return;
            }
            waiter.GetResult();
        }
        private static T Wait<T>(ValueTask<T> task) {
            var waiter = task.GetAwaiter();
            if (!waiter.IsCompleted) {
                return task.AsTask().GetAwaiter().GetResult();
            }
            return waiter.GetResult();
        }


    }

}


