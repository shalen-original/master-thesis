using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

/*
    Receives API commands for plane-fixed augmentations from the UDP
    manager and applies them.
*/

[RequireComponent(typeof(UDPManager))]
public class PlaneFixedExternalDrawingsManager : MonoBehaviour
{
    public Material LineMaterial;
    public GameObject CabinAugmentationsRoot;

    private UDPManager _udpManager;
    private Dictionary<string, GameObject> _planeFixedExternalMeshes;

    void Start()
    {
        _udpManager = GetComponent<UDPManager>();
        _udpManager.OnUDPCommandReceived.AddListener(OnUDPCommandReceived);

        _planeFixedExternalMeshes = new Dictionary<string, GameObject>();
    }

    void OnUDPCommandReceived(string type, JObject command)
    {
        if (type == "PF_CREATE_MESH")
            OnCreateMesh(command.ToObject<CreateMeshCommand>());
        else if (type == "PF_SET_MESH_ACTIVE")
            OnSetMeshActive(command.ToObject<SetMeshActiveCommand>());
        else if (type == "PF_SET_MESH_VERTICES")
            OnSetMeshVertices(command.ToObject<SetMeshVerticesCommand>());
        else if (type == "PF_SET_MESH_INDICES")
            OnSetMeshIndices(command.ToObject<SetMeshIndicesCommand>());
        else if (type == "PF_COMMIT_MESH_CHANGES")
            OnCommitMeshChanges(command.ToObject<CommitMeshChangesCommand>());
        else if (type == "PF_UPDATE_MESH_ORIGIN")
            OnUpdateMeshOrigin(command.ToObject<UpdateMeshOriginCommand>());
        else if (type == "PF_DELETE_MESH")
            OnDeleteMesh(command.ToObject<DeleteMeshCommand>());
    }

    private void OnCreateMesh(CreateMeshCommand cmd)
    {
        if (_planeFixedExternalMeshes.ContainsKey(cmd.Id))
        {
            _udpManager.SendUDPMessage($"Mesh with id '{cmd.Id}' already exists, cannot recreate it");
            return;
        }

        var go = new GameObject(cmd.Id);
        go.SetActive(false);
        go.AddComponent<MeshFilter>();
        go.transform.SetParent(CabinAugmentationsRoot.transform, false);
        go.transform.localPosition = cmd.OriginPositionMeters;
        go.transform.localEulerAngles = cmd.OriginRotationRadians;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = LineMaterial;

        var elm = go.AddComponent<PlaneFixedExternalLineMesh>();
        elm.InitFields();

        _planeFixedExternalMeshes.Add(cmd.Id, go);
    }

    private void OnSetMeshActive(SetMeshActiveCommand cmd)
    {
        var elm = _checkIdAndGet(cmd.Id);
        if (elm == null) return;

        elm.gameObject.SetActive(cmd.Active);
    }

    private void OnSetMeshIndices(SetMeshIndicesCommand cmd)
    {
        var elm = _checkIdAndGet(cmd.Id);
        if (elm == null) return;

        if (!cmd.StartIndex.HasValue && cmd.Indices.Count % 2 != 0)
        {
            _udpManager.SendUDPMessage("Cannot add " + cmd.Indices.Count + " indices, an even number is required");
            return;
        }

        if (!cmd.StartIndex.HasValue)
        {
            elm.AddIndices(cmd.Indices);
            return;
        }

        try
        {
            elm.SetIndices(cmd.Indices, (int)cmd.StartIndex.Value);
        }
        catch (Exception e)
        {
            _udpManager.SendUDPMessage("Error while trying to set indices, message: " + e.Message);
        }

    }

    private void OnSetMeshVertices(SetMeshVerticesCommand cmd)
    {
        var elm = _checkIdAndGet(cmd.Id);
        if (elm == null) return;

        if (!cmd.StartIndex.HasValue)
        {
            elm.AddVertices(cmd.Vertices);
            return;
        }

        try
        {
            elm.SetVertices(cmd.Vertices, (int)cmd.StartIndex.Value);
        }
        catch (Exception e)
        {
            _udpManager.SendUDPMessage("Error while trying to set vertices, message: " + e.Message);
        }
    }

    private void OnCommitMeshChanges(CommitMeshChangesCommand cmd)
    {
        var elm = _checkIdAndGet(cmd.Id);
        if (elm == null) return;

        elm.CommitMeshChanges();
    }

    private void OnDeleteMesh(DeleteMeshCommand cmd)
    {
        var elm = _checkIdAndGet(cmd.Id);
        if (elm == null) return;

        Destroy(elm.gameObject);
        _planeFixedExternalMeshes.Remove(cmd.Id);
    }

    private void OnUpdateMeshOrigin(UpdateMeshOriginCommand cmd)
    {
        if (!_planeFixedExternalMeshes.ContainsKey(cmd.Id))
        {
            _udpManager.SendUDPMessage($"Mesh with id '{cmd.Id}' does not exist or is not plane fixed");
            return;
        }

        var tr = _planeFixedExternalMeshes[cmd.Id].transform;

        if (cmd.OriginPositionMeters.HasValue)
            tr.localPosition = cmd.OriginPositionMeters.Value;

        if (cmd.OriginRotationRadians.HasValue)
            tr.localEulerAngles = cmd.OriginRotationRadians.Value;
    }

    private PlaneFixedExternalLineMesh _checkIdAndGet(string id)
    {
        if (_planeFixedExternalMeshes.ContainsKey(id))
            return _planeFixedExternalMeshes[id].GetComponent<PlaneFixedExternalLineMesh>();

        _udpManager.SendUDPMessage($"Mesh with id '{id}' does not exist");
        return null;
    }

    private class CreateMeshCommand
    {
        public string Id;
        public Vector3 OriginPositionMeters;
        public Vector3 OriginRotationRadians;
    }

    private class SetMeshActiveCommand
    {
        public string Id;
        public bool Active;
    }

    private class SetMeshVerticesCommand
    {
        public string Id;
        public uint? StartIndex;
        public List<ColoredVertex> Vertices;
    }

    private class SetMeshIndicesCommand
    {
        public string Id;
        public uint? StartIndex;
        public List<int> Indices;
    }

    private class CommitMeshChangesCommand
    {
        public string Id;
    }

    private class DeleteMeshCommand
    {
        public string Id;
    }

    private class UpdateMeshOriginCommand
    {
        public string Id;
        public Vector3? OriginPositionMeters;
        public Vector3? OriginRotationRadians;
    }
}
