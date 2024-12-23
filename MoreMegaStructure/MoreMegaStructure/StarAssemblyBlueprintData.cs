using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreMegaStructure
{
    public class StarAssemblyBlueprintData
    {

        public static void PasteOnCurrent()
        {
            if(MoreMegaStructure.curDysonSphere != null)
            {
                ImportInto(MoreMegaStructure.curDysonSphere.starData.index);
            }
        }


        public static void CopyFromCurrent()
        {
            if (MoreMegaStructure.curDysonSphere != null)
            {
                ExportFrom(MoreMegaStructure.curDysonSphere.starData.index);
            }
        }


        public static void ImportInto(int starIndex)
        {
            try
            {

            }
            catch (Exception)
            {

            }
        }


        public static void ExportFrom(int starIndex)
        {

        }
    }
}
