using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

/*
    Receives commands over the LAN and allows to move and
    rotate GameObjects. It also allows to dump the new
    pose of a GameObject to a UDP packet. Allows to implement
    a sort of "remote Unity editor" that enables refining
    the position of holograms while wearing the Hololens.
*/

[RequireComponent(typeof(UDPManager))]
public class RemoteEditor : MonoBehaviour
{
    private UDPManager _udpManager;

    void Start()
    {
        _udpManager = GetComponent<UDPManager>();
        _udpManager.OnUDPCommandReceived.AddListener(OnCommandReceived);
    }

    void OnCommandReceived(string type, JObject cmd)
    {
        if (type == "MOVE_UNITY_OBJECT")
        {
            var c = cmd.ToObject<MoveUnityObjectCommand>();
            var go = GameObject.Find(c.ObjectName);

            if (c.Kind == MoveUnityObjectCommand.Kinds.TRANSLATION)
            {
                go.transform.Translate(c.Axis * c.Amount, Space.Self);
            }
            else
            {
                go.transform.Rotate(c.Axis, c.Amount, Space.Self);
            }
        }

        if (type == "DUMP_UNITY_OBJECT_STATUS")
        {
            var go = FindGameObjectByName(cmd["objectName"].ToString());

            var msg = new DumpObjectDataMessage();
            msg.LocalPositionX = go.transform.localPosition.x;
            msg.LocalPositionY = go.transform.localPosition.y;
            msg.LocalPositionZ = go.transform.localPosition.z;
            msg.LocalEulerAnglesX = go.transform.localEulerAngles.x;
            msg.LocalEulerAnglesY = go.transform.localEulerAngles.y;
            msg.LocalEulerAnglesZ = go.transform.localEulerAngles.z;
            msg.ActiveSelf = go.activeSelf;
            msg.ActiveInHierarchy = go.activeInHierarchy;
            _udpManager.SendUDPJSONMessage(msg);
        }

        if (type == "DUMP_HIERARCHY")
        {
            var rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var items = new List<string>();
            for (int i = 0; i < rootGameObjects.Length; i++)
            {
                items.AddRange(TraverseTransformDescendants(rootGameObjects[i].transform));
            }
            _udpManager.SendUDPJSONMessage(items);
        }
    }

    private List<string> TraverseTransformDescendants(Transform tr, int depth = 0)
    {
        List<string> ans = new List<string>();
        ans.Add(new string('-', depth) + (depth != 0 ? " " : "") + tr.gameObject.name);
        for (int i = 0; i < tr.childCount; i++)
        {
            ans.AddRange(TraverseTransformDescendants(tr.GetChild(i), depth + 1));
        }
        return ans;
    }

    private GameObject FindGameObjectByName(string name)
    {
        var rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < rootGameObjects.Length; i++)
        {
            if (rootGameObjects[i].name == name)
                return rootGameObjects[i];

            var res = rootGameObjects[i].transform.Find(name);
            if (res != null)
                return res.gameObject;
        }
        return null;
    }

    private class MoveUnityObjectCommand
    {
        public enum Kinds { TRANSLATION, ROTATION }

        public string ObjectName;
        public Kinds Kind;
        public Vector3 Axis;
        public float Amount;

        [JsonConstructor]
        public MoveUnityObjectCommand(string ObjectName, string Kind, string Axis, float Amount)
        {
            this.ObjectName = ObjectName;

            if (Axis == "x")
                this.Axis = Vector3.right;
            else if (Axis == "y")
                this.Axis = Vector3.up;
            else
                this.Axis = Vector3.forward;

            this.Kind = (Kinds)Enum.Parse(typeof(Kinds), Kind);
            this.Amount = Amount;
        }
    }

    public class DumpObjectDataMessage
    {
        public string Type = "DUMP_OBJECT_DATA_MESSAGE";
        public float LocalPositionX;
        public float LocalPositionY;
        public float LocalPositionZ;
        public float LocalEulerAnglesX;
        public float LocalEulerAnglesY;
        public float LocalEulerAnglesZ;
        public bool ActiveSelf;
        public bool ActiveInHierarchy;
    }

    public class DumpHierarchyMessage
    {
        public string Type = "DUMP_HIERARCHY_MESSAGE";
        public List<string> Hierarchy = new List<string>();
    }
}
