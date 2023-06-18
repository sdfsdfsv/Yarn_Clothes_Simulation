using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshData : MonoBehaviour
{
    // uv coords
    public struct MSVertex
    {
        public float u;
        public float v;
        public Vector2 map()
        {
            return new Vector2(u, v);
        }
    }



    // vertex indices: v0, v1, v2
    public struct Face
    {
        public uint v0;
        public uint v1;
        public uint v2;
        public Vector3 map()
        {
            return new Vector3(v0, v1, v2);
        }
    }

    // FEM-matrix (inverse of (e1, e2))
    // and first mat-space vertex U0 in a triangle
    public struct DinvU
    {
        public float Dinv11;
        public float Dinv21;
        public float Dinv12;
        public float Dinv22;
        public float U0x;
        public float U0y;
    }

    // Deformation Gradient F
    public struct FDefo
    {
        public float F11;
        public float F21;
        public float F31;
        public float F12;
        public float F22;
        public float F32;

        public static explicit operator FDefo(DinvU v)
        {
            throw new NotImplementedException();
        }
    }

    // Ixx, Ixy, Iyy, IIxx, IIxy, IIyy;
    public struct Strain
    {
        public float Ixx;
        public float Ixy;
        public float Iyy;
        public float IIxx;
        public float IIxy;
        public float IIyy;
    }

    public VectorBuffer<Vector3> X;
    public VectorBuffer<MSVertex> U;
    public VectorBuffer<Face> F;
    public VectorBuffer<Face> Fms;  // world- and material-space faces

    public VectorBuffer<DinvU> invDmU;             // FEM-matrix (inverse of (e1, e2))
    public VectorBuffer<Vector3> normals;         // world-space face normals
    public VectorBuffer<Vector3> vertex_normals;  // world-space vertex normals
    public VectorBuffer<FDefo> defF;               // per-face deformation gradient
    public VectorBuffer<FDefo> vertex_defF;        // per-vertex deformation gradient
    public VectorBuffer<Strain> strains;           // per-face I,II (flattened)
    public VectorBuffer<Strain> vertex_strains;    // per-vertex I,II
    public List<Queue<KeyValuePair<int, float>>> v2f;           // vertex-to-face weights

    public bool Empty()
    {
        return (X.cpu().Count == 0) || (Fms.cpu().Count == 0);
    }

    // Compute barycentric coordinates of point p in material-space triangle with index tri
    public Vector3 BarycentricMS(int tri, Vector2 p)
    {
        // Implement your barycentric coordinate calculation here
        return Vector3.zero; // Placeholder return value
    }

    // Compute FEM-matrix
    public void ComputeInvDm()
    {
      
        List<List<int>> Fms = new List<List<int>>(); // Replace this with your data

        List<List<Vector2>> invDmU_cpu = new List<List<Vector2>>();
        invDmU_cpu.Resize(Fms.Count);
        List<Vector2> Uc = new List<Vector2>(); // Replace this with your data

        for (int i = 0; i < invDmU_cpu.Count; i++)
        {
            List<int> ixs = Fms[i];

            Matrix2 Dm = new Matrix2();
            Vector2 U0 = Uc[ixs[0]];
            Dm.col[0] = Uc[ixs[1]] - U0;
            Dm.col[1] = Uc[ixs[2]] - U0;

            invDmU_cpu[i] = Dm.Inverse();
        }
    
    }

    // Compute face adjacency
    public void ComputeFaceAdjacency()
    {
        // Implement your face adjacency computation here
    }

    // Compute vertex-to-face weights
    public void ComputeV2FMap(bool shepardWeights = true)
    {
        // Implement your vertex-to-face weight computation here
    }

    // Compute normals, deformation gradient, I, II
    public void ComputeFaceData()
    {
        normals.m_data = new List<Vector3>(F.cpu().Count);
        defF.m_data= new List<FDefo>(F.cpu().Count);
        strains.m_data = new List<Strain>(F.cpu().Count);

        for (int f = 0; f < normals.cpu().Count; f++)
        {
            var ixs = F.cpu()[f].map();
            Vector3 e01 = X.cpu()[(int)ixs[1]] - X.cpu()[(int)ixs[0]];
            Vector3 e02 = X.cpu()[(int)ixs[2]] - X.cpu()[(int)ixs[0]];

            Vector3 n = Vector3.Cross(e01, e02);
            float inv2A = 1f / n.magnitude;
            n *= inv2A;
            normals.cpu()[f] = n;

            FDefo defoF = new FDefo();
            defoF. = e01;
            defoF.col(1) = e02;
            defoF *= (FDefo)invDmU.cpu()[f] ;
            var s = strains[f];

            defF[f] = defoF;

            // in plane
            float C00 = defoF.col(0).sqrMagnitude;
            float C01 = Vector3.Dot(defoF.col(0), defoF.col(1));
            float C11 = defoF.col(1).sqrMagnitude;
            s[0] = C00;  // NOTE: FtF not S !
            s[1] = C01;
            s[2] = C11;

            // bending
            // II = F.T Lam F = sum_i thetai / (2 A li) F^T ti o F^t ti  , with ti of
            // length li
            var adj = Fmsadj[f];
            s.tail < 3 > ().SetZero();
            for (int i = 0; i < 3; i++)
            {
                if (adj.faces[i] < 0)
                    continue;
                Vector3 ei = X.cpu()[ixs[(i + 1) % 3]].map() - X.cpu()[ixs[i]].map();
                Vector3 ni = Vector3.Cross(
                    X.cpu()[Fc[adj.faces[i]].map()[adj.opp[i]]].map() - X.cpu()[ixs[i]].map(),
                    ei
                );
                Vector2 FTti = defoF.transpose() * Vector3.Cross(ei, n);
                float invli = 1f / ei.magnitude;
                float theta = -SignedAngle(n, ni, ei * invli);
                float c = theta * inv2A * invli;
                s[3] += c * FTti[0] * FTti[0];
                s[4] += c * FTti[0] * FTti[1];
                s[5] += c * FTti[1] * FTti[1];
            }
        }
    }

    public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
    {
        float unsignedAngle = Vector3.Angle(from, to);
        float sign = Mathf.Sign(Vector3.Dot(axis, Vector3.Cross(from, to)));
        return unsignedAngle * sign;
    }


    // Compute vertex normals
    public void ComputeVertexNormals()
    {
        var fdata = normals.cpu(); // Assuming VectorBuffer is a class that holds a collection of Vector6 objects
        var vdata = vertex_normals.cpu();


        for (int i = 0; i < v2f.Count; i++)
        {
            Vector3 v = Vector3.zero;

            foreach (var fw in v2f[i])
            {
                v.x += fdata[fw.Key].x * fw.Value;

            }
            v.Normalize();
            vdata[i] = v;
        }
    }

    // Compute vertex deformation gradient
    public void ComputeVertexDefF()
    {
        var fdata = defF.cpu(); // Assuming VectorBuffer is a class that holds a collection of Vector6 objects
        var vdata = vertex_defF.cpu();


        for (int i = 0; i < v2f.Count; i++)
        {
            FDefo v = new FDefo();

            foreach (var fw in v2f[i])
            {
                v.F11 += fdata[fw.Key].F11 * fw.Value;
                v.F12 += fdata[fw.Key].F12 * fw.Value;
                v.F21 += fdata[fw.Key].F21 * fw.Value;
                v.F22 += fdata[fw.Key].F22 * fw.Value;
                v.F31 += fdata[fw.Key].F31 * fw.Value;
                v.F32 += fdata[fw.Key].F32 * fw.Value;
            }

            vdata[i] = v;
        }
    }


    // Compute vertex strains
    public void ComputeVertexStrains()
    {
        var fdata = strains.cpu(); // Assuming VectorBuffer is a class that holds a collection of Vector6 objects
        var vdata = vertex_strains.cpu();


        for (int i = 0; i < v2f.Count; i++)
        {
            Strain v = new Strain();

            foreach (var fw in v2f[i])
            {
                v.Ixx += fdata[fw.Key].Ixx * fw.Value;
                v.Iyy += fdata[fw.Key].Iyy * fw.Value;
                v.Ixy += fdata[fw.Key].Ixy * fw.Value;
                v.IIxx += fdata[fw.Key].IIxx * fw.Value;
                v.IIyy += fdata[fw.Key].IIyy * fw.Value;
                v.IIxy += fdata[fw.Key].IIxy * fw.Value;
            }

            vdata[i] = v;
        }
    }

}
