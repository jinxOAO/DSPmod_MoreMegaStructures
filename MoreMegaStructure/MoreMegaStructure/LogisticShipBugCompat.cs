using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreMegaStructure
{

    [BepInDependency("kumor.plugin.CustomCreateBirthStar")]
    [BepInPlugin("Gnimaerd.DSP.plugin.MMSCCBSCompat", "MMSCCBSCompat", "1.0")]
    public class CustomCreateBirthStarCompat : BaseUnityPlugin
    {
        public static bool enabled = false;
        public void Awake()
        {
            enabled = true;
        }
    }

    [BepInDependency("dsp.galactic-scale.2")]
    [BepInPlugin("Gnimaerd.DSP.plugin.MMSGS2Compat", "MMSGS2Compat", "1.0")]
    public class GalacticScaleCompat : BaseUnityPlugin
    {
        public static bool enabled = false;
        public void Awake()
        {
            enabled = true;
        }
    }
}
