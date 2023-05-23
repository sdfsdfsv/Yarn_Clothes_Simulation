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

    [Header("update Properties")]

    [Space]
    
    public int frameRate;

    public ComputeShader computeShader;

    public ComputeBuffer verticesBuffer;

    public ComputeBuffer normalsBuffer;

    public ComputeBuffer velocitiesBuffer;

    public GameObject plane;

    public List<GameObject> sphereColliders;

    public int ITERATIONS;

    public float stiffnessX;

    public float stiffnessY;

    public float mass;

    public float damping;

    [Header("�ṹ��")]
    public float structuralForce;

    public readonly float strcturalLen;

    [Header("����")]
    public float shearForce;

    public readonly float shearLen;

    [Header("������")]
    public float flexionForce;

    public readonly float flexionLen;

    private void InComputeShader()
    {
        int kernel = computeShader.FindKernel("YarnMain");

        computeShader.SetInt("subdivision", subdivision);

        computeShader.SetFloat("width", width);

        computeShader.SetFloat("height", height);


        // ����Compute Buffer����
        verticesBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);

        // ����������
        Vector3[] newVertices = new Vector3[mesh.vertices.Length];

        // �������
        Array.Copy(mesh.vertices, newVertices, mesh.vertices.Length);

        // ��vertices���鴫��Compute Buffer��
        verticesBuffer.SetData(newVertices);

        // ����Compute Shader�е�verticesBuffer
        computeShader.SetBuffer(kernel, "vertices", verticesBuffer);


        // ����Compute Buffer����
        normalsBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);

        // ����������
        Vector3[] newNormals = new Vector3[mesh.normals.Length];

        // �������
        Array.Copy(mesh.normals, newVertices, mesh.normals.Length);

        // ��vertices���鴫��Compute Buffer��
        normalsBuffer.SetData(newNormals);

        // ����Compute Shader�е�verticesBuffer
        computeShader.SetBuffer(kernel, "normals", normalsBuffer);


        // ����Compute Buffer����
        velocitiesBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof (float) * 3);

        velocitiesBuffer.SetData(new Vector3[mesh.vertices.Length]);

        computeShader.SetBuffer(kernel, "velocities", velocitiesBuffer);


        vertices4Compute = new Vector3[subdivision * subdivision];

    }

    private Vector3[] vertices4Compute;

    void UpComputeShader()
    {
        // �� Compute Shader �м��㶥������
        int kernel = computeShader.FindKernel("YarnMain");

        computeShader.SetMatrix("transformMatrix", transform.localToWorldMatrix);

        computeShader.SetMatrix("intransformMatrix", transform.worldToLocalMatrix);

        computeShader.SetFloat("dt", Time.deltaTime);

        computeShader.SetFloat("mass", mass);

        computeShader.SetFloat("damping", damping);

        computeShader.SetFloat("planeHeight", plane.transform.position.y+0.1f);

        computeShader.SetVector("spherePos", sphereColliders[0].transform.position);

        computeShader.SetFloat("sphereRad", sphereColliders[0].transform.localScale.x / 2);

        computeShader.SetFloat("stiffnessX", stiffnessX);

        computeShader.SetFloat("stiffnessY", stiffnessY);

        computeShader.SetVector("springKs", new Vector3(structuralForce, shearForce, flexionForce));

        computeShader.SetVector("springLens", new Vector3(width/(subdivision-1), width/(subdivision-1)*Mathf.Sqrt(2), 2*width/(subdivision-1)));

        computeShader.SetBool("applyConstraints", false);

        computeShader.Dispatch(kernel, subdivision / 8, subdivision / 8, 1);

        computeShader.SetBool("applyConstraints", true);

        for (int i = 0; i < ITERATIONS; i++)
        {

            computeShader.Dispatch(kernel, subdivision / 8, subdivision / 8, 1);
        
        }

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
        // �ͷ�Compute Buffer����
        verticesBuffer.Release();

        normalsBuffer.Release();
    }

}