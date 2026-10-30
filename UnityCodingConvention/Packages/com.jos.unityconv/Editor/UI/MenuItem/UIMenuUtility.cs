using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityConv.UI.EditorTools
{
    /// <summary>
    /// GameObject > UI (Canvas) 메뉴 공용, 선택한 UI 아래 또는 Canvas 아래에 배치
    /// </summary>
    internal static class UIMenuUtility
    {
        /// <summary>
        /// 선택한 오브젝트가 Canvas 안이면 그 아래, 아니면 Canvas 아래 (Canvas가 없으면 생성), Undo 등록 후 선택
        /// </summary>
        public static void Place(GameObject go, MenuCommand menuCommand)
        {
            GameObject parent = menuCommand.context as GameObject;
            if (parent == null || parent.GetComponentInParent<Canvas>() == null)
            {
                parent = GetOrCreateCanvas().gameObject;
            }
            GameObjectUtility.SetParentAndAlign(go, parent);

            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        private static Canvas GetOrCreateCanvas()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
            }

            // 입력 모듈은 프로젝트 입력 설정(Input System / 기존 Input)에 따라 달라 직접 만들지 않음
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                Debug.LogWarning("씬에 EventSystem이 없어 클릭이 동작하지 않습니다. GameObject > UI > Event System으로 추가하세요.");
            }
            return canvas;
        }
    }
}
