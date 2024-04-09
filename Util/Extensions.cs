using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Godot;
using RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;

public static class Extensions
{
    /// <summary>
    /// Add / Remove children from a node until it has the specified number of children
    /// </summary>
    /// <param name="parent">Parent node to add/remove children from</param>
    /// <param name="packedScene">packed scene to instantiate new children from</param>
    /// <param name="count">the target number of children</param>
    public static void AddRemoveChildren(this Node parent, PackedScene packedScene, int count)
    {
        if(count < 0) { return; }


        int childCount = parent.GetChildCount();

        if(childCount == count)
        {
            return;
        } else if(childCount < count)
        {
            for (int i = 0; i < count - childCount; i++)
            {
                parent.AddChild(packedScene.Instantiate());
            }
        } else
        {
            for (int i = 0; i < childCount - count; i++)
            {
                Node child = parent.GetChild(childCount - i - 1);
                if(child != null)
                {
                    child.QueueFree();
                }
            }
        }
    }



    /// <summary>
    /// Fisher–Yates shuffle of a list
    /// </summary>
    /// <typeparam name="T">list element type</typeparam>
    /// <param name="list">list to be shuffled</param>
    public static void Shuffle<T>(this IList<T> list)
    {

        Span<byte> rngBytes = stackalloc byte[Marshal.SizeOf<uint>()];
        for(uint i = (uint)list.Count - 1; i > 0; i--)
        {
            rng.GetBytes(rngBytes);

            uint j;
            unsafe
            {
                fixed(byte* pRngBytes = rngBytes)
                {
                    j = *(uint*)pRngBytes;
                }
            }

            j %= i + 1;

            if(j != i)
            {
                T value = list[(int)i];
                list[(int)i] = list[(int)j];
                list[(int)j] = value;
            }
        }
    }

    private static RandomNumberGenerator rng = RandomNumberGenerator.Create();


    /// <summary>
    /// Get a random ulong
    /// </summary>
    /// <returns></returns>
    public static ulong RandomULong()
    {
        Span<byte> rngBytes = stackalloc byte[Marshal.SizeOf<ulong>()];
        rng.GetBytes(rngBytes);
        ulong ret;
        unsafe
        {
            fixed (byte* pRngBytes = rngBytes)
            {
                ret = *(ulong*)pRngBytes;
            }
        }
        return ret;
    }

    /// <summary>
    /// Get a random number of type T
    /// </summary>
    /// <returns></returns>
    public static T RandomNumber<T>()
    {
        Span<byte> rngBytes = stackalloc byte[Marshal.SizeOf<T>()];
        rng.GetBytes(rngBytes);
        T ret;
        unsafe
        {
            fixed (byte* pRngBytes = rngBytes)
            {
                ret = *(T*)pRngBytes;
            }
        }
        return ret;
    }


    /// <summary>
    /// Select count number of list members randomly
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static T[] RandomChoice<T>(this IEnumerable<T> list, int count)
    {
        T[] ret = new T[count];

        T[] array = list.ToArray();

        HashSet<uint> selectedIndicies = new HashSet<uint>();

        Span<byte> rngBytes = stackalloc byte[Marshal.SizeOf<uint>()];

        uint arrayLength = (uint)array.Length;
        for (int i = 0; i < Math.Clamp(count,0,array.Length); i++)
        {
            uint j;
            do
            {
                rng.GetBytes(rngBytes);
                unsafe
                {
                    fixed (byte* pRngBytes = rngBytes)
                    {
                        j = *(uint*)pRngBytes;
                    }
                }
                j %= arrayLength;
            } while (selectedIndicies.Contains(j));

            ret[i] = array[j];
            selectedIndicies.Add(j);
        }

        return ret;
    }

    public static T[] PopRandom<T>(this HashSet<T> hashSet, int count)
    {
        T[] ret = new T[count];
        T[] array = hashSet.ToArray();

        HashSet<uint> selectedIndicies = new HashSet<uint>();

        Span<byte> rngBytes = stackalloc byte[Marshal.SizeOf<uint>()];

        uint arrayLength = (uint)array.Length;
        for (int i = 0; i < Math.Clamp(count, 0, array.Length); i++)
        {
            uint j;
            do
            {
                rng.GetBytes(rngBytes);
                unsafe
                {
                    fixed (byte* pRngBytes = rngBytes)
                    {
                        j = *(uint*)pRngBytes;
                    }
                }
                j %= arrayLength;
            } while (selectedIndicies.Contains(j));

            selectedIndicies.Add(j);
            T item = array[j];
            ret[i] = item;
            hashSet.Remove(item);
        }

        return ret;

    }

    public static T PopRandom<T>(this HashSet<T> hashSet)
    {
        T[] array = hashSet.ToArray();

        Span<byte> rngBytes = stackalloc byte[Marshal.SizeOf<uint>()];

        uint j;
        rng.GetBytes(rngBytes);
        unsafe
        {
            fixed (byte* pRngBytes = rngBytes)
            {
                j = *(uint*)pRngBytes;
            }
        }
        j %= (uint)array.Length;

        T item = array[j];
        hashSet.Remove(item);
        return item;
    }
}