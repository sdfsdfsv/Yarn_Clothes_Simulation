using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UI;
using UnityEditor;
using System.Linq;

public partial class YarnMeshGenerator : MonoBehaviour
{
    [Space]

    [Header("update Properties")]

    [Space]

    public bool fixedFramerate;

    public int frameRate;

    public ComputeShader computeShader;

    public ComputeBuffer verticesBuffer;

    public ComputeBuffer verticesOutBuffer;

    public ComputeBuffer normalsBuffer;

    public ComputeBuffer velocitiesBuffer;

    public GameObject plane;

    public List<GameObject> sphereColliders;

    public Vector3 windForce;

    [Range(1, 20)]
    public int ITERATIONS;

    public float stiffnessX;

    public float stiffnessY;

    public float mass;

    public float damping;

    public float frictionFactor;

    [Header("结构力")]
    public float structuralForce;

    public readonly float strcturalLen;

    [Header("剪力")]
    public float shearForce;

    public readonly float shearLen;

    [Header("弯曲力")]
    public float flexionForce;

    public readonly float flexionLen;


    private void InComputeShader()
    {
        int kernel = computeShader.FindKernel("YarnMain");

        computeShader.SetInt("subdivision", subdivision);

        computeShader.SetFloat("width", width);

        computeShader.SetFloat("height", height);



        // Create Compute Buffer objects
        verticesBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);

        verticesOutBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);

        normalsBuffer = new ComputeBuffer(mesh.normals.Length, sizeof(float) * 3);

        velocitiesBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);



        // Create new arrays
        Vector3[] newVertices = new Vector3[mesh.vertices.Length];

        Vector3[] newVerticesOut = new Vector3[mesh.vertices.Length];

        Vector3[] newNormals = new Vector3[mesh.normals.Length];



        // Deep copy arrays
        Array.Copy(mesh.vertices, newVertices, mesh.vertices.Length);

        Array.Copy(mesh.vertices, newVerticesOut, mesh.vertices.Length);

        Array.Copy(mesh.normals, newNormals, mesh.normals.Length);



        // Set data to Compute Buffers
        verticesBuffer.SetData(newVertices);

        verticesOutBuffer.SetData(newVerticesOut);

        normalsBuffer.SetData(newNormals);

        velocitiesBuffer.SetData(new Vector3[mesh.vertices.Length]);



        // Set Compute Shader buffers
        computeShader.SetBuffer(kernel, "vertices", verticesBuffer);

        computeShader.SetBuffer(kernel, "verticesOut", verticesOutBuffer);

        computeShader.SetBuffer(kernel, "normals", normalsBuffer);

        computeShader.SetBuffer(kernel, "velocities", velocitiesBuffer);



        vertices4Compute = new Vector3[subdivision * subdivision];
    }

    private Vector3[] vertices4Compute;

    void UpComputeShader()
    {
        // 在 Compute Shader 中计算顶点数据
        int kernel = computeShader.FindKernel("YarnMain");

        computeShader.SetMatrix("transformMatrix", transform.localToWorldMatrix);

        computeShader.SetMatrix("intransformMatrix", transform.worldToLocalMatrix);

        computeShader.SetFloat("dt", Time.deltaTime / ITERATIONS);

        computeShader.SetFloat("mass", mass);

        computeShader.SetFloat("damping", damping);

        computeShader.SetFloat("frictionFactor", frictionFactor);

        computeShader.SetFloat("planeHeight", plane.transform.position.y + 0.1f);

        computeShader.SetVector("spherePos", sphereColliders[0].transform.position);

        computeShader.SetFloat("sphereRad", sphereColliders[0].transform.localScale.x / 2);

        computeShader.SetVector("windForce", windForce);

        computeShader.SetFloat("stiffnessX", stiffnessX);

        computeShader.SetFloat("stiffnessY", stiffnessY);

        computeShader.SetVector("springKs", new Vector3(structuralForce, shearForce, flexionForce));

        computeShader.SetVector("springLens", new Vector3(width / (subdivision - 1), width / (subdivision - 1) * Mathf.Sqrt(2), 2.0f * width / (subdivision - 1)));

        computeShader.SetBool("writeIn", false);


        for (int i = 0; i < ITERATIONS; i++)
        {

            computeShader.Dispatch(kernel, subdivision / 8, subdivision / 8, 1);

        }

        computeShader.SetBool("writeIn", true);

        computeShader.Dispatch(kernel, subdivision / 8, subdivision / 8, 1);


        UpdateMesh();



    }




    void UpdateMesh()
    {
        verticesBuffer.GetData(vertices4Compute);

        this.mesh.vertices = vertices4Compute;

        this.mesh.RecalculateNormals();

        meshFilter.sharedMesh = this.mesh;

        meshCollider.sharedMesh = this.mesh;
    }

    void Update()
    {

        UpComputeShader();

        VisualPoints();
    }

    void OnDestroy()
    {
        // 释放Compute Buffer对象
        verticesBuffer.Release();

        normalsBuffer.Release();
    }

}