﻿using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.UI;

[CustomEditor(typeof(SuperImage), true)]
[CanEditMultipleObjects]
public class SuperImageEditor : GraphicEditor
{
    SerializedProperty m_FillMethod;
    SerializedProperty m_FillOrigin;
    SerializedProperty m_FillAmount;
    SerializedProperty m_FillClockwise;
    SerializedProperty m_Type;
    SerializedProperty m_FillCenter;
    SerializedProperty m_Sprite;
    SerializedProperty m_PreserveAspect;
    SerializedProperty m_UseSpriteMesh;
    SerializedProperty m_FillKeepAngle;
    SerializedProperty m_SlicedAnchor;
    GUIContent m_SpriteContent;
    GUIContent m_SpriteTypeContent;
    GUIContent m_ClockwiseContent;
    AnimBool m_ShowSlicedOrTiled;
    AnimBool m_ShowSliced;
    AnimBool m_ShowTiled;
    AnimBool m_ShowFilled;
    AnimBool m_ShowType;

    protected override void OnEnable()
    {
        base.OnEnable();

        m_SpriteContent = EditorGUIUtility.TrTextContent("Source Image");
        m_SpriteTypeContent     = EditorGUIUtility.TrTextContent("Image Type");
        m_ClockwiseContent      = EditorGUIUtility.TrTextContent("Clockwise");

        m_Sprite                = serializedObject.FindProperty("m_Sprite");
        m_Type                  = serializedObject.FindProperty("m_Type");
        m_FillCenter            = serializedObject.FindProperty("m_FillCenter");
        m_FillMethod            = serializedObject.FindProperty("m_FillMethod");
        m_FillOrigin            = serializedObject.FindProperty("m_FillOrigin");
        m_FillClockwise         = serializedObject.FindProperty("m_FillClockwise");
        m_FillAmount            = serializedObject.FindProperty("m_FillAmount");
        m_PreserveAspect        = serializedObject.FindProperty("m_PreserveAspect");
        m_UseSpriteMesh         = serializedObject.FindProperty("m_UseSpriteMesh");
        m_FillKeepAngle         = serializedObject.FindProperty("m_FillKeepAngle");
        m_SlicedAnchor         = serializedObject.FindProperty("m_SlicedAnchor");

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
        m_ShowType.valueChanged.RemoveListener(Repaint);
        m_ShowSlicedOrTiled.valueChanged.RemoveListener(Repaint);
        m_ShowSliced.valueChanged.RemoveListener(Repaint);
        m_ShowTiled.valueChanged.RemoveListener(Repaint);
        m_ShowFilled.valueChanged.RemoveListener(Repaint);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SpriteGUI();
        AppearanceControlsGUI();
        RaycastControlsGUI();

        m_ShowType.target = m_Sprite.objectReferenceValue != null;
        if (EditorGUILayout.BeginFadeGroup(m_ShowType.faded))
            TypeGUI();
        EditorGUILayout.EndFadeGroup();

        SetShowNativeSize(false);
        if (EditorGUILayout.BeginFadeGroup(m_ShowNativeSize.faded))
        {
            EditorGUI.indentLevel++;

            if ((Image.Type)m_Type.enumValueIndex == Image.Type.Simple)
                EditorGUILayout.PropertyField(m_UseSpriteMesh);

            EditorGUILayout.PropertyField(m_PreserveAspect);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFadeGroup();
        NativeSizeButtonGUI();

        serializedObject.ApplyModifiedProperties();
    }

    void SetShowNativeSize(bool instant)
    {
        Image.Type type = (Image.Type)m_Type.enumValueIndex;
        bool showNativeSize = (type == Image.Type.Simple || type == Image.Type.Filled) && m_Sprite.objectReferenceValue != null;
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
                Image.Type oldType = (Image.Type)m_Type.enumValueIndex;
                if (newSprite.border.SqrMagnitude() > 0)
                {
                    m_Type.enumValueIndex = (int)Image.Type.Sliced;
                }
                else if (oldType == Image.Type.Sliced)
                {
                    m_Type.enumValueIndex = (int)Image.Type.Simple;
                }
            }
        }
    }

    /// <summary>
    /// Sprites's custom properties based on the type.
    /// </summary>

    protected void TypeGUI()
    {
        EditorGUILayout.PropertyField(m_Type, m_SpriteTypeContent);

        ++EditorGUI.indentLevel;
        {
            Image.Type typeEnum = (Image.Type)m_Type.enumValueIndex;

            bool showSlicedOrTiled = (!m_Type.hasMultipleDifferentValues && (typeEnum == Image.Type.Sliced || typeEnum == Image.Type.Tiled));
            if (showSlicedOrTiled && targets.Length > 1)
                showSlicedOrTiled = targets.Select(obj => obj as Image).All(img => img.hasBorder);

            m_ShowSlicedOrTiled.target = showSlicedOrTiled;
            m_ShowSliced.target = (showSlicedOrTiled && !m_Type.hasMultipleDifferentValues && typeEnum == Image.Type.Sliced);
            m_ShowTiled.target = (showSlicedOrTiled && !m_Type.hasMultipleDifferentValues && typeEnum == Image.Type.Tiled);
            m_ShowFilled.target = (!m_Type.hasMultipleDifferentValues && typeEnum == Image.Type.Filled);

            Image image = target as Image;
            if (EditorGUILayout.BeginFadeGroup(m_ShowSlicedOrTiled.faded))
            {
                if (image.hasBorder)
                    EditorGUILayout.PropertyField(m_FillCenter);
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_ShowSliced.faded))
            {
                EditorGUILayout.PropertyField(m_SlicedAnchor);
                if (image.sprite != null && !image.hasBorder)
                    EditorGUILayout.HelpBox("This Image doesn't have a border.", MessageType.Warning);
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_ShowTiled.faded))
            {
                if (image.sprite != null && !image.hasBorder && (image.sprite.texture.wrapMode != TextureWrapMode.Repeat || image.sprite.packed))
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
                bool keepAngle = false;
                switch ((Image.FillMethod)m_FillMethod.enumValueIndex)
                {
                    case Image.FillMethod.Horizontal:
                        m_FillOrigin.intValue = (int)(Image.OriginHorizontal)EditorGUILayout.EnumPopup("Fill Origin", (Image.OriginHorizontal)m_FillOrigin.intValue);
                        break;
                    case Image.FillMethod.Vertical:
                        m_FillOrigin.intValue = (int)(Image.OriginVertical)EditorGUILayout.EnumPopup("Fill Origin", (Image.OriginVertical)m_FillOrigin.intValue);
                        break;
                    case Image.FillMethod.Radial90:
                        keepAngle = true;
                        m_FillOrigin.intValue = (int)(Image.Origin90)EditorGUILayout.EnumPopup("Fill Origin", (Image.Origin90)m_FillOrigin.intValue);
                        break;
                    case Image.FillMethod.Radial180:
                        keepAngle = true;
                        m_FillOrigin.intValue = (int)(Image.Origin180)EditorGUILayout.EnumPopup("Fill Origin", (Image.Origin180)m_FillOrigin.intValue);
                        break;
                    case Image.FillMethod.Radial360:
                        keepAngle = true;
                        m_FillOrigin.intValue = (int)(Image.Origin360)EditorGUILayout.EnumPopup("Fill Origin", (Image.Origin360)m_FillOrigin.intValue);
                        break;
                }
                EditorGUILayout.PropertyField(m_FillAmount);
                if (keepAngle)
                {
                    EditorGUILayout.PropertyField(m_FillKeepAngle);
                }
                if ((Image.FillMethod)m_FillMethod.enumValueIndex > Image.FillMethod.Vertical)
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

    static MethodInfo m_SpriteDrawUtility_DrawSprite = null;

    /// <summary>
    /// Draw the Image preview.
    /// </summary>
    public override void OnPreviewGUI(Rect rect, GUIStyle background)
    {
        Image image = target as Image;
        if (image == null) return;

        Sprite sf = image.sprite;
        if (sf == null) return;

        if (m_SpriteDrawUtility_DrawSprite == null)
        {
            foreach(var type in typeof(UnityEditor.UI.ImageEditor).Assembly.GetTypes())
            {
                if (type.Name == "SpriteDrawUtility")
                {
                    m_SpriteDrawUtility_DrawSprite = type.GetMethod("DrawSprite", 
                                                                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                                                                    null,
                                                                    new System.Type[] { typeof(Sprite), typeof(Rect), typeof(Color) },
                                                                    null);
                    break;
                }
            }
        }
        if (m_SpriteDrawUtility_DrawSprite == null) return;
        m_SpriteDrawUtility_DrawSprite.Invoke(null, new object[] { sf, rect, image.canvasRenderer.GetColor() });
    }

    /// <summary>
    /// A string containing the Image details to be used as a overlay on the component Preview.
    /// </summary>
    /// <returns>
    /// The Image details.
    /// </returns>

    public override string GetInfoString()
    {
        Image image = target as Image;
        Sprite sprite = image.sprite;

        int x = (sprite != null) ? Mathf.RoundToInt(sprite.rect.width) : 0;
        int y = (sprite != null) ? Mathf.RoundToInt(sprite.rect.height) : 0;

        return string.Format("Image Size: {0}x{1}", x, y);
    }
}
