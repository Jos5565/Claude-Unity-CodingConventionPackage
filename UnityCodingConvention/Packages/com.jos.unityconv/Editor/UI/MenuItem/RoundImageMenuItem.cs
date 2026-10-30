using UnityEditor;
using UnityEngine;

namespace UnityConv.UI.EditorTools
{
    internal static class RoundImageMenuItem
    {
        [MenuItem("GameObject/UI (Canvas)/Round Image", false)]
        private static void CreateRoundImage(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Round Image", typeof(RectTransform));
            go.AddComponent<RoundImage>();

            UIMenuUtility.Place(go, menuCommand);
        }
    }
}
