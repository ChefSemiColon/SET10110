using System;
using System.Collections.Generic;
using UnityEngine;

public class Intersections
{
    


    private readonly Vector3[] v;
    private readonly Vector2[] u;
    private readonly int[] t;
    private readonly bool[] positive;

    private Ray edgeRay;

    public Intersections()
    {
        v = new Vector3[3];
        u = new Vector2[3];
        t = new int[3];
        positive = new bool[3];
    }

 
    public ValueTuple<Vector3, Vector2> Intersect(Plane plane, Vector3 first, Vector3 second, Vector2 uv1, Vector2 uv2)
    {
        edgeRay.origin = first;
        edgeRay.direction = (second - first).normalized;
        float dist;
        float maxDist = Vector3.Distance(first, second);

        if (!plane.Raycast(edgeRay, out dist))
            throw new UnityException("Line-Plane intersect in wrong direction");
        else if (dist > maxDist)
            throw new UnityException("Intersect outside of line");

        var returnVal = new ValueTuple<Vector3, Vector2>
        {
            Item1 = edgeRay.GetPoint(dist)
        };

        var relativeDist = dist / maxDist;
        returnVal.Item2.x = Mathf.Lerp(uv1.x, uv2.x, relativeDist);
        returnVal.Item2.y = Mathf.Lerp(uv1.y, uv2.y, relativeDist);
        return returnVal;
    }



    public bool TrianglePlaneIntersect(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, int startIdx, ref Plane plane, TempMesh posMesh, TempMesh negMesh, Vector3[] intersectVectors)
    {
        int i;

        for (i = 0; i < 3; ++i)
        {
            t[i] = triangles[startIdx + i];
            v[i] = vertices[t[i]];
            u[i] = uvs[t[i]];
        }

        posMesh.ContainsKeys(triangles, startIdx, positive);

        if (positive[0] == positive[1] && positive[1] == positive[2])
        {

            (positive[0] ? posMesh : negMesh).AddOgTriangle(t);
            return false;
        }

        int lonelyPoint = 0;
        if (positive[0] != positive[1])
            lonelyPoint = positive[0] != positive[2] ? 0 : 1;
        else
            lonelyPoint = 2;

        int prevPoint = lonelyPoint - 1;
        if (prevPoint == -1) prevPoint = 2;
        int nextPoint = lonelyPoint + 1;
        if (nextPoint == 3) nextPoint = 0;

        ValueTuple<Vector3, Vector2> newPointPrev = Intersect(plane, v[lonelyPoint], v[prevPoint], u[lonelyPoint], u[prevPoint]);
        ValueTuple<Vector3, Vector2> newPointNext = Intersect(plane, v[lonelyPoint], v[nextPoint], u[lonelyPoint], u[nextPoint]);

        (positive[lonelyPoint] ? posMesh : negMesh).AddSlicedTriangle(t[lonelyPoint], newPointNext.Item1, newPointPrev.Item1, newPointNext.Item2, newPointPrev.Item2);

        (positive[prevPoint] ? posMesh : negMesh).AddSlicedTriangle(t[prevPoint], newPointPrev.Item1, newPointPrev.Item2, t[nextPoint]);

        (positive[prevPoint] ? posMesh : negMesh).AddSlicedTriangle(t[nextPoint], newPointPrev.Item1, newPointNext.Item1, newPointPrev.Item2, newPointNext.Item2);

        if (positive[lonelyPoint])
        {
            intersectVectors[0] = newPointPrev.Item1;
            intersectVectors[1] = newPointNext.Item1;
        }
        else
        {
            intersectVectors[0] = newPointNext.Item1;
            intersectVectors[1] = newPointPrev.Item1;
        }
        return true;
    }


    public static bool BoundPlaneIntersect(Mesh mesh, ref Plane plane)
    {
        float r = mesh.bounds.extents.x * Mathf.Abs(plane.normal.x) +
            mesh.bounds.extents.y * Mathf.Abs(plane.normal.y) +
            mesh.bounds.extents.z * Mathf.Abs(plane.normal.z);

        float s = Vector3.Dot(plane.normal, mesh.bounds.center) - (-plane.distance);

        return Mathf.Abs(s) <= r;
    }

}
