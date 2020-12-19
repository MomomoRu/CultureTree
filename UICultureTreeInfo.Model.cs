using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Hotfix
{
    public partial class UICultureTreeInfo : AnimatorForm
    {
        public CIV_Text txtTittle;
        public Image imgTechIcon;
        public CIV_Text txtTechName;
        public Transform rootPeople;
        public CIV_Text txtPeople;
        public CIV_Text txtPeopleCond;
        public Image InfoScrollRect;
        public CIV_Text txtInfo;
        public CIV_Text txtAddPeople;
        public Image InfoScrollRect_withPIC;
        public Image imgBuildOrSolider;
        public CIV_Text txtInfoWithPic;
        public CIV_Text txtAddPeopleWithPic;
        public Transform rootCondition;
        public CIV_Text txtCondTitle;
        public CIV_Text txtCondition;
        public Transform rootResource;
        public Transform rootPolitic;
        public CIV_Text txtPolitic;
        public Transform rootCraft;
        public CIV_Text txtCraft;
        public Transform rootGold;
        public CIV_Text txtGold;
        public CIV_Text txtSuccess;
        public Transform rootBottom;
        public Transform rootLocked;
        public CIV_Text txtLocked;
        public Transform rootBottomBtn;
        public CIV_Text txtTime;
        public CIV_Button btnOK;
        public CIV_Text txtOK;
        public CIV_Text txtCost;
        public CIV_Button btnInstant;
        public CIV_Text txtInstant;
        public CIV_Button btnClose;
        
        private void clearListeners()
        {
            btnOK.onClick.RemoveAllListeners();
            btnInstant.onClick.RemoveAllListeners();
            btnClose.onClick.RemoveAllListeners();
       }
    }
}