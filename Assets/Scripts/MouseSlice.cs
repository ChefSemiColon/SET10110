using System.Collections.Generic;
using UnityEngine;

public class MouseSlice : MonoBehaviour
{

    public GameObject plane;
    public Transform ObjectContainer;


    public float separation;

    private Plane slicePlane = new Plane();
    public bool drawPlane;

    public ScreenLineRenderer lineRenderer;

    private MeshCutter meshCutter;
    private TempMesh biggerMesh, smallerMesh;
    private GameObject loadedSpark;
    #region Utility Functions

    void DrawPlane(Vector3 start, Vector3 end, Vector3 normalVec)
    {
        Quaternion rotate = Quaternion.FromToRotation(Vector3.up, normalVec);

        plane.transform.localRotation = rotate;
        plane.transform.position = (end + start) / 2;
        plane.SetActive(true);
    }

    #endregion

    void Start()
    {
        meshCutter = new MeshCutter(238609);
        loadedSpark = Resources.Load<GameObject>("Sparks");
    }

    private void OnEnable()
    {
        lineRenderer.OnLineDrawn += OnLineDrawn;
    }

    private void OnDisable()
    {
        lineRenderer.OnLineDrawn -= OnLineDrawn;
    }

    private void OnLineDrawn(Vector3 start, Vector3 end, Vector3 depth)
    {
        var planeTangent = (end - start).normalized;

        if (planeTangent == Vector3.zero)
            planeTangent = Vector3.right;

        var normalVec = Vector3.Cross(depth, planeTangent);

        if (drawPlane) DrawPlane(start, end, normalVec);

        SliceObjects(start, normalVec);
    }


    void SliceObjects(Vector3 point, Vector3 normal)
    {
        var toSlice = GameObject.FindGameObjectsWithTag("Sliceable");

        List<Transform> positive = new List<Transform>(),
            negative = new List<Transform>();

        GameObject obj;
        bool slicedAny = false;
        for (int i = 0; i < toSlice.Length; ++i)
        {
            obj = toSlice[i];

            var transformedNormal = ((Vector3)(obj.transform.localToWorldMatrix.transpose * normal)).normalized;

            slicePlane.SetNormalAndPosition(
                transformedNormal,
                obj.transform.InverseTransformPoint(point));

            slicedAny = SliceObject(ref slicePlane, obj, positive, negative) || slicedAny;
        }

        if (slicedAny)
            SeparateMeshes(positive, negative, normal);
    }

    bool SliceObject(ref Plane slicePlane, GameObject obj, List<Transform> positiveObjects, List<Transform> negativeObjects)
    {
        var mesh = obj.GetComponent<MeshFilter>().mesh;
        if (!meshCutter.SliceMesh(mesh, ref slicePlane))
        {
            if (slicePlane.GetDistanceToPoint(meshCutter.GetFirstVertex()) >= 0)
                positiveObjects.Add(obj.transform);
            else
                negativeObjects.Add(obj.transform);

            return false;
        }


        bool posBigger = meshCutter.PositiveMesh.surfacearea > meshCutter.NegativeMesh.surfacearea;
        if (posBigger)
        {
            biggerMesh = meshCutter.PositiveMesh;
            smallerMesh = meshCutter.NegativeMesh;
        }
        else
        {
            biggerMesh = meshCutter.NegativeMesh;
            smallerMesh = meshCutter.PositiveMesh;
        }

        GameObject newObject = Instantiate(obj, ObjectContainer);
        newObject.transform.SetPositionAndRotation(obj.transform.position, obj.transform.rotation);
        var newObjMesh = newObject.GetComponent<MeshFilter>().mesh;


        ReplaceMesh(mesh, biggerMesh);
        ReplaceMesh(newObjMesh, smallerMesh);

        (posBigger ? positiveObjects : negativeObjects).Add(obj.transform);
        (posBigger ? negativeObjects : positiveObjects).Add(newObject.transform);
        GameObject newSparks = loadedSpark;
        newSparks = GameObject.Instantiate(newSparks);
        newSparks.transform.position = newObject.transform.position;
        newSparks.GetComponent<ParticleSystem>().Play();
        Object.Destroy(newSparks, 0.5f);

        if(newObject.TryGetComponent<MeshCollider>(out MeshCollider newCollider))
        {
            newCollider.sharedMesh = newObjMesh;
        }

        if (obj.TryGetComponent<MeshCollider>(out MeshCollider collider))
        {
            collider.sharedMesh = mesh;
        }

        return true;
    }



    void ReplaceMesh(Mesh mesh, TempMesh tempMesh, MeshCollider collider = null)
    {
        mesh.Clear();
        mesh.SetVertices(tempMesh.vertices);
        mesh.SetTriangles(tempMesh.triangles, 0);
        mesh.SetNormals(tempMesh.normals);
        mesh.SetUVs(0, tempMesh.uvs);

        mesh.RecalculateTangents();
        if (collider != null && collider.enabled)
        {
            collider.sharedMesh = mesh;
            collider.convex = true;
        }
    }

    void SeparateMeshes(Transform posTransform, Transform negTransform, Vector3 localPlaneNormal)
    {

        Vector3 worldNormal = ((Vector3)(posTransform.worldToLocalMatrix.transpose * localPlaneNormal)).normalized;

        Vector3 separationVec = worldNormal * separation;
        posTransform.position += separationVec;
        negTransform.position -= separationVec;
    }

    void SeparateMeshes(List<Transform> positives, List<Transform> negatives, Vector3 worldPlaneNormal)
    {
        int i;
        var separationVector = worldPlaneNormal * separation;

        for (i = 0; i < positives.Count; ++i)
            positives[i].transform.position += separationVector;

        for (i = 0; i < negatives.Count; ++i)
            negatives[i].transform.position -= separationVector;
    }
}
