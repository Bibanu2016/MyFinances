namespace MyFinances.Controllers
{
    public static class MyBitOperations
    {
        public static bool GetBit(byte aByte, int pos)
        {
            // Shift the desired bit to the least significant bit (position 0)
            // and then use AND with 1 to check its value.
            // The result will be non-zero (true) if the bit is set, and zero (false) otherwise.
            if (pos < 0 || pos > 7) { return false; }
            return ((aByte >> pos) & 1) != 0;
        }

        public static byte SetBit(byte b, int bit, bool value)
        {
            return (byte)(
                (b & ~(1 << bit)) |
                ((value ? 1 : 0) << bit)
            );
        }

    }
}
