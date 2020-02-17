using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField]
    Camera mainCam;
    [SerializeField]
    MeshFilter mF;
    [SerializeField]
    MeshCollider mC;
    public float forceMultiplier = 1f;

    public bool recalculateBounds = true;
    public bool recalculateNormals = true;
    public bool dynamicMeshCollision = true;

    public float tapRadius = 0.2f;

    public bool volumeRayCast;
    public bool bShowHitPoint;
    public bool bShowSamplePoint;

    private GameObject shownHitPointObj;
    private Vector3 prevPos;
    private bool prevTouch = false;
    private bool isPull = false;

    void Awake()
    {
        mF = gameObject.GetComponent<MeshFilter>();
        mF.sharedMesh = Instantiate(mF.sharedMesh) as Mesh;

        CheckSwapColliders();
    }
    
    private void Update()
    {
        if (!Input.GetMouseButton(0))
        {
            prevTouch = false;
            isPull = false;
            return;
        }
        
        var tapPoint = Input.mousePosition;
        if (!prevTouch)
        {
            prevPos = tapPoint;
            prevTouch = true;
        }
        var diff = prevPos.y - tapPoint.y;
        if (diff > 0.0f)
        {
            isPull = false;
        }
        else if (diff < 0.0f)
        {
            isPull = true;
        }
        else
        {
            //動かしてないときはキープ
        }
        prevPos = tapPoint;

        tapPoint.z = -mainCam.transform.position.z;
        var tapWorldPoint = mainCam.ScreenToWorldPoint(tapPoint);
        var rayStartPoint = new Vector3(tapWorldPoint.x, 5.0f, 0.0f);

        RaycastHit hit;
        if (volumeRayCast && !isPull)
        {
            if (!Physics.SphereCast(rayStartPoint, tapRadius, Vector3.down, out hit))
            {
                return;
            }

        }
        else
        {
            if (!Physics.Raycast(rayStartPoint, Vector3.down, out hit))
            {
                return;
            }
        }


        if (bShowHitPoint)
        {
            if (shownHitPointObj == null)
            {
                shownHitPointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shownHitPointObj.transform.localScale = Vector3.one * 0.05f;
                shownHitPointObj.GetComponent<Collider>().enabled = false;
            }
            shownHitPointObj.transform.position = hit.point;
        }
        else
        {
            if (shownHitPointObj != null)
            {
                GameObject.Destroy(shownHitPointObj);
                shownHitPointObj = null;
            }

        }
        DeformAtPoint(hit.point, isPull);
    }

    private GameObject[] sampleObjects;

    public void DeformAtPoint(in Vector3 inHitPoint, bool inIsPull, bool updateMesh = true)
    {
        float impactScale = 1f;
        float tFM = forceMultiplier;
        if (inIsPull)
        {
            tFM *= -1.0f;
        }

        float maxDist = tapRadius / transform.localScale.magnitude;
        maxDist *= maxDist;

        Vector3[] softVerts = mF.sharedMesh.vertices;
        Vector3 localVert;
        float nearDistance = float.MaxValue;

        var hitPoint = inHitPoint;

        Vector3 localPoint = hitPoint;

        hitPoint = transform.InverseTransformPoint(inHitPoint);

        var center = new Vector3(hitPoint.x, 0.0f, 0.0f);
        center = transform.InverseTransformPoint(center);

        Vector3 centerToHit = (hitPoint - center);

        Vector3 hitToCenter = (center - hitPoint);

        if (bShowSamplePoint)
        {
            if (sampleObjects == null)
            {
                sampleObjects = new GameObject[softVerts.Length];
            }
        }
        else
        {
            if (sampleObjects != null && sampleObjects.Length > 0)
            {
                for (int i = 0; i < sampleObjects.Length; ++i)
                {
                    if (sampleObjects[i] != null)
                    {
                        GameObject.Destroy(sampleObjects[i]);
                        sampleObjects[i] = null;
                    }
                }
            }
        }


        for (int i = 0; i < softVerts.Length; i++)
        {
            localVert = softVerts[i];
            var centerToLocal = (localVert - center);
            var angle = Vector3.SignedAngle(centerToHit, centerToLocal, Vector3.right);
            var quaternion = Quaternion.AngleAxis(angle, Vector3.right);
            localPoint = quaternion * hitPoint;

            if (bShowSamplePoint)
            {
                if (sampleObjects[i] == null)
                {
                    var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    obj.transform.localScale = Vector3.one * 0.05f;
                    obj.GetComponent<Collider>().enabled = false;
                    sampleObjects[i] = obj;
                }
                sampleObjects[i].transform.position = localPoint;
            }
            var normal = (quaternion * hitToCenter).normalized;
            float tmpDist = Vector3.SqrMagnitude(localPoint - localVert);
            if (tmpDist < maxDist)
            {
                var power = (maxDist - tmpDist) * tFM;
                var add = normal * power;
                softVerts[i] = new Vector3(localVert.x, localVert.y + add.y, localVert.z + add.z);
            }
        }

        if (updateMesh)
        {
            FinalizeNewMesh(softVerts);
        }
    }

    void FinalizeNewMesh(Vector3[] softVerts)
    {
        mF.sharedMesh.vertices = softVerts;
        if (recalculateBounds)
        {
            mF.sharedMesh.RecalculateBounds();
        }

        if (recalculateNormals)
        {
            mF.sharedMesh.RecalculateNormals();
        }

        CheckDynamic();
    }


    void CheckDynamic()
    {
        if (dynamicMeshCollision)
        {
            if (!mC)
            {
                CheckSwapColliders();
            }
            mC.sharedMesh = mF.sharedMesh;
        }
    }

    void CheckSwapColliders()
    {
        if (dynamicMeshCollision)
        {
            swapColliders();
        }
    }

    void swapColliders()
    {
        foreach (Collider cld in GetComponents<Collider>())
        {
            Destroy(cld);
        }
        mC = gameObject.AddComponent<MeshCollider>();
        mC.sharedMesh = mF.sharedMesh;
    }

}
