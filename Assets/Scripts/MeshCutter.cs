using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCutter
{

    public TempMesh PositiveMesh { get; private set; }
    public TempMesh NegativeMesh { get; private set; }

    private List<Vector3> addedPairs;

    private readonly List<Vector3> originalVertices;
    private readonly List<int> originalTriangles;
    private readonly List<Vector3> originalNormals;
    private readonly List<Vector2> originalUvs;

    private readonly Vector3[] intersectPair;
    private readonly Vector3[] tempTriangle;

    private Intersections intersect;

    private readonly float threshold = 1e-6f;

    public MeshCutter(int initialArraySize)
    {
        PositiveMesh = new TempMesh(initialArraySize);
        NegativeMesh = new TempMesh(initialArraySize);

        addedPairs = new List<Vector3>(initialArraySize);
        originalVertices = new List<Vector3>(initialArraySize);
        originalNormals = new List<Vector3>(initialArraySize);
        originalUvs = new List<Vector2>(initialArraySize);
        originalTriangles = new List<int>(initialArraySize * 3);

        intersectPair = new Vector3[2];
        tempTriangle = new Vector3[3];

        intersect = new Intersections();
    }

 
    public bool SliceMesh(Mesh mesh, ref Plane slice)
    {

        mesh.GetVertices(originalVertices);

        if (!Intersections.BoundPlaneIntersect(mesh, ref slice))
            return false;

        mesh.GetTriangles(originalTriangles, 0);
        mesh.GetNormals(originalNormals);
        mesh.GetUVs(0, originalUvs);
        PositiveMesh.Clear();
        NegativeMesh.Clear();
        addedPairs.Clear();

        for (int i = 0; i < originalVertices.Count; ++i)
        {
            if (slice.GetDistanceToPoint(originalVertices[i]) >= 0)
                PositiveMesh.AddVertex(originalVertices, originalNormals, originalUvs, i);
            else
                NegativeMesh.AddVertex(originalVertices, originalNormals, originalUvs, i);
        }

        if (NegativeMesh.vertices.Count == 0 || PositiveMesh.vertices.Count == 0)
            return false;

        for (int i = 0; i < originalTriangles.Count; i += 3)
        {
            if (intersect.TrianglePlaneIntersect(originalVertices, originalUvs, originalTriangles, i, ref slice, PositiveMesh, NegativeMesh, intersectPair))
                addedPairs.AddRange(intersectPair);
        }

        if (addedPairs.Count > 0)
        {
            FillBoundaryFace(addedPairs);
            return true;
        }
        else
        {
            throw new UnityException("Error: if added pairs is empty, we should have returned false earlier");
        }
    }

    public Vector3 GetFirstVertex()
    {
        if (originalVertices.Count == 0)
            throw new UnityException("Error: Either the mesh has no vertices or GetFirstVertex was called before SliceMesh.");
        else
            return originalVertices[0];
    }

    public static Vector3 FindCenter(List<Vector3> pairs)
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        for (int i = 0; i < pairs.Count; i += 2)
        {
            center += pairs[i];
            count++;
        }

        return center / count;
    }

    #region Boundary fill method


    private void FillBoundaryGeneral(List<Vector3> added)
    {
        ReorderList(added);

        Vector3 center = FindCenter(added);

        tempTriangle[2] = center;

        for (int i = 0; i < added.Count; i += 2)
        {
            tempTriangle[0] = added[i];
            tempTriangle[1] = added[i + 1];

            PositiveMesh.AddTriangle(tempTriangle);

            tempTriangle[0] = added[i + 1];
            tempTriangle[1] = added[i];

            NegativeMesh.AddTriangle(tempTriangle);
        }
    }


    private void FillBoundaryFace(List<Vector3> added)
    {
        ReorderList(added);

        var face = FindRealPolygon(added);

        int t_fwd = 0,
            t_bwd = face.Count - 1,
            t_new = 1;
        bool incr_fwd = true;

        while (t_new != t_fwd && t_new != t_bwd)
        {
            AddTriangle(face, t_bwd, t_fwd, t_new);

            if (incr_fwd) t_fwd = t_new;
            else t_bwd = t_new;

            incr_fwd = !incr_fwd;
            t_new = incr_fwd ? t_fwd + 1 : t_bwd - 1;
        }
    }


    private List<Vector3> FindRealPolygon(List<Vector3> pairs)
    {
        List<Vector3> vertices = new List<Vector3>();
        Vector3 edge1, edge2;

        for (int i = 0; i < pairs.Count; i += 2)
        {
            edge1 = (pairs[i + 1] - pairs[i]);
            if (i == pairs.Count - 2)
                edge2 = pairs[1] - pairs[0];
            else
                edge2 = pairs[i + 3] - pairs[i + 2];

            edge1.Normalize();
            edge2.Normalize();

            if (Vector3.Angle(edge1, edge2) > threshold)
                vertices.Add(pairs[i + 1]);
        }

        return vertices;
    }

    public static void ReorderList(List<Vector3> pairs)
    {
        int nbFaces = 0;
        int faceStart = 0;
        int i = 0;

        while (i < pairs.Count)
        {
            for (int j = i + 2; j < pairs.Count; j += 2)
            {
                if (pairs[j] == pairs[i + 1])
                {
                    SwitchPairs(pairs, i + 2, j);
                    break;
                }
            }


            if (i + 3 >= pairs.Count)
            {
                break;
            }
            else if (pairs[i + 3] == pairs[faceStart])
            {
                nbFaces++;
                i += 4;
                faceStart = i;
            }
            else
            {
                i += 2;
            }
        }
    }
    private static void SwitchPairs(List<Vector3> pairs, int pos1, int pos2)
    {
        if (pos1 == pos2) return;

        Vector3 temp1 = pairs[pos1];
        Vector3 temp2 = pairs[pos1 + 1];
        pairs[pos1] = pairs[pos2];
        pairs[pos1 + 1] = pairs[pos2 + 1];
        pairs[pos2] = temp1;
        pairs[pos2 + 1] = temp2;
    }
    private void AddTriangle(List<Vector3> face, int t1, int t2, int t3)
    {
        tempTriangle[0] = face[t1];
        tempTriangle[1] = face[t2];
        tempTriangle[2] = face[t3];
        PositiveMesh.AddTriangle(tempTriangle);

        tempTriangle[1] = face[t3];
        tempTriangle[2] = face[t2];
        NegativeMesh.AddTriangle(tempTriangle);
    }
    #endregion

}

