using System.Collections.Generic;
using System.Linq;
using UnityGameFramework.Runtime;
using GameFramework.Event;
using UnityEngine;
using DG.Tweening;

namespace Game.Hotfix
{
    public partial class UICultureTree : AnimatorForm
    {
        private List<UITechPoint> techPoints = new List<UITechPoint>();
        private List<UITechLink> techLinks = new List<UITechLink>();

        private List<int> techPointEntityIDs = new List<int>();
        private List<int> techLinkEntityIDs = new List<int>();

        private Enchant techEnchant;
        private bool updateLearnTime = false;
        private ParticleSystem fxCollectParticle;
        private RectTransform catchRect;
        private RectTransform contentRect;
        private Vector3 contentAnchorPosition = Vector3.zero;
        private int focusTechID = -1;

        // 取得中央科技點
        public UITechPoint CenterPoint() { return techPoints[0]; }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            catchRect = CachedTransform.GetComponent<RectTransform>();
            contentRect = Content.GetComponent<RectTransform>();
            contentAnchorPosition = contentRect.anchoredPosition;
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            focusTechID = (userData == null) ? -1 : (int)userData;
            fxCollectParticle = fxCollect.GetComponentInChildren<ParticleSystem>();

            GameCore.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, onShowEntitySuccessEvent);
            GameCore.Event.Subscribe(NetDataEvent.AddEnchantEvent.EventId, onAddEnchant);
            GameCore.Event.Subscribe(NetDataEvent.UpdateTechEvent.EventId, onUpdateTech);
            GameCore.Event.Subscribe(NetDataEvent.LearnTechEvent.EventId, onLearnTech);

            btnLearn.onClick.AddListener(()=>
            {
                techEnchant = GameCore.NetData.GetEnchant(NetDataComponent.TechEnchantID);
                if (GameCore.NetData.EnchantFinish(NetDataComponent.TechEnchantID))
                {
                    fxCollect.gameObject.SetActive(true);

                    // 採收科技
                    GameCore.NetData.SendLearnTech(techEnchant.Param1, NE_LearnTechC.Types.EOperate.EGet);
                }
                else
                {                    
                    var learningTech = GetTechPoint(techEnchant.Param1);
                    if (learningTech != null)
                    {
                        FocusLearningTechPoint(learningTech);

                        // 開啟研發中科技資訊頁面
                        GameCore.UI.OpenUIForm(UIFormId.UICultureTreeInfo, learningTech);
                    }                        
                }
            });

            btnBack.onClick.AddListener(()=> { GameCore.UI.CloseUIForm(this); });

            RefreshBackground();
            DisplayMap();
            RefreshLearningProgress();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            GameCore.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, onShowEntitySuccessEvent);
            GameCore.Event.Unsubscribe(NetDataEvent.AddEnchantEvent.EventId, onAddEnchant);
            GameCore.Event.Unsubscribe(NetDataEvent.UpdateTechEvent.EventId, onUpdateTech);
            GameCore.Event.Unsubscribe(NetDataEvent.LearnTechEvent.EventId, onLearnTech);

            base.OnClose(isShutdown, userData);
            clearListeners();
            ClearMap();
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (updateLearnTime)
            {
                // 研究中特效時間倒數
                techEnchant = GameCore.NetData.GetEnchant(NetDataComponent.TechEnchantID);
                if (techEnchant == null)
                {
                    updateLearnTime = false;
                }
                else
                {
                    var remainTime = GameCore.NetData.GetRemainTime(NetDataComponent.TechEnchantID);
                    if (remainTime != null)
                    {
                        if (remainTime.Value > 0)
                        {
                            txtTime.SetText(StringExtension.GetTimeString(remainTime.Value));
                            int passTime = (int)GameCore.NetData.UTC - techEnchant.StartTime;
                            sliderLearn.value = (float)passTime / (float)techEnchant.Duration;
                        }
                        else
                        {
                            var techData = GameCore.DataTable.GetData<CultureTreeData>(techEnchant.Param1);
                            fxResearching.gameObject.SetActive(false);
                            fxUnlock.gameObject.SetActive(true);
                            txtTimeTitle.SetText(techData.string_id);
                            txtTime.SetText(500005);
                            sliderLearn.value = 1.0f;
                            updateLearnTime = false;
                            GameCore.UI.CloseUIForm(this);
                        }
                    }                    
                }
            }
        }

        protected override void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
            base.OnDepthChanged(uiGroupDepth, depthInUIGroup);

            // 待深度值設定後, 才刷新科技點Depth資訊
            for (int i = 0; i < techPoints.Count; i++)
            {
                techPoints[i].SetDepth(Depth + 10);
            }
        }

        private void RefreshLearningProgress()
        {
            var techEnchant = GameCore.NetData.GetEnchant(NetDataComponent.TechEnchantID);
            if (techEnchant == null)
            {
                if (fxCollectParticle.isPlaying)
                {
                    // 播放收取特效, 待播畢才關閉介面
                    MEC.Timing.CallDelayed(2f, () => 
                    {
                        btnLearn.gameObject.SetActive(false);
                        fxResearching.gameObject.SetActive(false);
                        fxCollect.gameObject.SetActive(false);
                        fxUnlock.gameObject.SetActive(false);
                        updateLearnTime = false;
                    });
                }
                else
                {
                    btnLearn.gameObject.SetActive(false);
                    fxResearching.gameObject.SetActive(false);
                    fxCollect.gameObject.SetActive(false);
                    fxUnlock.gameObject.SetActive(false);
                    updateLearnTime = false;
                }                
            }
            else
            {
                btnLearn.gameObject.SetActive(true);
                fxResearching.gameObject.SetActive(true);
                var techData = GameCore.DataTable.GetData<CultureTreeData>(techEnchant.Param1);
                var iconSet = CultureTreeDataEditor.Setting.GetTechIconSet((CultureTreeTechType)techData.type);
                imgLearnFill.LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgPointTimeBar);
                imgLearn.LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgPointLearned);

                txtTimeTitle.SetText(techData.string_id);
                txtTime.SetText(StringExtension.GetTimeString(GameCore.NetData.GetRemainTime(NetDataComponent.TechEnchantID).Value));
                int passTime = (int)GameCore.NetData.UTC - techEnchant.StartTime;
                sliderLearn.value = passTime / techEnchant.Duration;

                updateLearnTime = true;
            }
        }

        private void ClearMap()
        {
            for (int i = 0; i < techPoints.Count; i++)
            {
                var techPointID = techPoints[i].Id;
                if (GameCore.Entity.HasEntity(techPointID))
                    GameCore.Entity.HideEntity(techPointID);
            }
            techPoints.Clear();

            for (int i = 0; i < techLinks.Count; i++) 
            {
                var techLinkID = techLinks[i].Id;
                if (GameCore.Entity.HasEntity(techLinkID))
                    GameCore.Entity.HideEntity(techLinkID);
            }
            techLinks.Clear();

            techPointEntityIDs.Clear();
            techLinkEntityIDs.Clear();
        }

        // 產生科技點
        private void ShowTechPoints()
        {
            // Note : 第一筆為虛擬起始科技點
            for (int i = 0; i < CultureTreeDataEditor.TechPointDatas.Count; i++)
            {
                int pointID = GameCore.Entity.GenerateSerialId();
                GameCore.Entity.ShowTechPoint(new UITechPointData(pointID, CultureTreeConfig.techPointEntityID, rootTechPoint.transform, CultureTreeDataEditor.TechPointDatas[i], (i == 0)));                
                techPointEntityIDs.Add(pointID);
            }
        }

        // 產生科技鏈結
        private void ShowTechLinks()
        {
            for (int i = 0; i < CultureTreeDataEditor.TechLinkDatas.Count; i++)
            {
                int linkID = GameCore.Entity.GenerateSerialId();
                var linkData = CultureTreeDataEditor.TechLinkDatas[i];
                GameCore.Entity.ShowTechLink(new UITechLinkData(linkID, CultureTreeConfig.techLinkEntityID, rootTechLink.transform, linkData, techPoints[linkData.LinkA], techPoints[linkData.LinkB]));
                techLinkEntityIDs.Add(linkID);
            }
        }

        // 顯示順序:先顯示科技點, 再設定科技鏈結, 再進行狀態刷新
        private void DisplayMap()
        {
            ShowTechPoints();            
        }

        private UICultureTreeUnitBase.UnitStatus GetTechPointInitStatus(UITechPoint techPoint)
        {
            // 中央起點一開始就解鎖
            if (techPoint.IsCenterPoint)
                return UICultureTreeUnitBase.UnitStatus.Learned;
            else
            {
                var techInfo = GameCore.NetData.GetTechInfo(techPoint.TableID);
                return (techInfo == null) ? UICultureTreeUnitBase.UnitStatus.Locked : UICultureTreeUnitBase.UnitStatus.Learned;
            }
        }

        private void RefreshUnitStatus()
        {
            // 設置科技點初始狀態
            techPoints.ForEach((techPoint) => {　techPoint.SetStatus(GetTechPointInitStatus(techPoint));　});           

            // 調整和learned point有串聯的科技點狀態
            techPoints.ForEach((techPoint) => 
            {
                var techData = GameCore.DataTable.GetData<CultureTreeData>(techPoint.TableID);
                if (techPoint.SimulateState == UICultureTreeUnitBase.UnitStatus.Locked &&
                    techData.CheckPopulationCondition(GameCore.NetData.Character.Population) &&
                    techData.CheckPreTechCondition() &&
                    IsLinkToCenter(techPoint))
                    techPoint.SetStatus(UICultureTreeUnitBase.UnitStatus.Unlock); 
            });

            // 以最後的status表現狀態轉變
            for (int i = 0; i < techPoints.Count; i++)
                techPoints[i].RefreshView();

            // Links要在科技點設置後才刷新(由兩點決定狀態)
            for (int i = 0; i < techLinks.Count; i++)
                RefreshLinkState(techLinks[i]);
        }

        private void CheckLearnedPointLinkToCenter()
        {
            List<UITechPoint> uncheckList = new List<UITechPoint>();

            foreach (var item in techPoints)
                if (item.SimulateState == UICultureTreeUnitBase.UnitStatus.Learned)
                    uncheckList.Add(item);

            List<UITechPoint> checkedList = new List<UITechPoint>();

            for (int i = 0; i < uncheckList.Count; i++)
            {
                var check = uncheckList[i];

                if (checkedList.Contains(check))
                    continue;

                var allLinks = GetAllLinkLearnedPtGroup(check);

                //是否與星盤中心點連線?
                bool isLinkToCenter = allLinks.Contains(CenterPoint());

                for (int j = 0; j < allLinks.Count; j++)
                {
                    if (!isLinkToCenter)
                    {
                        allLinks[j].SetStatus(UICultureTreeUnitBase.UnitStatus.Locked);
                    }
                    checkedList.Add(allLinks[j]);
                }
            }
        }

        //取得所有連線的亮點(包括自己)
        private List<UITechPoint> GetAllLinkLearnedPtGroup(UITechPoint _point)
        {
            var linkPoints = new List<UITechPoint>();

            linkPoints.Add(_point);

            GetLinkLearnedPtsReclusive(_point, ref linkPoints);

            return linkPoints;
        }

        private void GetLinkLearnedPtsReclusive(UITechPoint _points, ref List<UITechPoint> _result)
        {
            var directLinkPoints = GetDirectLinkedPoints(_points);

            for (int i = 0; i < directLinkPoints.Count; i++)
            {
                var directLinkPoint = directLinkPoints[i];

                if (directLinkPoint.SimulateState == UICultureTreeUnitBase.UnitStatus.Learned)
                {
                    if (!_result.Contains(directLinkPoint))
                    {
                        _result.Add(directLinkPoint);
                        GetLinkLearnedPtsReclusive(directLinkPoint, ref _result);
                    }
                }
            }
        }

        //取得所有和_point有直接連線的點(不包含自己)
        private List<UITechPoint> GetDirectLinkedPoints(UITechPoint _point)
        {
            List<UITechPoint> result = new List<UITechPoint>();

            for (int i = 0; i < techLinks.Count; i++)
            {
                var link = techLinks[i];

                if (link.StartPoint == _point)
                {
                    if (!result.Contains(link.EndPoint))
                        result.Add(link.EndPoint);

                    continue;
                }

                if (link.EndPoint == _point)
                {
                    if (!result.Contains(link.StartPoint))
                        result.Add(link.StartPoint);

                    continue;
                }
            }

            return result;
        }

        private void RefreshLinkState(UITechLink _link)
        {
            if (_link.StartPoint.SimulateState == UICultureTreeUnitBase.UnitStatus.Learned && _link.EndPoint.SimulateState == UICultureTreeUnitBase.UnitStatus.Learned)
                _link.SetStatus(UICultureTreeUnitBase.UnitStatus.Learned);
            else
                _link.SetStatus(UICultureTreeUnitBase.UnitStatus.Locked);
        }

        // 科技點是否和中央科技點相連
        private bool IsLinkToCenter(UITechPoint _tech)
        {
            var allLinks = GetAllLinkLearnedPtGroup(_tech);
            return allLinks.Contains(CenterPoint());
        }

        private UITechPoint GetTechPoint(int techID)
        {
            var idx = techPoints.FindIndex(tech => tech.TableID == techID);
            return idx == -1 ? null : techPoints[idx];
        }

        // 對焦至學習中科技點
        private void FocusLearningTechPoint(UITechPoint techPoint)
        {
            Vector3 centerPos = new Vector3(0, -870);
            Vector3 diff = centerPos - techPoint.AnchorPosition;
            Vector3 shift = contentAnchorPosition + diff;
            var halfW = catchRect.rect.width * 0.5f;
            var halfH = catchRect.rect.height * 0.5f;
            var finalX = Mathf.Clamp(shift.x, contentRect.rect.xMin + halfW, contentRect.rect.xMax - halfW);
            var finalY = Mathf.Clamp(shift.y, contentRect.rect.yMin + halfH, contentRect.rect.yMax - halfH);
            contentRect.DOAnchorPos(new Vector2(finalX, finalY), 0.5f);
        }

        // 對焦至學習中科技點
        private void FocusLearningTechPoint(int techID)
        {
            var techPoint = GetTechPoint(techID);
            if (techPoint != null)
            {
                FocusLearningTechPoint(techPoint);
            }
        }

        // 若於主UI進行過科技採收動作, 則採收科技播放解鎖特效
        private void PerformCollectTech()
        {
            var techID = GameCore.Setting.GetInt(Constant.SettingKey.CultureTreeCollectTechID);
            if (techID > 0)
            {
                var techPoint = GetTechPoint(techID);
                FocusLearningTechPoint(techPoint);
                GameCore.Entity.ShowAppendParticle(new AppendParticleEntityData(GameCore.Entity.GenerateSerialId(), Constant.Resource.TechLearnDoneFX, 0, techPoint.CachedTransform, 1, techPoint.Depth+1));                               
                GameCore.Setting.SetInt(Constant.SettingKey.CultureTreeCollectTechID, 0);
            }
        }

        #region event

        // 按下一次技能點的階段 : 未解鎖 > 可解鎖 > 已解鎖
        private void OnClickTech(UITechPoint _tech)
        {
            if (_tech.IsCenterPoint)
                return;

            // 調整有連接到此tech point的link, 其endPoint為此 tech point
            for (int i = 0; i < techLinks.Count; i++)
            {
                var link = techLinks[i];
                if (link.StartPoint == _tech)
                {
                    link.SwitchStartEndPoint();
                }
            }

            GameCore.UI.OpenUIForm(UIFormId.UICultureTreeInfo, _tech);
        }

        private void onShowEntitySuccessEvent(object sender, GameFramework.Event.GameEventArgs e)
        {
            ShowEntitySuccessEventArgs eventArgs = e as ShowEntitySuccessEventArgs;

            if (techPointEntityIDs.Contains(eventArgs.Entity.Id))
            {
                var techPoint = eventArgs.Entity.GetComponent<UITechPoint>();
                if (techPoint == null)
                    return;

                techPoint.SetBtnAction(() => { OnClickTech(techPoint); });
                techPoint.SetDepth(Depth + 10);
                techPoints.Add(techPoint);

                // TechPoint 全數載入完成
                if (techPoints.Count == CultureTreeDataEditor.TechPointDatas.Count)
                {
                    // 開始載入鏈結
                    ShowTechLinks();
                }
            }
            else if (techLinkEntityIDs.Contains(eventArgs.Entity.Id))
            {
                var techLink = eventArgs.Entity.GetComponent<UITechLink>();
                if (techLink == null)
                    return;

                techLinks.Add(techLink);

                // TechLink 全數載入完成
                if (techLinks.Count == CultureTreeDataEditor.TechLinkDatas.Count)
                {
                    // 狀態刷新
                    RefreshUnitStatus();

                    PerformCollectTech();

                    if (focusTechID > 0)
                    {
                        FocusLearningTechPoint(focusTechID);
                    }
                }
            }
        }

        private void onAddEnchant(object sender, GameEventArgs e)
        {
            var msg = e as NetDataEvent.AddEnchantEvent;

            if (msg.EnchantId != NetDataComponent.TechEnchantID)
                return;
                            
            RefreshLearningProgress();

            var techEnchant = GameCore.NetData.GetEnchant(NetDataComponent.TechEnchantID);
            var techPoint = GetTechPoint(techEnchant.Param1);
            if (techPoint == null)
                return;

            techPoint.SetStatus(UICultureTreeUnitBase.UnitStatus.Unlock);
            techPoint.RefreshView();
        }

        private void onUpdateTechPoint(int techID)
        {
            var techPoint = GetTechPoint(techID);
            if (techPoint == null)
                return;

            var techInfo = GameCore.NetData.GetTechInfo(techID);
            if (techInfo == null)
            {
                techPoint.SetStatus(UICultureTreeUnitBase.UnitStatus.Unlock);
                techPoint.RefreshView();
            }
            else
            {
                if (techInfo.Level > 0)
                {
                    techPoint.SetStatus(UICultureTreeUnitBase.UnitStatus.Learned);
                    techPoint.RefreshView();
                }
            }  

            RefreshUnitStatus();
        }

        private void onUpdateTech(object sender, GameEventArgs e)
        {
            var msg = e as NetDataEvent.UpdateTechEvent;

            RefreshLearningProgress();
            onUpdateTechPoint(msg.TechID);
        }

        private void onLearnTech(object sender, GameEventArgs e)
        {
            var msg = e as NetDataEvent.LearnTechEvent;

            RefreshLearningProgress();
            onUpdateTechPoint(msg.TechID);
        }

        #endregion
    }
}