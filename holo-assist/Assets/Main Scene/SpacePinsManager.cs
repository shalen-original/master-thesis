using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.WorldLocking.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
    Whenever a QR code position is updated, this class forwards the
    new position to the World Locking Tools library in order to
    improve the alignment. The `PosesRingBuffer` allows to easily
    average over the last 10 positions detected for a QR code, ensuring
    a higher stability in the alignment.
*/

[RequireComponent(typeof(QRCodeManager))]
public class SpacePinsManager : MonoBehaviour
{
    public GameObject SpacePinsCollection;

    private QRCodeManager _qrCodeManager;
    private Orienter _sharedOrienter;
    private Dictionary<string, ActiveSpacePin> _activeSpacePins;

    private class PosesRingBuffer
    {
        public const int SIZE = 10;
        private Vector3[] positionsList;
        private Quaternion[] rotationsList;
        private int lastOccupiedIndex;
        private int count;

        public PosesRingBuffer()
        {
            positionsList = new Vector3[SIZE];
            rotationsList = new Quaternion[SIZE];
            lastOccupiedIndex = -1;
            count = 0;
        }

        public void Push(Pose value)
        {
            if (count < SIZE)
                count += 1;

            lastOccupiedIndex += 1;
            lastOccupiedIndex %= SIZE;

            positionsList[lastOccupiedIndex] = value.position;
            rotationsList[lastOccupiedIndex] = value.rotation;
        }

        public Pose AveragePose
        {
            get
            {
                if (count == 0)
                    return Pose.identity;

                var avgPosition = Vector3.zero;
                float x = 0, y = 0, z = 0, w = 0;

                for (int i = 0; i < count; i++)
                {
                    avgPosition += positionsList[i];

                    x += rotationsList[i].x;
                    y += rotationsList[i].y;
                    z += rotationsList[i].z;
                    w += rotationsList[i].w;
                }

                float k = 1.0f / Mathf.Sqrt(x * x + y * y + z * z + w * w);

                return new Pose(
                    avgPosition / count, 
                    new Quaternion(x * k, y * k, z * k, w * k)
                );
            }
        }
    }

    private class ActiveSpacePin
    {
        public Transform PinTransform;
        public TimedColorFader PinTimedColorFader;
        public SpacePinOrientable SpacePin;
        public SpatialGraphNode SpatialNode;
        public PosesRingBuffer Poses;
    }

    private void Start()
    {
        // This is the way that WorldLockingTools uses to ensure
        // that the various space pins each have a consistent
        // rotation with respect to all the other space pins.
        // The single orienter is shared among all the space pins.
        _sharedOrienter = gameObject.AddComponent<Orienter>();

        _qrCodeManager = GetComponent<QRCodeManager>();
        _qrCodeManager.QRCodeAdded.AddListener(OnQRCodeUpdate);
        _qrCodeManager.QRCodeUpdated.AddListener(OnQRCodeUpdate);
        _qrCodeManager.QRCodeRemoved.AddListener(OnQRCodeRemoved);

        _activeSpacePins = new Dictionary<string, ActiveSpacePin>();
    }

    private bool InitializeSpacePin(QRCodeManager.QRCodeData data)
    {
        var spacePinTransform = SpacePinsCollection.transform.Find(data.Data);

        if (spacePinTransform == null)
        {
            return false;
        }

        var sp = spacePinTransform.gameObject.AddComponent<SpacePinOrientable>();
        sp.Orienter = _sharedOrienter;

        _activeSpacePins.Add(data.Data, new ActiveSpacePin
        {
            PinTransform = spacePinTransform,
            PinTimedColorFader = spacePinTransform.gameObject.GetComponent<TimedColorFader>(),
            SpacePin = sp,
            SpatialNode = SpatialGraphNode.FromStaticNodeId(data.SpatialGraphNodeId),
            Poses = new PosesRingBuffer()
        });

        return true;
    }

    private void OnQRCodeUpdate(QRCodeManager.QRCodeData data)
    {
        if (!_activeSpacePins.ContainsKey(data.Data))
        {
            if (!InitializeSpacePin(data))
            {
                // The items of the SpacePinsCollection are generated at runtime by HoloAssist.cs
                // when a new QR Code is detected. Also HoloAssist.cs relies on events generated by
                // QRCodeManager to know when it should create these objects. Since the order in which
                // different Unity components receive different UnityEvent on the same handler is
                // not defined, it could happen that this `InitializeSpacePin` is invoked before
                // the space pin object is actually created. In this case, it is not a big problem,
                // as on the next QRCodeUpdated the HoloAssist.cs event handler will for sure have run
                // and therefore the space pin game object will exist and the initialization will succeed.
                return;
            }
        }

        var updatedSpacePin = _activeSpacePins[data.Data];

        if (updatedSpacePin.SpatialNode.TryLocate(FrameTime.OnUpdate, out Pose spongyPose))
        {
            var frozen = WorldLockingManager.GetInstance().FrozenFromSpongy.Multiply(spongyPose);
            var distanceHololensToQrCode = (Camera.main.transform.position - frozen.position).magnitude;

            const float DISTANCE_THRESHOLD_CM = 80;
            if (distanceHololensToQrCode > (DISTANCE_THRESHOLD_CM * 0.01))
            {
                return;
            }

            updatedSpacePin.PinTimedColorFader.Highlight();
            updatedSpacePin.Poses.Push(spongyPose);
            updatedSpacePin.SpacePin.SetSpongyPose(updatedSpacePin.Poses.AveragePose);
        } else
        {
            // Either the location hasn't changed or the location is unavailable (e.g. loss of tracking),
            // in any case we don't do anything
        }
    }

    private void OnQRCodeRemoved(QRCodeManager.QRCodeData data)
    {
        if (_activeSpacePins.TryGetValue(data.Data, out ActiveSpacePin sp))
        {
            _activeSpacePins.Remove(data.Data);
            sp.SpacePin.Reset();
            Destroy(sp.SpacePin);
        }
    }

}