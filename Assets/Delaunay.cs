///From https://github.com/mattatz/unity-triangulation2D

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Vertex
{
    private Vector2 position;
    public Vector2 Position { get { return position; } }
    public float x { get { return position.x; } }
    public float y { get { return position.y; } }
    public Vertex(Vector2 pos)
    {
        position = pos;
    }
    public Vertex(float x, float y)
    {
        position = new Vector2(x, y);
    }

    private int reference;
    public int Reference { get { return reference; } }
    public int Increment() { return ++reference; }
    public int Decrement() { return --reference; }
}

public class Edge
{
    public Vertex start;
    public Vertex end;
    public Edge(Vertex s, Vertex e)
    {
        start = s;
        end = e;
    }
    public void Reverse()
    {
        var temp = start;
        start = end;
        end = temp;
    }
    public Vertex GetOther(Vertex c)
    {
        return c == start ? end : c == end ? start : null;
    }
    public bool ContainsPoint(Vector2 pos)
    {
        return end.Position == pos || start.Position == pos;
    }
    public bool HasPoint(Vertex v)
    {
        return start == v || end == v;
    }

    private int reference;
    public int Reference { get { return reference; } }
    public int Increment()
    {
        start.Increment();
        end.Increment();
        return ++reference;
    }

    public int Decrement()
    {
        start.Decrement();
        end.Decrement();
        return --reference;
    }
}

public class Triangle
{
    public Vertex v0, v1, v2;
    public Edge e0, e1, e2;
    private Vector2 circlePos;
    private float radius2;
    public Color Color;
    public Triangle(Edge a, Edge b, Edge c)
    {
        e0 = a;
        e1 = b;
        e2 = c;
        v0 = e0.start;
        v1 = e0.end;
        v2 = (e2.start == v0 || e2.start == v1) ? e2.end : e2.start;
        //计算外接圆的圆心和半径平方
        float x1 = v0.x, y1 = v0.y;
        float x2 = v1.x, y2 = v1.y;
        float x3 = v2.x, y3 = v2.y;
        float circle_x = ((y2 - y1) * (y3 * y3 - y1 * y1 + x3 * x3 - x1 * x1) - (y3 - y1) * (y2 * y2 - y1 * y1 + x2 * x2 - x1 * x1)) / (2f * ((x3 - x1) * (y2 - y1) - (x2 - x1) * (y3 - y1)));
        float circle_y = ((x2 - x1) * (x3 * x3 - x1 * x1 + y3 * y3 - y1 * y1) - (x3 - x1) * (x2 * x2 - x1 * x1 + y2 * y2 - y1 * y1)) / (2f * ((y3 - y1) * (x2 - x1) - (y2 - y1) * (x3 - x1)));
        radius2 = (circle_x - x1) * (circle_x - x1) + (circle_y - y1) * (circle_y - y1);
        circlePos = new Vector2(circle_x, circle_y);
        //
        Color = Color.white;
    }

    public bool HasPoint(Vertex p)
    {
        return (v0 == p) || (v1 == p) || (v2 == p);
    }
    public bool HasCommonPoint(Triangle t)
    {
        return HasPoint(t.v0) || HasPoint(t.v1) || HasPoint(t.v2);
    }
    public bool HasEdge(Edge e)
    {
        return e0 == e || e1 == e || e2 == e;
    }
    public Vertex ExcludePoint(Edge e)
    {
        if (!e.HasPoint(v0)) return v0;
        if (!e.HasPoint(v1)) return v1;
        return v2;
    }
    public Vertex ExcludePoint(Vector2 p0, Vector2 p1)
    {
        if (p0 != v0.Position && p1 != v0.Position) return v0;
        if (p0 != v1.Position && p1 != v1.Position) return v1;
        return v2;
    }
    public Edge[] ExcludeEdge(Edge e)
    {
        if (e0.Equals(e)) { return new Edge[] { e1, e2 }; }
        if (e1.Equals(e)) { return new Edge[] { e0, e2 }; }
        return new Edge[] { e0, e1 };
    }
    public bool Equals(Triangle t)
    {
        return HasPoint(t.v0) && HasPoint(t.v1) && HasPoint(t.v2);
    }

    public bool ContainsInExternalCircle(Vertex v)
    {
        return (v.Position - circlePos).sqrMagnitude <= radius2;
    }

    public Vector3[] GetPoints()
    {
        return new Vector3[] { v0.Position, v1.Position, v2.Position };
    }
}

public class Delaunay  {
    private Vertex[] supperTri;
    private List<Vertex> _vertexs;
    private List<Edge> _edges;
    private List<Triangle> _triangles;
    public List<Edge> Edges { get { return _edges; } }
    public List<Triangle> Triangles { get { return _triangles; } }

    public Delaunay()
    {
        _vertexs = new List<Vertex>();
        _edges = new List<Edge>();
        _triangles = new List<Triangle>();
    }

    public void TestTriangle()
    {
        Triangulate(new Vertex[]{
            new Vertex(-2.5f,0),
            new Vertex(-1.5f,3),
            new Vertex(0,3),
            new Vertex(2.5f,0)
            //new Vector2(2,0,0),
            //new Vector2(2,1,0),
            //new Vector2(3,3,0)
        });
    }

    public void Triangulate(Vertex[] vertexs)
    {
        if (vertexs.Length < 3) return;
        _vertexs.Clear();
        _edges.Clear();
        _triangles.Clear();
        _vertexs.AddRange(vertexs);
        //确定外包围三角形
        CreateOutSide(ref _vertexs);
        for (int i = 0, n = _vertexs.Count; i < n; i++)
        {
            var v = _vertexs[i];
            UpdateTriangulation(v);
        }
        //删除超级三角形
        DeleteOutter();
    }

    private void DeleteOutter()
    {
        _triangles.RemoveAll(tri =>
        {
            foreach (var point in supperTri)
            {
                if (tri.v0 == point || tri.v1 == point || tri.v2 == point)
                {
                    return true;
                }
            }
            return false;
        });
    }

    private void CreateOutSide(ref List<Vertex> vertexList)
    {
        //遍历点，并找出包括所有顶点在内的矩形区
        float xMin = vertexList[0].x, xMax = vertexList[0].x;
        float yMin = vertexList[0].y, yMax = vertexList[0].y;
        foreach (var vertex in vertexList)
        {
            if (vertex.x > xMax)
                xMax = vertex.x;
            if (vertex.x < xMin)
                xMin = vertex.x;
            if (vertex.y > yMax)
                yMax = vertex.y;
            if (vertex.y < yMin)
                yMin = vertex.y;
        }
        //确定囊括矩形的巨型三角形
        Vector2 vSource = new Vector2((xMax + xMin) * 0.5f, (yMin + yMax) * 0.5f);
        float halfLength = vSource.x - xMin;
        float halfHeight = vSource.y - yMin;
        halfLength = Mathf.Max(halfLength, halfHeight + 1);
        Vertex vl = new Vertex(vSource.x - halfLength * 3f, vSource.y - halfHeight * 3f);
        Vertex vm = new Vertex(vSource.x, vSource.y + halfHeight * 3f);
        Vertex vr = new Vertex(vSource.x + halfLength * 3f, vSource.y - halfHeight * 3f);
        //逆时针制造包围网
        Edge e = new Edge(vm, vl);
        _edges.Add(e);
        e = new Edge(vl, vr);
        _edges.Add(e);
        e = new Edge(vr, vm);
        _edges.Add(e);
        //超级三角形将在最后销毁
        supperTri = new Vertex[] { vl, vm, vr };
        vertexList.AddRange(supperTri);
        AddTriangle(vl, vr, vm);
    }

    private void UpdateTriangulation(Vertex n)
    {
        var tempTriangles = new List<Triangle>();
        var tempEdge = new List<Edge>();
        //插入一个点
        var v = CheckAndAddVertex(n);
        //将所有外接圆包含该点的三角形挨个拆散并组成新的3个三角形
        tempTriangles = _triangles.FindAll(t => t.ContainsInExternalCircle(v));
        tempTriangles.ForEach(t =>
        {
            tempEdge.Add(t.e0);
            tempEdge.Add(t.e1);
            tempEdge.Add(t.e2);
            AddTriangle(t.v0, t.v1, v);
            AddTriangle(t.v1, t.v2, v);
            AddTriangle(t.v2, t.v0, v);
            RemoveTriangle(t);
        });
        //从拆散的三角形中遍历线段，并找到共边的两个三角形进行LOP优化
        while (tempEdge.Count != 0)
        {
            Edge e = tempEdge.Last();
            tempEdge.RemoveAt(tempEdge.Count - 1);

            var commonT = _triangles.FindAll(t => t.HasEdge(e));
            //没有共边三角形
            if (commonT.Count <= 1) continue;
            //按理来说，我们始终保持着三角网在插入前是delaunay三角网
            //也就是说，插入新的点所形成的三角网共某个边也只会有2个三角形
            var abc = commonT[0];
            var abd = commonT[1];
            //如果三角形重复，就销毁
            if (abc.Equals(abd))
            {
                RemoveTriangle(abc);
                RemoveTriangle(abd);
                continue;
            }
            //获取需要做LOP优化的四边形
            var a = e.start;
            var b = e.end;
            var c = abc.ExcludePoint(e);
            var d = abd.ExcludePoint(e);
            //用abc的外接圆进行判断，如果包括，就优化
            if (abc.ContainsInExternalCircle(d))
            {
                RemoveTriangle(abc);
                RemoveTriangle(abd);

                AddTriangle(a, c, d); // add acd
                AddTriangle(b, c, d); // add bcd
                var e0 = abc.ExcludeEdge(e);
                tempEdge.Add(e0[0]);
                tempEdge.Add(e0[1]);
                var e1 = abd.ExcludeEdge(e);
                tempEdge.Add(e1[0]);
                tempEdge.Add(e1[1]);
            }
        }
    }

    private Vertex CheckAndAddVertex(Vertex n)
    {
        var idx = _vertexs.FindIndex(v => { return v.Position == n.Position; });
        if (idx < 0)
        {
            _vertexs.Add(n);
            return n;
        }
        return _vertexs[idx];
    }

    private Triangle AddTriangle(Vertex a, Vertex b, Vertex c)
    {
        Edge e0 = CheckAndAddEdge(a, b);
        Edge e1 = CheckAndAddEdge(b, c);
        Edge e2 = CheckAndAddEdge(c, a);
        Triangle t = new Triangle(e0, e1, e2);
        _triangles.Add(t);
        return t;
    }

    private void RemoveTriangle(Triangle t)
    {
        int idx = _triangles.IndexOf(t);
        if (idx < 0) return;
        _triangles.RemoveAt(idx);
        //修改边的引用计数
        if (t.e0.Decrement() <= 0) RemoveEdge(t.e0);
        if (t.e1.Decrement() <= 0) RemoveEdge(t.e1);
        if (t.e2.Decrement() <= 0) RemoveEdge(t.e2);

    }

    private void RemoveEdge(Edge edge)
    {
        _edges.Remove(edge);
        if (edge.start.Reference <= 0) _vertexs.Remove(edge.start);
        if (edge.end.Reference <= 0) _vertexs.Remove(edge.end);
    }

    private Edge CheckAndAddEdge(Vertex a, Vertex b)
    {
        int idx = FindEdge(a, b, _edges);
        Edge e = null;
        if (idx < 0)
        {
            e = new Edge(a, b);
            _edges.Add(e);
        }
        else
        {
            e = _edges[idx];
        }
        e.Increment();
        return e;
    }

    private int FindEdge(Vertex a, Vertex b, List<Edge> _edges)
    {
        return _edges.FindIndex(e => (e.start == a && e.end == b) || (e.start == b && e.end == a));
    }

}
