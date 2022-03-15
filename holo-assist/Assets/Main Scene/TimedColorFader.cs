using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class TimedColorFader : MonoBehaviour
{
    public Color HighlightColor = Color.green;
    public Color NormalColor = Color.red;
    public float FadeOutTimeSeconds = 2;

    private Material meshMaterial;
    private float currentTime = 0;

    void Start()
    {
        meshMaterial = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime = Mathf.Clamp(currentTime - Time.deltaTime, 0, FadeOutTimeSeconds);
            meshMaterial.color = Color.Lerp(NormalColor, HighlightColor, currentTime / FadeOutTimeSeconds);
        }
    }

    public void Highlight()
    {
        currentTime = FadeOutTimeSeconds;
        meshMaterial.color = HighlightColor;
    }
}
