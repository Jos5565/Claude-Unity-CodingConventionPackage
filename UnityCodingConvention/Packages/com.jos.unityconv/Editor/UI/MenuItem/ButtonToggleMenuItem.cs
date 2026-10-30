using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UnityConv.UI.EditorTools
{
    internal static class ButtonToggleMenuItem
    {
        private const string UISpritePath = "UI/Skin/UISprite.psd";

        [MenuItem("GameObject/UI (Canvas)/Button Toggle", false)]
        private static void CreateButtonToggle(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Button Toggle", typeof(RectTransform), typeof(Image));
            ((RectTransform)go.transform).sizeDelta = new Vector2(160f, 30f);

            Image image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(UISpritePath);
            image.type = Image.Type.Sliced;

            // Image를 먼저 붙여야 Reset에서 Target Graphic, Off 상태를 가져감
            go.AddComponent<ButtonToggle>();

            UIMenuUtility.Place(go, menuCommand);
        }
    }
}
