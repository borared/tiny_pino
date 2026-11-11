using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Procedural 3D Rope Mesh generator & physics simulator based on OptimizedRopesAndCables (Ali10555).
/// Generates a real 3D cylindrical rope mesh with UV coordinates along a smooth curve connecting StartPoint and EndPoint.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class RopeMoment : MonoBehaviour
{
    public event Action OnPointsChanged;

    [Header("Rope Anchors")]
    [Tooltip("Top anchor (e.g. the black sphere). The rope starts here.")]
    public Transform startPoint;

    [Tooltip("Bottom end of the rope. Can be another object, handle, or a child Transform.")]
    public Transform endPoint;

    [Tooltip("Optional middle object hanging on the rope (e.g. necklace, bead).")]
    public Transform midPoint;

    [Header("Rope Shape & Detail")]
    [Tooltip("Length of the rope. If longer than distance between points, it sags naturally.")]
    public float ropeLength = 6f;

    [Range(0.01f, 1f)]
    [Tooltip("Thickness/radius of the 3D rope cylinder.")]
    public float ropeRadius = 0.05f;

    [Range(4, 60)]
    [Tooltip("Number of longitudinal divisions along the rope curve.")]
    public int lengthSegments = 20;

    [Range(3, 24)]
    [Tooltip("Radial resolution around the rope cylinder (8-12 is ideal).")]
    public int radialSegments = 8;

    [Tooltip("Texture tiling per meter of rope length.")]
    public float tilingPerMeter = 2.0f;

    [Header("Physics & Motion")]
    [Tooltip("Stiffness of the rope spring reaction.")]
    public float stiffness = 250f;

    [Tooltip("Damping to settle oscillation.")]
    public float damping = 12f;

    [Range(1f, 15f)]
    [Tooltip("Weight curve factor for Rational Bezier.")]
    public float midPointWeight = 1f;

    [Range(0.2f, 0.8f)]
    public float midPointPosition = 0.5f;

    [Header("Wind & Sway Effect")]
    [Tooltip("Enable dynamic breeze/wind sway like in RopeWindEffect.")]
    public bool enableWindEffect = true;
    [Range(0f, 150f)]
    public float windForce = 15f;
    [Tooltip("Degrees direction of the wind (0 is forward, 90 is right).")]
    [Range(-360f, 360f)]
    public float windDirectionDegrees = 45f;

    [Header("Material")]
    public Material ropeMaterial;

    // Internal simulation
    private Vector3 currentValue;
    private Vector3 currentVelocity;
    private Vector3 targetValue;
    private float windSeed;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh ropeMesh;

    // Mesh vertex buffers
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();
    private readonly List<Vector2> uvs = new List<Vector2>();

    private Vector3 prevStartPos;
    private Vector3 prevEndPos;

    public bool IsEndpointsValid => startPoint != null && endPoint != null;

    void Awake()
    {
        InitializeComponents();
    }

    void OnEnable()
    {
        InitializeComponents();
        RecalculateRope();
    }

    void Start()
    {
        InitializeComponents();
        if (IsEndpointsValid)
        {
            currentValue = CalculateMidPoint();
            targetValue = currentValue;
            currentVelocity = Vector3.zero;
        }
        GenerateRopeMesh();
    }

    void InitializeComponents()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (ropeMaterial != null && meshRenderer != null)
        {
            meshRenderer.sharedMaterial = ropeMaterial;
        }

        if (ropeMesh == null)
        {
            ropeMesh = new Mesh { name = "DynamicRopeMesh" };
            meshFilter.sharedMesh = ropeMesh;
        }
    }

    void Update()
    {
        if (!IsEndpointsValid)
            return;

        Vector3 currentMid = CalculateMidPoint();
        targetValue = currentMid;

        if (midPoint != null)
        {
            midPoint.position = GetRationalBezierPoint(startPoint.position, currentValue, endPoint.position, midPointPosition, 1f, midPointWeight, 1f);
        }

        // In editor mode without play, update mesh if anchors moved
        if (!Application.isPlaying)
        {
            if (startPoint.position != prevStartPos || endPoint.position != prevEndPos)
            {
                currentValue = targetValue;
                GenerateRopeMesh();
                prevStartPos = startPoint.position;
                prevEndPos = endPoint.position;
            }
        }
        else
        {
            GenerateRopeMesh();
        }
    }

    void FixedUpdate()
    {
        if (!IsEndpointsValid || !Application.isPlaying)
            return;

        SimulatePhysics();
    }

    void SimulatePhysics()
    {
        float dt = Time.fixedDeltaTime;
        float dampingFactor = Mathf.Max(0f, 1f - damping * dt);
        Vector3 acceleration = (targetValue - currentValue) * stiffness * dt;
        currentVelocity = currentVelocity * dampingFactor + acceleration;
        currentValue += currentVelocity * dt;

        if (Vector3.Distance(currentValue, targetValue) < 0.005f && currentVelocity.magnitude < 0.005f)
        {
            currentValue = targetValue;
            currentVelocity = Vector3.zero;
        }
    }

    public void AddGrabBend(Vector3 handleVelocityChange)
    {
        currentVelocity -= handleVelocityChange * 2f;
    }

    public Vector3 GetPointAt(float t)
    {
        if (!IsEndpointsValid)
            return transform.position;

        Vector3 mid = Application.isPlaying ? currentValue : CalculateMidPoint();
        return GetRationalBezierPoint(startPoint.position, mid, endPoint.position, t, 1f, midPointWeight, 1f);
    }

    private Vector3 CalculateMidPoint()
    {
        Vector3 p0 = startPoint.position;
        Vector3 p1 = endPoint.position;
        Vector3 mid = Vector3.Lerp(p0, p1, midPointPosition);

        float distance = Vector3.Distance(p0, p1);
        float sagFactor = Mathf.Max(0f, ropeLength - distance);

        float k = Mathf.Lerp(0.493f, 0.323f, Mathf.InverseLerp(1f, 15f, midPointWeight));
        float w = 1f + k * Mathf.Log(Mathf.Max(1f, midPointWeight));
        float yFactor = sagFactor / w;

        mid.y -= yFactor;
        return mid;
    }

    private Vector3 GetRationalBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t, float w0, float w1, float w2)
    {
        Vector3 wp0 = w0 * p0;
        Vector3 wp1 = w1 * p1;
        Vector3 wp2 = w2 * p2;

        float denominator = w0 * Mathf.Pow(1 - t, 2) + 2 * w1 * (1 - t) * t + w2 * Mathf.Pow(t, 2);
        if (Mathf.Abs(denominator) < 0.0001f)
            return p0;

        return (wp0 * Mathf.Pow(1 - t, 2) + wp1 * 2 * (1 - t) * t + wp2 * Mathf.Pow(t, 2)) / denominator;
    }

    /// <summary>
    /// Generates true 3D procedural cylindrical mesh with proper normals and UV coordinates.
    /// </summary>
    public void GenerateRopeMesh()
    {
        if (!IsEndpointsValid || meshFilter == null)
            return;

        if (ropeMesh == null)
        {
            ropeMesh = new Mesh { name = "DynamicRopeMesh" };
            meshFilter.sharedMesh = ropeMesh;
        }

        Vector3[] curvePoints = new Vector3[lengthSegments + 1];
        for (int i = 0; i <= lengthSegments; i++)
        {
            curvePoints[i] = GetPointAt((float)i / lengthSegments);
        }

        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        float accumulatedLength = 0f;

        for (int i = 0; i <= lengthSegments; i++)
        {
            Vector3 forward;
            if (i < lengthSegments)
                forward = curvePoints[i + 1] - curvePoints[i];
            else
                forward = curvePoints[i] - curvePoints[i - 1];

            if (forward.sqrMagnitude < 0.00001f)
                forward = Vector3.down;

            Quaternion rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = j * Mathf.PI * 2f / radialSegments;
                Vector3 ringOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ropeRadius;
                Vector3 worldVertex = curvePoints[i] + rotation * ringOffset;

                vertices.Add(transform.InverseTransformPoint(worldVertex));

                float u = (float)j / radialSegments;
                float v = accumulatedLength * tilingPerMeter;
                uvs.Add(new Vector2(u, v));
            }

            if (i < lengthSegments)
            {
                accumulatedLength += Vector3.Distance(curvePoints[i], curvePoints[i + 1]);
            }
        }

        // Triangles along segments
        int ringStride = radialSegments + 1;
        for (int i = 0; i < lengthSegments; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int current = i * ringStride + j;
                int next = current + 1;
                int rowNext = current + ringStride;
                int rowNextNext = rowNext + 1;

                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(rowNext);

                triangles.Add(next);
                triangles.Add(rowNextNext);
                triangles.Add(rowNext);
            }
        }

        // Top Cap
        int startCapIndex = vertices.Count;
        vertices.Add(transform.InverseTransformPoint(curvePoints[0]));
        uvs.Add(new Vector2(0.5f, 0f));
        Quaternion startRot = Quaternion.LookRotation((curvePoints[1] - curvePoints[0]).normalized);
        for (int j = 0; j <= radialSegments; j++)
        {
            float angle = j * Mathf.PI * 2f / radialSegments;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ropeRadius;
            vertices.Add(transform.InverseTransformPoint(curvePoints[0] + startRot * offset));
            uvs.Add(new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, (Mathf.Sin(angle) + 1f) * 0.5f));

            if (j < radialSegments)
            {
                triangles.Add(startCapIndex);
                triangles.Add(startCapIndex + j + 2);
                triangles.Add(startCapIndex + j + 1);
            }
        }

        // Bottom Cap
        int endCapIndex = vertices.Count;
        vertices.Add(transform.InverseTransformPoint(curvePoints[lengthSegments]));
        uvs.Add(new Vector2(0.5f, accumulatedLength * tilingPerMeter));
        Quaternion endRot = Quaternion.LookRotation((curvePoints[lengthSegments] - curvePoints[lengthSegments - 1]).normalized);
        for (int j = 0; j <= radialSegments; j++)
        {
            float angle = j * Mathf.PI * 2f / radialSegments;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ropeRadius;
            vertices.Add(transform.InverseTransformPoint(curvePoints[lengthSegments] + endRot * offset));
            uvs.Add(new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, (Mathf.Sin(angle) + 1f) * 0.5f));

            if (j < radialSegments)
            {
                triangles.Add(endCapIndex);
                triangles.Add(endCapIndex + j + 1);
                triangles.Add(endCapIndex + j + 2);
            }
        }

        ropeMesh.Clear();
        ropeMesh.SetVertices(vertices);
        ropeMesh.SetTriangles(triangles, 0);
        ropeMesh.SetUVs(0, uvs);
        ropeMesh.RecalculateNormals();
        ropeMesh.RecalculateBounds();
    }

    public void RecalculateRope()
    {
        if (IsEndpointsValid)
        {
            currentValue = CalculateMidPoint();
            targetValue = currentValue;
            currentVelocity = Vector3.zero;
            GenerateRopeMesh();
            OnPointsChanged?.Invoke();
        }
    }

    void OnValidate()
    {
        InitializeComponents();
        if (ropeMaterial != null && meshRenderer != null)
        {
            meshRenderer.sharedMaterial = ropeMaterial;
        }
        GenerateRopeMesh();
    }
}


