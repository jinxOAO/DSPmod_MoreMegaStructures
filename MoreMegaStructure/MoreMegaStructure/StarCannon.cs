using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using System.IO;
using UnityEngine.UI;

namespace MoreMegaStructure
{
    class StarCannon
    {
        static bool isBattleActive = false;

        static int state = 0;
        static int timeTick = 0;
        static VectorLF3 targetUPos = new VectorLF3(0, 0, 0);
        static List<Quaternion> revertRotates = new List<Quaternion>();

        public static GameObject fireButtonObj = null;
        public static Button fireButton = null;
        public static Text fireButtonText = null;
        public static Image fireButtonImage = null;

        static Color cannonDisableColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        static Color cannonChargingColor = new Color(0.42f, 0.2f, 0.2f, 1f);
        //static Color cannonReadyColor = new Color(0.877f, 0.547f, 0.368f, 1f);
        //static Color cannonReadyColor = new Color(0.951f, 0.579f, 0.150f, 1f);
        static Color cannonReadyColor = new Color(0f, 0.499f, 0.824f, 1f);
        //static Color cannonAimingColor = new Color(0.877f, 0.547f, 0.368f, 1f);
        static Color cannonAimingColor = new Color(0.973f, 0.359f, 0.170f, 1f);
        static Color cannonFiringColor = new Color(1f, 0.16f, 0.16f, 1f);

        public static void InitUI()
        {
            isBattleActive = MoreMegaStructure.isBattleActive;
            if (!isBattleActive || fireButtonObj != null) return;
            
            GameObject alertUIObj = GameObject.Find("UI Root/Overlay Canvas/In Game/AlertUI");
            GameObject oriButton = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window/storage-box-0/popup-box/sd-option-button-1");

            if (alertUIObj == null || oriButton == null) return;

            fireButtonObj = GameObject.Instantiate(oriButton);
            fireButtonObj.name = "FireButton";
            fireButtonObj.transform.SetParent(alertUIObj.transform, false);
            fireButtonObj.transform.localPosition = new Vector3(-80, -110, 0);
            fireButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 40);

            fireButton = fireButtonObj.GetComponent<Button>();
            fireButton.onClick.RemoveAllListeners();
            fireButton.onClick.AddListener(() => { OnFireButtonClick(); });
            fireButtonText = fireButtonObj.transform.Find("button-text").GetComponent<Text>();
            fireButtonText.text = "恒星炮开火".Translate();
            fireButtonText.fontSize = 18;
            fireButtonText.resizeTextMinSize = 16;
            fireButtonText.resizeTextMaxSize = 18;
            fireButtonImage = fireButtonObj.GetComponent<Image>();
            fireButtonImage.color = cannonReadyColor;
            
        }

        public static void OnFireButtonClick()
        {
            UIRealtimeTip.Popup("nope!");
        }


    }
}
