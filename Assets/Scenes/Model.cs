using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public struct AxesInfo
{
    const int AXES_MAX_LENGTH = 32;
    public uint lenSX, lenSA, lenSY;
    public float[] SX;
    public float[] SA;
    public float[] SY;
    public float[] invSX;
    public float[] invSA;
    public float[] invSY;

    public AxesInfo()
    {
        SX = new float[AXES_MAX_LENGTH];
        SA = new float[AXES_MAX_LENGTH];
        SY = new float[AXES_MAX_LENGTH];
        invSX = new float[AXES_MAX_LENGTH];
        invSA = new float[AXES_MAX_LENGTH];
        invSY = new float[AXES_MAX_LENGTH];
    }
}




public struct DisplacementEntry
{
    public float x, y, z, th;

    public Vector4 Map()
    {
        return new Vector4(x, y, z, th);
    }

}


//public struct AxWrapper
//{
//    public uint len;
//    public float[] data;
//    public float[] invdata;

//    public AxWrapper(uint len, float[] data, float[] invdata)
//    {
//        this.len = len;
//        this.data = data;
//        this.invdata = invdata;
//    }
//}


public class Model
{
    bool m_initialized;
    PeriodicYarnPattern m_pyp;

    public VectorBuffer<AxesInfo> m_tex_sxsasy_axes;
    public VectorBuffer<DisplacementEntry> m_tex_sxsasy_data;

    public PeriodicYarnPattern getPYP()
    {
        return m_pyp;
    }

    public bool isInitialized()
    {
        return m_initialized;
    }

    public VectorBuffer<AxesInfo> getTexAxes()
    {
        return m_tex_sxsasy_axes;
    }

    public VectorBuffer<DisplacementEntry> getTexData()
    {
        return m_tex_sxsasy_data;
    }


    public Vector4 displacement(List<float> strain, uint pix)
    {
        Vector3 strain3D = new Vector3(strain[0], strain[1], strain[2]);
        Vector4 g = sample3D(strain3D, pix);
        return g;
    }

    private Vector4 sample3D(Vector3 strain, uint pix)
    {
        // Retrieve axes and data from buffers
        AxesInfo axes = m_tex_sxsasy_axes.cpu()[0];
        List<DisplacementEntry> data = m_tex_sxsasy_data.cpu();

        Vector4 sample_at(int i_sx, int i_sa, int i_sy, int pix)
        {
            // Calculate the index within the data array
            int loc = (int)(i_sx + axes.lenSX * (i_sa + axes.lenSA * (i_sy + axes.lenSY * (int)pix)));

            // Retrieve the sample at the calculated index
            DisplacementEntry entry = data[loc];
            return new Vector4(entry.x, entry.y, entry.z, entry.th);
        }

        // Calculate indices and interpolation weights
        float a_sx=0f, a_sa=0f, a_sy = 0f;
        int i_sx=0, i_sa = 0, i_sy = 0;

        for (int i = 0; i < 3; i++)
        {
            float val = strain[i];
            float[] ax = (i == 0) ? axes.SX : ((i == 1) ? axes.SA : axes.SY);
            float[] invax = (i == 0) ? axes.invSX : ((i == 1) ? axes.invSA : axes.invSY);
            uint len = (i == 0) ? axes.lenSX : ((i == 1) ? axes.lenSA : axes.lenSY);

            int c = System.Array.BinarySearch(ax, 1, (int)(len - 1), val);
            if (c < 0)
                c = ~c - 1;
            c = Mathf.Clamp(c, 0, (int)len - 2);

            float val0 = ax[c];
            float invval0 = invax[c];
            float val1 = ax[c + 1];
            float invval1 = invax[c + 1];

            float a = (val - val0) * invval0;
            a = Mathf.Clamp01(a);

            if (i == 0)
            {
                a_sx = a;
                i_sx = c;
            }
            else if (i == 1)
            {
                a_sa = a;
                i_sa = c;
            }
            else
            {
                a_sy = a;
                i_sy = c;
            }
        }

        // Perform trilinear interpolation
        Vector4 g = Vector4.zero;
        g += (1 - a_sx) * (1 - a_sa) * (1 - a_sy) * sample_at(i_sx, i_sa, i_sy, (int)pix);
        g += (1 - a_sx) * (1 - a_sa) * a_sy * sample_at(i_sx, i_sa, i_sy + 1, (int)pix);
        g += (1 - a_sx) * a_sa * (1 - a_sy) * sample_at(i_sx, i_sa + 1, i_sy, (int)pix);
        g += (1 - a_sx) * a_sa * a_sy * sample_at(i_sx, i_sa + 1, i_sy + 1, (int)pix);
        g += a_sx * (1 - a_sa) * (1 - a_sy) * sample_at(i_sx + 1, i_sa, i_sy, (int)pix);
        g += a_sx * (1 - a_sa) * a_sy * sample_at(i_sx + 1, i_sa, i_sy + 1, (int)pix);
        g += a_sx * a_sa * (1 - a_sy) * sample_at(i_sx + 1, i_sa + 1, i_sy, (int)pix);
        g += a_sx * a_sa * a_sy * sample_at(i_sx + 1, i_sa + 1, i_sy + 1, (int)pix);

        return g;
    }

}