using System.IO;
using System.Text;
using UnityEngine;
using System.Globalization;

/**
 * http://wiki.unity3d.com/index.php/ObjExporter
 */
public class MeshToObj
{
    public static string MeshToString(MeshFilter mf)
    {
        // Sooo, this was fun to discover. When the "ToString" method of a float
        // is invoked, like how we do below when writing the OBJ file, the current
        // locale is taken into account to decide whether to use a dot or a comma as
        // the decimal separator (1.0 vs 1,0). Some OBJ readers are sensitive to this
        // difference, and only accept the dot separator. Therefore, here the culture is
        // temporarily changed to ensure that the correct separator is used.
        var oldCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        Mesh m = mf.mesh;

        StringBuilder sb = new StringBuilder();

        sb.Append("g ").Append(mf.name).Append("\n");
        foreach (Vector3 v in m.vertices)
        {
            sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.normals)
        {
            sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.uv)
        {
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }
        for (int material = 0; material < m.subMeshCount; material++)
        {
            //sb.Append("\n");
            //sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            //sb.Append("usemap ").Append(mats[material].name).Append("\n");

            int[] triangles = m.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
            }
        }

        System.Threading.Thread.CurrentThread.CurrentCulture = oldCulture;
        return sb.ToString();
    }

    public static void MeshToFile(string filename, MeshFilter mf)
    {
        using (StreamWriter sw = new StreamWriter(filename))
        {
            sw.Write(MeshToString(mf));
        }
    }
}
