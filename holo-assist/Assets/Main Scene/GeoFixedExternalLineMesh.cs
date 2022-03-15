using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GeodesyHelper;

/*
    Behind the scenes, each geo-fixed augmentation is backed by
    a `GeoFixedExternalLineMesh`.
*/

#if RENDER_GEOFIXED_WITH_CPU
using static ProjectionHelper;
using static DoubleNumerics;
#endif

public class GeoFixedVertex
{
    public ECEFPoint Position;
    public Color Color;
}

[RequireComponent(typeof(MeshFilter))]
public class GeoFixedExternalLineMesh : MonoBehaviour
{
    private List<GeoFixedVertex> _vertices;
    private List<int> _indices;
    private Mesh _rawMesh;

    public bool InterpolateOnCommit = true;
    public float InterpolatedSegmentMaxLengthMeters = 30;

#if RENDER_GEOFIXED_WITH_CPU
    private List<GeoFixedVertex> _interpolatedVertices = new List<GeoFixedVertex>();
#endif

    public void InitFields()
    {
        // This method is required because Unity does not
        // want you to touch the constructor of something that
        // inherits from MonoBehaviour *and* it doesn't
        // call neither Awake() nor Start() on an inactive
        // GameObject (the default status for a GameObject with an
        // ExternalLineMesh component)
        _vertices = new List<GeoFixedVertex>();
        _indices = new List<int>();
        _rawMesh = new Mesh();
        _rawMesh.name = gameObject.name + " MESH";

        GetComponent<MeshFilter>().mesh = _rawMesh;
    }

    public void SetIndices(List<int> newIndices, int startIndex)
    {
        Debug.Assert(newIndices.Count <= _indices.Count - startIndex);
        for (int i = startIndex; i < newIndices.Count; i++)
        {
            _indices[i] = newIndices[i - startIndex];
        }
    }

    public void AddIndices(List<int> newIndices)
    {
        _indices.AddRange(newIndices);
    }

    public void SetVertices(List<GeoFixedVertex> newVertices, int startIndex)
    {
        Debug.Assert(newVertices.Count <= _vertices.Count - startIndex);
        for (int i = startIndex; i < newVertices.Count; i++)
        {
            _vertices[i] = newVertices[i - startIndex];
        }
    }

    public void AddVertices(List<GeoFixedVertex> newVertices)
    {
        _vertices.AddRange(newVertices);
    }

    public void CommitMeshChanges()
    {
        var (newVertices, newIndices) = InterpolateOnCommit ? InterpolateMesh() : (_vertices, _indices);

        Vector3[] meshVertices = new Vector3[newVertices.Count];
        Color[] meshColors = new Color[newVertices.Count];
        for (int i = 0; i < newVertices.Count; i++)
        {
            meshVertices[i] = newVertices[i].Position.RawToVector3();
            meshColors[i] = newVertices[i].Color;
        }

        int[] empty = { };
        _rawMesh.SetIndices(empty, MeshTopology.Lines, 0, false);
        _rawMesh.SetVertices(meshVertices);
        _rawMesh.SetColors(meshColors);
        _rawMesh.SetIndices(newIndices, MeshTopology.Lines, 0, false);

        // This mesh is weird, because its points get transformed by a
        // lot in the vertex shader. This means that the bounds that Unity
        // computes are most often wrong and hide the mesh even though it
        // should be shown. This is why I manually set the bounds to be
        // huge, so that basically this mesh is never culled by Unity.
        _rawMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f);

#if RENDER_GEOFIXED_WITH_CPU
        _interpolatedVertices = newVertices;
#endif
    }

    private (List<GeoFixedVertex>, List<int>) InterpolateMesh()
    {
        var newVertices = new List<GeoFixedVertex>(_vertices);
        var newIndices = new List<int>();

        for (int i = 0; i < _indices.Count; i += 2)
        {
            var p1 = _vertices[_indices[i]];
            var p2 = _vertices[_indices[i + 1]];

            List<GeoFixedVertex> newSegment =
                LinearlyInterpolateSegment(p1.Position, p2.Position, InterpolatedSegmentMaxLengthMeters)
                .Select(pEcef =>
                {
                    var a = new GeoFixedVertex();
                    a.Position = pEcef;
                    a.Color = p1.Color;
                    return a;
                }).ToList();

            newSegment.RemoveAt(0); // Remove p1
            newSegment.RemoveAt(newSegment.Count - 1); // Remove p2

            if (newSegment.Count == 0)
            {
                // The old segment was short enough and has been kept untouched
                newIndices.Add(_indices[i]);
                newIndices.Add(_indices[i + 1]);

                continue;
            }

            for (int j = 0; j < newSegment.Count; j++)
            {
                float t = (1f / newSegment.Count) * (j + 1);
                newSegment[j].Color = Color.Lerp(p1.Color, p2.Color, t);
            }

            int firstNewVertexIndex = newVertices.Count;
            newVertices.AddRange(newSegment);

            newIndices.Add(_indices[i]);
            newIndices.Add(firstNewVertexIndex);
            for (int j = 0; j < newSegment.Count - 1; j++)
            {
                newIndices.Add(firstNewVertexIndex + j);
                newIndices.Add(firstNewVertexIndex + j + 1);
            }
            newIndices.Add(firstNewVertexIndex + newSegment.Count - 1);
            newIndices.Add(_indices[i + 1]);

        }

        return (newVertices, newIndices);
    }

#if RENDER_GEOFIXED_WITH_CPU

    public void UpdateGeoFixed(WGS84Point newEnuOrigin, bool projectionEnabled, CylinderProjectionInfo info){
        var vs = new Vector3[_interpolatedVertices.Count];
        for (int i = 0; i < _interpolatedVertices.Count; i++){

            var vertex = _interpolatedVertices[i].Position.ToENU(newEnuOrigin).ToUnity().ToVector3();

            if (projectionEnabled)
            {
                var vertexInWorldSpace = gameObject.transform.TransformPoint(vertex);
                Vector3Double v = Vector3Double.From(vertexInWorldSpace);
                ProjectToCylinder(ref v, info);
                vertex = gameObject.transform.InverseTransformPoint(v.ToVector3());
            }
                
            vs[i] = vertex;
        }

        _rawMesh.SetVertices(vs);
    }
#endif


}