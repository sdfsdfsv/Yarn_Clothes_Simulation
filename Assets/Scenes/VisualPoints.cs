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

    [Header("visual Points")]

    [Space]

    public Dictionary<int, GameObject> visualPoints = new Dictionary<int, GameObject>();

    public Dictionary<GameObject, int> pointsRef= new Dictionary<GameObject, int>();

    public GameObject pointPref;

    public bool showPoints; 

    public void VisualPoints()
    {

        if (!showPoints)
        {
            foreach(var point in visualPoints.Values)
            {
                point.gameObject.SetActive(false);
            }
            return;
        }

        foreach (var point in visualPoints.Values)
        {
            point.gameObject.SetActive(true);
        }

        int cnt = 0;
        foreach (var vertex in mesh.vertices)
        {
            GameObject sphere;

            if (visualPoints.ContainsKey(cnt))
            {
                sphere = visualPoints[cnt];
            }
            else
            {
                // 实例化小球体
                sphere = visualPoints[cnt] = Instantiate(pointPref,GameObject.Find("Yarn Cloth").transform);

                sphere.transform.localScale = Vector3.one * 4f / subdivision ;

                pointsRef[sphere] = cnt;
            }
            
            sphere.transform.localPosition = (vertex);

            cnt++;
        }
    }


}