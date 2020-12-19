using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Hotfix
{
    public class CultureTreeConfig : ScriptableObject
    {
        [Serializable]
        public class IconSet
        {
            public CultureTreeTechType techType;

            [Header("Tech point :")]
            public string imgPointLocked;
            public string imgPointUnlock;
            public string imgPointLearned;
            public string imgPointTimeBar;

            [Header("Tech line :")]
            public string imgLineLight;
            public string imgLineUnlight;

            [Header("Lock :")]
            public string imgLock;
        }

        public static int techPointEntityID = 26;
        public static int techLinkEntityID = 27;
        public float performUnlockTime = 0.25f;     // 解鎖的演出時間
        public List<IconSet> techIcon;

        public IconSet GetTechIconSet(CultureTreeTechType techType)
        {
            return techType == CultureTreeTechType.None ? null : techIcon[(int)techType - 1];
        }
    }

}
