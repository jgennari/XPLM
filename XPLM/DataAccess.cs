﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FlyByWireless.XPLM.DataAccess;

[Flags]
public enum DataTypes
{
    Unknown = 0,
    Int = 1,
    Float = 2,
    Double = 4,
    FloatArray = 8,
    IntArray = 16,
    Data = 32
}

public sealed class IntVector
{
    [DllImport(Defs.Lib)]
    static extern int XPLMGetDatavi(nint handle, ref int values, int offset, int max);

    readonly nint _handle;

    readonly int _offset;

    public int Count => XPLMGetDatavi(_handle, ref Unsafe.NullRef<int>(), 0, 0);

    internal IntVector(nint handle, int offset) => (_handle, _offset) = (handle, offset);

    public int Read(Span<int> destination) =>
        XPLMGetDatavi(_handle, ref MemoryMarshal.GetReference(destination), _offset, destination.Length);

    public void Write(ReadOnlySpan<int> source)
    {
        [DllImport(Defs.Lib)]
        static extern void XPLMSetDatavi(nint handle, ref int values, int offset, int count);

        XPLMSetDatavi(_handle, ref MemoryMarshal.GetReference(source), _offset, source.Length);
    }
}

public sealed class FloatVector
{
    [DllImport(Defs.Lib)]
    static extern int XPLMGetDatavf(nint handle, ref float values, int offset, int max);

    readonly nint _handle;

    readonly int _offset;

    public int Count => XPLMGetDatavf(_handle, ref Unsafe.NullRef<float>(), 0, 0);

    internal FloatVector(nint handle, int offset) => (_handle, _offset) = (handle, offset);

    public int Read(Span<float> destination) =>
        XPLMGetDatavf(_handle, ref MemoryMarshal.GetReference(destination), _offset, destination.Length);

    public void Write(ReadOnlySpan<float> source)
    {
        [DllImport(Defs.Lib)]
        static extern void XPLMSetDatavf(nint handle, ref float values, int offset, int count);

        XPLMSetDatavf(_handle, ref MemoryMarshal.GetReference(source), _offset, source.Length);
    }
}

public sealed class ByteVector
{
    [DllImport(Defs.Lib)]
    internal static extern int XPLMGetDatab(nint handle, ref byte values, int offset, int max);

    [DllImport(Defs.Lib)]
    internal static extern void XPLMSetDatab(nint handle, ref byte values, int offset, int count);

    readonly nint _handle;

    readonly int _offset;

    public int Count => XPLMGetDatab(_handle, ref Unsafe.NullRef<byte>(), 0, 0);

    internal ByteVector(nint handle, int offset) => (_handle, _offset) = (handle, offset);

    public int Read(Span<byte> destination) =>
        XPLMGetDatab(_handle, ref MemoryMarshal.GetReference(destination), _offset, destination.Length);

    public void Write(ReadOnlySpan<byte> source) =>
        XPLMSetDatab(_handle, ref MemoryMarshal.GetReference(source), _offset, source.Length);
}

public sealed class Data<T> where T : unmanaged
{
    readonly nint _id;

    internal Data(nint id) => _id = id;

    public unsafe T Value
    {
        get
        {
            T value = default;
            var read = ByteVector.XPLMGetDatab(_id, ref *(byte*)&value, 0, sizeof(T));
            Debug.Assert(read == sizeof(T));
            return value;
        }
        set => ByteVector.XPLMSetDatab(_id, ref *(byte*)&value, 0, sizeof(T));
    }
}

public sealed class DataRef
{
    public static DataRef? Find(string name)
    {
        [DllImport(Defs.Lib)]
        static extern nint XPLMFindDataRef([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

        var i = XPLMFindDataRef(name);
        return i == 0 ? null : new(i);
    }

    internal readonly nint _id;

    public bool CanWrite
    {
        get
        {
            [DllImport(Defs.Lib)]
            static extern int XPLMCanWriteDataRef(nint handle);

            return XPLMCanWriteDataRef(_id) != 0;
        }
    }

    public bool IsGood
    {
        get
        {
            [DllImport(Defs.Lib)]
            static extern int XPLMIsDataRefGood(nint handle);

            return XPLMIsDataRefGood(_id) != 0;
        }
    }

    public DataTypes Types
    {
        get
        {
            [DllImport(Defs.Lib)]
            static extern int XPLMGetDataRefTypes(nint handle);

            return (DataTypes)XPLMGetDataRefTypes(_id);
        }
    }

    public int AsInt
    {
        get
        {
            [DllImport(Defs.Lib)]
            static extern int XPLMGetDatai(nint handle);

            return XPLMGetDatai(_id);
        }
        set
        {
            [DllImport(Defs.Lib)]
            static extern void XPLMSetDatai(nint handle, int value);

            XPLMSetDatai(_id, value);
        }
    }

    public float AsFloat
    {
        get
        {
            [DllImport(Defs.Lib)]
            static extern float XPLMGetDataf(nint handle);

            return XPLMGetDataf(_id);
        }
        set
        {
            [DllImport(Defs.Lib)]
            static extern void XPLMSetDataf(nint handle, float value);

            XPLMSetDataf(_id, value);
        }
    }

    public double AsDouble
    {
        get
        {
            [DllImport(Defs.Lib)]
            static extern double XPLMGetDatad(nint handle);

            return XPLMGetDatad(_id);
        }
        set
        {
            [DllImport(Defs.Lib)]
            static extern void XPLMSetDatad(nint handle, double value);

            XPLMSetDatad(_id, value);
        }
    }

    public IntVector AsIntVector(int offset) => new(_id, offset);

    public FloatVector AsFloatVector(int offset) => new(_id, offset);

    public ByteVector AsByteVector(int offset) => new(_id, offset);

    public Data<T> As<T>() where T : unmanaged => new(_id);

    internal DataRef(nint id) => _id = id;
}

public interface IAccessor
{
    int AsInt { get => 0; set { } }

    float AsFloat { get => 0; set { } }

    double AsDouble { get => 0; set { } }

    int CountIntVector() => ReadIntVector(0, Span<int>.Empty);
    int ReadIntVector(int offset, Span<int> destination) => 0;
    void WriteIntVector(int offset, ReadOnlySpan<int> source) { }

    int CountFloatVector() => ReadFloatVector(0, Span<float>.Empty);
    int ReadFloatVector(int offset, Span<float> destination) => 0;
    void WriteFloatVector(int offset, ReadOnlySpan<float> source) { }

    int CountByteVector() => ReadByteVector(0, Span<byte>.Empty);
    int ReadByteVector(int offset, Span<byte> destination) => 0;
    void WriteByteVector(int offset, ReadOnlySpan<byte> source) { }
}

public sealed class IntAccessor : IAccessor
{
    public int AsInt { get; set; }

    public float AsFloat { get => AsInt; set => AsInt = (int)value; }

    public double AsDouble { get => AsInt; set => AsInt = (int)value; }
}

public sealed class FloatAccessor : IAccessor
{
    public float AsFloat { get; set; }

    public double AsDouble { get => AsFloat; set => AsFloat = (float)value; }
}

public sealed class DoubleAccessor : IAccessor
{
    public float AsFloat { get => (float)AsDouble; set => AsDouble = value; }

    public double AsDouble { get; set; }
}

public sealed class IntArrayAccessor : IAccessor
{
    readonly int[] _array;

    public IntArrayAccessor(int length) => _array = new int[length];

    int CountIntVector() => _array.Length;
    int ReadIntVector(int offset, Span<int> destination)
    {
        var length = Math.Min(_array.Length - offset, destination.Length);
        _array.AsSpan(offset, length).CopyTo(destination);
        return length;
    }
    void WriteIntVector(int offset, ReadOnlySpan<int> source)
    {
        var count = Math.Min(_array.Length - offset, source.Length);
        source[..count].CopyTo(_array.AsSpan(offset, count));
    }
}

public sealed class FloatArrayAccessor : IAccessor
{
    readonly float[] _array;

    public FloatArrayAccessor(int length) => _array = new float[length];

    int CountFloatVector() => _array.Length;
    int ReadFloatVector(int offset, Span<float> destination)
    {
        var length = Math.Min(_array.Length - offset, destination.Length);
        _array.AsSpan(offset, length).CopyTo(destination);
        return length;
    }
    void WriteFloatVector(int offset, ReadOnlySpan<float> source)
    {
        var count = Math.Min(_array.Length - offset, source.Length);
        source[..count].CopyTo(_array.AsSpan(offset, count));
    }
}

public sealed class Accessor<T> : IAccessor where T : unmanaged
{
    readonly T _data;

    public Accessor() =>
        Debug.Assert(typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double));

#pragma warning disable CA1822 // Mark members as static
    int CountByteVector() => Unsafe.SizeOf<T>();
#pragma warning restore CA1822 // Mark members as static
    unsafe int ReadByteVector(int offset, Span<byte> destination)
    {
        var length = Math.Min(sizeof(T) - offset, destination.Length);
        fixed (void* p = &_data)
            new ReadOnlySpan<byte>((byte*)p + offset, length).CopyTo(destination);
        return length;
    }
    unsafe void WriteByteVector(int offset, ReadOnlySpan<byte> source)
    {
        var count = Math.Min(sizeof(T) - offset, source.Length);
        fixed (void* p = &_data)
            source[..count].CopyTo(new Span<byte>((byte*)p + offset, count));
    }
}

public sealed class DataRefRegistration : IDisposable
{
    public static DataRefRegistration Register<T>(string name, bool isWritable, Accessor<T> accessor) where T : unmanaged =>
        new(name, DataTypes.Data, isWritable, accessor);

    public DataRef DataRef { get; }

    readonly GCHandle _handle;

    public DataRefRegistration(string name, bool isWritable, IntAccessor accessor) :
        this(name, DataTypes.Int | DataTypes.Float | DataTypes.Double, isWritable, accessor)
    { }

    public DataRefRegistration(string name, bool isWritable, FloatAccessor accessor) :
        this(name, DataTypes.Float | DataTypes.Double, isWritable, accessor)
    { }

    public DataRefRegistration(string name, bool isWritable, DoubleAccessor accessor) :
        this(name, DataTypes.Float | DataTypes.Double, isWritable, accessor)
    { }

    public DataRefRegistration(string name, bool isWritable, IntArrayAccessor accessor) :
       this(name, DataTypes.IntArray, isWritable, accessor)
    { }

    public DataRefRegistration(string name, bool isWritable, FloatArrayAccessor accessor) :
        this(name, DataTypes.FloatArray, isWritable, accessor)
    { }

    public unsafe DataRefRegistration(string name, DataTypes type, bool isWritable, IAccessor accessor)
    {
        [DllImport(Defs.Lib)]
        unsafe static extern nint XPLMRegisterDataAccessor([MarshalAs(UnmanagedType.LPUTF8Str)] string dataName, int dataType, int isWritable,
            delegate* unmanaged<nint, int> readInt, delegate* unmanaged<nint, int, void> writeInt,
            delegate* unmanaged<nint, float> readFloat, delegate* unmanaged<nint, float, void> writeFloat,
            delegate* unmanaged<nint, double> readDouble, delegate* unmanaged<nint, double, void> writeDouble,
            delegate* unmanaged<nint, int*, int, int, int> readIntVector,
            delegate* unmanaged<nint, int*, int, int, void> writeIntVector,
            delegate* unmanaged<nint, float*, int, int, int> readFloatVector,
            delegate* unmanaged<nint, float*, int, int, void> writeFloatVector,
            delegate* unmanaged<nint, byte*, int, int, int> readByteVector,
            delegate* unmanaged<nint, byte*, int, int, void> writeByteVector,
            nint readRefcon, nint writeRefcon);

        static IAccessor A(nint handle) => (IAccessor)GCHandle.FromIntPtr(handle).Target!;

        [UnmanagedCallersOnly]
        static int ReadInt(nint handle) => A(handle).AsInt;

        [UnmanagedCallersOnly]
        static void WriteInt(nint handle, int value) => A(handle).AsInt = value;

        [UnmanagedCallersOnly]
        static float ReadFloat(nint handle) => A(handle).AsFloat;

        [UnmanagedCallersOnly]
        static void WriteFloat(nint handle, float value) => A(handle).AsFloat = value;

        [UnmanagedCallersOnly]
        static double ReadDouble(nint handle) => A(handle).AsDouble;

        [UnmanagedCallersOnly]
        static void WriteDouble(nint handle, double value) => A(handle).AsDouble = value;

        [UnmanagedCallersOnly]
        static int ReadIntVector(nint handle, int* values, int offset, int maxLength)
        {
            var a = A(handle);
            return values == null ? a.CountIntVector() : a.ReadIntVector(offset, new(values, maxLength));
        }

        [UnmanagedCallersOnly]
        static void WriteIntVector(nint handle, int* values, int offset, int count) =>
            A(handle).WriteIntVector(offset, new(values, count));

        [UnmanagedCallersOnly]
        static int ReadFloatVector(nint handle, float* values, int offset, int maxLength)
        {
            var a = A(handle);
            return values == null ? a.CountFloatVector() : a.ReadFloatVector(offset, new(values, maxLength));
        }

        [UnmanagedCallersOnly]
        static void WriteFloatVector(nint handle, float* values, int offset, int count) =>
            A(handle).WriteFloatVector(offset, new(values, count));

        [UnmanagedCallersOnly]
        static int ReadByteVector(nint handle, byte* values, int offset, int maxLength)
        {
            var a = A(handle);
            return values == null ? a.CountByteVector() : a.ReadByteVector(offset, new(values, maxLength));
        }

        [UnmanagedCallersOnly]
        static void WriteByteVector(nint handle, byte* values, int offset, int count) =>
            A(handle).WriteByteVector(offset, new(values, count));

        var isInt = type.HasFlag(DataTypes.Int);
        var isFloat = type.HasFlag(DataTypes.Float);
        var isDouble = type.HasFlag(DataTypes.Double);
        var isIntArray = type.HasFlag(DataTypes.IntArray);
        var isFloatArray = type.HasFlag(DataTypes.FloatArray);
        var isData = type.HasFlag(DataTypes.Data);
        nint r = GCHandle.ToIntPtr(_handle = GCHandle.Alloc(accessor));
        DataRef = new(XPLMRegisterDataAccessor(name, (int)type, isWritable ? 1 : 0,
            isInt ? &ReadInt : null, isInt ? &WriteInt : null,
            isFloat ? &ReadFloat : null, isFloat ? &WriteFloat : null,
            isDouble ? &ReadDouble : null, isDouble ? &WriteDouble : null,
            isIntArray ? &ReadIntVector : null, isIntArray ? &WriteIntVector : null,
            isFloatArray ? &ReadFloatVector : null, isFloatArray ? &WriteFloatVector : null,
            isData ? &ReadByteVector : null, isData ? &WriteByteVector : null,
            r, r
        ));
    }

    ~DataRefRegistration() => Dispose();

    bool _disposed;
    public void Dispose()
    {
        if (!_disposed)
        {
            [DllImport(Defs.Lib)]
            static extern void XPLMUnregisterDataAccessor(nint dataRef);

            XPLMUnregisterDataAccessor(DataRef._id);
            _handle.Free();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

public sealed class Shared : IDisposable
{
    public static unsafe bool TryShare(string name, DataTypes type, Action notification, out Shared share)
    {
        [DllImport(Defs.Lib)]
        static extern unsafe int XPLMShareData([MarshalAs(UnmanagedType.LPUTF8Str)] string dataName, DataTypes dataType, delegate* unmanaged<nint, void> notification, nint handle);

        var h = GCHandle.Alloc(notification);
        if (XPLMShareData(name, type, &Notify, GCHandle.ToIntPtr(h)) == 0)
            return false;
        share = new(name, type, h);
        return true;
    }

    [UnmanagedCallersOnly]
    internal static void Notify(nint handle) => ((Action)GCHandle.FromIntPtr(handle).Target!)();

    public string Name { get; }

    public DataTypes Type { get; }

    readonly GCHandle _handle;

    internal Shared(string name, DataTypes type, GCHandle handle) =>
        (Name, Type, _handle) = (name, type, handle);

    ~Shared() => Dispose();

    bool _disposed;
    public unsafe void Dispose()
    {
        if (!_disposed)
        {
            [DllImport(Defs.Lib)]
            static extern unsafe void XPLMUnshareData([MarshalAs(UnmanagedType.LPUTF8Str)] string dataName, DataTypes dataType, delegate* unmanaged<nint, void> notification, nint handle);

            XPLMUnshareData(Name, Type, &Notify, GCHandle.ToIntPtr(_handle));
            _handle.Free();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public DataRef? FindDataRef() => DataRef.Find(Name);
}