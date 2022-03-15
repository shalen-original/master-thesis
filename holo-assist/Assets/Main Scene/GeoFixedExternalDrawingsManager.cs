using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using static GeodesyHelper;
using static DoubleNumerics;
using System.Linq;

/*
    Receives API commands for geo-fixed augmentations from the UDP 
    manager and applies them. When receiving GeoFixedVertex from 
    the API calls, it converts them to the ECEF coordinate system.
    If geo-fixed augmentations are rendered on the CPU, the Update
    method of this class performs the necessary conversions from ECEF
    to Unity coordinates. If they are rendered on the GPU, the 
    Update method sets the correct parameters for the shader that
    performs these transformations on the GPU.
*/

#if RENDER_GEOFIXED_WITH_CPU
using static ProjectionHelper;
#endif

[RequireComponent(typeof(UDPManager))]
public class GeoFixedExternalDrawingsManager : MonoBehaviour
{
    public Interactable ProjectionEnabledButton;

    public GameObject ENUOrigin;
    public GameObject SimulatorViewProjectionPoint;
    public GameObject SimulatorCylinderCenter;
    public GameObject SimulatorCylinderRadiusMarker;

    public Material GeoFixedLineMaterial;
    public Material NormalLineMaterial;

    private UDPManager _udpManager;
    private Dictionary<string, GameObject> _geoFixedExternalMeshes;

    private bool _projectionEnabled = true;

    void Start()
    {
        _udpManager = GetComponent<UDPManager>();
        _udpManager.OnUDPCommandReceived.AddListener(OnUDPCommandReceived);

        _geoFixedExternalMeshes = new Dictionary<string, GameObject>();

        ProjectionEnabledButton.OnClick.AddListener(() => _projectionEnabled = !_projectionEnabled);
    }

    void Update()
    {
        var planePosition = _udpManager.CurrentSimulatorStatus.planePositionOnEarth;
        var cylRadius = SimulatorCylinderRadiusMarker.transform.localPosition.magnitude;

#if RENDER_GEOFIXED_WITH_CPU
        CylinderProjectionInfo infoGeofixed;
        infoGeofixed.cylinderCenter = Vector3Double.From(SimulatorCylinderCenter.transform.position);
        infoGeofixed.projectionEyePoint = Vector3Double.From(SimulatorViewProjectionPoint.transform.position);
        infoGeofixed.cylinderRadius = cylRadius;

        foreach (var ed in _geoFixedExternalMeshes.Values)
        {
            ed.GetComponent<GeoFixedExternalLineMesh>().UpdateGeoFixed(planePosition, _projectionEnabled, infoGeofixed);
        }
#else
        Vector4 projCenterInWorld = SimulatorViewProjectionPoint.transform.position;
        Vector4 cylCenterInWorld = SimulatorCylinderCenter.transform.position;

        projCenterInWorld.w = 1.0f;
        cylCenterInWorld.w = 1.0f;

        // The fourth coordinate is unused, but apparently
        // there isn't an easy way to pass a Vector3 to a shader
        var newEnuOrigin = new Vector4(
            (float) planePosition.LatitudeRadians,
            (float) planePosition.LongitudeRadians,
            (float) planePosition.AltitudeMeters,
            1.0f
        );

        GeoFixedLineMaterial.SetVector("_CurrentENUOriginWGS", newEnuOrigin);
        GeoFixedLineMaterial.SetVector("_ProjectionCenterInWorldSpace", projCenterInWorld);
        GeoFixedLineMaterial.SetVector("_CylinderCenterInWorldSpace", cylCenterInWorld);
        GeoFixedLineMaterial.SetFloat("_CylinderRadius", cylRadius);
        GeoFixedLineMaterial.SetInteger("_ProjectionEnabled", _projectionEnabled ? 1 : 0);
#endif
    }

    void OnUDPCommandReceived(string type, JObject command)
    {
        if (type == "CREATE_MESH")
            OnCreateMesh(command.ToObject<CreateMeshCommand>());
        else if (type == "SET_MESH_ACTIVE")
            OnSetMeshActive(command.ToObject<SetMeshActiveCommand>());
        else if (type == "SET_MESH_VERTICES")
            OnSetMeshVertices(command.ToObject<SetMeshVerticesCommand>());
        else if (type == "SET_MESH_INDICES")
            OnSetMeshIndices(command.ToObject<SetMeshIndicesCommand>());
        else if (type == "COMMIT_MESH_CHANGES")
            OnCommitMeshChanges(command.ToObject<CommitMeshChangesCommand>());
        else if (type == "DELETE_MESH")
            OnDeleteMesh(command.ToObject<DeleteMeshCommand>());  
    }

    private void OnCreateMesh(CreateMeshCommand cmd)
    {
        if (_geoFixedExternalMeshes.ContainsKey(cmd.Id))
        {
            _udpManager.SendUDPMessage($"Mesh with id '{cmd.Id}' already exists, cannot recreate it");
            return;
        }

        var go = new GameObject(cmd.Id);
        go.SetActive(false);
        go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        go.transform.SetParent(ENUOrigin.transform, false);

        var elm = go.AddComponent<GeoFixedExternalLineMesh>();
        elm.InitFields();
        elm.InterpolateOnCommit = cmd.InterpolateOnCommit;
        elm.InterpolatedSegmentMaxLengthMeters = cmd.InterpolatedSegmentMaxLengthMeters;
        
#if RENDER_GEOFIXED_WITH_CPU
        mr.sharedMaterial = NormalLineMaterial;
#else
        mr.sharedMaterial = GeoFixedLineMaterial;
#endif
        _geoFixedExternalMeshes.Add(cmd.Id, go);
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
        } catch (Exception e)
        {
            _udpManager.SendUDPMessage("Error while trying to set indices, message: " + e.Message);
        }
        
    }

    private void OnSetMeshVertices(SetMeshVerticesCommand cmd)
    {
        var elm = _checkIdAndGet(cmd.Id);
        if (elm == null) return;

        List<GeoFixedVertex> vs = cmd.vertices.Select(v => v.ToInternalRepresentation()).ToList();

        if (!cmd.StartIndex.HasValue)
        {
            elm.AddVertices(vs);
            return;
        }

        try
        {
            elm.SetVertices(vs, (int)cmd.StartIndex.Value);
        } catch (Exception e)
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
        _geoFixedExternalMeshes.Remove(cmd.Id);
    }
    private GeoFixedExternalLineMesh _checkIdAndGet(string id)
    {
        if (_geoFixedExternalMeshes.ContainsKey(id))
            return _geoFixedExternalMeshes[id].GetComponent<GeoFixedExternalLineMesh>();

        _udpManager.SendUDPMessage($"Mesh with id '{id}' does not exist");
        return null;
    }

    public class ExternalLineMeshVertexDto
    {
        public WGS84Point OriginWGS;
        public Color Color;
        public Vector3 LocalPositionMeters = Vector3.zero;
        public Vector3 LocalRotationRadians = Vector3.zero;

        private Quaternion LocalRotationAsQuaternionDouble()
        {
            var rotX = Quaternion.AngleAxis(LocalRotationRadians.x * Mathf.Rad2Deg, Vector3.right);
            var rotY = Quaternion.AngleAxis(LocalRotationRadians.y * Mathf.Rad2Deg, Vector3.up);
            var rotZ = Quaternion.AngleAxis(LocalRotationRadians.z * Mathf.Rad2Deg, Vector3.forward);
            return Quaternion.identity * rotZ * rotX * rotY;
        }

        public GeoFixedVertex ToInternalRepresentation()
        {
            var v = new GeoFixedVertex();

            var rot = LocalRotationAsQuaternionDouble();
            var pointInEnu = /*enuOrigin = (0, 0, 0) + */ rot * LocalPositionMeters; // Rotation happens in ENU space
            var pointInEcef = ENUPoint.FromUnity(Vector3Double.From(pointInEnu)).ToECEF(OriginWGS);

            v.Position = ECEFPoint.RawFrom(pointInEcef.RawToVector3Double());
            v.Color = Color;

            return v;
        }
    }

    private class CreateMeshCommand
    { 
        public string Id;
        public bool InterpolateOnCommit;
        public float InterpolatedSegmentMaxLengthMeters;
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
        public List<ExternalLineMeshVertexDto> vertices;
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
}
