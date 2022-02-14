using System.Collections.Generic;


public static class EarClipDecomp
{
    // Return a list of triangles with the vertices
    public static List<MeshPolygon> Triangulate(Polygon polygon)
    {
        List<MeshPolygon> triangles = new List<MeshPolygon>();
        MeshPolygon triangle;

        int earIndex = 0;
        
        polygon.ReverseWindingOrder();

        // No ears
        if (polygon.GetVerticesCount() < 3) return triangles;

        // Set the values for the vertices
        for (int i = 0; i < polygon.GetVerticesCount(); i++)
        {
            UpdateVertex(i, polygon);
        }

        // Start clipping the ears
        while (polygon.GetVerticesCount() > 3)
        {
            var earFound = false;

            // find the most extruded ear
            for (int j = 0; j < polygon.GetVerticesCount(); j++)
            {
                if (!polygon.GetVertex(j).isEar) continue;

                if (!earFound)
                {
                    earFound = true;
                    earIndex = j;
                }
                else
                {
                    float vAngle = GeometryHelper.GetAngle(polygon.GetPoint(j - 1), polygon.GetPoint(j),
                        polygon.GetPoint(j + 1));
                    float earAngle = GeometryHelper.GetAngle(polygon.GetPoint(earIndex - 1), polygon.GetPoint(earIndex),
                        polygon.GetPoint(earIndex + 1));

                    if (vAngle > earAngle)
                    {
                        earIndex = j;
                    }
                }
            }

            if (!earFound)
            {
                return triangles;
            }

            triangle = new MeshPolygon();
            triangle.AddPoint(polygon.GetPoint(earIndex - 1));
            triangle.AddPoint(polygon.GetPoint(earIndex));
            triangle.AddPoint(polygon.GetPoint(earIndex + 1));
            triangles.Add(triangle);

            // Clip the ear
            polygon.RemovePoint(earIndex);

            // Update the adjacent vertices
            UpdateVertex(earIndex - 1, polygon);
            UpdateVertex(earIndex, polygon);

        }

        // Add the last ear
        if (polygon.GetVerticesCount() == 3)
        {
            triangle = new MeshPolygon();
            triangle.AddPoint(polygon.GetPoint(0));
            triangle.AddPoint(polygon.GetPoint(1));
            triangle.AddPoint(polygon.GetPoint(2));
            triangles.Add(triangle);
            return triangles;
        }


        return triangles;
    }


    
    // Return a list of triangles with the vertices
    public static List<int> TriangulateIndex(Polygon polygon)
    {
        List<int> triangles = new List<int>();

        int earIndex = 0;
        
        polygon.ReverseWindingOrder();

        // No ears
        if (polygon.GetVerticesCount() < 3) return triangles;

        // Set the values for the vertices
        for (int i = 0; i < polygon.GetVerticesCount(); i++)
        {
            UpdateVertex(i, polygon);
        }

        // Start clipping the ears
        while (polygon.GetVerticesCount() > 3)
        {
            var earFound = false;

            // find the most extruded ear
            for (int j = 0; j < polygon.GetVerticesCount(); j++)
            {
                if (!polygon.GetVertex(j).isEar) continue;

                if (!earFound)
                {
                    earFound = true;
                    earIndex = j;
                }
                else
                {
                    float vAngle = GeometryHelper.GetAngle(polygon.GetPoint(j - 1), polygon.GetPoint(j),
                        polygon.GetPoint(j + 1));
                    float earAngle = GeometryHelper.GetAngle(polygon.GetPoint(earIndex - 1), polygon.GetPoint(earIndex),
                        polygon.GetPoint(earIndex + 1));

                    if (vAngle > earAngle)
                    {
                        earIndex = j;
                    }
                }
            }

            if (!earFound)
            {
                return triangles;
            }

            triangles.Add(polygon.GetVertex(earIndex - 1).index);
            triangles.Add(polygon.GetVertex(earIndex).index);
            triangles.Add(polygon.GetVertex(earIndex + 1).index);

            // Clip the ear
            polygon.RemovePoint(earIndex);

            // Update the adjacent vertices
            UpdateVertex(earIndex - 1, polygon);
            UpdateVertex(earIndex, polygon);

        }

        // Add the last ear
        if (polygon.GetVerticesCount() == 3)
        {
            triangles.Add(polygon.GetVertex(0).index);
            triangles.Add(polygon.GetVertex(1).index);
            triangles.Add(polygon.GetVertex(2).index);
            return triangles;
        }


        return triangles;
    }
    
    
    // Update the vertex if it is an ear or not 
    public static void UpdateVertex(int vIndex, Polygon polygon)
    {
        Vertex v1 = polygon.GetVertex(vIndex - 1);
        Vertex v2 = polygon.GetVertex(vIndex);
        Vertex v3 = polygon.GetVertex(vIndex + 1);

        if (!GeometryHelper.IsReflex(v1.position, v2.position, v3.position)) // Check if the vertex is convex
        {
            v2.isEar = true;

            for (int i = 0; i < polygon.GetVerticesCount(); i++)
            {
                // Make sure the point is not a part of the ear
                if (polygon.GetPoint(i) == polygon.GetPoint(vIndex) ||
                    polygon.GetPoint(i) == polygon.GetPoint(vIndex + 1) ||
                    polygon.GetPoint(i) == polygon.GetPoint(vIndex - 1))
                    continue;

                // Check if the vertex is inside the ear, if so it won't be an ear
                if (GeometryHelper.PointInTriangle(v1.position, v2.position, v3.position, polygon.GetPoint(i)))
                {
                    v2.isEar = false;
                    break;
                }
            }
        }
        else
        {
            v2.isEar = false;
        }
    }
    
    
    // Get an index of 
    
}