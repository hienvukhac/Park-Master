using UnityEngine;

public class CarStartPoint : MonoBehaviour
{
    [Header("Mã định danh ghép cặp")]
    [Tooltip("Màu của xe xuất phát tại điểm này (Khớp với SlotColorType của ô đỗ)")]
    public ColorType colorType = ColorType.Red;

    [Tooltip("Mã cặp kết nối trực tiếp với ô đỗ ParkingSlot (nếu > 0)")]
    public int pairId = 0;

    [HideInInspector] public CarController car;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        Color c = ColorPalette.GetColor(colorType);
        Gizmos.color = c;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(new Vector3(0, 0.45f, 0), new Vector3(1.4f, 0.8f, 2.5f));
        Gizmos.DrawCube(new Vector3(0, 0.45f, 0), new Vector3(1.35f, 0.75f, 2.45f) * 0.3f);

        Gizmos.color = Color.yellow;
        Vector3 forwardTip = Vector3.forward * 1.6f + Vector3.up * 0.45f;
        Gizmos.DrawLine(Vector3.up * 0.45f, forwardTip);
        Gizmos.DrawLine(forwardTip, forwardTip + new Vector3(-0.3f, 0, -0.4f));
        Gizmos.DrawLine(forwardTip, forwardTip + new Vector3(0.3f, 0, -0.4f));
    }
#endif
}