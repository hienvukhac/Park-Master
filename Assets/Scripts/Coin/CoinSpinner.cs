using UnityEngine;

public class CoinSpinner : MonoBehaviour
{
    [Header("Cấu hình xoay tròn")]
    [Tooltip("Tốc độ xoay (độ/giây)")]
    [SerializeField] private float rotateSpeed = 300f;

    [Tooltip("Trục xoay")]
    [SerializeField] private Vector3 rotateAxis = Vector3.up;

    [Tooltip("Xoay theo tọa độ World để đảm bảo luôn quay đứng thẳng đều")]
    [SerializeField] private bool rotateInWorldSpace = true;

    [Header("Hiệu ứng nhấp nhô lơ lửng")]
    [Tooltip("Bật/Tắt hiệu ứng bồng bềnh lên xuống nhẹ nhàng")]
    [SerializeField] private bool enableBobbing = true;

    [Tooltip("Biên độ nhấp nhô (mét)")]
    [SerializeField] private float bobHeight = 0.08f;

    [Tooltip("Tốc độ nhấp nhô")]
    [SerializeField] private float bobSpeed = 2.5f;

    private Vector3 basePosition;

    private void Start()
    {
        basePosition = this.transform.position;
    }

    private void Update()
    {
        Space space = rotateInWorldSpace ? Space.World : Space.Self;
        transform.Rotate(rotateAxis * (rotateSpeed * Time.deltaTime), space);

        if (enableBobbing)
        {
            float newY = basePosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            Vector3 pos = transform.position;
            pos.y = newY;
            transform.position = pos;
        }
    }
}
