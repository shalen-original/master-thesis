using System.Collections.Generic;
using UnityEngine;

/*
    Behind the scenes, each plane-fixed augmentation is backed by
    a `PlaneFixedExternalLineMesh`.
*/

public struct ColoredVertex
{
    public Vector3 Position;
    public Color Color;
}

[RequireComponent(typeof(MeshFilter))]
public class PlaneFixedExternalLineMesh : MonoBehaviour
{
    private List<ColoredVertex> _vertices;
    private List<int> _indices;
    private Mesh _rawMesh;

    public void InitFields()
    {
        // This method is required because Unity does not
        // want you to touch the constructor of something that
        // inherits from MonoBehaviour *and* it doesn't
        // call neither Awake() nor Start() on an inactive
        // GameObject (the default status for a GameObject with a
        // PlaneFixedExternalLineMesh component)
        _vertices = new List<ColoredVertex>();
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

    public void SetVertices(List<ColoredVertex> newVertices, int startIndex)
    {
        Debug.Assert(newVertices.Count <= _vertices.Count - startIndex);
        for (int i = startIndex; i < newVertices.Count; i++)
        {
            _vertices[i] = newVertices[i - startIndex];
        }
    }

    public void AddVertices(List<ColoredVertex> newVertices)
    {
        _vertices.AddRange(newVertices);
    }

    public void CommitMeshChanges()
    {
        Vector3[] meshVertices = new Vector3[_vertices.Count];
        Color[] meshColors = new Color[_vertices.Count];
        for (int i = 0; i < _vertices.Count; i++)
        {
            meshVertices[i] = _vertices[i].Position;
            meshColors[i] = _vertices[i].Color;
        }

        _rawMesh.SetVertices(meshVertices);
        _rawMesh.SetColors(meshColors);
        _rawMesh.SetIndices(_indices, MeshTopology.Lines, 0);
    }
}
