using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Helpers
{
    public static class Helpers
    {
        public static bool ByteArrayEquals(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;
            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }
            return true;
        }

        public static bool IsEmptyByteArray(byte[] array)
        {
            foreach (var b in array)
            {
                if (b != 0)
                    return false;
            }
            return true;
        }
    }
}
