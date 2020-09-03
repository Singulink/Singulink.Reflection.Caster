# Singulink.Reflection.Caster

[![Join the chat](https://badges.gitter.im/Singulink/community.svg)](https://gitter.im/Singulink/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![View nuget packages](https://img.shields.io/nuget/v/Singulink.Reflection.Caster.svg)](https://www.nuget.org/packages/Singulink.Reflection.Caster/)
[![Build and Test](https://github.com/Singulink/Singulink.Reflection.Caster/workflows/build%20and%20test/badge.svg)](https://github.com/Singulink/Singulink.Reflection.Caster/actions?query=workflow%3A%22build+and+test%22)

**Singulink.Reflection.Caster** provides dynamic and generic casting capabilities between types determined at runtime. The casting functionality is provided by cached delegates created from compiled expressions so the casts are very fast - the only overhead is a delegate call if you use the generic methods. Both checked and unchecked casts are supported.

### About Singulink

*Shameless plug*: We are a small team of engineers and designers dedicated to building beautiful, functional and well-engineered software solutions. We offer very competitive rates as well as fixed-price contracts and welcome inquiries to discuss any custom development / project support needs you may have.

This package is part of our **Singulink Libraries** collection. Visit https://github.com/Singulink to see our full list of publicly available libraries and other open-source projects.

## Installation

Simply install the `Singulink.Reflection.Caster` package from NuGet into your project.

**Supported Runtimes**: Anywhere .NET Standard 2.0+ is supported, including:
- .NET Core 2.0+
- .NET Framework 4.6.1+
- Mono 5.4+
- Xamarin.iOS 10.14+
- Xamarin.Android 8.0+

## API

You can view the API on [FuGet](https://www.fuget.org/packages/Singulink.Reflection.Caster). All the functionality is exposed via static methods on the `Caster` class in the `Singulink.Reflection` namespace.

## Usage

Normally if you want to cast between two types then the compiler must be able to statically determine what implicit/explicit conversion operator should be used. This is problematic for generic types, so this does not work, for example:

```c#
int GetValueAsInt<T>(T value)
{
    return (int)value;
}
```

If you know that `T` is (or should be) castable to `int` then you can do this library to work around the issue and effectively get the desired behavior:

```c#
int GetValueAsInt<T>(T value)
{
    return Caster.Cast<T, int>(value);
}
```

If the types are not known statically then there is a set of dynamic cast methods that can be used which accept `Type` parameters. For example:

```c#
object intValue = 123456;

// Cast intValue to decimal:

object decimalValue = Caster.DynamicCast(intValue, typeof(decimal));

// Cast intValue to a byte:

object byteValue = Caster.DynamicCheckedCast(intValue, typeof(byte)); // OverflowException
object byteValue = Caster.DynamicCast(intValue, typeof(byte)); // byteValue = 64

```

The dynamic cast methods above call `GetType()` on the value and then perform a fast dictionary lookup to get a cached delegate that casts between the types, but you can also grab the caster delegate and store it in a field to avoid the cache lookup:

```c#

// Store the caster delegate:
Func<object, object> caster = Caster.GetCaster(typeof(int), typeof(byte));

// Use the caster delegate directly:
object intValue = 123456;
object byteValue = caster.Invoke(intValue);
```

## How is this different than Convert.ChangeType()?

### No dependency on IConvertible

The `ChangeType` method relies on types implementing the `IConvertible` interface, where-as `Caster` executes the same instructions as normal casts in code and thus works even with types that do not implement `IConvertible`.

### Behavior

Since `Caster` generates the same code as normal casts, its behavior can differ significantly from `ChangeType` which often does a lot of voodoo magic to coerce values into the requested type. For example, `ChangeType` attempts to parse string values and converts between boolean and numeric data types, both things that casts (and thus `Caster`) do not do. If you are expecting normal casting behavior then using `ChangeType` can lead to unexpected results in many circumstances.

Another difference is that `ChangeType` does not work for class hierarchies, so this fails:

```c#
class A { }
class B : A { }

object b = new B();
Convert.ChangeType(b, typeof(A)); // InvalidCastException

```

### Performance

Performance is significantly improved in this library, particularly when using the generic casting methods that do not box the input and return values, resulting in less allocations and lower GC pressure.

### Unchecked Casts

`Caster` has checked and unchecked versions of all the methods or you can pass a boolean parameter to determine whether a checked cast is performed.
