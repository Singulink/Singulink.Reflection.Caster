# Singulink.Reflection.Caster

[![Join the chat](https://badges.gitter.im/Singulink/community.svg)](https://gitter.im/Singulink/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![View nuget packages](https://img.shields.io/nuget/v/Singulink.Reflection.Caster.svg)](https://www.nuget.org/packages/Singulink.Reflection.Caster/)
[![Build and Test](https://github.com/Singulink/Singulink.Reflection.Caster/workflows/build%20and%20test/badge.svg)](https://github.com/Singulink/Singulink.Reflection.Caster/actions?query=workflow%3A%22build+and+test%22)

This library provides dynamic and generic casting capabilities between types determined at runtime. The casting functionality is provided by cached delegates created from compiled expressions so the casts are very fast - the only overhead is a delegate call if you use the generic methods. Both checked and unchecked casts are supported.

## Installation

Simply install the `Singulink.Reflection.Caster` package from NuGet into your project.

Supported Runtimes: Anywhere .NET Standard 2.0 is supported, including .NET Framework 4.6.1+ and .NET Core 2.0+.

## Usage

All the functionality is exposed via static methods the `Caster` class. If the types are statically known then the generic methods are the fastest and easiest to use. For example, if you wanted to write a method that converts a generic value to a generic enum, you could do this:

```c#
// Convert input to an enum type:

public static TEnum ToEnum<TValue, TEnum>(TValue value)
    where TValue : unmanaged
    where TEnum : Enum
{
    return Caster.Cast<TValue, TEnum>(value);
}
```

If the types are not known statically then there are a set of dynamic cast methods that can be used. For example:

```c#
object intValue = 123456;

// Cast intValue to decimal:

object decimalValue = Caster.DynamicCast(intValue, typeof(decimal));

// Cast intValue to a byte:

object byteValue = Caster.DynamicCheckedCast(intValue, typeof(byte)); // OverflowException
object byteValue = Caster.DynamicCast(intValue, typeof(byte)); // byteValue = 64

```

The dynamic cast methods above call `GetType()` on the value and then perform a fast dictionary lookup to get a cached caster delegate that converts between the types, but you can also grab the caster delegate and store it in a field to avoid the cache lookup:

```c#

// Store the caster delegate:
Func<object, object> caster = Caster.GetCaster(typeof(int), typeof(byte));

// Use the caster delegate directly:
object intValue = 123456;
object byteValue = caster.Invoke(intValue);
```