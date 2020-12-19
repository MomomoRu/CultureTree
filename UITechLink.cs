using UnityEngine;
using UnityEngine.UI;

namespace Game.Hotfix
{
    public class UITechLink : UICultureTreeUnitBase
    {
        [SerializeField] private RectTransform m_LineEnd = null;
        [SerializeField] private RectTransform m_LineBody = null;
        [SerializeField] private Image imgLine = null;

        private EntityRequest<AppendParticleEntity> unlockFX;
        private RectTransform bgLineBody;       // 顯示亮線時的底部暗線
        private float curLinkLength;        
        private bool initViewFinish = false;    // 初次顯示不須播放特效
        private float finalLength = 0.0f;
        private float learnPassTime = 999.0f;
        private bool updateLink = false;

        public override Vector3 UiFollowPosition { get { return mRectTrans.anchoredPosition3D + m_LineEnd.anchoredPosition3D * 0.5f; } }

        public override UnitType unitType { get { return UnitType.Linkline; } }

        public UITechPoint StartPoint { get; private set; }

        public UITechPoint EndPoint { get { return mEndPoint; } }
        private UITechPoint mEndPoint = null;

        public CultureTreeTechType techType
        {
            get
            {
                UITechPoint typePoint;
                if (StartPoint.IsCenterPoint)
                    typePoint = EndPoint;
                else if (EndPoint.IsCenterPoint)
                    typePoint = StartPoint;
                else
                {
                    typePoint = (StartPoint.techType != CultureTreeTechType.Special) ? StartPoint : EndPoint;
                }
                return typePoint.techType;
            }
        }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            unlockFX = new EntityRequest<AppendParticleEntity>();
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            updateLink = false;
            var data = userData as UITechLinkData;
            SetStartPoint(data.StartPoint);
            SetAnchor(data.AnchorPoint);            
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (updateLink)
                UpdateBody(true);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);

            unlockFX.Release();

            if (bgLineBody != null)
                Destroy(bgLineBody.gameObject);
        }

        public bool LinkWithPoint(UITechPoint _checkPoint)
        {
            if (_checkPoint == null)
                return false;

            if (StartPoint == _checkPoint)
                return true;

            if (mEndPoint == _checkPoint)
                return true;

            return false;
        }

        public void SetStartPoint(UITechPoint _point)
        {
            StartPoint = _point;

            UpdateBody();
        }

        public override void MovePosition2D(Vector2 _pos)
        {
            _pos -= mRectTrans.anchoredPosition;
            m_LineEnd.anchoredPosition3D = new Vector3(_pos.x, _pos.y, 0f);
        }

        public override bool SetAnchor(UICultureTreeUnitBase _anchorUnit)
        {
            var tech = _anchorUnit as UITechPoint;

            if (tech == null)
                return false;

            if (tech == StartPoint)
                return false;

            if (tech == mEndPoint)
                return false;

            mHasAnchor = true;
            mEndPoint = tech;
            transform.SetAsFirstSibling();

            UpdateBody();

            return true;
        }

        public void SwitchStartEndPoint()
        {
            var startPoint = StartPoint;
            StartPoint = mEndPoint;
            mEndPoint = startPoint;

            UpdateBody();
        }

        public override void SetStatus(UnitStatus _status)
        {
            var iconSet = CultureTreeDataEditor.Setting.GetTechIconSet(techType);

            switch (_status)
            {
                case UnitStatus.Learned:
                    imgLine.LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgLineLight);
                    if (SimulateState != _status && initViewFinish)
                    {
                        bgLineBody = Instantiate(m_LineBody, m_LineBody.parent);
                        bgLineBody.SetAsFirstSibling();
                        bgLineBody.GetComponent<Image>().LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgLineUnlight);

                        m_LineBody.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);

                        unlockFX.LoadEntity(new AppendParticleEntityData(GameCore.Entity.GenerateSerialId(), Constant.Resource.GetTechLineFX(techType), 0, m_LineBody),
                        (fxEntity) => 
                        {   
                            curLinkLength = 0f;
                            learnPassTime = 0.0f;
                            updateLink = true;
                        });
                    }
                    break;
                case UnitStatus.Unlock:
                case UnitStatus.Locked:
                    imgLine.LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgLineUnlight);
                    break;
            }

            SimulateState = _status;
            initViewFinish = true;
        }

        public override bool RevertUnit()
        {
            if (!mHasAnchor)
                return false;

            return true;
        }

        private void UpdateBody(bool checkUpdateFlag = false)
        {
            if (StartPoint != null)
                mRectTrans.anchoredPosition3D = StartPoint.UiFollowPosition;

            if (mEndPoint != null)
            {
                Vector3 endPos = mEndPoint.UiFollowPosition - mRectTrans.anchoredPosition3D;
                m_LineEnd.anchoredPosition3D = new Vector3(endPos.x, endPos.y, 0f);
            }

            var performTime = CultureTreeDataEditor.Setting.performUnlockTime;

            finalLength = m_LineEnd.anchoredPosition3D.magnitude;

            curLinkLength = (SimulateState == UnitStatus.Learned && learnPassTime / performTime < 1) ? Mathf.Lerp(0.0f, finalLength, learnPassTime / performTime) : finalLength;

            Vector3 bodyPos = m_LineEnd.anchoredPosition3D * 0.5f;

            //m_LineBody.anchoredPosition3D = mRectTrans.anchoredPosition3D;
            m_LineBody.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, curLinkLength);
            m_LineBody.localRotation = Quaternion.FromToRotation(Vector3.up, bodyPos);

            learnPassTime += Time.deltaTime;

            if (checkUpdateFlag)
            {
                updateLink = (learnPassTime / CultureTreeDataEditor.Setting.performUnlockTime <= 1);
                UpdateFX(!updateLink, curLinkLength);

                if (!updateLink)
                {
                    // 亮線演出結束
                    Destroy(bgLineBody.gameObject);
                }
            }
        }

        private void UpdateFX(bool finish, float pos)
        {   
            if (finish)
            {
                unlockFX.Release();
            }
            else
            {
                unlockFX.entity.CachedTransform.localPosition = new Vector3(0.0f, pos, 0.0f);
            }
        }
    }
}

