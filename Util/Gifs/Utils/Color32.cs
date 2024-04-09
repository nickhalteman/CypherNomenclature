using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


[StructLayout(LayoutKind.Explicit)]
public struct Color32
{
    [FieldOffset(0)]
    public byte r;
    [FieldOffset(1)]
    public byte g;
    [FieldOffset(2)]
    public byte b;
    [FieldOffset(3)]
    public byte a;
    public Color32(byte r = 0, byte g = 0, byte b = 0, byte a = 0)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }
}
