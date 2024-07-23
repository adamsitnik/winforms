﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Formats.Nrbf;

namespace System.Private.Windows.Core.BinaryFormat.Deserializer;

#pragma warning disable SYSLIB0050 // Type or member is obsolete

/// <summary>
///  Base class for <see cref="BinaryFormat.ObjectRecord"/> deserialization.
/// </summary>
internal abstract partial class ObjectRecordDeserializer
{
    // Used to indicate that the value is missing from the deserialized objects.
    private protected static object s_missingValueSentinel = new();

    internal Formats.Nrbf.SerializationRecord ObjectRecord { get; }

    [AllowNull]
    internal object Object { get; private protected set; }

    private protected IDeserializer Deserializer { get; }

    private protected ObjectRecordDeserializer(Formats.Nrbf.SerializationRecord objectRecord, IDeserializer deserializer)
    {
        Deserializer = deserializer;
        ObjectRecord = objectRecord;
    }

    /// <summary>
    ///  Continue parsing.
    /// </summary>
    /// <returns>The id that is necessary to complete parsing or a default <see cref="SerializationRecordId"/> if complete.</returns>
    internal abstract SerializationRecordId Continue();

    /// <summary>
    ///  Gets the actual object for a member value primitive or record. Returns <see cref="s_missingValueSentinel"/> if
    ///  the object record has not been encountered yet.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected (object? value, SerializationRecordId id) UnwrapMemberValue(object? memberValue)
    {
        if (memberValue is null) // PayloadReader does not return NullRecord, just null
        {
            return (null, default);
        }
        else if (memberValue is not Formats.Nrbf.SerializationRecord serializationRecord) // a primitive value
        {
            return (memberValue, default);
        }
        else if (serializationRecord.RecordType is SerializationRecordType.BinaryObjectString)
        {
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)serializationRecord;
            return (stringRecord.Value, stringRecord.Id);
        }
        else if (serializationRecord.RecordType is SerializationRecordType.MemberPrimitiveTyped)
        {
            return (serializationRecord.GetMemberPrimitiveTypedValue(), default);
        }
        else
        {
            // ClassRecords & ArrayRecords
            return TryGetObject(serializationRecord.Id);
        }

        (object? value, SerializationRecordId id) TryGetObject(SerializationRecordId id)
        {
            if (!Deserializer.DeserializedObjects.TryGetValue(id, out object? value))
            {
                return (s_missingValueSentinel, id);
            }

            ValidateNewMemberObjectValue(value);
            return (value, id);
        }
    }

    /// <summary>
    ///  Called for new non-primitive reference types.
    /// </summary>
    private protected virtual void ValidateNewMemberObjectValue(object value) { }

    /// <summary>
    ///  Returns <see langword="true"/> if the given record's value needs an updater applied.
    /// </summary>
    private protected bool DoesValueNeedUpdated(object value, SerializationRecordId valueRecord) =>
        // Null SerializationRecordId is a primitive of some sort.
        !valueRecord.Equals(default)
            // IObjectReference is going to have it's object replaced.
            && (value is IObjectReference
                // Value types that aren't "complete" need to be reapplied.
                || (Deserializer.IncompleteObjects.Contains(valueRecord) && value.GetType().IsValueType));

    [RequiresUnreferencedCode("Calls System.Private.Windows.Core.BinaryFormat.Deserializer.ClassRecordParser.Create(ClassRecord, IDeserializer)")]
    internal static ObjectRecordDeserializer Create(SerializationRecordId id, Formats.Nrbf.SerializationRecord record, IDeserializer deserializer) => record switch
    {
        Formats.Nrbf.ClassRecord classRecord => ClassRecordDeserializer.Create(classRecord, deserializer),
        Formats.Nrbf.ArrayRecord arrayRecord => new ArrayRecordDeserializer(arrayRecord, deserializer),
        _ => throw new SerializationException($"Unexpected record type for {id}.")
    };
}

#pragma warning restore SYSLIB0050 // Type or member is obsolete
