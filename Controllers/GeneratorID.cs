using System.Collections;

namespace MyFinances.Controllers
{
    public static class GeneratorID
    {
        private static readonly DateOnly date = new(2000, 01, 01);
        public static Int64 Int64IDBasedOnDate(DateTime dt)
        {
            DateOnly refDate = DateOnly.FromDateTime(dt);
            int d = refDate.DayNumber - date.DayNumber;
            Random random = new();
            int p1 = random.Next(999);
            int p2 = random.Next(999);
            if (d >= 365000) { d = d % 365000; }
            return d * 1000000 + (p1 * 1000) + p2;
        }

        public static int TransDictKeyId(string name)
        {
            // the key is generated from the ASCII code of the first two letters of the key and a random number between 0 to 999
            if (name.Length == 0) return 0;
            char[] letters = name.ToCharArray();
            int result = (int)letters[0] * 1000000 + (int)letters[1] * 1000;
            Random rnd = new();
            result += rnd.Next(1000);
            return result;
        }
    }
}
