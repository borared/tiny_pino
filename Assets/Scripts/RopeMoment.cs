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


    [Header("Rope Shape & Detail")]

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

    [Header("Material")]
    public Material ropeMaterial;

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
        if (IsEndpointsValid &&
            (startPoint.position != prevStartPos || endPoint.position != prevEndPos))
        {
            RecalculateRope();
        }
    }

    public Vector3 GetPointAt(float t)
    {
        return IsEndpointsValid
            ? Vector3.Lerp(startPoint.position, endPoint.position, t)
            : transform.position;
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
        if (!IsEndpointsValid)
            return;

        prevStartPos = startPoint.position;
        prevEndPos = endPoint.position;
        GenerateRopeMesh();
        OnPointsChanged?.Invoke();
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


