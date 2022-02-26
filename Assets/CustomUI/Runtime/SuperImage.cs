using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

public class SuperImage : Image
{
    [SerializeField] private bool m_FillKeepAngle = false;

    public bool fillKeepAngle
    {
        get { return m_FillKeepAngle; }
        set
        {
            if (m_FillKeepAngle == value) return;
            m_FillKeepAngle = value;
            SetVerticesDirty();
        }
    }

    private Sprite activeSprite { get { return overrideSprite != null ? overrideSprite : sprite; } }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        if (activeSprite == null)
        {
            base.OnPopulateMesh(toFill);
            return;
        }
        if (type == Image.Type.Filled && !preserveAspect && hasBorder)
        {
            GenerateFilledSprite(toFill);
            return;
        }
        base.OnPopulateMesh(toFill);
    }

    private Vector4 GetAdjustedBorders(Vector4 border, Rect adjustedRect)
    {
        Rect originalRect = rectTransform.rect;

        for (int axis = 0; axis <= 1; axis++)
        {
            float borderScaleRatio;

            // The adjusted rect (adjusted for pixel correctness)
            // may be slightly larger than the original rect.
            // Adjust the border to match the adjustedRect to avoid
            // small gaps between borders (case 833201).
            if (originalRect.size[axis] != 0)
            {
                borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                border[axis] *= borderScaleRatio;
                border[axis + 2] *= borderScaleRatio;
            }

            // If the rect is smaller than the combined borders, then there's not room for the borders at their normal size.
            // In order to avoid artefacts with overlapping borders, we scale the borders down to fit.
            float combinedBorders = border[axis] + border[axis + 2];
            if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0)
            {
                borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                border[axis] *= borderScaleRatio;
                border[axis + 2] *= borderScaleRatio;
            }
        }
        return border;
    }

    static void AddQuad(VertexHelper vertexHelper, Vector2 posMin, Vector2 posMax, Color32 color, Vector2 uvMin, Vector2 uvMax)
    {
        int startIndex = vertexHelper.currentVertCount;

        vertexHelper.AddVert(new Vector3(posMin.x, posMin.y, 0), color, new Vector2(uvMin.x, uvMin.y));
        vertexHelper.AddVert(new Vector3(posMin.x, posMax.y, 0), color, new Vector2(uvMin.x, uvMax.y));
        vertexHelper.AddVert(new Vector3(posMax.x, posMax.y, 0), color, new Vector2(uvMax.x, uvMax.y));
        vertexHelper.AddVert(new Vector3(posMax.x, posMin.y, 0), color, new Vector2(uvMax.x, uvMin.y));

        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }

    static void AddQuad(VertexHelper vertexHelper,
                        Vector2 posMin, Vector2 posMax,
                        Color32 color,
                        Vector2 uvMin, Vector2 uvMax,
                        Vector2 startPos, Vector2 endPos)
    {
        int startIndex = vertexHelper.currentVertCount;
        int count = 0;
        Vector3 pos1 = new Vector3(posMin.x, posMin.y, 0);
        Vector2 uv1 = new Vector2(uvMin.x, uvMin.y);
        float c1 = Vector2Cross(startPos, endPos, pos1);
        Vector3 pos2 = new Vector3(posMin.x, posMax.y, 0);
        Vector2 uv2 = new Vector2(uvMin.x, uvMax.y);
        float c2 = Vector2Cross(startPos, endPos, pos2);
        Vector3 pos3 = new Vector3(posMax.x, posMax.y, 0);
        Vector2 uv3 = new Vector2(uvMax.x, uvMax.y);
        float c3 = Vector2Cross(startPos, endPos, pos3);
        Vector3 pos4 = new Vector3(posMax.x, posMin.y, 0);
        Vector2 uv4 = new Vector2(uvMax.x, uvMin.y);
        float c4 = Vector2Cross(startPos, endPos, pos4);
        AddVert(vertexHelper, pos1, pos4, color, uv1, uv4, startPos, endPos, c1, c4, ref count);
        AddVert(vertexHelper, pos2, pos1, color, uv2, uv1, startPos, endPos, c2, c1, ref count);
        AddVert(vertexHelper, pos3, pos2, color, uv3, uv2, startPos, endPos, c3, c2, ref count);
        AddVert(vertexHelper, pos4, pos3, color, uv4, uv3, startPos, endPos, c4, c3, ref count);

        for (int i = 1; i < count - 1; i++)
        {
           vertexHelper.AddTriangle(startIndex, startIndex + i, startIndex + i + 1); 
        }
    }

    static void AddVert(VertexHelper vertexHelper,
                        Vector3 curPos, Vector3 lastPos,
                        Color32 color,
                        Vector2 curUV, Vector2 lastUV,
                        Vector2 startPos, Vector2 endPos,
                        float curCrossV, float lastCrossV,
                        ref int count)
    {
        if (curCrossV >= 0)
        {
            if (lastCrossV < 0)
            {
                Vector3 crossPos = Vector3.zero;
                if (LineSegmentIntersection(startPos, endPos, lastPos, curPos, ref crossPos))
                {
                    Vector2 crossUV = CalcLerpUV(crossPos, curPos, lastPos, curUV, lastUV);
                    vertexHelper.AddVert(crossPos, color, crossUV);
                    count++;
                }
            }
            vertexHelper.AddVert(curPos, color, curUV);
            count++;
        }
        else
        {
            if (lastCrossV >= 0)
            {
                Vector3 crossPos = Vector3.zero;
                if (LineSegmentIntersection(startPos, endPos, lastPos, curPos, ref crossPos))
                {
                    Vector2 crossUV = CalcLerpUV(crossPos, curPos, lastPos, curUV, lastUV);
                    vertexHelper.AddVert(crossPos, color, crossUV);
                    count++;
                }
            }
        }
    }

    static Vector2 CalcLerpUV(Vector2 crossPos, Vector2 curPos, Vector2 lastPos, Vector2 curUV, Vector2 lastUV)
    {
        Vector2 crossUV = curPos - lastPos;
        if (Mathf.Approximately(crossUV.x, 0f))
        {
            crossUV.x = curUV.x;
        }
        else
        {
            crossUV.x = (curUV.x - lastUV.x) * (crossPos.x - lastPos.x) / crossUV.x + lastUV.x;
        }
        if (Mathf.Approximately(crossUV.y, 0f))
        {
            crossUV.y = curUV.y;
        }
        else
        {
            crossUV.y = (curUV.y - lastUV.y) * (crossPos.y - lastPos.y) / crossUV.y + lastUV.y;
        }
        return crossUV;
    }

    static float Vector2Cross(Vector2 p1, Vector2 p2, Vector3 pos)
    {
        return ((p2.x - p1.x) * (pos.y - p1.y) - (p2.y - p1.y) * (pos.x - p1.x));
    }

    static bool LineSegmentIntersection(Vector2 p1, Vector2 p2, Vector3 p3, Vector3 p4, ref Vector3 result)
    {
        float bx = p2.x - p1.x;
        float by = p2.y - p1.y;
        float dx = p4.x - p3.x;
        float dy = p4.y - p3.y;
        float bDotDPerp = bx * dy - by * dx;

        if (bDotDPerp == 0)
        {
            return false;
        }
        float cx = p3.x - p1.x;
        float cy = p3.y - p1.y;
        float u = (cx * by - cy * bx) / bDotDPerp;
        if (u < 0 || u > 1)
        {
            return false;
        }
        result = new Vector3(p3.x + u * dx, p3.y + u * dy, 0);
        return true;
    }

    static readonly Vector2[] s_VertScratch = new Vector2[5];
    static readonly Vector2[] s_UVScratch = new Vector2[5];

    void GenerateFilledSprite(VertexHelper toFill)
    {
        toFill.Clear();

        if (fillAmount < 0.001f)
            return;

        Vector4 outer, inner, padding, border;

        if (activeSprite != null)
        {
            outer = DataUtility.GetOuterUV(activeSprite);
            inner = DataUtility.GetInnerUV(activeSprite);
            padding = DataUtility.GetPadding(activeSprite);
            border = activeSprite.border;
        }
        else
        {
            outer = Vector4.zero;
            inner = Vector4.zero;
            padding = Vector4.zero;
            border = Vector4.zero;
        }

        Rect rect = GetPixelAdjustedRect();
        Vector4 adjustedBorders = GetAdjustedBorders(border / pixelsPerUnit, rect);
        padding = padding / pixelsPerUnit;

        float rectX = rect.x;
        float rectY = rect.y;
        float rectW = rect.width;
        float rectH = rect.height;
        s_VertScratch[0] = new Vector2(padding.x, padding.y);
        s_VertScratch[3] = new Vector2(rectW - padding.z, rectH - padding.w);

        s_VertScratch[1].x = adjustedBorders.x;
        s_VertScratch[1].y = adjustedBorders.y;

        s_VertScratch[2].x = rectW - adjustedBorders.z;
        s_VertScratch[2].y = rectH - adjustedBorders.w;

        for (int i = 0; i < 4; ++i)
        {
            s_VertScratch[i].x += rect.x;
            s_VertScratch[i].y += rect.y;
        }

        s_UVScratch[0] = new Vector2(outer.x, outer.y);
        s_UVScratch[1] = new Vector2(inner.x, inner.y);
        s_UVScratch[2] = new Vector2(inner.z, inner.w);
        s_UVScratch[3] = new Vector2(outer.z, outer.w);


        if (fillAmount >= 0.999f)
        {
            for (int x = 0; x < 3; ++x)
            {
                int x2 = x + 1;

                for (int y = 0; y < 3; ++y)
                {
                    int y2 = y + 1;
                    AddQuad(toFill,
                        new Vector2(s_VertScratch[x].x, s_VertScratch[y].y),
                        new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y),
                        color,
                        new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
                        new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y));
                }
            }
            return;
        }

        if (fillMethod == FillMethod.Radial360)
        {
            Vector2 halfRectSize = new Vector2(rectW * 0.5f, rectH * 0.5f);
            s_VertScratch[4] = s_VertScratch[3];
            s_VertScratch[3] = s_VertScratch[2];
            s_VertScratch[2] = s_VertScratch[0] + halfRectSize;
            s_UVScratch[4] = s_UVScratch[3];
            s_UVScratch[3] = s_UVScratch[2];
            s_UVScratch[2] = new Vector2((inner.z - inner.x) * 0.5f + inner.x, (inner.w - inner.y) * 0.5f + inner.y);
            AddMultiQuad(toFill, halfRectSize, !fillClockwise, 0, 2, 0, 2, 0);
            AddMultiQuad(toFill, halfRectSize, fillClockwise, 0, 2, 2, 4, 1);
            AddMultiQuad(toFill, halfRectSize, !fillClockwise, 2, 4, 2, 4, 2);
            AddMultiQuad(toFill, halfRectSize, fillClockwise, 2, 4, 0, 2, 3);
            return;
        }

        Vector2 startPos = Vector2.zero;
        Vector2 endPos = Vector2.zero;

        if (fillMethod == FillMethod.Horizontal)
        {
            float fill = rectW * fillAmount;
            if (fillOrigin == 1)
            {
                startPos.x = endPos.x = rectX + rectW - fill;
                endPos.y = rectY;
                startPos.y = rectY + rectH;
            }
            else
            {
                startPos.x = endPos.x = rectX + fill;
                startPos.y = rectY;
                endPos.y = rectY + rectH;
            }
        }
        else if (fillMethod == FillMethod.Vertical)
        {
            float fill = rectH * fillAmount;
            if (fillOrigin == 1)
            {
                startPos.y = endPos.y = rectY + rectH - fill;
                startPos.x = rectX;
                endPos.x = rectX + rectW;
            }
            else
            {
                startPos.y = endPos.y = rectY + fill;
                endPos.x = rectX;
                startPos.x = rectX + rectW;
            }
        }
        else if (fillMethod == FillMethod.Radial90)
        {
            if (fillOrigin == 0)
            {
                startPos = s_VertScratch[0];
            }
            else if (fillOrigin == 1)
            {
                startPos.Set(s_VertScratch[0].x, s_VertScratch[3].y);
            }
            else if (fillOrigin == 2)
            {
                startPos = s_VertScratch[3];
            }
            else
            {
                startPos.Set(s_VertScratch[3].x, s_VertScratch[0].y);
            }
            
            if (m_FillKeepAngle)
            {
                endPos = CalcRadialCutOffset(startPos, fillAmount, fillClockwise, fillOrigin, 0f, 90f);
            }
            else
            {
                endPos = CalcRadialCutOffset(startPos, fillAmount, fillClockwise, fillOrigin, 0f, 90f, rectW, rectH);
            }

        }
        else if (fillMethod == FillMethod.Radial180)
        {
            if (fillOrigin == 0)
            {
                float halfRectW = rectW * 0.5f;
                startPos.Set(halfRectW + rectX, rectY);
                if (m_FillKeepAngle)
                {
                    endPos = CalcRadialCutOffset(startPos, fillAmount, fillClockwise, fillOrigin, -90f, 180f);
                }
                else
                {
                    endPos = CalcRadialCutOffset(startPos, fillAmount, fillClockwise, fillOrigin, -90f, 180f, halfRectW, rectH);
                }
            }
            else if (fillOrigin == 1)
            {
                float halfRectH = rectH * 0.5f;
                startPos.Set(rectX, halfRectH + rectY);
                if (m_FillKeepAngle)
                {
                    endPos = CalcRadialCutOffset(startPos, fillAmount, fillClockwise, fillOrigin, 0f, 180f);
                }
                else
                {
                    endPos = CalcRadialCutOffset(startPos, fillAmount, fillClockwise, fillOrigin, 0f, 180f, rectW, halfRectH);
                }
            }
            else if (fillOrigin == 2)
            {
                float halfRectW = rectW * 0.5f;
                startPos.Set(halfRectW + rectX, rectY + rectH);
                if (m_FillKeepAngle)
                {
                    endPos = CalcRadialCutOffset(startPos, fillAmount, fillClockwise, fillOrigin, -90f, 180f);
                }
                else
                {
                    endPos = CalcRadialCutOffset(startPos, fillAmount, fillClockwise, fillOrigin, -90f, 180f, halfRectW, rectH);
                }
            }
            else
            {
                float halfRectH = rectH * 0.5f;
                startPos.Set(rectX + rectW, halfRectH + rectY);
                if (m_FillKeepAngle)
                {
                    endPos = CalcRadialCutOffset(startPos, fillAmount, fillClockwise, fillOrigin, 0f, 180f);
                }
                else
                {
                    endPos = CalcRadialCutOffset(startPos, fillAmount, fillClockwise, fillOrigin, 0f, 180f, rectW, halfRectH);
                }
            }
        }
        else
        {
            return;
        }

        for (int x = 0; x < 3; ++x)
        {
            int x2 = x + 1;

            for (int y = 0; y < 3; ++y)
            {
                int y2 = y + 1;
                AddQuad(toFill,
                    new Vector2(s_VertScratch[x].x, s_VertScratch[y].y),
                    new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y),
                    color,
                    new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
                    new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y),
                    startPos,
                    endPos);
            }
        }
    }

    void AddMultiQuad(VertexHelper toFill, Vector2 halfRectSize, bool invert, int xMin, int xMax, int yMin, int yMax, int idx)
    {
        float val = fillClockwise ?
                    fillAmount * 4f - ((idx + fillOrigin) % 4) :
                    fillAmount * 4f - (3 - ((idx + fillOrigin) % 4));
        if (val < 0.001f) return;
        bool full = ((val > 0.999f));
        int x, x2, y, y2;
        if (full)
        {
            for (x = xMin; x < xMax; ++x)
            {
                x2 = x + 1;

                for (y = yMin; y < yMax; ++y)
                {
                    y2 = y + 1;
                    AddQuad(toFill,
                        new Vector2(s_VertScratch[x].x, s_VertScratch[y].y),
                        new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y),
                        color,
                        new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
                        new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y));
                }
            }
        }
        else
        {
            Vector2 startPos = s_VertScratch[2];
            Vector2 endPos;
            if (m_FillKeepAngle)
            {
                endPos = CalcRadialCutOffset(startPos, val, fillClockwise, idx, -180f, 90f);
            }
            else
            {
                endPos = CalcRadialCutOffset(startPos, val, fillClockwise, idx, -180f, 90f, halfRectSize.x, halfRectSize.y);
            }
            for (x = xMin; x < xMax; ++x)
            {
                x2 = x + 1;

                for (y = yMin; y < yMax; ++y)
                {
                    y2 = y + 1;
                    AddQuad(toFill,
                        new Vector2(s_VertScratch[x].x, s_VertScratch[y].y),
                        new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y),
                        color,
                        new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
                        new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y),
                        startPos,
                        endPos);
                }
            }
        }
    }

    static Vector2 CalcRadialCutOffset(Vector2 startPos, float fill, bool invert, int corner, float startAngle, float maxAngle, float width = 1, float height = 1)
    {
        if ((corner & 1) == 1) invert = !invert;
        // Convert 0-1 value into 0 to 90 degrees angle in radians
        float angle = Mathf.Clamp01(fill);
        if (!invert) angle = 1f - angle;
        angle *= maxAngle * Mathf.Deg2Rad;
        if (!Mathf.Approximately(startAngle, 0f))
            angle += startAngle * Mathf.Deg2Rad;

        // Calculate the effective X and Y factors
        float cos = Mathf.Cos(angle) * height;
        float sin = Mathf.Sin(angle) * width;
        Vector2 endPos = startPos;
        if (corner == 0)
        {
            if (invert)
            {
                endPos.Set(startPos.x + sin, startPos.y + cos);
            }
            else
            {
                endPos.Set(startPos.x - sin, startPos.y - cos);
            }
        }
        else if (corner == 1)
        {
            if (invert)
            {
                endPos.Set(startPos.x - sin, startPos.y + cos);
            }
            else
            {
                endPos.Set(startPos.x + sin, startPos.y - cos);
            }
        }
        else if (corner == 2)
        {
            if (invert)
            {
                endPos.Set(startPos.x - sin, startPos.y - cos);
            }
            else
            {
                endPos.Set(startPos.x + sin, startPos.y + cos);
            }
        }
        else
        {
            if (invert)
            {
                endPos.Set(startPos.x + sin, startPos.y - cos);
            }
            else
            {
                endPos.Set(startPos.x - sin, startPos.y + cos);
            }
        }
        return endPos;
    }
}
