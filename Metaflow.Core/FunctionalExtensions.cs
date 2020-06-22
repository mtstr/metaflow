using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Metaflow
{
    public static class FunctionalExtensions
    {
        public static Maybe<T> Maybe<T>(this T? value) where T : struct => !value.HasValue ? Metaflow.Maybe<T>.Nothing : new Maybe<T>(value.Value);

        public static Maybe<T> Maybe<T>(this T value) where T : struct => new Maybe<T>(value);


        public static Task<Maybe<TResult>> Traverse<T, TResult>(this Maybe<T> source, Func<T, Task<TResult>> f) where T : struct where TResult : struct
        {
            return source.Match<Task<Maybe<TResult>>>(
                nothing: Task.FromResult(Metaflow.Maybe<TResult>.Nothing),
                just: async x => new Maybe<TResult>(await f(x)));
        }

        public static async Task<TResult> Bind<T, TResult>(this Task<T> task, Func<T, TResult> f)
        {
            var value = await task;

            return await Task.FromResult(f(value));
        }

        public static Task Traverse<T>(this Maybe<T> source, Func<T, Task> f) where T : struct
        {
            return source.Match(
                nothing: Task.FromResult(Metaflow.Maybe<T>.Nothing),
                just: x => f(x));
        }

        public static IEnumerable<TResult> Traverse<T, TResult>(this Maybe<T> source, Func<T, IEnumerable<TResult>> f) where T : struct
        {
            return source.Match(
                nothing: new List<TResult>(),
                just: x => f(x));
        }

        public static async Task<IEnumerable<TResult>> Traverse<T, TResult>(this Task<Maybe<T>> source, Func<T, IEnumerable<TResult>> f) where T : struct
        {
            var t = await source;

            return t.Traverse(f);
        }

        public static Task<Maybe<TResult>> Traverse<T, TResult>(this Maybe<T> source, Func<T, Task<Maybe<TResult>>> f) where T : struct where TResult : struct
        {
            return source.Match(
                nothing: Task.FromResult(Metaflow.Maybe<TResult>.Nothing),
                just: x => f(x));
        }

        public static async Task Bind<T>(this Task<Maybe<T>> task, Func<T, Task> f) where T : struct
        {
            await (await task).Match(
                nothing: Task.CompletedTask,
                just: x => f(x));
        }

        public static async Task<TResult> Match<T, TResult>(
            this Task<Maybe<T>> task,
            TResult nothing,
            Func<T, TResult> just) where T : struct
        {
            var m = await task;
            return m.Match(nothing: nothing, just: just);
        }

        public static async Task<Maybe<TResult>> Map<T, TResult>(
            this Task<Maybe<T>> source,
            Func<T, TResult> selector) where T : struct where TResult : struct
        {
            var m = await source;
            return m.Map(selector);
        }

        public static async Task<IEnumerable<TResult>> Map<T, TResult>(
            this Task<IEnumerable<T>> source,
            Func<T, TResult> selector)
        {
            var m = await source;
            return m.Select(selector);
        }

        public static async Task<(Maybe<T1>, T2)> Bind<T, T1, T2>(this Task<Maybe<T>> task,
            Func<T, Task<(Maybe<T1>, T2)>> f) where T : struct where T1 : struct
        {
            Maybe<T> maybe = await task;

            return await maybe.Match<Task<(Maybe<T1>, T2)>>(
                nothing: Task.FromResult((Metaflow.Maybe<T1>.Nothing, default(T2))),
                just: x => f(x)
            );
        }

        public static async Task<Maybe<TResult>> Bind<T, TResult>(this Task<Maybe<T>> task,
            Func<T, Task<Maybe<TResult>>> f) where T : struct where TResult : struct
        {
            Maybe<T> maybe = await task;

            return await maybe.Traverse(f);
        }

        public static async Task<T> PassThrough<T>(this Task<T> task, Func<T, Task> f)
        {
            T t = await task;

            await f(t);

            return t;
        }


        public static async Task<Maybe<T>> PassThrough<T>(this Task<Maybe<T>> task, Func<T, Task> f) where T : struct
        {
            Maybe<T> t = await task;

            await t.Traverse(f);

            return t;
        }

        public static async Task<IEnumerable<T>> PassThrough<T>(this Task<IEnumerable<T>> task, Func<T, Task> f)
        {
            var t = await task;

            foreach (var a in t)
                await f(a);

            return t;
        }

        public static async Task<Maybe<T>> Bind<T>(this Task<IEnumerable<T>> task, Func<IEnumerable<T>, T?> f) where T : struct
        {
            var list = await task;

            return f(list).Maybe();
        }

        public static async Task<IEnumerable<TResult>> Bind<T, TResult>(this Task<IEnumerable<T>> task,
            Func<T, TResult> f)
        {
            return (await task).Select(f);
        }

        public static async Task<IEnumerable<TResult>> Bind<T, TResult>(this Task<IEnumerable<T>> task,
            Func<T, Maybe<TResult>> f, TResult nothing) where TResult : struct
        {
            return (await task).Select(t => f(t).Match(nothing, v => v)).Where(v => !v.Equals(nothing));
        }

        public static async Task<IEnumerable<TResult>> Bind<T, TResult>(this Task<IEnumerable<T>> task,
            Func<T, Task<Maybe<TResult>>> f, TResult nothing) where TResult : struct
        {
            // TODO Why this does not work?!
            // return (await task).Select(t => f(t).Match(null, v => v)).Select(async t=> await t).Where(v => v != null);

            var result = new List<TResult>();

            foreach (var a in await task)
            {
                var c = await f(a).Match(nothing, v => v);

                if (!c.Equals(nothing)) result.Add(c);
            }

            return result;
        }

        public static async Task<IEnumerable<TResult>> Bind<T, TResult>(this Task<IEnumerable<T>> task,
            Func<T, Task<Maybe<TResult>>> f, Func<TResult, bool> isNothing, TResult nothing) where TResult : struct
        {
            var result = new List<TResult>();

            foreach (var a in await task)
            {
                var c = await f(a).Match(nothing, v => v);

                if (!isNothing(c))
                    result.Add(c);
            }

            return result;
        }
    }
}
