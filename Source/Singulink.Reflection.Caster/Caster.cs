using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Singulink.Reflection
{
    /// <summary>
    /// Provides dynamic and generic casting capabilities between types determined at runtime.
    /// </summary>
    public static class Caster
    {
        /// <summary>
        /// Determines if one type can be cast to another type.
        /// </summary>
        /// <param name="fromType">The input object type.</param>
        /// <param name="toType">The output object type.</param>
        /// <returns>True if a valid cast exists, otherwise false.</returns>
        /// <remarks>
        /// <para>The result of this method is not cached internally making this is a relatively slow operation, so you should cache the result yourself if the
        /// value is needed frequently.</para>
        /// </remarks>
        public static bool IsValidCast(Type fromType, Type toType)
        {
            try {
                Expression.Convert(Expression.Parameter(fromType, "fromValue"), toType);
                return true;
            }
            catch (InvalidOperationException) {
                return false;
            }
        }

        /// <summary>
        /// Performs a static cast between the given types with optional overflow checking.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        /// <param name="checkOverflow">True to perform an overflow check, otherwise false.</param>
        [return: MaybeNull, NotNullIfNotNull("value")]
        public static TTo Cast<TFrom, TTo>(TFrom value, bool checkOverflow)
        {
            return checkOverflow ? StaticCache<TFrom, TTo>.CheckedCast(value) : StaticCache<TFrom, TTo>.Cast(value);
        }

        /// <summary>
        /// Performs a static cast between the given types without overflow checking.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        [return: MaybeNull, NotNullIfNotNull("value")]
        public static TTo Cast<TFrom, TTo>(TFrom value) => StaticCache<TFrom, TTo>.Cast(value);

        /// <summary>
        /// Performs a static cast between the given types with overflow checking.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        [return: MaybeNull, NotNullIfNotNull("value")]
        public static TTo CheckedCast<TFrom, TTo>(TFrom value) => StaticCache<TFrom, TTo>.CheckedCast(value);

        /// <summary>
        /// Performs a dynamic cast between the given value's type and the specified type without overflow checking.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        /// <param name="toType">The type that the value should be cast to.</param>
        [return: NotNullIfNotNull("value")]
        public static object? DynamicCast(object? value, Type toType) => DynamicCast(value, toType, false);

        /// <summary>
        /// Performs a dynamic cast between the given value's type and the specified type with overflow checking.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        /// <param name="toType">The type that the value should be cast to.</param>
        [return: NotNullIfNotNull("value")]
        public static object? DynamicCheckedCast(object? value, Type toType) => DynamicCast(value, toType, true);

        /// <summary>
        /// Performs a dynamic cast between the given value's type and the specified type with optional overflow checking.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        /// <param name="toType">The type that the value should be cast to.</param>
        /// <param name="checkOverflow">True to perform an overflow check, otherwise false.</param>
        [return: NotNullIfNotNull("value")]
        public static object? DynamicCast(object? value, Type toType, bool checkOverflow)
        {
            if (value == null) {
                if (!DynamicCache.NullableLookup.TryGetValue(toType, out bool isNullable))
                    DynamicCache.NullableLookup[toType] = isNullable = !toType.IsValueType || Nullable.GetUnderlyingType(toType) != null;

                if (!isNullable)
                    throw new ArgumentNullException(nameof(value), $"Cannot cast null to non-nullable value type '{toType}'.");

                return null;
            }

            return GetCaster(value.GetType(), toType, checkOverflow).Invoke(value);
        }

        /// <summary>
        /// Gets a delegate that performs a static cast between two types without overflow checking.
        /// </summary>
        /// <param name="fromType">The input value type.</param>
        /// <param name="toType">The type that the value should be cast to.</param>
        public static Func<object?, object?> GetCaster(Type fromType, Type toType) => GetCaster(fromType, toType, DynamicCache.CasterLookup);

        /// <summary>
        /// Gets a delegate that performs a static cast between two types with overflow checking.
        /// </summary>
        /// <param name="fromType">The input value type.</param>
        /// <param name="toType">The type that the value should be cast to.</param>
        public static Func<object?, object?> GetCheckedCaster(Type fromType, Type toType) => GetCaster(fromType, toType, DynamicCache.CheckedCasterLookup);

        /// <summary>
        /// Gets a delegate that performs a static cast between two types with optional overflow checking.
        /// </summary>
        /// <param name="fromType">The input value type.</param>
        /// <param name="toType">The type that the value should be cast to.</param>
        /// <param name="checkOverflow">True to perform an overflow check, otherwise false.</param>
        public static Func<object?, object?> GetCaster(Type fromType, Type toType, bool checkOverflow)
        {
            return GetCaster(fromType, toType, checkOverflow ? DynamicCache.CheckedCasterLookup : DynamicCache.CasterLookup);
        }

        private static Func<object?, object?> GetCaster(Type fromType, Type toType, DynamicCasterDictionary casterCache)
        {
            var key = (fromType, toType);

            if (!casterCache.TryGetValue(key, out var caster)) {
                bool hasCheckedCastor = HasCheckedCaster(fromType, toType);

                if (casterCache == DynamicCache.CasterLookup || !hasCheckedCastor) {
                    caster = DynamicCache.CasterLookup.GetOrAdd(key, key => CreateCaster<object?, object?>(key.From, key.To, false));

                    if (!hasCheckedCastor)
                        DynamicCache.CheckedCasterLookup.TryAdd(key, caster);
                }
                else {
                    caster = DynamicCache.CheckedCasterLookup.GetOrAdd(key, key => CreateCaster<object?, object?>(key.From, key.To, true));
                }
            }

            return caster;
        }

        private static Func<TIn, TOut> CreateCaster<TIn, TOut>(Type fromType, Type toType, bool checkedOverflow = false)
        {
            if (!IsValidCast(fromType, toType))
                throw new InvalidCastException();

            Expression body;

            var parameter = Expression.Parameter(typeof(TIn), "value");
            var input = typeof(TIn) == fromType ? (Expression)parameter : Expression.Convert(parameter, fromType);

            if (fromType == toType)
                body = parameter;
            else
                body = checkedOverflow ? Expression.ConvertChecked(input, toType) : Expression.Convert(input, toType);

            if (typeof(TOut) != toType)
                body = Expression.Convert(body, typeof(TOut));

            return Expression.Lambda<Func<TIn, TOut>>(body, parameter).Compile();
        }

        private static bool HasCheckedCaster(Type from, Type to)
        {
            return IsPrimitiveOrEnum(from) && IsPrimitiveOrEnum(to);

            static bool IsPrimitiveOrEnum(Type type) => type.IsPrimitive || type.IsEnum;
        }

        private static class StaticCache<TFrom, TTo>
        {
            private static readonly Func<TFrom, TTo> _caster = CreateCaster<TFrom, TTo>(typeof(TFrom), typeof(TTo), false);
            private static readonly Func<TFrom, TTo> _checkedCaster = HasCheckedCaster(typeof(TFrom), typeof(TTo)) ? CreateCaster<TFrom, TTo>(typeof(TFrom), typeof(TTo), true) : _caster;

            public static TTo Cast(TFrom value) => (_caster ?? throw new InvalidCastException()).Invoke(value);

            public static TTo CheckedCast(TFrom value) => (_checkedCaster ?? throw new InvalidCastException()).Invoke(value);
        }

        private static class DynamicCache
        {
            public static readonly DynamicCasterDictionary CasterLookup = new DynamicCasterDictionary();
            public static readonly DynamicCasterDictionary CheckedCasterLookup = new DynamicCasterDictionary();
            public static readonly ConcurrentDictionary<Type, bool> NullableLookup = new ConcurrentDictionary<Type, bool>();
        }

        private class DynamicCasterDictionary : ConcurrentDictionary<(Type From, Type To), Func<object?, object?>>
        {
        }
    }
}
