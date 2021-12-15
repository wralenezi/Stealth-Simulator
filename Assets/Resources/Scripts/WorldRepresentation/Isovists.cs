using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using ClipperLib;
using UnityEngine;

public class Isovists : MonoBehaviour
{
    private List<MeshPolygon> m_NavMesh;
    private List<List<Polygon>> m_Isovists;

    private GameObject m_FovGameObject;
    private FieldOfView m_Fov;

    public void Initiate(List<MeshPolygon> navMesh)
    {
        m_Isovists = new List<List<Polygon>>();

        m_NavMesh = navMesh;

        // Create the Isovists
        CreateIsovists();

        //  Separate the isovists
        CalculateVisibility();
    }

    public void CreateIsovists()
    {
        foreach (var navMeshPolygon in m_NavMesh)
        {
            AddFoV();

            m_FovGameObject.transform.position = navMeshPolygon.GetCentroidPosition();

            m_Fov.CastFieldOfView();

            List<Vector3> fov = m_Fov.GetFovVertices();

            Polygon p = new Polygon();

            foreach (var v in fov)
                p.AddPoint(v);

            List<Polygon> isovist = new List<Polygon>() {p};

            m_Isovists.Add(isovist);
        }
    }


    public void CalculateVisibility()
    {
        for (int i = 0; i < 2; i++)
        {
            for (int j = i + 1; j < 4; j++)
            {
                if (SeparatePolygons(i, j))
                {
                    i = 0;
                    break;
                }
            }
        }
    }


    // Separate the two polygons and add their intersection as a new polygon
    // param i: index of the first polygon
    // param j: index of the second polygon
    public bool SeparatePolygons(int i, int j)
    {
        // Intersect the two polygons to see if there is any
        List<Polygon> intersection =
            PolygonHelper.MergePolygons(m_Isovists[i], m_Isovists[j], ClipType.ctIntersection);


        if (intersection.Count > 0)
        {
            List<Polygon> first = PolygonHelper.MergePolygons(intersection, m_Isovists[i], ClipType.ctDifference);

            for (int k = 0; k < first.Count; k++)
            {
                List<Polygon> newPoly = new List<Polygon>() {first[k]};
                m_Isovists.Add(newPoly);
            }

            List<Polygon> second = PolygonHelper.MergePolygons(intersection, m_Isovists[j], ClipType.ctDifference);

            for (int k = 0; k < second.Count; k++)
            {
                List<Polygon> newPoly = new List<Polygon>() {second[k]};
                m_Isovists.Add(newPoly);
            }

            for (int k = 0; k < intersection.Count; k++)
            {
                List<Polygon> newPoly = new List<Polygon>() {intersection[k]};
                m_Isovists.Add(newPoly);
            }

            m_Isovists.RemoveAt(i);
            m_Isovists.RemoveAt(j);

            return true;
        }

        return false;
    }


    public void AddFoV()
    {
        // The game object that contains the field of view
        GameObject fovGameObject = new GameObject("FoV");

        // Assign it as a child to the guard
        var transform1 = transform;
        fovGameObject.transform.parent = transform1;
        fovGameObject.transform.position = transform1.position;

        m_FovGameObject = fovGameObject;
        m_Fov = fovGameObject.AddComponent<FieldOfView>();
        m_Fov.Initiate(361f, 100f, new Color32(255, 255, 255, 50));
    }


    private void OnDrawGizmos()
    {
        // if (m_Isovists != null)
        //     foreach (var isovist in m_Isovists)
        //     foreach (var p in isovist)
        //     {
        //         p.Draw("");
        //     }
    }
}