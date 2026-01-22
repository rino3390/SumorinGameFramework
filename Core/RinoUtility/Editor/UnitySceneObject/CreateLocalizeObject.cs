using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

namespace Rino.GameFramework.RinoUtility.Editor
{
    /// <summary>
    /// 在 Unity 場景中建立本地化物件的工具類別
    /// </summary>
    public class CreateLocalizeObject
    {
        /// <summary>
        /// 在場景中建立本地化文字物件（包含 TextMeshProUGUI 和 LocalizeStringEvent 元件）
        /// </summary>
        /// <param name="menuCommand">選單指令</param>
        [MenuItem("GameObject/UI/Localize/Text", false, 0)]
        public static void CreateLocalizeText(MenuCommand menuCommand)
        {
            CreateLocalizeObj("LocalizeText", typeof(TextMeshProUGUI), typeof(LocalizeStringEvent));
        }

        /// <summary>
        /// 建立帶有指定元件的本地化物件
        /// </summary>
        /// <param name="name">物件名稱</param>
        /// <param name="types">要附加的元件類型</param>
        public static void CreateLocalizeObj(string name, params Type[] types)
        {
            var newObj = ObjectFactory.CreateGameObject(name, types);
            Place(newObj);
        }

        /// <summary>
        /// 將 GameObject 放置到場景中並設定選取狀態
        /// </summary>
        /// <param name="gameObject">要放置的 GameObject</param>
        public static void Place(GameObject gameObject)
        {
            gameObject.transform.position = Vector3.zero;
            StageUtility.PlaceGameObjectInCurrentStage(gameObject);
            GameObjectUtility.EnsureUniqueNameForSibling(gameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
            Selection.activeObject = gameObject;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
