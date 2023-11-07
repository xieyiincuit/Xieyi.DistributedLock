using System.Reflection;
using System.Text;

namespace Xieyi.DistributedLock.Test;

internal class TestHelper
{
    public static byte[] StringToBytes(string value)
    {
        if (value == null)
            return null;

        return Encoding.UTF8.GetBytes(value);
    }

    public static bool ArrayEquals(byte[] array1, byte[] array2)
    {
        if (ReferenceEquals(array1, array2))
            return true;

        if (array1 == null || array2 == null)
            return false;

        if (array1.Length != array2.Length)
            return false;

        for (int i = 0; i < array1.Length; ++i)
        {
            if (array1[i] != array2[i])
                return false;
        }

        return true;
    }

    public static object GetPropertyValue(object instance, string name)
    {
        var type = instance.GetType();
        var propertyInfo = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
        return propertyInfo?.GetValue(instance);
    }

    public static object GetFieldValue(object instance, string name)
    {
        var type = instance.GetType();
        var fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        return fieldInfo?.GetValue(instance);
    }
}