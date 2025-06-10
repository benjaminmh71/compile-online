using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CompileOnline.Game
{
    public static class Utility
    {
        public static Godot.RandomNumberGenerator random = new Godot.RandomNumberGenerator();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.RandiRange(0, n);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
