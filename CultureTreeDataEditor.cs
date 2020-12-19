using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityGameFramework.Runtime;

namespace Game.Hotfix
{
    [Serializable]
    public class TechTreeSavePoint
    {
        public int ID;
        public int x;
        public int y;
    }

    [Serializable]
    public class TechTreeSaveLink
    {
        public int LinkA;
        public int LinkB;
    }

    [Serializable]
    public class TechTreeSave
    {
        public TechTreeSavePoint[] Points;  // 第一筆為虛擬起始科技點
        public TechTreeSaveLink[] Links;
    }

    public static class CultureTreeDataEditor
    {
        public static CultureTreeConfig Setting { get; private set; }

        // 文化樹存檔之科技點資料 : 第一筆為虛擬起始科技點
        public static List<TechTreeSavePoint> TechPointDatas = new List<TechTreeSavePoint>();

        // 文化樹存檔之科技線段資料
        public static List<TechTreeSaveLink> TechLinkDatas = new List<TechTreeSaveLink>();

        public static string EDIT_FILE_NAME = "CultureTreeMap.txt";
        public static string EDIT_FILE_PATH { get { return "Assets/GameMain/BulitInData/Resources/"; } }
        private static string CONFIG_DATA_SAVE_PATH { get { return "Assets/GameMain/BulitInData/Resources/Settings/CultureTreeConfig.asset"; } }
        private static string CONFIG_DATA_LOAD_PATH { get { return "Settings/CultureTreeConfig"; } }

        public static bool SaveData(UITechPoint centerTechPoint, ref List<UITechPoint> _techs, ref List<UITechLink> _links)
        {
#if UNITY_EDITOR
            if (centerTechPoint == null)
            {
                EditorUtility.DisplayDialog("存檔錯誤", "儲存失敗，必須要有起始中央科技！", "確認");
                return false;
            }

            string savePath = string.Format("{0}{1}", EDIT_FILE_PATH, EDIT_FILE_NAME);

            if (File.Exists(savePath))
            {
                if (!EditorUtility.DisplayDialog("存檔", string.Format("此舉將會覆蓋原有的檔案 {0}，確認是否覆蓋?", savePath), "確認", "取消"))
                    return false;

                File.Delete(savePath);
            }

            List<UITechPoint> allPoints = new List<UITechPoint>();
            allPoints.AddRange(_techs);

            TechTreeSave save = new TechTreeSave();
            save.Points = new TechTreeSavePoint[allPoints.Count];
            save.Links = new TechTreeSaveLink[_links.Count];

            for (int i = 0; i < allPoints.Count; i++)
            {
                var point = allPoints[i];

                TechTreeSavePoint savePt = new TechTreeSavePoint();
                savePt.ID = point.TableID;
                savePt.x = (int)point.UiFollowPosition.x;
                savePt.y = (int)point.UiFollowPosition.y;

                save.Points[i] = savePt;
            }

            for (int i = 0; i < _links.Count; i++)
            {
                var link = _links[i];

                TechTreeSaveLink lk = new TechTreeSaveLink();
                lk.LinkA = allPoints.IndexOf(link.StartPoint);
                lk.LinkB = allPoints.IndexOf(link.EndPoint);

                save.Links[i] = lk;
            }

            string saveJson = JsonUtility.ToJson(save);

            var sr = File.CreateText(savePath);
            sr.WriteLine(saveJson);
            sr.Close();

            AssetDatabase.Refresh();
#endif
            return true;
        }


        public static void LoadData(UnityAction<TechTreeSavePoint> _createTech, UnityAction<TechTreeSaveLink> _createLink, UnityAction _loadPrepareAction, UnityAction _completeAction = null)
        {
            if (_loadPrepareAction != null)
                _loadPrepareAction();

            GameCore.Resource.LoadAsset(Path.GetFileNameWithoutExtension(EDIT_FILE_NAME),
               new GameFramework.Resource.LoadAssetCallbacks(
               (assetName, asset, duration, _userData) =>
               {
                   TechTreeSave data = JsonUtility.FromJson<TechTreeSave>(((TextAsset)asset).text);

                   for (int i = 0; i < data.Points.Length; i++)
                   {
                       var point = data.Points[i];

                       if (_createTech != null)
                           _createTech(point);
                   }

                   for (int i = 0; i < data.Links.Length; i++)
                   {
                       var link = data.Links[i];

                       if (_createLink != null)
                           _createLink(link);
                   }

                   if (_completeAction != null)
                       _completeAction();

                   Log.Info("CultureTreeMap Loading Success. {0} with duration {1}.", assetName, duration);
               },
               (assetName, status, errorMessage, _userData) =>
               {
                   Log.Error("CultureTreeMap Loading Error. {0} with status {1}, status. {2}", assetName, status, errorMessage);
               })
            );
        }

        // 讀取文明樹設定資料
        public static void LoadCultureTreeData()
        {
            LoadData(
                (tech) =>
                {
                    TechPointDatas.Add(tech);
                },
                (link) =>
                {
                    TechLinkDatas.Add(link);
                },
                () =>
                {
                    TechPointDatas.Clear();
                    TechLinkDatas.Clear();
                }
            );
        }

#region CultureTreeConfig

#if UNITY_EDITOR
        [MenuItem("Tools/CultureTree/創建 CultureTreeConfig")]
        private static void CreateCultureTreeConfigFile()
        {
            CultureTreeConfig data = ScriptableObject.CreateInstance<CultureTreeConfig>();

            if (File.Exists(CONFIG_DATA_SAVE_PATH))
                return;

            AssetDatabase.CreateAsset(data, CONFIG_DATA_SAVE_PATH);
        }
#endif
        // 加載文明樹資源設定
        public static void LoadCultureTreeConfigFile()
        {
            GameCore.Resource.LoadAsset(CONFIG_DATA_LOAD_PATH,
                new GameFramework.Resource.LoadAssetCallbacks(
                    (assetName, asset, duration, _userData) =>
                    {
                        Setting = (CultureTreeConfig)asset;
                        Log.Info("CultureTreeConfig Loading Success. {0} with duration {1}.", assetName, duration);
                    },
                    (assetName, status, errorMessage, _userData) =>
                    {
                        Log.Error("CultureTreeConfig Loading Error. {0} with status {1}, status. {2}", assetName, status, errorMessage);
                    }));
        }
#endregion
    }
}

