﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Drawing;
using System.Runtime.Serialization.BinaryFormat;

namespace System.Windows.Forms.BinaryFormat;

internal static class BinaryFormattedObjectExtensions
{
    internal delegate bool TryGetDelegate(BinaryFormattedObject format, [NotNullWhen(true)] out object? value);

    internal static bool TryGet(TryGetDelegate get, BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
    {
        try
        {
            return get(format, out value);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidCastException)
        {
            // This should only really happen with corrupted data.
            Debug.Fail(ex.Message);
            value = default;
            return false;
        }
    }

    /// <summary>
    ///  Tries to get this object as a <see cref="PointF"/>.
    /// </summary>
    public static bool TryGetPointF(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
    {
        return TryGet(Get, format, out value);

        static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
        {
            value = default;

            if (format.RootRecord is not ClassRecord classInfo
                || !classInfo.IsTypeNameMatching(typeof(PointF))
                || !classInfo.HasMember("x")
                || !classInfo.HasMember("y"))
            {
                return false;
            }

            value = new PointF(classInfo.GetSingle("x"), classInfo.GetSingle("y"));

            return true;
        }
    }

    /// <summary>
    ///  Tries to get this object as a <see cref="RectangleF"/>.
    /// </summary>
    public static bool TryGetRectangleF(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
    {
        return TryGet(Get, format, out value);

        static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
        {
            value = default;

            if (format.RootRecord is not ClassRecord classInfo
                || !classInfo.IsTypeNameMatching(typeof(RectangleF))
                || !classInfo.HasMember("x")
                || !classInfo.HasMember("y")
                || !classInfo.HasMember("width")
                || !classInfo.HasMember("height"))
            {
                return false;
            }

            value = new RectangleF(
                classInfo.GetSingle("x"),
                classInfo.GetSingle("y"),
                classInfo.GetSingle("width"),
                classInfo.GetSingle("height"));

            return true;
        }
    }

    /// <summary>
    ///  Tries to get this object as a primitive type or string.
    /// </summary>
    /// <returns><see langword="true"/> if this represented a primitive type or string.</returns>
    public static bool TryGetPrimitiveType(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
    {
        return TryGet(Get, format, out value);

        static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
        {
            if (format.RootRecord.RecordType is RecordType.BinaryObjectString)
            {
                value = ((PrimitiveTypeRecord<string>)format.RootRecord).Value;
                return true;
            }
            else if (format.RootRecord.RecordType is RecordType.MemberPrimitiveTyped)
            {
                value = GetMemberPrimitiveTypedValue(format.RootRecord);
                return true;
            }

            value = null;
            return false;
        }
    }

    internal static object GetMemberPrimitiveTypedValue(this SerializationRecord record)
    {
        Debug.Assert(record.RecordType is RecordType.MemberPrimitiveTyped);

        return record switch
        {
            PrimitiveTypeRecord<string> primitive => primitive.Value,
            PrimitiveTypeRecord<bool> primitive => primitive.Value,
            PrimitiveTypeRecord<byte> primitive => primitive.Value,
            PrimitiveTypeRecord<sbyte> primitive => primitive.Value,
            PrimitiveTypeRecord<char> primitive => primitive.Value,
            PrimitiveTypeRecord<short> primitive => primitive.Value,
            PrimitiveTypeRecord<ushort> primitive => primitive.Value,
            PrimitiveTypeRecord<int> primitive => primitive.Value,
            PrimitiveTypeRecord<uint> primitive => primitive.Value,
            PrimitiveTypeRecord<long> primitive => primitive.Value,
            PrimitiveTypeRecord<ulong> primitive => primitive.Value,
            PrimitiveTypeRecord<float> primitive => primitive.Value,
            PrimitiveTypeRecord<double> primitive => primitive.Value,
            PrimitiveTypeRecord<decimal> primitive => primitive.Value,
            PrimitiveTypeRecord<TimeSpan> primitive => primitive.Value,
            PrimitiveTypeRecord<DateTime> primitive => primitive.Value,
            PrimitiveTypeRecord<IntPtr> primitive => primitive.Value,
            _ => ((PrimitiveTypeRecord<UIntPtr>)record).Value
        };
    }

    /// <summary>
    ///  Tries to get this object as a <see cref="List{T}"/> of <see cref="PrimitiveType"/>.
    /// </summary>
    public static bool TryGetPrimitiveList(this BinaryFormattedObject format, [NotNullWhen(true)] out object? list)
    {
        return TryGet(Get, format, out list);

        static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? list)
        {
            list = null;

            if (format.RootRecord is not ClassRecord classInfo
                || !classInfo.HasMember("_items")
                || !classInfo.HasMember("_size")
                || classInfo.GetObject("_size") is not int size
                || !classInfo.TypeName.IsConstructedGenericType
                || classInfo.TypeName.GetGenericTypeDefinition().Name != typeof(List<>).Name
                || classInfo.TypeName.GetGenericArguments().Length != 1
                || classInfo.GetObject("_items") is not ArrayRecord arrayRecord
                || !IsPrimitiveArrayRecord(arrayRecord))
            {
                return false;
            }

            // BinaryFormatter serializes the entire backing array, so we need to trim it down to the size of the list.
            list = arrayRecord switch
            {
                ArrayRecord<string> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<bool> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<byte> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<sbyte> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<char> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<short> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<ushort> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<int> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<uint> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<long> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<ulong> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<float> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<double> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<decimal> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<TimeSpan> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                ArrayRecord<DateTime> ar => ar.ToArray(maxLength: Array.MaxLength).CreateTrimmedList(size),
                _ => throw new InvalidOperationException()
            };

            return true;
        }
    }

    /// <summary>
    ///  Tries to get this object as a <see cref="ArrayList"/> of <see cref="PrimitiveType"/> values.
    /// </summary>
    public static bool TryGetPrimitiveArrayList(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
    {
        return TryGet(Get, format, out value);

        static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
        {
            value = null;

            if (format.RootRecord is not ClassRecord classInfo
                || !classInfo.IsTypeNameMatching(typeof(ArrayList))
                || !classInfo.HasMember("_items")
                || !classInfo.HasMember("_size")
                || classInfo.GetObject("_size") is not int size
                || classInfo.GetObject("_items") is not ArrayRecord<object> arrayRecord
                || size > arrayRecord.Length)
            {
                return false;
            }

            ArrayList arrayList = new(size);
            object?[] array = arrayRecord.ToArray(maxLength: size);
            for (int i = 0; i < size; i++)
            {
                if (array[i] is SerializationRecord)
                {
                    return false;
                }

                arrayList.Add(array[i]);
            }

            value = arrayList;
            return true;
        }
    }

    /// <summary>
    ///  Tries to get this object as an <see cref="Array"/> of primitive types.
    /// </summary>
    public static bool TryGetPrimitiveArray(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
    {
        return TryGet(Get, format, out value);

        static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
        {
            if (!IsPrimitiveArrayRecord(format.RootRecord))
            {
                value = null;
                return false;
            }

            value = format.RootRecord switch
            {
                ArrayRecord<string> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<bool> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<byte> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<sbyte> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<char> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<short> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<ushort> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<int> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<uint> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<long> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<ulong> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<float> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<double> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<decimal> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<TimeSpan> ar => ar.ToArray(maxLength: Array.MaxLength),
                ArrayRecord<DateTime> ar => ar.ToArray(maxLength: Array.MaxLength),
                _ => throw new InvalidOperationException()
            };

            return value is not null;
        }
    }

    /// <summary>
    ///  Tries to get this object as a binary formatted <see cref="Hashtable"/> of <see cref="PrimitiveType"/> keys and values.
    /// </summary>
    public static bool TryGetPrimitiveHashtable(this BinaryFormattedObject format, [NotNullWhen(true)] out Hashtable? hashtable)
    {
        bool success = format.TryGetPrimitiveHashtable(out object? value);
        hashtable = (Hashtable?)value;
        return success;
    }

    /// <summary>
    ///  Tries to get this object as a binary formatted <see cref="Hashtable"/> of <see cref="PrimitiveType"/> keys and values.
    /// </summary>
    public static bool TryGetPrimitiveHashtable(this BinaryFormattedObject format, [NotNullWhen(true)] out object? hashtable)
    {
        return TryGet(Get, format, out hashtable);

        static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? hashtable)
        {
            hashtable = null;

            // Note that hashtables with custom comparers and/or hash code providers will have that information before
            // the value pair arrays.
            if (format.RootRecord.RecordType != RecordType.SystemClassWithMembersAndTypes
                || format.RootRecord is not ClassRecord classInfo
                || !classInfo.IsTypeNameMatching(typeof(Hashtable))
                || !classInfo.HasMember("Keys")
                || !classInfo.HasMember("Values")
                || format[2] is not ArrayRecord<object?> keysRecord
                || format[3] is not ArrayRecord<object?> valuesRecord
                || keysRecord.Length != valuesRecord.Length)
            {
                return false;
            }

            Hashtable temp = new((int)keysRecord.Length);
            object?[] keys = keysRecord.ToArray(maxLength: Array.MaxLength);
            object?[] values = valuesRecord.ToArray(maxLength: Array.MaxLength);
            for (int i = 0; i < keys.Length; i++)
            {
                object? key = keys[i];
                object? value = values[i];

                if (key is null or SerializationRecord || value is SerializationRecord)
                {
                    return false;
                }

                temp[key] = value;
            }

            hashtable = temp;
            return true;
        }
    }

    /// <summary>
    ///  Tries to get this object as a binary formatted <see cref="NotSupportedException"/>.
    /// </summary>
    public static bool TryGetNotSupportedException(
        this BinaryFormattedObject format,
        out object? exception)
    {
        return TryGet(Get, format, out exception);

        static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? exception)
        {
            exception = null;

            if (format.RootRecord is not ClassRecord classInfo
                || classInfo.IsTypeNameMatching(typeof(NotSupportedException)))
            {
                return false;
            }

            exception = new NotSupportedException(classInfo.GetString("Message"));
            return true;
        }
    }

    /// <summary>
    ///  Try to get a supported .NET type object (not WinForms).
    /// </summary>
    public static bool TryGetFrameworkObject(
        this BinaryFormattedObject format,
        [NotNullWhen(true)] out object? value)
        => format.TryGetPrimitiveType(out value)
            || format.TryGetPrimitiveList(out value)
            || format.TryGetPrimitiveArray(out value)
            || format.TryGetPrimitiveArrayList(out value)
            || format.TryGetPrimitiveHashtable(out value)
            || format.TryGetRectangleF(out value)
            || format.TryGetPointF(out value)
            || format.TryGetNotSupportedException(out value);

    private static bool IsPrimitiveArrayRecord(SerializationRecord serializationRecord)
        => serializationRecord.RecordType is RecordType.ArraySingleString or RecordType.ArraySinglePrimitive;
}
