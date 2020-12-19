using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Game.Hotfix
{
    public enum CultureTreeTechType
    {
        None = 0,
        Special = 1,    // 特殊類
        Economy = 2,    // 經濟類
        Attack = 3,     // 攻擊類
        Defense = 4     // 防禦類
    }

    [RequireComponent(typeof(Button))]
    public abstract class UICultureTreeUnitBase : UIItem
    {
        public enum UnitType
        {
            TechPoint,
            Linkline,
        }

        public enum UnitStatus
        {
            Locked = 0, // 未解鎖
            Unlock,     // 可解鎖 or 解鎖中
            Learned,    // 已解鎖
        }

        [SerializeField]
        protected Button m_Btn = null;

        public abstract Vector3 UiFollowPosition { get; }

        public abstract UnitType unitType { get; }

        public UnitStatus SimulateState { get; protected set; }

        protected UnitStatus mViewState { get; set; }

        protected RectTransform mRectTrans { get; private set; }

        protected bool mHasAnchor = false;

        #region 公開方法

        public void SetBtnAction(UnityAction _action)
        {
            m_Btn.onClick.RemoveAllListeners();
            m_Btn.onClick.AddListener(_action);
        }

        public abstract void MovePosition2D(Vector2 _pos);

        public abstract bool RevertUnit();

        public abstract bool SetAnchor(UICultureTreeUnitBase _anchorUnit);

        public abstract void SetStatus(UnitStatus _status);

        #endregion

        #region Mono

        protected virtual void Awake()
        {
            mViewState = SimulateState;
            mRectTrans = this.gameObject.GetComponent<RectTransform>();

            if (m_Btn == null)
                m_Btn = this.gameObject.GetComponent<Button>();
        }

        public virtual void RefreshView() { }

        #endregion
    }

}