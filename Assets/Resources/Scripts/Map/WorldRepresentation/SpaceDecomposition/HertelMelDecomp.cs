using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class HertelMelDecomp
{
    // Hertel-Melhorn decomposition
    public static List<MeshPolygon> ConvexPartition(Polygon polygon)
    {
        int i11;

        // Check if the poly is already convex
        bool isConvex = true;

        if (Equals(polygon, null)) return null;

        for (i11 = 0; i11 < polygon.GetVerticesCount(); i11++)
        {
            // The previous vertex
            var i12 = i11 - 1;

            // The next vertex index
            var i13 = i11 + 1;

            // Check if the angle is reflex
            if (GeometryHelper.IsReflex(polygon.GetPoint(i12), polygon.GetPoint(i11), polygon.GetPoint(i13)))
            {
                isConvex = false;
                break;
            }
        }

        // if there are no reflex nodes then the polygon is already convex
        if (isConvex)
        {
            List<MeshPolygon> outputPolygons = new List<MeshPolygon>();
            MeshPolygon meshPolygon = polygon as MeshPolygon;

            if (Equals(meshPolygon, null)) return null;
            
            outputPolygons.Add(meshPolygon);
            
            return outputPolygons;
        }

        // Triangulate the polygon
        var triangles = EarClipDecomp.Triangulate(polygon);

        if (triangles == null) return null;


        return RemoveNonEssentialDiagonals(triangles);
    }
    
    // Remove the nonessential diagonals
        public static List<MeshPolygon> RemoveNonEssentialDiagonals(List<MeshPolygon> triangles)
    {
        MeshPolygon poly2 = null;
        int i21 = 0, i22 = 0;


        // Loop through the triangles
        int currentPoly = 0;
        while (currentPoly < triangles.Count)
        {
            var poly1 = triangles[currentPoly];

            int i11;
            for (i11 = 0; i11 < poly1.GetVerticesCount(); i11++)
            {
                // Get the first line in the polygon
                var d1 = poly1.GetPoint(i11);
                var i12 = i11 + 1;
                var d2 = poly1.GetPoint(i12);

                // Find another polygon who shares this line
                var isDiagonal = false;
                for (int j1 = currentPoly; j1 < triangles.Count; j1++)
                {
                    poly2 = triangles[j1];

                    if (poly1 == poly2) continue;

                    // Find a common line with the next polygon to determine if it is a diagonal 
                    for (i21 = 0; i21 < poly2.GetVerticesCount(); i21++)
                    {
                        if ((d2.x != poly2.GetPoint(i21).x) || (d2.y != poly2.GetPoint(i21).y)) continue;

                        i22 = i21 + 1;

                        if ((d1.x != poly2.GetPoint(i22).x) || (d1.y != poly2.GetPoint(i22).y)) continue;

                        // The line is found in an adjacent polygon
                        isDiagonal = true;
                        break;
                    }

                    if (isDiagonal) break;
                }


                // If no diagonal has been found between the current polygon and all the others then move to the next
                if (!isDiagonal)
                    continue;


                // First point of the diagonal
                var p2 = poly1.GetPoint(i11);

                // Get the previous vertex
                var i13 = i11 - 1;
                var p1 = poly1.GetPoint(i13);

                // Get the next vertex on the other polygon
                var i23 = i22 + 1;
                var p3 = poly2.GetPoint(i23);

                // If the formed angle is reflex then the diagonal is essential
                if (GeometryHelper.IsReflex(p1, p2, p3)) continue;

                // Get vertex on the other end of the diagonal
                p2 = poly1.GetPoint(i12);

                // Get the next vertex on the first polygon 
                i13 = i12 + 1;
                p3 = poly1.GetPoint(i13);

                // Get the previous vertex on the other polygon
                i23 = i21 - 1;
                p1 = poly2.GetPoint(i23);

                // If the formed angle is reflex then the diagonal is essential
                if (GeometryHelper.IsReflex(p1, p2, p3)) continue;


                var newPoly = new MeshPolygon();

                int j;
                for (j = i12; j != i11; j = (j + 1) % (poly1.GetVerticesCount()))
                    newPoly.AddPoint(poly1.GetPoint(j));


                for (j = i22; j != i21; j = (j + 1) % (poly2.GetVerticesCount()))
                    newPoly.AddPoint(poly2.GetPoint(j));

                triangles.Remove(poly2);
                poly1 = newPoly;
                triangles[currentPoly] = poly1;

                i11 = -1;
            }

            currentPoly++;
        }
        

        return triangles;
    }

        
        // Build NavMesh connections
        public static void BuildNavMesh(List<MeshPolygon> navMeshPolygons)
        {
            MeshPolygon poly2 = null;
            int i21 = 0;

            // Loop through the polygons
            int currentPoly = 0;
            while (currentPoly < navMeshPolygons.Count)
            {
                var poly1 = navMeshPolygons[currentPoly];

                int i11;
                for (i11 = 0; i11 < poly1.GetVerticesCount(); i11++)
                {
                    // Get the first line in the polygon
                    var d1 = poly1.GetPoint(i11);
                    var i12 = i11 + 1;
                    var d2 = poly1.GetPoint(i12);

                    // Find another polygon who shares this line
                    var isDiagonal = false;
                    for (int j1 = currentPoly; j1 < navMeshPolygons.Count; j1++)
                    {
                        poly2 = navMeshPolygons[j1];

                        if (poly1 == poly2) continue;

                        // Find a common line with the next polygon to determine if it is a diagonal 
                        for (i21 = 0; i21 < poly2.GetVerticesCount(); i21++)
                        {
                            if ((d2.x != poly2.GetPoint(i21).x) || (d2.y != poly2.GetPoint(i21).y)) continue;

                            var i22 = i21 + 1;

                            if ((d1.x != poly2.GetPoint(i22).x) || (d1.y != poly2.GetPoint(i22).y)) continue;

                            // The line is found in an adjacent polygon
                            isDiagonal = true;
                            break;
                        }

                        if (isDiagonal) break;
                    }

                    // If no diagonal has been found between the current polygon and all the others then move to the next
                    if (!isDiagonal)
                        continue;

                    poly1.AddNeighborPolygon(poly2, i11);
                    poly2.AddNeighborPolygon(poly1, i21);
                }

                currentPoly++;
            }
        }
}