using Il2CppInterop.Runtime.InteropTypes.Arrays;

public static class InteropFix
{
    public static T Cast<T>(T value)
    {
        return value;
    }

    public static Il2CppStructArray<T> Cast<T>(T[] value) where T : unmanaged
    {
        return value;
    }

    public static T[] Cast<T>(Il2CppArrayBase<T> value)
    {
        return value;
    }
}
