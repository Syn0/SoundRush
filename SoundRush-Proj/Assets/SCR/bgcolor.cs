using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bgcolor : MonoBehaviour
{
    public UnityEngine.UI.RawImage img;

    public Color color1;
    public Color color1_target;
    public Color color2;
    public Color color2_target;

    private Texture2D backgroundTexture;

    void Awake()
    {
        backgroundTexture = new Texture2D(1, 2);
        backgroundTexture.wrapMode = TextureWrapMode.Clamp;
        backgroundTexture.filterMode = FilterMode.Bilinear;

        color1_target = Random.ColorHSV(0f, 1f, 0.2f, 0.6f, 0.7f, 0.8f);

        backgroundTexture.SetPixels(new Color[] { color1, color2 });
        backgroundTexture.Apply();
        img.texture = backgroundTexture;
    }

    private void SetColor(Color color1, Color color2)
    {
        backgroundTexture.SetPixels(new Color[] { color1, color2 });
        backgroundTexture.Apply();
    }

    public void SetTargetColors(Color color1_t, Color color2_t)
    {
        color1_target = color1_t;
        color2_target = color2_t;
    }

    public void FixedUpdate()
    {
        color1 = Color.Lerp(color1, color1_target, Time.deltaTime);
        color2 = Color.Lerp(color2, color2_target, Time.deltaTime);
        SetColor(color2, color1);
    }
}
