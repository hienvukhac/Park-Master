using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CarMovement : MonoBehaviour
{
    [Header("Tốc độ xe")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float rotationSpeed = 15f;

    public bool IsDriving { get; private set; } = false;

    private List<Vector3> currentPath = new List<Vector3>();
    private int currentPointIndex = 0;

    public event Action OnPathComplete;

    public void StartMovement(List<Vector3> path)
    {
        if (path == null || path.Count < 2) return;

        currentPath = path;
        currentPointIndex = 0;
        IsDriving = true;

        Vector3 initialDir = currentPath[1] - currentPath[0];
        initialDir.y = 0f;

        if (initialDir.sqrMagnitude > 0.001f)
        {
            this.transform.rotation = Quaternion.LookRotation(initialDir);
        }
    }

    public void StopMovement()
    {
        IsDriving = false;
        currentPointIndex = 0;
    }

    private void Update()
    {
        if (!IsDriving || currentPath == null || currentPointIndex >= currentPath.Count) return;

        Vector3 target = currentPath[currentPointIndex];
        target.y = this.transform.position.y;

        this.transform.position = Vector3.MoveTowards(this.transform.position, target, speed * Time.deltaTime);
        Vector3 direction = target - this.transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        float arriveThreshold = (currentPointIndex == currentPath.Count - 1) ? 0.04f : 0.12f;
        if (Vector3.Distance(this.transform.position, target) < arriveThreshold)
        {
            currentPointIndex++;

            if (currentPointIndex >= currentPath.Count)
            {
                IsDriving = false;
                OnPathComplete?.Invoke();
            }
        }
    }
}
