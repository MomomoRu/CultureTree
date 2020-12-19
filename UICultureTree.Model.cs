using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Game.Hotfix
{
    public partial class UICultureTree : AnimatorForm
    {
        public Transform Viewport;
        public Image Content;
        public Transform rootTechLink;
        public Transform rootTechPoint;
        public Transform rootTechUnit;
        public CIV_Button btnBack;
        public CIV_Button btnLearn;
        public CIV_Text txtTimeTitle;
        public CIV_Text txtTime;
        public Image imgLearn;
        public Slider sliderLearn;
        public Image imgLearnFill;
        public Transform fxResearching;
        public Transform fxUnlock;
        public Transform fxCollect;
        
        private void clearListeners()
        {
            btnBack.onClick.RemoveAllListeners();
            btnLearn.onClick.RemoveAllListeners();
       }
    }
}