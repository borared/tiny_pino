
using UnityEngine;

public class BulbLight : MonoBehaviour
{
    private Light bulbLight;

    void Start()
    {
        bulbLight = GetComponent<Light>();

        if (bulbLight == null)
        {
            bulbLight = gameObject.AddComponent<Light>();
        }

        bulbLight.type = LightType.Point;
        bulbLight.color = new Color(1f, 0.05f, 0.25f);
        bulbLight.intensity = 8f;
        bulbLight.range = 10f;
        bulbLight.shadows = LightShadows.Soft;
        bulbLight.renderMode = LightRenderMode.ForcePixel;
        bulbLight.enabled = true;
    }
}
