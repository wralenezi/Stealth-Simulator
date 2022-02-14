using System.Collections.Generic;
using ClipperLib;
using UnityEngine;

// Helper class for polygons operations
public static class PolygonHelper
{

    // Helper function
    public static T GetKeyByValue<T, W>(this Dictionary<T, W> dict, W val)
    {
        T key = default;
        foreach (KeyValuePair<T, W> pair in dict)
        {
            if (EqualityComparer<W>.Default.Equals(pair.Value, val))
            {
                key = pair.Key;
                break;
            }
        }

        return key;
    }
    
    // Cut holes in a polygon to prepare it for triangulations
    public static Polygon CutHoles(List<Polygon> polygons)
    {
        // Create the containers for the outer polygon and the hole polygons
        Polygon outerPoly = new Polygon(polygons[0]);
        var holePolygons = new List<Polygon>();

        foreach (var p in polygons)
            if (p.DetermineWindingOrder() == Properties.outerPolygonWinding)
                outerPoly = new Polygon(p);
            else
                holePolygons.Add(new Polygon(p));


        // Keep looping as long as there are hole polygons
        while (holePolygons.Count > 0)
        {
            var currentHole = holePolygons[0];
            var holePointIndex = 0;

            // Find the hole with the right most vertex
            foreach (var poly in holePolygons)
                // Check if there are holes with the largest x, and set them to be merged
                for (var i = 0; i < poly.GetVerticesCount(); i++)
                    if (poly.GetPoint(i).x >= currentHole.GetPoint(holePointIndex).x)
                    {
                        currentHole = poly;
                        holePointIndex = i;
                    }

            if (currentHole.GetVerticesCount() == 0)
                continue;

            // Cut the hole and merge it with the outer polygon
            outerPoly = CutHoleInShape(outerPoly, currentHole);

            // Remove the hole after it is merged
            holePolygons.Remove(currentHole);
        }

        
        return outerPoly;
    }
    
        // Cut hole in the outer polygon
        static Polygon CutHoleInShape(Polygon outerPoly, Polygon holePoly)
    {
        // Find the hole vertex with the largest X value
        Vertex rightMostHoleVertex = holePoly.GetVertex(0);
        foreach (Vertex v in holePoly.GetPoints())
            if (v.position.x > rightMostHoleVertex.position.x)
                rightMostHoleVertex = v;


        // Find the first outer vertex on the right of the right most vertex of the hole
        Vertex P = outerPoly.GetVertex(0);
        float shortestDistance = Mathf.Infinity;
        foreach (Vertex v in outerPoly.GetPoints())
            if (v.position.x > rightMostHoleVertex.position.x &&
                Vector2.Distance(rightMostHoleVertex.position, v.position) < shortestDistance)
            {
                P = v;
                shortestDistance = Vector2.Distance(rightMostHoleVertex.position, v.position);
            }

        // Make sure the line made with P vertex does not intersect and other outer polygon line
        int p = 0;
        int pIndex = P.index;
        while (p < outerPoly.GetVerticesCount())
        {
            bool isOuterIntersect = false;
            int CutPointCount = 0;
            for (int j = 0; j < outerPoly.GetVerticesCount(); j++)
            {
                if (isOuterIntersect)
                    break;

                isOuterIntersect = GeometryHelper.DoLinesIntersect(rightMostHoleVertex.position, P.position,
                    outerPoly.GetPoint(j), outerPoly.GetPoint(j + 1), false);

                CutPointCount += outerPoly.GetPoint(j).Equals(P.position) ? 1 : 0;
            }


            bool isHoleIntersect = false;
            for (int j = 0; j < holePoly.GetVerticesCount(); j++)
            {
                if (isHoleIntersect)
                    break;

                isHoleIntersect = GeometryHelper.DoLinesIntersect(rightMostHoleVertex.position, P.position,
                    holePoly.GetPoint(j), holePoly.GetPoint(j + 1), false);
            }

            if (isHoleIntersect || isOuterIntersect || CutPointCount > 1)
            {
                P = outerPoly.GetVertex(++pIndex);
                p++;
            }
            else
            {
                break;
            }
        }


        // Now we just form our output array by injecting the hole vertices into place
        // we know we have to inject the hole into the main array after point P going from
        // rightMostHoleVertex around and then back to P.

        int mIndex = rightMostHoleVertex.index;
        int injectPoint = P.index;

        Polygon newPoly = new Polygon();

        for (int i = 0; i <= injectPoint; i++)
            newPoly.AddPoint(outerPoly.GetPoint(i));


        for (int i = 0; i <= holePoly.GetVerticesCount(); i++)
            newPoly.AddPoint(holePoly.GetPoint(i + mIndex));


        for (int i = injectPoint; i < outerPoly.GetVerticesCount(); i++)
            newPoly.AddPoint(outerPoly.GetPoint(i));

        return newPoly;
    }


        public static List<Polygon> MergePolygons(List<Polygon> firstPolygon, List<Polygon> secondPolygon,
            ClipType clipType)
        {
            // Result merged polygon
            List<Polygon> mergedPolygon = new List<Polygon>();


            // Prepare the second (main) polygon to be merged
            List<CyclicalList<IntPoint>> subj = new List<CyclicalList<IntPoint>>();

            foreach (Polygon p in secondPolygon)
            {
                if (p.GetVerticesCount() > 0)
                {
                    subj.Add(new CyclicalList<IntPoint>());
                    for (int i = 0; i < p.GetVerticesCount(); i++)
                    {
                        subj[subj.Count - 1].Add(new IntPoint(p.GetPoint(i).x, p.GetPoint(i).y));
                    }
                }
            }


            // The current seen area so it will be merged with the previous area
            List<CyclicalList<IntPoint>> clip = new List<CyclicalList<IntPoint>>();

            foreach (Polygon p in firstPolygon)
            {
                if (p.GetVerticesCount() > 0)
                {
                    clip.Add(new CyclicalList<IntPoint>());
                    for (int i = 0; i < p.GetVerticesCount(); i++)
                    {
                        clip[clip.Count - 1].Add(new IntPoint(p.GetPoint(i).x, p.GetPoint(i).y));
                    }
                }
            }


            // Merge the two polygons
            List<CyclicalList<IntPoint>> solution = new List<CyclicalList<IntPoint>>();

            Clipper c = new Clipper();
            c.AddPaths(clip, PolyType.ptClip, true);
            c.AddPaths(subj, PolyType.ptSubject, true);
            c.Execute(clipType, solution, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
            
            
            // Fill the result 
            int polygonIndex = 0;
            if (solution.Count > 0)
            {
                foreach (CyclicalList<IntPoint> polygon in solution)
                {
                    // if the merge is between seen area then the first polygon is seen area and the rest are seen area holes
                    mergedPolygon.Add(new Polygon());

                    for (int i = 0; i < polygon.Count; i++)
                    {
                        mergedPolygon[mergedPolygon.Count - 1].AddPoint(new Vector2(polygon[i].X, polygon[i].Y));
                    }

                    // Smooth the polygon
                    mergedPolygon[mergedPolygon.Count - 1].SmoothPolygon(0.2f);
                    
                    polygonIndex++;
                }
            }

            return mergedPolygon;
        }

        // Smooth a list of polygons
        public static void SmoothPolygons(List<Polygon> overallPolygon,  float minDistance)
        {
            foreach (Polygon p in overallPolygon)
                p.SmoothPolygon(minDistance);
        }
        
        // Check if a node is in outer polygon and outside inner polygon (obstacle) 
        public static bool IsPointInPolygons(List<Polygon> polygons, Vector2 point)
        {
            bool isIn = false;


            if (polygons.Count > 0)
                for (int i = 0; i < polygons.Count; i++)
                    if (polygons[i].IsPointInPolygon(point,true))
                    {
                        if (polygons[i].DetermineWindingOrder() == Properties.outerPolygonWinding)
                            isIn = true;
                        else
                        {
                            // If the point is inside the obstacle then its out
                            return false;
                        }
                    }
            
            return isIn;
        }
        
        public static VisibilityPolygon GetIntersectionArea(Polygon firstPolygon, Polygon secondPolygon)
        {
            // Prepare the second (main) polygon to be merged
            List<CyclicalList<IntPoint>> subj = new List<CyclicalList<IntPoint>>();

            if (secondPolygon.GetVerticesCount() > 0)
            {
                subj.Add(new CyclicalList<IntPoint>());
                for (int i = 0; i < secondPolygon.GetVerticesCount(); i++)
                    subj[subj.Count - 1].Add(new IntPoint(secondPolygon.GetPoint(i).x, secondPolygon.GetPoint(i).y));
            }


            // The current seen area so it will be merged with the previous area
            List<CyclicalList<IntPoint>> clip = new List<CyclicalList<IntPoint>>();

            if (firstPolygon.GetVerticesCount() > 0)
            {
                clip.Add(new CyclicalList<IntPoint>());
                for (int i = 0; i < firstPolygon.GetVerticesCount(); i++)
                    clip[clip.Count - 1].Add(new IntPoint(firstPolygon.GetPoint(i).x, firstPolygon.GetPoint(i).y));
            }


            // Merge the two polygons
            List<CyclicalList<IntPoint>> solution = new List<CyclicalList<IntPoint>>();

            Clipper c = new Clipper();
            c.AddPaths(clip, PolyType.ptClip, true);
            c.AddPaths(subj, PolyType.ptSubject, true);
            c.Execute(ClipType.ctIntersection, solution, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);


            // Result merged polygon
            VisibilityPolygon mergedPolygon = new VisibilityPolygon();

            // Fill the result 
            if (solution.Count > 0)
                foreach (CyclicalList<IntPoint> polygon in solution)
                {
                    for (int i = 0; i < polygon.Count; i++)
                        mergedPolygon.AddPoint(new Vector2(polygon[i].X, polygon[i].Y));
                }

            return mergedPolygon;
        }

        

        // Get the area of a polygon with holes
        public static float GetPolygonArea(List<Polygon> polygons)
        {
            float area = 0f;
            
            foreach (var polygon in polygons)
                if (polygon.DetermineWindingOrder() == Properties.outerPolygonWinding)
                    area += polygon.GetArea();
                else
                    area -= polygon.GetArea();


            return area;
        }
        
        
        
        
        
        
}