using GameFramework.Event;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Game.Hotfix.WorldMap
{
    public class WorldMapCultureTree : WorldMapProcedureBase
    {
        private int? uiFormSerialId;
        private int? resourceUiSerialId;

        protected override void OnEnter(ProcedureOwner procedureOwner, object userData)
        {
            base.OnEnter(procedureOwner, userData);

            uiFormSerialId = GameCore.UI.OpenUIForm(UIFormId.UICultureTree, userData);
            resourceUiSerialId = GameCore.UI.OpenUIForm(UIFormId.UIResourceInfo);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);

            if (GameCore.UI.HasUIForm(uiFormSerialId.Value))
                GameCore.UI.CloseUIForm(uiFormSerialId.Value);

            if (GameCore.UI.HasUIForm(resourceUiSerialId.Value))
                GameCore.UI.CloseUIForm(resourceUiSerialId.Value);
        }

        protected override void OnCloseUIFormComplete(object sender, GameEventArgs e)
        {
            CloseUIFormCompleteEventArgs eventArgs = e as CloseUIFormCompleteEventArgs;

            if (!uiFormSerialId.HasValue || eventArgs.SerialId != uiFormSerialId.Value)
                return;

            // 恢復主介面
            var uiForm = GameCore.UI.GetUIForm(UIFormId.UIWorldMain, Constant.UI.GroupNames[(int)GameFramework.UI.UILevel.Default]);
            GameCore.UI.RefocusUIForm(uiForm.UIForm);

            // 返回世界地圖視角狀態
            ChangeState<WorldMapView>(owner);
        }
    }
}