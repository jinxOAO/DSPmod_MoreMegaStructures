using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreMegaStructure
{
    class Utils
    {
        static Random randSeed = new Random();

        public static VectorLF3 RandPosDelta()
        {
            return new VectorLF3(randSeed.NextDouble() - 0.5, randSeed.NextDouble() - 0.5, randSeed.NextDouble() - 0.5);
        }

        public static int RandInt(int min, int max)
        {
            return randSeed.Next(min, max);
        }
    }
}
