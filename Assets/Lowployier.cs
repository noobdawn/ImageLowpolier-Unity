using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PictureData
{
    private Texture2D sourceTexture;
    private Texture2D finalTexture;
    public int width;
    public int height;
    private Color[] colorArray;
    public PictureData(Texture2D tex)
    {
        sourceTexture = tex;
        finalTexture = sourceTexture;
        width = tex.width;
        height = tex.height;
        colorArray = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                colorArray[y * width + x] = sourceTexture.GetPixel(x, y);
            }
    }
    public Texture2D GetFinalTex()
    {
        return finalTexture;
    }

    #region Sobel
    /// <summary>
    /// 使用Sobel算子进行边缘检测
    /// </summary>
    public void SobelEdgeDetect(float Threshold)
    {
        finalTexture = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (Sobel(x, y) > Threshold)
                    finalTexture.SetPixel(x, y, Color.white);
                else
                    finalTexture.SetPixel(x, y, Color.black);
            }
        finalTexture.Apply();
    }
    private float Sobel(int x, int y)
    {
        float Gx = 0;
        Gx += Luminance(x - 1, y - 1) * -1;
        Gx += Luminance(x - 1, y) * -2;
        Gx += Luminance(x - 1, y + 1) * -1;
        Gx += Luminance(x + 1, y - 1) * 1;
        Gx += Luminance(x + 1, y) * 2;
        Gx += Luminance(x + 1, y + 1) * 1;
        float Gy = 0;
        Gy += Luminance(x - 1, y - 1) * 1;
        Gy += Luminance(x, y - 1) * 2;
        Gy += Luminance(x + 1, y - 1) * 1; 
        Gy += Luminance(x - 1, y + 1) * -1;
        Gy += Luminance(x, y + 1) * -2;
        Gy += Luminance(x + 1, y + 1) * -1;
        return Mathf.Sqrt(Gx * Gx + Gy * Gy);//出于性能考虑可以用 Mathf.Abs(Gx) + Mathf.Abs(Gy);
    }
    #endregion

    /// <summary>
    /// 计算NTSC色域下的亮度
    /// </summary>
    private float Luminance(Color c)
    {
        return c.r * 0.2125f + c.g * 0.7154f + c.b * 0.0721f;
    }
    private float Luminance(int x, int y)
    {
        return Luminance(GetColor(x, y));
    }
    private Color GetColor(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return Color.black;
        return colorArray[x + y * width];
    }

    #region Pick
    public void RandomPick(float PickRate, float BackPickRate)
    {
        Color[] tempArray = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                tempArray[y * width + x] = finalTexture.GetPixel(x, y);
            }
        finalTexture = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var r = UnityEngine.Random.Range(0f, 1f);
                if (tempArray[y * width + x].r == 1 && r >= PickRate)
                    finalTexture.SetPixel(x, y, Color.white);
                else if (r >= BackPickRate)
                    finalTexture.SetPixel(x, y, Color.white);
                else
                    finalTexture.SetPixel(x, y, Color.black);
            }
        //四个角强制设为点
        finalTexture.SetPixel(0, 0, Color.white);
        finalTexture.SetPixel(width - 1, 0, Color.white);
        finalTexture.SetPixel(0, height - 1, Color.white);
        finalTexture.SetPixel(width - 1, height - 1, Color.white);
        finalTexture.Apply();
    }
    #endregion

    #region Delaunay
    public Triangle[] Delaunay()
    {
        Vertex[] apexs = GetAllDelaunayApex();
        Delaunay delaunay = new Delaunay();
        delaunay.Triangulate(apexs);
        //为三角形添加颜色
        var tris = delaunay.Triangles;
        SampleColor(tris);
        return tris.ToArray();
    }

    private void SampleColor(List<Triangle> tris)
    {
        Color[] tempArray = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                tempArray[y * width + x] = sourceTexture.GetPixel(x, y);
            }
        tris.ForEach(t => {
            Vector2 center = (t.v0.Position + t.v1.Position + t.v2.Position) * 0.3333f;
            t.Color = tempArray[(int)center.y * width + (int)center.x];
        });
    }

    public Vertex[] GetAllDelaunayApex()
    {
        List<Vertex> apex = new List<Vertex>();
        Color[] tempArray = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                tempArray[y * width + x] = finalTexture.GetPixel(x, y);
            }
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (tempArray[y * width + x].r == 1)
                    apex.Add(new Vertex(x, y));
            }
        return apex.ToArray();
    }
    #endregion

}

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshFilter))]
public class Lowployier : MonoBehaviour {
    public Texture2D InputTex;
    public RawImage ImageCom;
    //梯度变化的门槛
    public float Threshold;
    //选取点的概率
    public float PickRate;
    public float BackPickRate;

    private PictureData picData;

    void OnGUI()
    {
        if (GUILayout.Button("载入"))
        {
            picData = new PictureData(InputTex);
            ImageCom.texture = picData.GetFinalTex();
            ImageCom.rectTransform.sizeDelta = new Vector2(InputTex.width, InputTex.height);
        }
        if (GUILayout.Button("边缘检测"))
        {
            if (picData == null)
                return;
            picData.SobelEdgeDetect(Threshold);
            ImageCom.texture = picData.GetFinalTex();
        }
        if (GUILayout.Button("随机取点"))
        {
            if (picData == null)
                return;
            picData.RandomPick(PickRate, BackPickRate);
            ImageCom.texture = picData.GetFinalTex();
        }
        if (GUILayout.Button("德洛内三角剖分"))
        {
            if (picData == null)
                return;
            var tris = picData.Delaunay();
            //将三角形转换为Mesh
            Mesh m = new Mesh();
            List<Vector3> verts = new List<Vector3>();
            List<int> indics = new List<int>();
            List<Color> colors = new List<Color>();
            int idx = 0;
            foreach(var tri in tris)
            {
                verts.AddRange(tri.GetPoints());
                indics.Add(idx);
                indics.Add(idx + 1);
                indics.Add(idx + 2);
                indics.Add(idx);
                indics.Add(idx + 2);
                indics.Add(idx + 1);
                colors.Add(tri.Color);
                colors.Add(tri.Color);
                colors.Add(tri.Color);
                idx += 3;
            }
            m.vertices = verts.ToArray();
            m.triangles = indics.ToArray();
            m.colors = colors.ToArray();
            m.RecalculateBounds();
            GetComponent<MeshFilter>().mesh = m;
            //制作一个平面碰撞体供采样用
            m = new Mesh();
            verts.Clear();
            indics.Clear();
            verts.Add(new Vector3(0, 0, 0));
            verts.Add(new Vector3(0, picData.height, 0));
            verts.Add(new Vector3(picData.width, 0, 0));
            verts.Add(new Vector3(picData.width, picData.height, 0));
            indics.AddRange(new int[] { 0, 2, 3, 0, 3, 1 });
            m.vertices = verts.ToArray();
            m.triangles = indics.ToArray();
            m.RecalculateBounds();
            GetComponent<MeshCollider>().sharedMesh = m;
        }
    }
}
