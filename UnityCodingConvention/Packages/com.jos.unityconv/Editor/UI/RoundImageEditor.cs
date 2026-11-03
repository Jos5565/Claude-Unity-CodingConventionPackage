using System.Linq;
using UnityEngine;
using UnityEditor.AnimatedValues;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.UI;


namespace UnityConv.UI.EditorTools
{
    /// <summary>
    /// Editor class used to edit UI Sprites.
    /// </summary>

    [CustomEditor(typeof(RoundImage), true)]
    [CanEditMultipleObjects]
    /// <summary>
    ///   Custom Editor for the Image Component.
    ///   Extend this class to write a custom editor for a component derived from Image.
    /// </summary>
    public class RoundImageEditor : GraphicEditor
    {
        SerializedProperty m_FillMethod;
        SerializedProperty m_FillOrigin;
        SerializedProperty m_FillAmount;
        SerializedProperty m_FillClockwise;
        SerializedProperty m_Type;
        SerializedProperty m_FillCenter;
        SerializedProperty m_Sprite;
        SerializedProperty m_SourceType;
        SerializedProperty m_Texture;
        SerializedProperty m_UVRect;
        SerializedProperty m_Round;
        SerializedProperty m_BorderWidth;
        SerializedProperty m_BorderColor;
        SerializedProperty m_BorderShape;
        SerializedProperty m_OutlineAlphaThreshold;

        SerializedProperty m_PreserveAspect;
        SerializedProperty m_UseSpriteMesh;
        SerializedProperty m_PixelsPerUnitMultiplier;
        GUIContent m_SpriteContent;
        GUIContent m_SpriteTypeContent;
        GUIContent m_ClockwiseContent;
        AnimBool m_ShowSlicedOrTiled;
        AnimBool m_ShowSliced;
        AnimBool m_ShowTiled;
        AnimBool m_ShowFilled;
        AnimBool m_ShowType;
        bool m_bIsDriven;
        private static readonly string[] SourceTypeLabels = { "Image", "RawImage" };
        private class Styles
        {
            public static GUIContent text = EditorGUIUtility.TrTextContent("Fill Origin");
            public static GUIContent[] OriginHorizontalStyle =
            {
                EditorGUIUtility.TrTextContent("Left"),
                EditorGUIUtility.TrTextContent("Right")
            };

            public static GUIContent[] OriginVerticalStyle =
            {
                EditorGUIUtility.TrTextContent("Bottom"),
                EditorGUIUtility.TrTextContent("Top")
            };

            public static GUIContent[] Origin90Style =
            {
                EditorGUIUtility.TrTextContent("BottomLeft"),
                EditorGUIUtility.TrTextContent("TopLeft"),
                EditorGUIUtility.TrTextContent("TopRight"),
                EditorGUIUtility.TrTextContent("BottomRight")
            };

            public static GUIContent[] Origin180Style =
            {
                EditorGUIUtility.TrTextContent("Bottom"),
                EditorGUIUtility.TrTextContent("Left"),
                EditorGUIUtility.TrTextContent("Top"),
                EditorGUIUtility.TrTextContent("Right")
            };

            public static GUIContent[] Origin360Style =
            {
                EditorGUIUtility.TrTextContent("Bottom"),
                EditorGUIUtility.TrTextContent("Right"),
                EditorGUIUtility.TrTextContent("Top"),
                EditorGUIUtility.TrTextContent("Left")
            };
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_SpriteContent = EditorGUIUtility.TrTextContent("Source Image");
            m_SpriteTypeContent = EditorGUIUtility.TrTextContent("Image Type");
            m_ClockwiseContent = EditorGUIUtility.TrTextContent("Clockwise");

            m_Sprite = serializedObject.FindProperty("m_Sprite");
            m_SourceType = serializedObject.FindProperty("m_SourceType");
            m_Texture = serializedObject.FindProperty("m_Texture");
            m_UVRect = serializedObject.FindProperty("m_UVRect");
            m_Type = serializedObject.FindProperty("m_Type");
            m_FillCenter = serializedObject.FindProperty("m_FillCenter");
            m_FillMethod = serializedObject.FindProperty("m_FillMethod");
            m_FillOrigin = serializedObject.FindProperty("m_FillOrigin");
            m_FillClockwise = serializedObject.FindProperty("m_FillClockwise");
            m_FillAmount = serializedObject.FindProperty("m_FillAmount");
            m_PreserveAspect = serializedObject.FindProperty("m_PreserveAspect");
            m_UseSpriteMesh = serializedObject.FindProperty("m_UseSpriteMesh");
            m_PixelsPerUnitMultiplier = serializedObject.FindProperty("m_PixelsPerUnitMultiplier");
            m_Round = serializedObject.FindProperty("m_Round");
            m_BorderWidth = serializedObject.FindProperty("m_BorderWidth");
            m_BorderColor = serializedObject.FindProperty("m_BorderColor");
            m_BorderShape = serializedObject.FindProperty("m_BorderShape");
            m_OutlineAlphaThreshold = serializedObject.FindProperty("m_OutlineAlphaThreshold");

            m_ShowType = new AnimBool(m_Sprite.objectReferenceValue != null);
            m_ShowType.valueChanged.AddListener(Repaint);

            var typeEnum = (Image.Type)m_Type.enumValueIndex;

            m_ShowSlicedOrTiled = new AnimBool(!m_Type.hasMultipleDifferentValues && typeEnum == Image.Type.Sliced);
            m_ShowSliced = new AnimBool(!m_Type.hasMultipleDifferentValues && typeEnum == Image.Type.Sliced);
            m_ShowTiled = new AnimBool(!m_Type.hasMultipleDifferentValues && typeEnum == Image.Type.Tiled);
            m_ShowFilled = new AnimBool(!m_Type.hasMultipleDifferentValues && typeEnum == Image.Type.Filled);
            m_ShowSlicedOrTiled.valueChanged.AddListener(Repaint);
            m_ShowSliced.valueChanged.AddListener(Repaint);
            m_ShowTiled.valueChanged.AddListener(Repaint);
            m_ShowFilled.valueChanged.AddListener(Repaint);

            SetShowNativeSize(true);

        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_ShowType.valueChanged.RemoveListener(Repaint);
            m_ShowSlicedOrTiled.valueChanged.RemoveListener(Repaint);
            m_ShowSliced.valueChanged.RemoveListener(Repaint);
            m_ShowTiled.valueChanged.RemoveListener(Repaint);
            m_ShowFilled.valueChanged.RemoveListener(Repaint);

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RoundImage image = target as RoundImage;
            RectTransform rect = image.GetComponent<RectTransform>();
            m_bIsDriven = (rect.drivenByObject as Slider)?.fillRect == rect;

            DrawSourceType();
            if (m_SourceType.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("선택한 오브젝트의 Source Type이 서로 다릅니다.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            if (IsRawImage)
            {
                RawImageGUI();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            SpriteGUI();
            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();
            RoundGUI();

            m_ShowType.target = m_Sprite.objectReferenceValue != null;
            if (EditorGUILayout.BeginFadeGroup(m_ShowType.faded))
                TypeGUI();
            EditorGUILayout.EndFadeGroup();

            SetShowNativeSize(false);
            if (EditorGUILayout.BeginFadeGroup(m_ShowNativeSize.faded))
            {
                EditorGUI.indentLevel++;

                if ((RoundImage.Type)m_Type.enumValueIndex == RoundImage.Type.Simple)
                    EditorGUILayout.PropertyField(m_UseSpriteMesh);

                EditorGUILayout.PropertyField(m_PreserveAspect);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
            NativeSizeButtonGUI();


            serializedObject.ApplyModifiedProperties();
        }

        private bool IsRawImage => m_SourceType.enumValueIndex == (int)RoundImage.SourceType.RawImage;

        /// <summary>
        /// Image / RawImage 전환 (ButtonToggle의 Button / Toggle 전환과 같은 Toolbar)
        /// </summary>
        private void DrawSourceType()
        {
            EditorGUI.showMixedValue = m_SourceType.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int sourceType = GUILayout.Toolbar(m_SourceType.hasMultipleDifferentValues ? -1 : m_SourceType.enumValueIndex, SourceTypeLabels);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck() && sourceType >= 0)
            {
                m_SourceType.enumValueIndex = sourceType;
                SetShowNativeSize(true);
            }
            EditorGUILayout.Space();
        }

        /// <summary>
        /// RawImage 모드: Texture, UV Rect와 공통 설정. Sprite 전용 항목(Image Type, Sprite Outline 등)은 숨김
        /// </summary>
        private void RawImageGUI()
        {
            EditorGUILayout.PropertyField(m_Texture, new GUIContent("Texture"));
            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();
            EditorGUILayout.PropertyField(m_UVRect, new GUIContent("UV Rect"));
            RoundGUI();

            SetShowNativeSize(false);
            NativeSizeButtonGUI();
        }

        void SetShowNativeSize(bool instant)
        {
            if (IsRawImage)
            {
                base.SetShowNativeSize(m_Texture.objectReferenceValue != null, instant);
                return;
            }
            RoundImage.Type type = (RoundImage.Type)m_Type.enumValueIndex;
            bool showNativeSize = (type == RoundImage.Type.Simple || type == RoundImage.Type.Filled) && m_Sprite.objectReferenceValue != null;
            base.SetShowNativeSize(showNativeSize, instant);
        }

        /// <summary>
        /// Draw the atlas and Image selection fields.
        /// </summary>

        protected void SpriteGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Sprite, m_SpriteContent);

            if (EditorGUI.EndChangeCheck())
            {
                var newSprite = m_Sprite.objectReferenceValue as Sprite;
                if (newSprite)
                {
                    RoundImage.Type oldType = (RoundImage.Type)m_Type.enumValueIndex;
                    if (newSprite.border.SqrMagnitude() > 0)
                    {
                        m_Type.enumValueIndex = (int)RoundImage.Type.Sliced;
                    }
                    else if (oldType == RoundImage.Type.Sliced)
                    {
                        m_Type.enumValueIndex = (int)RoundImage.Type.Simple;
                    }
                }
                (serializedObject.targetObject as RoundImage).DisableSpriteOptimizations();
            }
        }
        protected void RoundGUI()
        {
            // Sprite Outline은 Sprite 모양을 따라가므로 RawImage 모드에서는 쓰지 않음
            if (!IsRawImage)
                EditorGUILayout.PropertyField(m_BorderShape, new GUIContent("Border Shape"));
            bool spriteOutline = !IsRawImage && !m_BorderShape.hasMultipleDifferentValues &&
                m_BorderShape.enumValueIndex == (int)RoundImage.BorderShape.SpriteOutline;
            if (spriteOutline)
            {
                EditorGUILayout.PropertyField(m_OutlineAlphaThreshold, new GUIContent("Outline Alpha Threshold",
                    "Alpha cutoff for the sprite outline. Increase to ignore faint shadows."));
                EditorGUILayout.HelpBox("Sprite Outline uses Simple geometry and ignores Image Round and Use Sprite Mesh. " +
                    "Use a rectangular sprite atlas with rotation and Tight Packing disabled. Transparent holes are outlined too.", MessageType.Info);
                if (!m_Type.hasMultipleDifferentValues && m_Type.enumValueIndex != (int)RoundImage.Type.Simple)
                    EditorGUILayout.HelpBox("Sprite Outline supports Simple only. Other image types use Rounded Rect.", MessageType.Warning);
            }
            using (new EditorGUI.DisabledScope(spriteOutline && !m_Type.hasMultipleDifferentValues &&
                m_Type.enumValueIndex == (int)RoundImage.Type.Simple))
            {
                EditorGUI.BeginChangeCheck();
                float newRound = EditorGUILayout.FloatField("Image Round", m_Round.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    m_Round.floatValue = Mathf.Max(0f, newRound);
                }
            }

            EditorGUI.BeginChangeCheck();
            float newBorderWidth = EditorGUILayout.FloatField("Border Width", m_BorderWidth.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_BorderWidth.floatValue = Mathf.Max(0f, newBorderWidth);
            }

            EditorGUILayout.PropertyField(m_BorderColor, new GUIContent("Border Color",
                "Border opacity is independent of Graphic Color alpha, which controls content only. CanvasGroup fades both."));
        }
        /// <summary>
        /// Sprites's custom properties based on the type.
        /// </summary>

        protected void TypeGUI()
        {
            EditorGUILayout.PropertyField(m_Type, m_SpriteTypeContent);

            ++EditorGUI.indentLevel;
            {
                RoundImage.Type typeEnum = (RoundImage.Type)m_Type.enumValueIndex;

                bool showSlicedOrTiled = (!m_Type.hasMultipleDifferentValues && (typeEnum == RoundImage.Type.Sliced || typeEnum == RoundImage.Type.Tiled));
                if (showSlicedOrTiled && targets.Length > 1)
                    showSlicedOrTiled = targets.Select(obj => obj as Image).All(img => img.hasBorder);

                m_ShowSlicedOrTiled.target = showSlicedOrTiled;
                m_ShowSliced.target = (showSlicedOrTiled && !m_Type.hasMultipleDifferentValues && typeEnum == RoundImage.Type.Sliced);
                m_ShowTiled.target = (showSlicedOrTiled && !m_Type.hasMultipleDifferentValues && typeEnum == RoundImage.Type.Tiled);
                m_ShowFilled.target = (!m_Type.hasMultipleDifferentValues && typeEnum == RoundImage.Type.Filled);

                RoundImage image = target as RoundImage;
                if (EditorGUILayout.BeginFadeGroup(m_ShowSlicedOrTiled.faded))
                {
                    if (image.hasBorder)
                        EditorGUILayout.PropertyField(m_FillCenter);
                    EditorGUILayout.PropertyField(m_PixelsPerUnitMultiplier);
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(m_ShowSliced.faded))
                {
                    if (image.sprite != null && !image.hasBorder)
                        EditorGUILayout.HelpBox("This Image doesn't have a border.", MessageType.Warning);
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(m_ShowTiled.faded))
                {
                    if (image.sprite != null && !image.hasBorder && (image.sprite.texture != null && image.sprite.texture.wrapMode != TextureWrapMode.Repeat || image.sprite.packed))
                        EditorGUILayout.HelpBox("It looks like you want to tile a sprite with no border. It would be more efficient to modify the Sprite properties, clear the Packing tag and set the Wrap mode to Repeat.", MessageType.Warning);
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(m_ShowFilled.faded))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_FillMethod);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_FillOrigin.intValue = 0;
                    }
                    var shapeRect = EditorGUILayout.GetControlRect(true);
                    // switch ((RoundImage.FillMethod)m_FillMethod.enumValueIndex)
                    // {
                    //     case RoundImage.FillMethod.Horizontal:
                    //         EditorGUI.Popup(shapeRect, m_FillOrigin.intValue, Styles.OriginHorizontalStyle, Styles.text);
                    //         break;
                    //     case RoundImage.FillMethod.Vertical:
                    //         EditorGUI.Popup(shapeRect, m_FillOrigin, Styles.OriginVerticalStyle, Styles.text);
                    //         break;
                    //     case RoundImage.FillMethod.Radial90:
                    //         EditorGUI.Popup(shapeRect, m_FillOrigin, Styles.Origin90Style, Styles.text);
                    //         break;
                    //     case RoundImage.FillMethod.Radial180:
                    //         EditorGUI.Popup(shapeRect, m_FillOrigin, Styles.Origin180Style, Styles.text);
                    //         break;
                    //     case RoundImage.FillMethod.Radial360:
                    //         EditorGUI.Popup(shapeRect, m_FillOrigin, Styles.Origin360Style, Styles.text);
                    //         break;
                    // }

                    if (m_bIsDriven)
                        EditorGUILayout.HelpBox("The Fill amount property is driven by Slider.", MessageType.None);
                    using (new EditorGUI.DisabledScope(m_bIsDriven))
                    {
                        EditorGUILayout.PropertyField(m_FillAmount);
                    }

                    if ((RoundImage.FillMethod)m_FillMethod.enumValueIndex > RoundImage.FillMethod.Vertical)
                    {
                        EditorGUILayout.PropertyField(m_FillClockwise, m_ClockwiseContent);
                    }
                }
                EditorGUILayout.EndFadeGroup();
            }
            --EditorGUI.indentLevel;
        }

        /// <summary>
        /// All graphics have a preview.
        /// </summary>

        public override bool HasPreviewGUI() { return true; }

        /// <summary>
        /// Draw the Image preview.
        /// </summary>

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            RoundImage image = target as RoundImage;
            if (image == null) return;

            Sprite sf = image.sprite;
            if (sf == null) return;

            //   SpriteDrawUtility.DrawSprite(sf, rect, image.canvasRenderer.GetColor());
        }

        /// <summary>
        /// A string containing the Image details to be used as a overlay on the component Preview.
        /// </summary>
        /// <returns>
        /// The Image details.
        /// </returns>

        public override string GetInfoString()
        {
            RoundImage image = target as RoundImage;
            if (image.sourceType == RoundImage.SourceType.RawImage)
            {
                Texture texture = image.texture;
                int width = texture != null ? Mathf.RoundToInt(texture.width * image.uvRect.width) : 0;
                int height = texture != null ? Mathf.RoundToInt(texture.height * image.uvRect.height) : 0;
                return string.Format("RawImage Size: {0}x{1}", width, height);
            }
            Sprite sprite = image.sprite;

            int x = (sprite != null) ? Mathf.RoundToInt(sprite.rect.width) : 0;
            int y = (sprite != null) ? Mathf.RoundToInt(sprite.rect.height) : 0;

            return string.Format("Image Size: {0}x{1}", x, y);
        }
    }
}
