using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UI;
using UnityEditor;
using System.Linq;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public partial class YarnMeshGenerator : MonoBehaviour
{
    [Space]

    [Header("generator Properties")]

    [Space]

    public int subdivision;

    public float width;

    public float height;

    public MeshFilter meshFilter;

    public MeshCollider meshCollider;

    public MeshRenderer meshRenderer;

    public Mesh mesh;

    // Start is called before the first frame update
    void Start()
    {

        Application.targetFrameRate = frameRate;

        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        GenerateMesh();

        VisualPoints();

        InComputeShader();
    }


    public void GenerateMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[subdivision * subdivision];

        int triangleCnt = (subdivision - 1) * (subdivision - 1) * 6;

        int[] triangles = new int[triangleCnt];

        int tris = 0;

        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < subdivision; i++)
        {
            for (int j = 0; j < subdivision; j++)
            {
                int ind = i * subdivision + j;

                float x = i * width / subdivision; float y = j * height / subdivision;

                vertices[ind] = new Vector3(x, y, 0);

                uvs[ind] = new Vector2(i, j);

                if (i > 0 && j > 0)
                {
                    triangles[tris++] = ind - 1;

                    triangles[tris++] = ind;

                    triangles[tris++] = ind - subdivision;

                    triangles[tris++] = ind - subdivision - 1;

                    triangles[tris++] = ind - 1;

                    triangles[tris++] = ind - subdivision;
                }
            }
        }

        mesh.vertices = vertices;

        mesh.triangles = triangles;

        mesh.uv = uvs;

        mesh.RecalculateBounds();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        meshCollider.sharedMesh = mesh;

        this.mesh = mesh;

    }

}



[CustomEditor(typeof(YarnMeshGenerator))]
public class ObjectBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        YarnMeshGenerator myScript = (YarnMeshGenerator)target;

        if (GUILayout.Button("创建网格"))
        {
            myScript.GenerateMesh();
        }
        if (GUILayout.Button("创建可见点"))
        {
            myScript.VisualPoints();
        }
    }
}
