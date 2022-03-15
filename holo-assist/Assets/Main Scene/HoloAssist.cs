using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.WorldLocking.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using Microsoft.MixedReality.Toolkit.SpatialObjectMeshObserver;
#endif

/*
    Kitchen-sink for all the things that didn't have a home somewhere else.
    It handles most of the button presses of the main menu, sets some
    global Hololens configuration and ensures that the current plane
    rotation (as received from the simulator) is correctly applied to the
    plane ENU coordinate system.
*/

[RequireComponent(typeof(QRCodeManager))]
[RequireComponent(typeof(UDPManager))]
public class HoloAssist : MonoBehaviour
{
    public Interactable BtnToggleMesh;
    public Interactable BtnLogUdpPing;
    public Interactable BtnToggleSpatialAwareness;
    public Interactable BtnTogglePlaneActive;

    public TextMeshPro DebugLabel;

    public GameObject Plane;
    public GameObject ENUOrigin;
    public GameObject SimulatorCylinderCenter;
    public GameObject SimulatorViewProjectionPoint;
    public GameObject CylinderRadiusMarker;
    public GameObject SpacePinsCollection;
    public Mesh CubeMesh;
    public Material SpacePinsMaterial;

    public Material OcclusionMaterial;

    private Material _originalMaterial;
    private IMixedRealitySpatialAwarenessMeshObserver _meshObserver;

    private QRCodeManager _qrCodeManager;
    private UDPManager _udpManager;

    void Start()
    {
        BtnToggleMesh.OnClick.AddListener(OnBtnToggleMeshClick);
        BtnLogUdpPing.OnClick.AddListener(OnBtnLogUdpPing);     
        BtnToggleSpatialAwareness.OnClick.AddListener(OnToggleSpatialAwareness);
        BtnTogglePlaneActive.OnClick.AddListener(OnTogglePlaneActiveClick);

        _originalMaterial = Plane.GetComponent<Renderer>().material;
        Plane.GetComponent<Renderer>().material = OcclusionMaterial;

        _qrCodeManager = GetComponent<QRCodeManager>();
        _qrCodeManager.QRCodeAdded.AddListener(OnQRCodeDetected);
        _qrCodeManager.QRCodeUpdated.AddListener(OnQRCodeDetected);

        _udpManager = GetComponent<UDPManager>();

#if UNITY_EDITOR
        _meshObserver = CoreServices.GetSpatialAwarenessSystemDataProvider<SpatialObjectMeshObserver>();
#else
        _meshObserver = CoreServices.GetSpatialAwarenessSystemDataProvider<Microsoft.MixedReality.Toolkit.XRSDK.OpenXR.OpenXRSpatialAwarenessMeshObserver>();
#endif
        _meshObserver.DisplayOption = SpatialAwarenessMeshDisplayOptions.None;

        PointerUtils.SetMotionControllerRayPointerBehavior(PointerBehavior.AlwaysOff);
        PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOff);

#if !UNITY_EDITOR
        PointerUtils.SetGazePointerBehavior(PointerBehavior.AlwaysOff);
#endif

        DebugLabel.text = "Initialized";
    }

    void OnTogglePlaneActiveClick()
    {
        Plane.SetActive(!Plane.activeSelf);
    }

    void OnBtnToggleMeshClick()
    {
        var r = Plane.GetComponent<Renderer>();
        if (r.material == _originalMaterial)
        {
            r.material = OcclusionMaterial;
        } else
        {
            r.material = _originalMaterial;
        }
    }

    void OnBtnLogUdpPing()
    {
        WorldLockingManager.GetInstance().AnchorManager.Reset();

        Debug.Log("Ping in log!");
        _udpManager.SendUDPMessage("Ping in UDP!");

#if RENDER_GEOFIXED_WITH_CPU
        Debug.Log("Rendering geofixed external drawings with CPU");
        _udpManager.SendUDPMessage("Rendering geofixed external drawings with CPU");
#else
        Debug.Log("Rendering geofixed external drawings with GPU");
        _udpManager.SendUDPMessage("Rendering geofixed external drawings with GPU");
#endif

        var d = new QRCodeManager.QRCodeData();
        d.Data = "RFS-1";
        OnQRCodeDetected(d);

    }

    void OnToggleSpatialAwareness()
    {
        if (_meshObserver.DisplayOption == SpatialAwarenessMeshDisplayOptions.None)
        {
            _meshObserver.DisplayOption = SpatialAwarenessMeshDisplayOptions.Occlusion;
        } else
        {
            _meshObserver.DisplayOption = SpatialAwarenessMeshDisplayOptions.None;
        }
    }

    void OnQRCodeDetected(QRCodeManager.QRCodeData data)
    {
        // HoloAssist is quite simple in this respect. The first QR code that it
        // sees determines the model that is loaded, and everything else
        // is just ignored
        _qrCodeManager.QRCodeAdded.RemoveListener(OnQRCodeDetected);
        _qrCodeManager.QRCodeUpdated.RemoveListener(OnQRCodeDetected);

        var model = data.Data.Contains("-") ? data.Data.Split('-')[0] : data.Data;
        _udpManager.SendUDPMessage($"Detected QR Code '{data.Data}' for model '{model}'!");

        GameObject asset = Resources.Load<GameObject>($"Planes/{model}/plane-mesh");
        var actual = asset.transform.GetChild(0);
        Plane.GetComponent<MeshFilter>().mesh = actual.GetComponent<MeshFilter>().sharedMesh;
        Plane.GetComponent<Renderer>().material = OcclusionMaterial;

        var textAsset = Resources.Load<TextAsset>($"Planes/{model}/plane-measures");
        var measures = JsonConvert.DeserializeObject<PlaneMeasures>(textAsset.text);
        Plane.transform.localPosition = measures.planeMesh;

        ENUOrigin.transform.SetParent(measures.isPlaneCenterChildOfProjectionEyePoint ? SimulatorViewProjectionPoint.transform : null);
        ENUOrigin.transform.localPosition = measures.planeCenter;

        // As of now only the CYLINDER projection type is supported
        Assert.AreEqual(measures.projection["type"].ToString(), "CYLINDER");
        var projInfo = measures.projection.ToObject<CylinderProjectionInfo>();
        SimulatorCylinderCenter.transform.localPosition = projInfo.cylinderCenter;
        SimulatorViewProjectionPoint.transform.localPosition = projInfo.eyePoint;
        CylinderRadiusMarker.transform.localPosition = new Vector3(0, 0, projInfo.cylinderRadius);

        foreach (string spacePinName in measures.spacePins.Keys){
            var go = new GameObject(spacePinName);
            go.transform.SetParent(SpacePinsCollection.transform, false);
            go.transform.localPosition = measures.spacePins[spacePinName];
            go.transform.localScale = Vector3.one * 0.01f;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = CubeMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = SpacePinsMaterial;

            go.AddComponent<TimedColorFader>();
        }
        
    }

    void Update()
    {
        var cs = _udpManager.CurrentSimulatorStatus;

        /*
            https://www.evl.uic.edu/ralph/508S98/coordinates.html
            The yaw/pitch/roll coming from the simulator are in a right-handed coordinate system
            with the "up" axis pointing downwards. Unity uses a left-handed coordinate system with 
            the "up" axis pointing upwards. Therefore, we need to convert:

                yaw_{unity} = - (-yaw_{sim}) = yaw_{sim}
                pitch_{unity} = -pitch_{sim}
                roll_{unity} = -roll_{sim}
            
            For the `yaw`, the first minus sign is due to the right-handed to left-handed conversion,
            the second minus sign is because the direction of the "up" axis is flipped between the sim
            and Unity.
            However, what we obtained in this way is a rotation that would move the plane mesh to the
            correct orientation. What we want to achieve instead is to keep the plane mesh still and
            to move instead all the geofixed meshes around (while still obtaining the same end result).
            This requires a further transformation:
                
                yaw_{enu} = -yaw_{unity} = -yaw_{sim}
                pitch_{enu} = -pitch_{unity} = pitch_{sim}
                roll_{enu} = -roll_{unity} = roll_{sim}

        */
        var yawRot = Quaternion.AngleAxis(-Mathf.Rad2Deg * cs.yaw, Vector3.up);
        var pitchRot = Quaternion.AngleAxis(Mathf.Rad2Deg * cs.pitch, Vector3.right);
        var rollRot = Quaternion.AngleAxis(Mathf.Rad2Deg * cs.roll, Vector3.forward);
        ENUOrigin.transform.localRotation = Quaternion.identity * rollRot * pitchRot * yawRot;
    }


    private class PlaneMeasures
    {
        public Vector3 planeMesh;
        public Vector3 planeCenter;
        public JObject projection;
        public Dictionary<string, Vector3> spacePins;
        public bool isPlaneCenterChildOfProjectionEyePoint;
    }

    private class CylinderProjectionInfo {
        public Vector3 cylinderCenter;
        public float cylinderRadius;
        public Vector3 eyePoint;
    }
}
