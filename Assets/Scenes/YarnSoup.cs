using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

public struct VertexBaryData
{
    public float a, b, c;
    public int tri;
}

public struct VertexMSData
{
    public float u, v, h, t;
    public float nx, ny, nz;
    public float a;
    public uint pix;
}

public struct VertexWSData
{
    public float x, y, z, t;
    public float a;
    public float nx, ny, nz;
    public float u, v;
    public float r;

    public Vector3 MapX() => new Vector3(x, y, z);
    public Vector3 MapN() => new Vector3(nx, ny, nz);
    public Vector2 MapUV() => new Vector2(u, v);
}

public class YarnSoup
{
    private List<VertexMSData> X_ms = new List<VertexMSData>();
    private List<VertexWSData> X_ws = new List<VertexWSData>();
    private List<int[]> E = new List<int[]>();
    private List<VertexBaryData> B = new List<VertexBaryData>();
    private List<VertexBaryData> B0 = new List<VertexBaryData>();
    private List<int> indices = new List<int>();

    private int m_nvertices;
    private int m_nyarns;


    public void FillFromGrid(PeriodicYarnPattern pyp, Grid grid)
    {
        int n_tiles = grid.NumFilled();
        var ij2k = grid.GetIJ2K();
        var k2ij = grid.GetK2IJ();

        int n_verts_tile = pyp.Q.GetLength(0);
        int n_edges_tile = pyp.E.GetLength(0);
        int n_verts = n_tiles * n_verts_tile;
        int n_edges = n_tiles * n_edges_tile;

        int GlobalVix(int local_vix, int i, int j)
        {
            int cix = ij2k[i, j];
            Debug.Assert(cix >= 0);
            return local_vix + n_verts_tile * cix;
        }

        var tangents = new List<Vector3>();
        for (int i = 0; i < pyp.Q.GetLength(0); i++)
        {
            var x0 = pyp.Q[i, 0];
            var x1 = pyp.Q[pyp.VE[i, 1], 0];
            var edge = pyp.E[pyp.VE[i, 1]];
            var shift = new Vector3(edge[2] * pyp.px, edge[3] * pyp.py, 0);
            tangents.Add((x1 + shift - x0).normalized);
        }

        var Xms = new List<VertexMSData>(n_verts);
        for (int cix = 0; cix < n_tiles; cix++)
        {
            var (i, j) = k2ij[cix];
            int vix_shift = n_verts_tile * cix;
            var vpos_shift = grid.LowerLeft(i, j) - grid.GetPivot();

            for (int lvix = 0; lvix < n_verts_tile; lvix++)
            {
                var X = new VertexMSData();
                var Xvec = X.mapXT;
                Xvec[0] = pyp.Q[lvix, 0];
                Xvec[1] = pyp.Q[lvix, 1];
                Xvec[2] = pyp.Q[lvix, 2];
                Xvec[3] = pyp.Q[lvix, 3];
                X.a = 0;
                Xvec[0] += vpos_shift[0];
                Xvec[1] += vpos_shift[1];
                X.pix = lvix;

                var testij = grid.GetIndex(new Vector2(Xvec[0], Xvec[1]));
                if (testij.Item1 != i || testij.Item2 != j)
                {
                    Xvec[0] = pyp.Q[lvix, 0] * 0.99f;
                    Xvec[1] = pyp.Q[lvix, 1] * 0.99f;
                    Xvec[2] = pyp.Q[lvix, 2] * 0.99f;
                    Xvec[3] = pyp.Q[lvix, 3] * 0.99f;
                    Xvec[0] += vpos_shift[0];
                    Xvec[1] += vpos_shift[1];
                }

                Xms.Add(X);
            }

            for (int lvix = 0; lvix < n_verts_tile; lvix++)
            {
                var Bi = Xms[vix_shift + lvix].mapN;
                var b = pyp.RefD1[lvix];
                var t = tangents[lvix];
                Bi[0] = (b - Vector3.Dot(b, t) * t).normalized;
            }

            int eix_shift = n_edges_tile * cix;
            int lv0, lv1, dj, di, gvix0, gvix1;
            bool neighbor_exists;
            for (int leix = 0; leix < n_edges_tile; leix++)
            {
                var edge = pyp.E[leix];
                lv0 = edge[0];
                lv1 = edge[1];
                dj = edge[2];
                di = edge[3];

                neighbor_exists = grid.Inside(i + di, j + dj);
                if (neighbor_exists)
                    neighbor_exists = grid.Filled(i + di, j + dj);

                if (neighbor_exists)
                {
                    gvix0 = GlobalVix(lv0, i, j);
                    gvix1 = GlobalVix(lv1, i + di, j + dj);
                    E.SetRow(eix_shift + leix, new int[] { gvix0, gvix1 });
                }
                else
                {
                    Debug.Assert(!(di == 0 && dj == 0));
                }
            }
        }
    }
    public List<int> GetIndexBuffer()
    {
        return indices;
    }

    public List<VertexMSData> GetXms()
    {
        return X_ms;
    }

    public List<VertexWSData> GetXws()
    {
        return X_ws;
    }

    public int VertexArraySize()
    {
        return X_ms.Count;
    }

    public List<VertexBaryData> GetB0()
    {
        return B0;
    }

    public List<VertexBaryData> GetB()
    {
        return B;
    }

    public int NumVertices()
    {
        return m_nvertices;
    }

    public int NumYarns()
    {
        return m_nyarns;
    }


}