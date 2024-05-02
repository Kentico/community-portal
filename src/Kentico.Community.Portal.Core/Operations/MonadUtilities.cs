namespace CSharpFunctionalExtensions;

public static class MonadUtilities
{
    public static void Noop() { }
#pragma warning disable IDE0060 // Remove unused parameter
    public static void Noop<T>(T val) { }
#pragma warning restore IDE0060 // Remove unused parameter
}
