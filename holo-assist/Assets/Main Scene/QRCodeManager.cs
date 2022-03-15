using Microsoft.MixedReality.QR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

/*
    Wraps the Hololens API for detecting QR codes. Each detected QR code is
    broadcasted to the rest of the application via `UnityEvent`.
*/

[RequireComponent(typeof(UDPManager))]
public class QRCodeManager : MonoBehaviour
{
    public UnityEvent<QRCodeData> QRCodeAdded;
    public UnityEvent<QRCodeData> QRCodeUpdated;
    public UnityEvent<QRCodeData> QRCodeRemoved;

    private UDPManager _udpRec; //Useful for debugging from the Release mode of the Hololens (Debug.Logs don't work there)
    private QRCodeWatcher _qrCodeTracker;
    private bool _isTrackerRunning;
    private bool _isEnumerationCompleted;
    private readonly ConcurrentQueue<(EventType, QRCodeData)> _executeOnMainThreadQueue = new ConcurrentQueue<(EventType, QRCodeData)>();

    public class QRCodeData
    {
        public Guid Id;
        public string Data;
        public Guid SpatialGraphNodeId;
        public float PhysicalSideLength;
    }

    private enum EventType
    {
        ADDED, UPDATED, REMOVED, ENUMERATION_ENDED
    }

    public bool IsTrackerRunning
    {
        get { return _isTrackerRunning; }
    }

    async void Start()
    {
        _udpRec = GetComponent<UDPManager>();
        Assert.IsTrue(QRCodeWatcher.IsSupported());

        bool hololensReady = await EnureHololensIsReadyToInitializeCameraStuff();
        if (!hololensReady)
        {
            throw new Exception("Hololens not ready for QR code reading (or you are in the Unity Editor)");
        }

        var capabilityTask = QRCodeWatcher.RequestAccessAsync();
        var accessStatus = await capabilityTask;
        Assert.AreEqual(QRCodeWatcherAccessStatus.Allowed, accessStatus);

        _qrCodeTracker = new QRCodeWatcher();
        _qrCodeTracker.Added += OnQRCodeAdded;
        _qrCodeTracker.Updated += OnQRCodeUpdated;
        _qrCodeTracker.Removed += OnQRCodeRemoved;

        // This is invoked after the _qrCodeTracker has recalled from persistent
        // memory all the QR codes that it detected in previous sessions. As the positions
        // of these "historic" QR codes is sometimes roughly correct, but often
        // completely wrong, all the events before the "EnumerationCompleted" are
        // ignored.
        _qrCodeTracker.EnumerationCompleted += OnQRCodeEnumerationCompleted;

        _isEnumerationCompleted = false;
        _isTrackerRunning = false;

        StartTracking();
    }

    private void Update()
    {
        // See UDPReceiver::Update for an explaination. Apparently this is a common pattern.
        // In summary, the callbacks invoked by _qrCodeTracker happen in another thread, but
        // you can modify/use the Unity API only in the main Unity thread, which is the
        // one in which the "Update" method is executed.

        while (!_executeOnMainThreadQueue.IsEmpty)
        {
            _executeOnMainThreadQueue.TryDequeue(out (EventType, QRCodeData) item);
            var (type, data) = item;
            
            if (type == EventType.ENUMERATION_ENDED)
            {
                _isEnumerationCompleted = true;
            }

            if (_isEnumerationCompleted && type == EventType.ADDED)
            {
                QRCodeAdded.Invoke(data);
            }

            if (_isEnumerationCompleted && type == EventType.UPDATED)
            {
                QRCodeUpdated.Invoke(data);
            }

            if (_isEnumerationCompleted && type == EventType.REMOVED)
            {
                QRCodeRemoved.Invoke(data);
            }

        }
    }

    public void StartTracking()
    {
        _udpRec.SendUDPMessage("[QRCodeManager::StartTracking]");
        if (!_isTrackerRunning && _qrCodeTracker != null)
        {
            _qrCodeTracker.Start();
            _isTrackerRunning = true;
        }
    }

    public void StopTracking()
    {
        _udpRec.SendUDPMessage("[QRCodeManager::StopTracking]");
        if (_isTrackerRunning && _qrCodeTracker != null)
        {
            _qrCodeTracker.Stop();
            _isTrackerRunning = false;
        }
    }

    private void OnQRCodeAdded(object sender, QRCodeAddedEventArgs args)
    {
        _udpRec.SendUDPMessage("[QRCodeManager::OnQRCodeAdded] " + new string(args.Code.Data.ToCharArray()));
        // This is another "strange" of this development setup.
        // Copying the data in this way seems to be fundamental,
        // as otherwise the memory that backs the data contained in
        // `args` seems to be freed as soon as the callback ends,
        // leading to a segfault in DisplayQRCode when one tries
        // to use that data (on the Hololens, in Release mode).
        var data = new QRCodeData();
        data.Id = new Guid(args.Code.Id.ToString());
        data.Data = new string(args.Code.Data.ToCharArray());
        data.SpatialGraphNodeId = new Guid(args.Code.SpatialGraphNodeId.ToString());
        data.PhysicalSideLength = args.Code.PhysicalSideLength;
        _executeOnMainThreadQueue.Enqueue((EventType.ADDED, data));
    }

    private void OnQRCodeUpdated(object sender, QRCodeUpdatedEventArgs args)
    {
        _udpRec.SendUDPMessage("[QRCodeManager::OnQRCodeUpdated] " + new string(args.Code.Data.ToCharArray()));
        var data = new QRCodeData();
        data.Id = new Guid(args.Code.Id.ToString());
        data.Data = new string(args.Code.Data.ToCharArray());
        data.SpatialGraphNodeId = new Guid(args.Code.SpatialGraphNodeId.ToString());
        data.PhysicalSideLength = args.Code.PhysicalSideLength;
        _executeOnMainThreadQueue.Enqueue((EventType.UPDATED, data));
    }

    private void OnQRCodeRemoved(object sender, QRCodeRemovedEventArgs args)
    {
        _udpRec.SendUDPMessage("[QRCodeManager::OnQRCodeRemoved]");
        var data = new QRCodeData();
        data.Id = new Guid(args.Code.Id.ToString());
        data.Data = new string(args.Code.Data.ToCharArray());
        data.SpatialGraphNodeId = new Guid(args.Code.SpatialGraphNodeId.ToString());
        data.PhysicalSideLength = args.Code.PhysicalSideLength;
        _executeOnMainThreadQueue.Enqueue((EventType.REMOVED, data));
    }

    private void OnQRCodeEnumerationCompleted(object sender, object e)
    {
        _executeOnMainThreadQueue.Enqueue((EventType.ENUMERATION_ENDED, null));
    }

    private async Task<bool> EnureHololensIsReadyToInitializeCameraStuff()
    {
        // Before this fix, on the first install of the HoloAssist app on the
        // Hololens, the QRTracker would not work, because it would not correctly
        // await the premission request to access the camera.
        // This is a known issue on the QRTracking library from Microsoft
        // https://github.com/chgatla-microsoft/QRTracking/issues/32
        // The (always Microsoft) folks of the World Locking Tools had the
        // same problems and found the fix I implemented also in HoloAssist:
        // https://github.com/microsoft/MixedReality-WorldLockingTools-Samples/pull/41/files
        // The basic idea is that you await the initialization of the MediaCapture UWP thinghy
        // before initializing the QRTracker itself, so that by the time the QRTracker gets
        // initialized the camera subsystem has already required and awaited all the necessary
        // permissions.
#if WINDOWS_UWP
            try
            {
                var capture = new Windows.Media.Capture.MediaCapture();
                await capture.InitializeAsync();
                Debug.Log("Camera and Microphone permissions OK");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Debug.LogError("Camera and microphone permissions not granted.");
                return false;
            }
#else
        await Task.CompletedTask;
        return false;
#endif
    }

}
