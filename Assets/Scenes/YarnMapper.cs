using UnityEngine;

public class YarnMapper
{
    public struct Settings
    {
        public string modelfolder;
        public float min_yarn_length_per_r;
        public bool shepard_weights;
        public float deform_reference;
        public float linearized_bending;
        public bool shell_map;
        public bool repeat_frame;
        public bool gpu_compute;
        public float phong_deformation;
        public float svdclamp;
       
    }


    public Settings m_settings;

    Model m_model;
    private YarnSoup m_soup;

    private ComputeShader deformShader;
    private ComputeShader shellMapShader;



    public void Step()
    {
        MeshData mesh = new MeshData();
        m_model.getTexAxes().BufferData();
        m_model.getTexData().BufferData();

        mesh.ComputeFaceData();

        mesh.ComputeVertexNormals();
        mesh.ComputeVertexDefF();
        mesh.ComputeVertexStrains();

        mesh.X.BufferData();
        mesh.vertex_strains.BufferData();
        mesh.normals.BufferData();
        mesh.defF.BufferData();

        if (m_settings.phong_deformation > 0)
        {
            mesh.vertex_defF.BufferData();

        }

        if (m_soup.)

        // Method implementation
    }

}
