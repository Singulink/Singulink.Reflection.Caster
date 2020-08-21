# Singulink.Reflection.Caster

[![Join the chat](https://badges.gitter.im/Singulink/community.svg)](https://gitter.im/Singulink/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![View nuget packages](https://img.shields.io/nuget/v/Singulink.Reflection.Caster.svg)](https://www.nuget.org/packages/Singulink.Reflection.Caster/)
[![Build and Test](https://github.com/Singulink/Singulink.Reflection.Caster/workflows/build%20and%20test/badge.svg)](https://github.com/Singulink/Singulink.Reflection.Caster/actions?query=workflow%3A%22build+and+test%22)

This library provides dynamic and generic casting capabilities between types determined at runtime. The casting functionality is provided by cached delegates created from compiled expressions so the casts are very fast - the only overhead is a delegate call if you use the generic methods. Both checked and unchecked casts are supported.

## Installation

Simply install the `Singulink.Reflection.Caster` package from NuGet into your project.

**Supported Runtimes**: Anywhere .NET Standard 2.0 is supported, including .NET Framework 4.6.1+ and .NET Core 2.0+.

## API

You can view the API on [FuGet](https://www.fuget.org/packages/Singulink.Reflection.Caster). All the functionality is exposed via static methods on the `Caster` class.

## Usage

If the types are known at compile time then the generic methods are the fastest and easiest to use. For example, if you wanted to write a method that converts a generic value to a generic enum, you could do this:

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

Since `Caster` generates the same code as normal casts, its behavior can differ significantly from `ChangeType` which often does a lot of voodoo magic to coerce values into the requested type. For example, it attempts to parse string values and converts between boolean and numeric data types, both things that casts (and thus `Caster`) do not do. If you are expecting normal casting behavior then using `ChangeType` can lead to unexpected results in many circumstances.

Another difference is that `ChangeType` does not work well for class hierarchies, so this fails:

```c#
class A { }
class B : A { }

object b = new B();
Convert.ChangeType(b, typeof(A)); // InvalidCastException

```

### Performance

Performance is significantly improved in this library, particularly when using the generic casting methods which do not box the input and return values (thus reducing GC pressure).

### Unchecked Casts

`Caster` has checked and unchecked versions of all the methods or you can pass a boolean parameter to determine whether a checked cast is performed.
