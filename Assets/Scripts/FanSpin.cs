using UnityEngine;

[DefaultExecutionOrder(-10)]
public class FanSpin : MonoBehaviour
{
    public float speed = 50f;

    [SerializeField] private bool startsPowered = true;
    [SerializeField, Min(0.01f)] private float coastSeconds = 2f;

    public bool IsPowered { get; set; }

    private float currentSpeed;

    void Awake()
    {
        IsPowered = startsPowered;
    }

    void Update()
    {
        if (IsPowered)
        {
            currentSpeed = speed;
        }
        else
        {
            float slowdown = Mathf.Abs(speed) / coastSeconds;
            currentSpeed = Mathf.MoveTowards(
                currentSpeed, 0f, slowdown * Time.deltaTime);
        }

        transform.Rotate(0f, currentSpeed * Time.deltaTime, 0f, Space.Self);
    }
}