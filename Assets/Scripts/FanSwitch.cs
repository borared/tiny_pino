using UnityEngine;

public class FanSwitch : MonoBehaviour
{
    [SerializeField] private FanSpin fan;
    [SerializeField] private Vector3 flipAngles = new Vector3(0f, 0f, 90f);
    [SerializeField] private Transform leverPivot;

    private Quaternion offRotation;

    public bool IsOn => fan != null && fan.IsPowered;

    void Awake()
    {
        if (leverPivot != null)
            offRotation = leverPivot.localRotation;
    }

    public bool IsInRange(Vector3 playerPosition, float distance)
    {
        return isActiveAndEnabled && fan != null && leverPivot != null &&
            Vector3.Distance(playerPosition, leverPivot.position) <= distance;
    }

    public void Toggle()
    {
        if (fan == null || leverPivot == null)
            return;

        bool turnOn = !IsOn;
        leverPivot.localRotation = turnOn
            ? offRotation * Quaternion.Euler(flipAngles)
            : offRotation;

        fan.IsPowered = turnOn;
    }

    public void FlipUp()
    {
        if (!IsOn)
            Toggle();
    }
}