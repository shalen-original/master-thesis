using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using TMPro;
using System;

#if UNITY_EDITOR
using Microsoft.MixedReality.Toolkit.SpatialObjectMeshObserver;
#endif

public class ScannerManager : MonoBehaviour
{
    public Interactable btnStartScanning;
    public Interactable btnStopScanning;
    public Interactable btnMeshObserverChange;
    public Interactable btnDumpToString;
    public Interactable btnDumpToFile;
    public TextMeshPro debugLabel;

    private IMixedRealitySpatialAwarenessMeshObserver meshObserver;
    private string message;
    private int currentMeshObserverIndex = 0;

    private void Start()
    {
        btnStartScanning.OnClick.AddListener(OnStartScanningClick);
        btnStopScanning.OnClick.AddListener(OnStopScanningClick);
        btnMeshObserverChange.OnClick.AddListener(OnMeshObserverChangeClick);
        btnDumpToString.OnClick.AddListener(OnDumpToStringClick);
        btnDumpToFile.OnClick.AddListener(OnDumpToFileClick);

#if UNITY_EDITOR
        meshObserver = CoreServices.GetSpatialAwarenessSystemDataProvider<SpatialObjectMeshObserver>();
#else
        meshObserver = CoreServices.GetSpatialAwarenessSystemDataProvider<Microsoft.MixedReality.Toolkit.XRSDK.OpenXR.OpenXRSpatialAwarenessMeshObserver>();
#endif

    }

    void OnStartScanningClick()
    {
        meshObserver.Resume();
        message = "Started";
    }

    void OnStopScanningClick()
    {
        meshObserver.Suspend();
        message = "Stopped";
    }
    void OnDumpToStringClick()
    {
        message = "Stopping for dump";
        meshObserver.Suspend();
        foreach (SpatialAwarenessMeshObject samo in meshObserver.Meshes.Values)
        {
            message = MeshToObj.MeshToString(samo.Filter);
            break;
        }
    }

    void OnDumpToFileClick()
    {
        message = "Dumping to file...";
        meshObserver.Suspend();

        CombineInstance[] combine = new CombineInstance[meshObserver.Meshes.Count];

        int i = 0;
        foreach (SpatialAwarenessMeshObject samo in meshObserver.Meshes.Values)
        {
            combine[i].mesh = samo.Filter.mesh;
            combine[i].transform = Matrix4x4.identity;
            i++;
        }

        var mf = GetComponent<MeshFilter>();
        mf.mesh = new Mesh();
        mf.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; //We have many vertices
        mf.mesh.CombineMeshes(combine);
        MeshToObj.MeshToFile(Application.persistentDataPath + "\\" + DateTime.Now.ToString("yyyy'-'MM'-'dd'-'HH'-'mm'-'ss") + ".obj", mf);
        mf.mesh = null;

        message = "Stopped\n" + Application.persistentDataPath;
    }

    void OnMeshObserverChangeClick()
    {
        var dataProvidersAccess = CoreServices.SpatialAwarenessSystem as IMixedRealityDataProviderAccess;

        meshObserver.Suspend();
        meshObserver.Reset();

        if (currentMeshObserverIndex == 0)
        {
            meshObserver = dataProvidersAccess.GetDataProvider<Microsoft.MixedReality.Toolkit.XRSDK.WindowsMixedReality.WindowsMixedRealitySpatialMeshObserver>();
            currentMeshObserverIndex++;
        } else if (currentMeshObserverIndex == 1)
        {
            meshObserver = dataProvidersAccess.GetDataProvider<Microsoft.MixedReality.Toolkit.XRSDK.GenericXRSDKSpatialMeshObserver>();
            currentMeshObserverIndex++;
        } else
        {
            meshObserver = dataProvidersAccess.GetDataProvider<Microsoft.MixedReality.Toolkit.XRSDK.OpenXR.OpenXRSpatialAwarenessMeshObserver>();
            currentMeshObserverIndex = 0;
        }
    }

    private void Update()
    {
        long vertexCount = 0;
        foreach (SpatialAwarenessMeshObject samo in meshObserver.Meshes.Values)
        {
            vertexCount += samo.Filter.mesh.vertexCount;
        }

        debugLabel.text = meshObserver.Meshes.Count + " " + meshObserver.Name + "\n"
            + vertexCount + "\n"
            + message;
    }


}
