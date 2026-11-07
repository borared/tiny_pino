using UnityEngine;

public class FanSwitch : MonoBehaviour
{  
    [SerializeField] private FanSpin fan;
    [SerializeField] private Vector3 flipAngles = new Vector3(0f, 0f, 90f);
    [SerializeField] private Transform leverPivot;

    public void FlipUp()
    {
        if (fan == null || fan.enabled)
            return;

        leverPivot.localRotation *= Quaternion.Euler(flipAngles);

        fan.enabled = true;
    }
}