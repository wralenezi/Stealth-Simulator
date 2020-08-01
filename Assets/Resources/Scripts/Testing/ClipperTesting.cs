using System.Collections;
using System.Collections.Generic;
using ClipperLib;
using UnityEngine;

public class ClipperTesting : MonoBehaviour
{
    List<Polygon> m_polys;

    // Start is called before the first frame update
    void Start()
    {
        PolygonMergingTest();
    }

    void PolygonMergingTest()
    {
        m_polys = new List<Polygon>();
        
        Polygon poly1 = new Polygon();

        poly1.AddPoint(new Vector2(0f, 0.5f));
        poly1.AddPoint(new Vector2(0.5f, 1.5f));
        poly1.AddPoint(new Vector2(1f, 0.5f));
        poly1.AddPoint(new Vector2(1f, 2f));
        poly1.AddPoint(new Vector2(0f, 2f));


        List<Polygon> p1 = new List<Polygon>() {poly1};


        Polygon poly2 = new Polygon();

        poly2.AddPoint(new Vector2(0f, 0f));
        poly2.AddPoint(new Vector2(1f, 0f));
        poly2.AddPoint(new Vector2(1f, 1f));
        poly2.AddPoint(new Vector2(0f, 1f));


        List<Polygon> p2 = new List<Polygon>() {poly2};


        List<Polygon> intersection = PolygonHelper.MergePolygons(p1, p2, ClipType.ctIntersection);
        
        List<Polygon> diff = PolygonHelper.MergePolygons(intersection, p1, ClipType.ctDifference);

        Debug.Log(diff.Count);

        // m_polys.AddRange(p1);
        // m_polys.AddRange(p2);
        m_polys = diff;
    }

    private void OnDrawGizmos()
    {
        if (m_polys != null)
            foreach (Polygon p in m_polys)
                p.Draw("");
    }
}