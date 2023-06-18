using System.Collections.Generic;
using UnityEngine;

public struct PeriodicYarnPattern
{

    public float px = 0;
    public float py = 0;
    public float r = 0;
    public List<Vector4> Q = new List<Vector4>();        // vertex data [x y z t]
    public List<Vector4> E = new List<Vector4>();        // periodic edges [v0, v1, di, dj]
    public List<float> RL = new List<float>();           // restlengths (NOTE: stored per vertex outgoing edge, not per edge index!)
    public List<Vector3> RefD1 = new List<Vector3>();    // ref directors (NOTE: stored per vertex outgoing edge, not per edge index!)
    public Vector2 Qmin;
    public List<Vector4> VE = new List<Vector4>(); // vertex edge table [eix_prev, eix_next]

    public PeriodicYarnPattern()
    {
    }

    public void Rectangulize()
    {
        if (VE.Count != Q.Count)
            RecomputeVEtable();

        Vector2 minxy = Qmin;

        for (int i = 0; i < Q.Count; i++)
        {
            Vector2 xy = new Vector2(Q[i].x, Q[i].y);
            int dx = Mathf.FloorToInt((xy.x - minxy.x) / px);
            int dy = Mathf.FloorToInt((xy.y - minxy.y) / py);

            if (dx != 0 || dy != 0)
            {
                Q[i] -= new Vector4(px * dx, py * dy, 0, 0);
                int x = (int)VE[i].x;
                int y= (int)VE[i].y;
                E[x] += new Vector4(0,0,dx,0);
                E[y] -= new Vector4(0,0,dx,0);
                E[x] += new Vector4(0, 0, 0, dy);
                E[y] -= new Vector4(0, 0, 0, dy);
                
            }
        }

    }

    public bool IsPeriodicEdge(int eix)
    {
        return !(E[eix].z == 0 && E[eix].w == 0);
    }

    public void RecomputeVEtable()
    {
        VE = new List<Vector4>(Q.Count);
        for (int i = 0; i < Q.Count; i++)
            VE.Add(new Vector4(-1, -1));

        for (int i = 0; i < E.Count; i++)
        {
            int x = (int)E[i].x;
            int y = (int)E[i].y;
            VE[x] = new Vector4(VE[x].x, i, VE[x].z, VE[x].w);
            VE[y] = new Vector4(i, VE[y].y, VE[y].z, VE[y].w); ;
        }
    }

    public List<uint> ComputeSimpleYarns()
    {
        if (VE.Count != Q.Count)
            RecomputeVEtable();

        List<uint> ixs = new List<uint>();

        for (int i = 0; i < E.Count; ++i)
        {
            if (IsPeriodicEdge(i))
            {
                int eix = i;
                int vix = (int)E[eix].y;
                ixs.Add((uint)vix);
                eix = (int)VE[vix].y;

                while (!IsPeriodicEdge(eix))
                {
                    vix = (int)E[eix].y;
                    ixs.Add((uint)vix);
                    eix = (int)VE[vix].y;
                }

                ixs.Add(uint.MaxValue);
            }
        }

        return ixs;
    }
}
