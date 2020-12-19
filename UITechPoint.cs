using UnityEngine;
using UnityEngine.UI;

namespace Game.Hotfix
{
    public class UITechPoint : UICultureTreeUnitBase
    {
        [Header("Timer")]
        [SerializeField] GameObject rootTimer;
        [SerializeField] Slider sliderTime;
        [SerializeField] CIV_Text txtTime;
        [SerializeField] Image imgTimeBar;

        [Header("Locked")]
        [SerializeField] GameObject rootLocked;
        [SerializeField] CIV_Text txtLock;
        [SerializeField] Image imgLocked;
        [SerializeField] Image imgLockIcon;

        [Header("Unlock")]
        [SerializeField] GameObject rootUnlock;       
        [SerializeField] CIV_Text txtUnlockDesp;
        [SerializeField] Image imgUnlock;

        [Header("Learned")]
        [SerializeField] GameObject rootLearned;
        [SerializeField] CIV_Text txtDescription;
        [SerializeField] Image imgLearnedBg;
        [SerializeField] Image imgLearned;

        private CultureTreeData techData;
        private Enchant techEnchant;
        private bool updateUnlockTimer = false;
        private int maxLv = 1;
        private int lv;
        private EntityRequest<AppendParticleEntity> fxResearching;

        private Vector3 mAnchorPosition = Vector3.zero;
        public Vector3 AnchorPosition { get { return mAnchorPosition; } }

        public override Vector3 UiFollowPosition { get { return mRectTrans.anchoredPosition3D; } }

        public override UnitType unitType { get { return UnitType.TechPoint; } }

        public CultureTreeTechType techType { get; private set; }

        private int mTableID = -1;
        public int TableID { get { return mTableID; } }
        
        private bool isCenterPoint = false;
        public bool IsCenterPoint
        {
            get { return isCenterPoint; }
            set { isCenterPoint = value; }
        }

        // 是否達到等級上限
        public bool IsMaxLv { get { return lv == maxLv; } }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            fxResearching = new EntityRequest<AppendParticleEntity>();
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            var data = userData as UITechPointData;
            CachedRectTransform.anchoredPosition3D = new Vector3(data.PointData.x, data.PointData.y, 0f);
            SetAnchor(null);
            SettingID(data.PointData.ID);
            IsCenterPoint = data.IsCenterPoint;
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);

            fxResearching.Release();
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (updateUnlockTimer)
            {
                var remainTime = GameCore.NetData.GetRemainTime(NetDataComponent.TechEnchantID);
                if (remainTime != null)
                {
                    if (remainTime.Value > 0)
                    {
                        txtTime.text = StringExtension.GetTimeString(remainTime.Value);
                        int passTime = (int)GameCore.NetData.UTC - techEnchant.StartTime;
                        sliderTime.value = (float)passTime / (float)techEnchant.Duration;
                    }
                    else
                    {
                        fxResearching.Release();
                        rootTimer.SetActive(false);
                        updateUnlockTimer = false;
                    }
                }                
            }
        }

        private void SetTechType(CultureTreeTechType techType)
        {
            this.techType = techType;

            var iconSet = CultureTreeDataEditor.Setting.GetTechIconSet(techType);
            imgLockIcon.LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgLock);
            imgLocked.LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgPointLocked);
            imgUnlock.LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgPointUnlock);
            imgTimeBar.LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgPointTimeBar);
            imgLearnedBg.LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgPointUnlock);
            imgLearned.LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgPointLearned);
        }

        public void SettingID(int _tableID)
        {
            mTableID = _tableID;

            techData = GameCore.DataTable.GetData<CultureTreeData>(mTableID);
            if (techData != null)
            {
                maxLv = techData.max_lv;

                txtLock.SetText(techData.string_id);
                txtUnlockDesp.SetText(techData.string_id);
                txtDescription.SetText(techData.string_id);

                SetTechType((CultureTreeTechType)techData.type);
            }

            var techInfo = GameCore.NetData.GetTechInfo(TableID);
            lv = (techInfo == null) ? 0 : techInfo.Level;
        }

        public override void SetStatus(UnitStatus _status)
        {
            SimulateState = _status;
        }

        public override void MovePosition2D(Vector2 _pos)
        {
            mRectTrans.anchoredPosition3D = new Vector3(_pos.x, _pos.y, 0f);
        }

        public override bool SetAnchor(UICultureTreeUnitBase _anchorUnit)
        {
            mHasAnchor = true;
            mAnchorPosition = mRectTrans.anchoredPosition3D;

            return true;
        }

        public override bool RevertUnit()
        {
            if (!mHasAnchor)
                return false;

            if (TableID == -1)
                return false;

            mRectTrans.anchoredPosition3D = mAnchorPosition;
            return true;
        }

        public override void RefreshView()
        {
            // 中央虛擬科技點不進行處理
            if (IsCenterPoint)
                return;

            rootLocked.SetActive(SimulateState == UnitStatus.Locked);
            rootUnlock.SetActive(SimulateState == UnitStatus.Unlock);
            rootLearned.SetActive(SimulateState == UnitStatus.Learned);
            rootTimer.SetActive(false);

            switch (SimulateState)
            {
                case UnitStatus.Locked:
                    imgLockIcon.gameObject.SetActive(techData.IsUnlockByInspire());
                    break;

                case UnitStatus.Unlock:
                    RefreshUnlockTimer();                    
                    break;

                case UnitStatus.Learned:
                    var techInfo = GameCore.NetData.GetTechInfo(TableID);
                    if (lv != techInfo.Level)
                    {
                        lv = techInfo.Level;

                        // 升階待線段演出結束後, 播放解鎖特效
                        MEC.Timing.CallDelayed(CultureTreeDataEditor.Setting.performUnlockTime, () =>
                        {
                            GameCore.Entity.ShowAppendParticle(new AppendParticleEntityData(GameCore.Entity.GenerateSerialId(), Constant.Resource.TechLearnDoneFX, 0, CachedTransform));
                        });
                    }
                    imgLearned.fillAmount = (float)techInfo.Level / (float)maxLv;
                    RefreshUnlockTimer();
                    break;
            }
        }

        private void RefreshUnlockTimer()
        {
            techEnchant = GameCore.NetData.GetEnchant(NetDataComponent.TechEnchantID);
            if (techEnchant == null || techEnchant.Param1 != TableID)
            {
                rootTimer.SetActive(false);
                fxResearching.Release();
                updateUnlockTimer = false;
            }
            else
            {
                rootTimer.SetActive(true);
                txtTime.text = StringExtension.GetTimeString(GameCore.NetData.GetRemainTime(NetDataComponent.TechEnchantID).Value);
                int passTime = (int)GameCore.NetData.UTC - techEnchant.StartTime;
                sliderTime.value = (float)passTime / (float)techEnchant.Duration;
                updateUnlockTimer = true;
                fxResearching.LoadEntity(new AppendParticleEntityData(GameCore.Entity.GenerateSerialId(), Constant.Resource.TechLearningFX, 0, CachedTransform, 1, Depth + 10));
            }            
        }
    }
}