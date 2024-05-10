﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;

namespace System.Windows.Forms.BinaryFormat;

internal static class WinFormsBinaryFormattedObjectExtensions
{
    /// <summary>
    ///  Tries to get this object as a binary formatted <see cref="ImageListStreamer"/>.
    /// </summary>
    public static bool TryGetImageListStreamer(
        this BinaryFormattedObject format,
        out object? imageListStreamer)
    {
        return BinaryFormattedObjectExtensions.TryGet(Get, format, out imageListStreamer);

        static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? imageListStreamer)
        {
            imageListStreamer = null;

            if (format.RootRecord is not System.Runtime.Serialization.BinaryFormat.ClassRecord types
                || !types.IsTypeNameMatching(typeof(ImageListStreamer))
                || !types.HasMember("Data")
                || types.GetObject("Data") is not System.Runtime.Serialization.BinaryFormat.ArrayRecord<byte> data)
            {
                return false;
            }

            imageListStreamer = new ImageListStreamer(data.ToArray(maxLength: Array.MaxLength));
            return true;
        }
    }

    /// <summary>
    ///  Tries to get this object as a binary formatted <see cref="Bitmap"/>.
    /// </summary>
    public static bool TryGetBitmap(this BinaryFormattedObject format, out object? bitmap)
    {
        bitmap = null;

        if (format.RootRecord is not System.Runtime.Serialization.BinaryFormat.ClassRecord types
            || !types.IsTypeNameMatching(typeof(Bitmap))
            || !types.HasMember("Data")
            || types.GetObject("Data") is not System.Runtime.Serialization.BinaryFormat.ArrayRecord<byte> data)
        {
            return false;
        }

        bitmap = new Bitmap(new MemoryStream(data.ToArray(maxLength: Array.MaxLength)));
        return true;
    }

    /// <summary>
    ///  Try to get a supported object.
    /// </summary>
    public static bool TryGetObject(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value) =>
        format.TryGetFrameworkObject(out value)
        || format.TryGetBitmap(out value)
        || format.TryGetImageListStreamer(out value);
}
