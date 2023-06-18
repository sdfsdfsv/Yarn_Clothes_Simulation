using UnityEngine;
using System;
using System.Collections.Generic;

public class Grid
{
    private int nx, ny;               // number of cells
    private float cx, cy;             // cell size
    private Vector2 offset;           // position lower left corner of cell(0,0)
    private Vector2 pivot;            // offset to cell corners (e.g. to align with pyp)

    private int[,] ij2k;                         // map cell i,j -> filled cell index k
    private List<Tuple<int, int>> k2ij;          // map cell index k -> filled cell i,j
    private int n_filled;
    private List<Queue<int>> cellk2tris;

    public int GetNx() { return nx; }
    public int GetNy() { return ny; }
    public bool Inside(int i, int j) { return !(i < 0 || j < 0) && i < ny && j < nx; }
    public Vector2 LowerLeft(int i, int j)
    {
        Vector2 ll = new Vector2();
        ll.x = cx * j;
        ll.y = cy * i;
        ll += offset;
        return ll;
    }
    public Vector2 GetPivot() { return pivot; }

    public Tuple<int, int> GetIndex(Vector2 point) { return GetIndex(point.x, point.y); }
    public Tuple<int, int> GetIndex(float x, float y)
    {
        return new Tuple<int, int>((int)Mathf.Floor((y - offset.y) / cy), (int)Mathf.Floor((x - offset.x) / cx));
    }

    // sparse grid index mapping functions (full i,j -> sparse k)
    public int[,] GetIJ2K() { return ij2k; }
    public List<Tuple<int, int>> GetK2IJ() { return k2ij; }
    public bool Filled(int i, int j) { return ij2k[i, j] >= 0; }
    public int NumFilled() { return n_filled; }
    public Queue<int> Cell2Tris(int i, int j) { return cellk2tris[ij2k[i, j]]; }


    // create grid to cover mesh uv bounds with cell size (pyp.px, pyp.py)
    public void FromTiling(MeshData mesh, PeriodicYarnPattern pyp)
    {
        if (mesh.Empty())
        {
            nx = ny = 0;
            cx = cy = 0;
            offset = Vector2.zero;
            pivot = Vector2.zero;
            return;
        }

        // compute uv bounds
        var Uc = mesh.U;
        Vector2 uv_min = new Vector2(Uc[0].u, Uc[0].v);
        Vector2 uv_max = uv_min;

        for (int i = 1; i < Uc.Length(); i++)
        {
            uv_min = Vector2.Min(uv_min, new Vector2(Uc[i].u, Uc[i].v));
            uv_max = Vector2.Max(uv_max, new Vector2(Uc[i].u, Uc[i].v));
        }

        // grow bounds for added robustness
        uv_min -= new Vector2(0.1f * pyp.px, 0.1f * pyp.py);
        uv_max += new Vector2(0.1f * pyp.px, 0.1f * pyp.py);

        pivot = pyp.Qmin;  // offset to cell corners (e.g. to align with pyp)

        cx = pyp.px;
        cy = pyp.py;

        // compute offset and number
        int i_startx = Mathf.FloorToInt((uv_min.x - pivot.x) / cx);
        int i_endx = Mathf.FloorToInt((uv_max.x - pivot.x) / cx);
        nx = i_endx - i_startx + 1;
        int i_starty = Mathf.FloorToInt((uv_min.y - pivot.y) / cy);
        int i_endy = Mathf.FloorToInt((uv_max.y - pivot.y) / cy);
        ny = i_endy - i_starty + 1;

        offset.x = i_startx * cx + pivot.x;
        offset.y = i_starty * cy + pivot.y;

        Debug.LogFormat("UV Grid: [{0} x {1}]", ny, nx);
    }

    // compute which cells overlap which triangles and store the results
    public void OverlapTriangles(MeshData mesh, float eps = 1e-3f)
    {
        eps *= (cx + cy) * 0.5f;  // tolerance relative to cell size

        int n_tris = mesh.Fms.Length();
        var Fmsc = mesh.Fms;
        var Uc = mesh.U;

        // NOTE for now tri2cells is temporary, and used to construct its inverse
        List<List<Tuple<int, int>>> tri2cells = new List<List<Tuple<int, int>>>(n_tris);

        for (int tri = 0; tri < n_tris; tri++)
        {
            var ixs = Fmsc[tri];
            var coords = new MeshData.MSVertex[3];

            for (int i = 0; i < 3; i++)
            {
                coords[i] = Uc[(int)ixs.v0];
            }

            // Compute the bounding box of the triangle in grid coordinates
            Vector2 uv_min = Vector2.Min(Vector2.Min(new Vector2( coords[0].u, coords[0].v), new Vector2(coords[1].u, coords[1].v)), new Vector2(coords[2].u, coords[2].v));
            Vector2 uv_max = Vector2.Max(Vector2.Max(new Vector2(coords[0].u, coords[0].v), new Vector2(coords[1].u, coords[1].v)), new Vector2(coords[2].u, coords[2].v));

            // Grow the bounds for added robustness
            uv_min -= new Vector2(eps, eps);
            uv_max += new Vector2(eps, eps);

            // Find the overlapping cells
            int i_startx = Mathf.FloorToInt((uv_min.x - offset.x) / cx);
            int i_endx = Mathf.FloorToInt((uv_max.x - offset.x) / cx);
            int i_starty = Mathf.FloorToInt((uv_min.y - offset.y) / cy);
            int i_endy = Mathf.FloorToInt((uv_max.y - offset.y) / cy);

            // Add the overlapping cells to the triangle's list
            for (int i = i_startx; i <= i_endx; i++)
            {
                for (int j = i_starty; j <= i_endy; j++)
                {
                    tri2cells[tri].Add(new Tuple<int, int>(i, j));
                }
            }
        }

        // Construct the inverse mapping k2ij and populate cellk2tris
        ij2k = new int[ny, nx];
        k2ij = new List<Tuple<int, int>>();
        cellk2tris = new List<Queue<int>>(ny * nx);

        for (int i = 0; i < ny; i++)
        {
            for (int j = 0; j < nx; j++)
            {
                ij2k[i, j] = -1;
                cellk2tris.Add(new Queue<int>());
            }
        }

        for (int tri = 0; tri < n_tris; tri++)
        {
            foreach (Tuple<int, int> cell in tri2cells[tri])
            {
                int i = cell.Item1;
                int j = cell.Item2;

                if (ij2k[i, j] == -1)
                {
                    ij2k[i, j] = k2ij.Count;
                    k2ij.Add(new Tuple<int, int>(i, j));
                }

                int k = ij2k[i, j];
                cellk2tris[k].Enqueue(tri);
            }
        }

        n_filled = k2ij.Count;

        Debug.LogFormat("Triangles Overlap: {0} triangles in {1} filled cells", n_tris, n_filled);
    }
}
