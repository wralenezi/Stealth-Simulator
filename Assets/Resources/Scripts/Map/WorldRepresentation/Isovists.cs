using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using ClipperLib;
using UnityEngine;

public class Isovists : MonoBehaviour
{
    public bool ShowIsoPolygons;
    private List<Polygon> m_Isovists;

    private GameObject m_FovGameObject;
    private FieldOfView m_Fov;
    private Polygon _fovPolygon;

    public static Isovists Instance;

    public void Initiate(List<MeshPolygon> navMesh)
    {
        Instance = this;

        m_Isovists = new List<Polygon>();

        // Create the Isovists
        CreateIsovists(navMesh);
        _fovPolygon = new Polygon();
    }

    private void CreateIsovists(List<MeshPolygon> navMesh)
    {
        GameObject isovistGO = new GameObject("Isovists");
        isovistGO.transform.parent = transform;

        foreach (var navMeshPolygon in navMesh)
        {
            AddVisibilityPolygon(isovistGO.transform);

            m_FovGameObject.transform.position = navMeshPolygon.GetCentroidPosition();

            m_Fov.CastFieldOfView();

            List<Vector3> fov = m_Fov.GetFovVertices();

            Polygon p = new Polygon();

            foreach (var v in fov)
                p.AddPoint(v);

            m_Isovists.Add(p);
        }
    }

    /// <summary>
    /// Get a numerical value that represent how visible this point from the centers of all navMesh polygons. 0 visible to none, 1 is visible by all
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    // public float GetCoverRatio(Vector2 position)
    // {
    //     float visibilityRatio = 0f;
    //
    //     foreach (var v in m_Isovists)
    //         visibilityRatio += v.IsCircleInPolygon(position, 0.2f) ? 1f : 0f;
    //
    //     return 1f - visibilityRatio / m_Isovists.Count;
    // }
    public float GetCoverRatio(Vector2 position)
    {
        m_Fov.transform.position = position;
        m_Fov.CastFieldOfView();

        _fovPolygon.Clear();
        foreach (var vertex in m_Fov.GetFovVertices())
            _fovPolygon.AddPoint(vertex);

        float area = _fovPolygon.GetArea();
        float totalArea = MapManager.Instance.mapDecomposer.GetNavMeshArea();

        return 1f - area / totalArea;
    }

    private void AddVisibilityPolygon(Transform parent)
    {
        // The game object that contains the field of view
        GameObject fovGameObject = new GameObject("VisibilityPolygon");

        var transform1 = transform;
        fovGameObject.transform.parent = parent;
        fovGameObject.transform.position = transform1.position;

        m_FovGameObject = fovGameObject;
        m_Fov = fovGameObject.AddComponent<FieldOfView>();
        m_Fov.Initiate(361f, 100f, new Color32(255, 255, 255, 50));

        fovGameObject.SetActive(false);
    }


    private void OnDrawGizmos()
    {
        if (ShowIsoPolygons && !Equals(m_Isovists, null))
            foreach (var isovist in m_Isovists)
            {
                isovist.Draw("");
            }
    }
}