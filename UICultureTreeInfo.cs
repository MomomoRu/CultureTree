using System.Collections.Generic;
using UnityGameFramework;
using UnityEngine;
using UnityEngine.UI;
using GameFramework;
using GameFramework.Event;
using MEC;

namespace Game.Hotfix
{
    public partial class UICultureTreeInfo : AnimatorForm
    {
        private UITechPoint techPoint;
        private bool updateTime = false;
        private int diamond = 0;
        private int OrgTextStyle = 13;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            txtTittle.SetText(500011);
            txtPeopleCond.SetText(500007);
            txtCondTitle.SetText(500010);
            txtInstant.SetText(31);
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            techPoint = userData as UITechPoint;
            addListeners(techPoint);
            RefreshView(techPoint);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
            clearListeners();
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (updateTime)
            {
                int time = GameCore.NetData.GetRemainTime(NetDataComponent.TechEnchantID).Value;
                if (time > 0)
                    txtTime.SetText(StringExtension.GetTimeString(time));
                else
                    GameCore.UI.CloseUIForm(this);
            }
        }

        #region ui event

        private void onInstantLearnTech()
        {
            var techEnchant = GameCore.NetData.GetEnchant(NetDataComponent.TechEnchantID);
            if (techEnchant == null || techEnchant.Param1 != techPoint.TableID)
                GameCore.NetData.SendLearnTech(techPoint.TableID, NE_LearnTechC.Types.EOperate.EFinish, diamond);
            else
                GameCore.NetData.FinishEnchantByDiamond(NetDataComponent.TechEnchantID, diamond);
            
            GameCore.UI.CloseUIForm(this);
        }

        private void addListeners(UITechPoint techPoint)
        {
            btnClose.onClick.AddListener(() => { GameCore.UI.CloseUIForm(this); });

            // 研究 or 加速完成
            btnOK.onClick.AddListener(() =>
            {
                var techEnchant = GameCore.NetData.GetEnchant(NetDataComponent.TechEnchantID);
                if (techEnchant == null)
                {
                    // 研究
                    if (CheckLearnTechCondition())
                    {
                        GameCore.NetData.SendLearnTech(techPoint.TableID, NE_LearnTechC.Types.EOperate.ENormal);
                        GameCore.UI.CloseUIForm(this);
                    }                    
                }
                else
                {
                    if (techEnchant.Param1 == techPoint.TableID)
                    {
                        // 加速完成
                        GameCore.Utility.UnopenedComfirm();
                    }
                    else
                    {
                        // 目前有其他科技正在研究中
                        WorldSystems.MsgSys.AddPersonalMsg(GameCore.Localization.GetString(500006));
                    }
                }
            });

            // 立刻研究
            btnInstant.onClick.AddListener(() =>
            {
                if (GameCore.Setting.GetBool(Constant.SettingKey.DiamondConfirm, true))
                {
                    UIComfirm.ShowComfirm(GameCore.Localization.GetString(29),
                        Utility.Text.Format(GameCore.Localization.GetString(500013), diamond),
                        (obj) => { onInstantLearnTech(); }, 
                        null);
                }
                else
                {
                    onInstantLearnTech();
                }
            });
        }

        #endregion

        #region refresh ui

        private List<string> GetAttrubuteString(int attributeID)
        {
            if (attributeID > 0)
            {
                List<string> attributeStrings = new List<string>();
                var attribute = GameCore.DataTable.GetDataTable<Attributes>().GetDataRow(attributeID);
                var effectStrDatas = attribute.EffectStrDatas;
                var values = attribute.Values;
                for (int i = 0; i < effectStrDatas.Length; i++)
                {
                    int attributeValue = values[i][0];
                    string title = GameCore.Localization.GetString(effectStrDatas[i].GetText1ID(attributeValue));
                    string value = effectStrDatas[i].GetTextByType3(attributeValue);
                    attributeStrings.Add(Utility.Text.Format("{0}:{1}", title, value));
                }
                return attributeStrings;
            }
            return null;
        }

        private void RefreshDescription(CultureTreeData techData)
        {
            if (techData.IsUnlockByInspire())
            {
                InfoScrollRect.gameObject.SetActive(false);
                InfoScrollRect_withPIC.gameObject.SetActive(true);
                txtInfoWithPic.SetText(techData.string_id, GameFramework.Localization.StringEnum.Text1);
                txtAddPeopleWithPic.SetText(Utility.Text.Format("{0}:{1}", GameCore.Localization.GetString(500008), techData.ppl_add));
                imgBuildOrSolider.LoadIcon(techData.GetImageIconAtlas(), techData.img);
            }
            else
            {
                InfoScrollRect.gameObject.SetActive(true);
                InfoScrollRect_withPIC.gameObject.SetActive(false);
                txtInfo.SetText(techData.string_id, GameFramework.Localization.StringEnum.Text1);
                txtAddPeople.SetText(Utility.Text.Format("{0}:{1}", GameCore.Localization.GetString(500008), techData.ppl_add));
            }
        }

        private void RefreshCondition(bool enable, CultureTreeData techData = null)
        {
            rootCondition.gameObject.SetActive(enable);

            if (enable && techData != null)
                txtCondition.SetText(techData.string_id, GameFramework.Localization.StringEnum.Text2);
        }

        private void RefreshResource(bool enable, CultureTreeData techData = null)
        {
            // 消耗人口
            rootPeople.gameObject.SetActive(enable);

            // 消耗資源
            rootResource.gameObject.SetActive(enable);

            if (enable && techData != null)
            {
                // 消耗人口
                txtPeople.SetText(techData.ppl.GeneralFormat());
                txtPeople.ChangeStyle(GameCore.NetData.Character.Population > techData.ppl ? OrgTextStyle : TextStyleConfig.RedTextStyle);

                // 消耗資源
                rootGold.gameObject.SetActive(techData.gold > 0);
                rootPolitic.gameObject.SetActive(techData.politic > 0);
                rootCraft.gameObject.SetActive(techData.craft > 0);

                if (techData.gold > 0)
                {
                    txtGold.SetText(techData.gold.GeneralFormat());
                    txtGold.ChangeStyle(GameCore.NetData.Character.Gold > techData.gold ? OrgTextStyle : TextStyleConfig.RedTextStyle);
                }
                if (techData.politic > 0)
                {
                    txtPolitic.SetText(techData.politic.GeneralFormat());
                    txtPolitic.ChangeStyle(GameCore.NetData.Character.Politic > techData.politic ? OrgTextStyle : TextStyleConfig.RedTextStyle);
                }
                if (techData.craft > 0)
                {
                    txtCraft.SetText(techData.craft.GeneralFormat());
                    txtCraft.ChangeStyle(GameCore.NetData.Character.Craft > techData.craft ? OrgTextStyle : TextStyleConfig.RedTextStyle);
                }
            }

            txtSuccess.gameObject.SetActive(!enable);
            txtSuccess.SetText(500005);
        }

        private void RefreshConsumeTime(bool enableRoot, bool enableTimeInfo = false, CultureTreeData techData = null)
        {
            updateTime = false;
            rootBottom.gameObject.SetActive(enableRoot);
            rootBottomBtn.gameObject.SetActive(enableTimeInfo);
            rootLocked.gameObject.SetActive(!enableTimeInfo);

            if (!enableTimeInfo)
                return;

            var techEnchant = GameCore.NetData.GetEnchant(NetDataComponent.TechEnchantID);
            if (techEnchant != null && techEnchant.Param1 == techPoint.TableID)
            {
                // 當前科技正在研究中
                txtOK.SetText(32);
                txtTime.SetText(StringExtension.GetTimeString(GameCore.NetData.GetRemainTime(NetDataComponent.TechEnchantID).Value));
                txtCost.SetText(diamond.ToString());
                updateTime = true;
            }
            else
            {
                // 無研究中科技 or 有其他科技正在研究中
                txtOK.SetText(500004);
                txtTime.SetText(StringExtension.GetTimeString(techData.time));
                txtCost.SetText(diamond.ToString());
            }
        }

        private void RefreshView(UITechPoint techPoint)
        {
            var techData = GameCore.DataTable.GetData<CultureTreeData>(techPoint.TableID);
            var iconSet = CultureTreeDataEditor.Setting.GetTechIconSet((CultureTreeTechType)techData.type);
            imgTechIcon.LoadIcon(Constant.IconAtlas.CultureTree, iconSet.imgPointLearned);

            RefreshDescription(techData);

            var techInfo = GameCore.NetData.GetTechInfo(techPoint.TableID);
            if (techInfo != null)
            {
                txtTechName.SetText(Utility.Text.Format("{0} {1}/{2}", GameCore.Localization.GetString(techData.string_id), techInfo.Level, techData.max_lv));

                if (techInfo.Level == techData.max_lv)
                {
                    // 已研究完成
                    RefreshCondition(false);
                    RefreshResource(false);
                    RefreshConsumeTime(false);
                }
                else
                {
                    // 已學習之科技
                    RefreshCondition(false);
                    RefreshResource(true, techData);
                    RefreshConsumeTime(true, true, techData);
                }
            }
            else
            {
                RefreshResource(true, techData);

                rootBottom.gameObject.SetActive(true);
                txtTechName.SetText(Utility.Text.Format("{0} 0/{1}", GameCore.Localization.GetString(techData.string_id), techData.max_lv));

                switch (techPoint.SimulateState)
                {
                    case UICultureTreeUnitBase.UnitStatus.Locked:
                        RefreshCondition(true, techData);
                        RefreshConsumeTime(true, false);
                        if (!techData.CheckPopulationCondition(GameCore.NetData.Character.Population))
                            txtLocked.SetText(500014);
                        else if (techData.IsUnlockByInspire())
                            txtLocked.SetText(500015);
                        else if (techData.previous > 0)
                        {
                            // 有前置科技, 顯示前置科技資訊
                            var preTech = GameCore.DataTable.GetData<CultureTreeData>(techData.previous);
                            var lockInfo = Utility.Text.Format(GameCore.Localization.GetString(500017), GameCore.Localization.GetString(preTech.string_id), techData.previous_lv);
                            txtLocked.SetText(lockInfo);
                        }
                        else
                        {
                            txtLocked.SetText(500016);
                        }                        
                        break;

                    case UICultureTreeUnitBase.UnitStatus.Unlock:
                        RefreshCondition(false);
                        RefreshConsumeTime(true, true, techData);
                        break;
                }
            }

            ForceRefreshView();
        }

        private void ForceRefreshView()
        {
            Timing.CallDelayed(0.01f, () => 
            {
                gameObject.SetActive(false);
                gameObject.SetActive(true);                
            });            
        }

        #endregion

        private bool CheckLearnTechCondition()
        {
            var techData = GameCore.DataTable.GetData<CultureTreeData>(techPoint.TableID);

            // check resource
            if (GameCore.NetData.Character.Gold < techData.gold ||
                GameCore.NetData.Character.Politic < techData.politic ||
                GameCore.NetData.Character.Craft < techData.craft)
            {
                // TODO : 資源不足
                GameCore.Utility.UnopenedComfirm();
            }
            return true;
        }
    }
}