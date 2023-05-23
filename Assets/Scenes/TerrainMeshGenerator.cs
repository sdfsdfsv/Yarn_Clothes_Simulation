using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TerrainMeshGenerator : MonoBehaviour
{
    public int subdivision;
    public float range;

    public float width;
    public float height;
    public float depth;


    public Vector2 off;

    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public MeshRenderer meshRenderer;

    private void OnValidate()
    {
        GenerateMesh();
    }
    // Start is called before the first frame update
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        GenerateMesh();

    }

    public Vector2 GetGradient(Vector2 intPos, float t)
    {
        float rand = Mathf.Sin(Vector2.Dot(intPos,new Vector2(14.1f,41.11f)))*5.5f;

        rand=rand-Mathf.Floor(rand);

        float angle = 6.2f * rand + 4.0f * t * rand;

        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

    }

    public float Pseudo3dNoise(Vector3 pos)
    {
        Vector2 pxy = new Vector2(pos.x, pos.y);

        Vector2 i = new Vector2(Mathf.Floor(pos.x), Mathf.Floor(pos.y));

        Vector2 f = pxy - i;

        Vector2 blend = f * f * (Vector2.one * 3.0f - 2.0f * f);

        float downleft = Vector2.Dot(GetGradient(i + Vector2.zero, pos.z), f - Vector2.zero);

        float downright = Vector2.Dot(GetGradient(i + Vector2.right, pos.z), f - Vector2.right);

        float upleft = Vector2.Dot(GetGradient(i + Vector2.up, pos.z), f - Vector2.up);

        float upright = Vector2.Dot(GetGradient(i + Vector2.one, pos.z), f - Vector2.one);

        float noiseVal = mix(mix(downleft, downright, blend.x), mix(upleft, upright, blend.x), blend.y);

        return noiseVal ;
    }
    public void GenerateMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[subdivision * subdivision];

        int triangleCnt = (subdivision - 1) * (subdivision - 1) * 6;

        int[] triangles = new int[triangleCnt];

        int tris = 0;


        for (int i = 0; i < subdivision; i++)
        {
            for (int j = 0; j < subdivision; j++)
            {
                int ind = i * subdivision + j;

                float x = i * width / subdivision; float y = j * height / subdivision;

                vertices[ind] = new Vector3(x, y, depth*getdepth(off + (new Vector2(x,y))*range));

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

        mesh.RecalculateBounds();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        meshCollider.sharedMesh = mesh;

    }

    float getdepth(Vector2 uv)
    {
        const int ITERATIONS = 10;
        float noiseVal = 0.0f;
        float sum = 0.0f;
        float multiplier = 1.0f;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Vector3 noisePos = new Vector3(uv.x, uv.y, 11.2f / multiplier);
            noiseVal += multiplier * Mathf.Abs(Pseudo3dNoise(noisePos));
            sum += multiplier;
            multiplier *= 0.6f;
            uv = 2.0f * uv + 4.3f * Vector2.one;
        }
        //noiseVal /= 1;
        noiseVal /= sum;

        noiseVal = Mathf.Clamp( ( - 0.5f * (6.283185f * 3.0f * noiseVal) + 1.4f),0f,1f);
        return noiseVal;
    }
    float mix(float x, float y, float z)
    {
        return x * (1-z) + y * ( z);
    }
    // Update is called once per frame
    void Update()
    {

    }
}


[CustomEditor(typeof(TerrainMeshGenerator))]

public class TerrainMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainMeshGenerator myScript = (TerrainMeshGenerator)target;
        if (GUILayout.Button("´´½¨Íø¸ñ"))
        {
            myScript.GenerateMesh();
        }
    }
}