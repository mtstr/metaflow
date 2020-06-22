using System;
using System.Globalization;
using System.Text.Json;

namespace Metaflow
{

    public static class MaybeJsonExtensions
    {
        public static JsonSerializerOptions AddMaybeConverter(this JsonSerializerOptions options)
        {
            options.Converters.Add(new JsonConverterFactoryForMaybeOfT());
            return options;
        }
    }
    public sealed class Maybe<T> where T : struct
    {
        private readonly T? value;

        public Maybe(T value)
        {
            this.value = value;
        }

        public bool IsNothing => !value.HasValue;

        private Maybe()
        {
        }

        public Maybe<TResult> Map<TResult>(Func<T, TResult> f) where TResult : struct
        {
            return value.HasValue ? new Maybe<TResult>(f(value.Value)) : Maybe<TResult>.Nothing;
        }

        public Maybe<TResult> Bind<TResult>(Func<T, Maybe<TResult>> f) where TResult : struct
        {
            return value.HasValue ? f(value.Value) : Maybe<TResult>.Nothing;
        }

        public TResult Match<TResult>(TResult nothing, Func<T, TResult> just)
        {
            if (nothing == null)
                throw new ArgumentNullException(nameof(nothing));

            if (just == null)
                throw new ArgumentNullException(nameof(just));

            return value.HasValue ? just(value.Value) : nothing;
        }

        public static implicit operator Maybe<T>(T value) => value.Maybe();
        public static implicit operator Maybe<T>(T? value) => value.Maybe();
        public static implicit operator Maybe<T>(Nothing _) => Maybe<T>.Nothing;

        public static readonly Maybe<T> Nothing = new Maybe<T>();
    }
}
