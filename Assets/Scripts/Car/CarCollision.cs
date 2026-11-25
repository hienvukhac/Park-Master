using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class CarCollision : MonoBehaviour
{
    [Header("Hiệu ứng va chạm")]
    [SerializeField] private GameObject crashFxPrefab;

    public bool IsCrashed { get; private set; } = false;

    private Rigidbody rig;
    private BoxCollider boxCollider;

    public event Action<CarController, Vector3> OnCarCrashedEvent;
    private void Awake()
    {
        rig = this.GetComponent<Rigidbody>();
        boxCollider = this.GetComponent<BoxCollider>();
        ResetPhysicState();
    }

    public void ResetPhysicState()
    {
        IsCrashed = false;
        if(rig != null)
        {
            if (!rig.isKinematic)
            {
                rig.linearVelocity = Vector3.zero;
                rig.angularVelocity = Vector3.zero;
            }

            rig.isKinematic = true;
            rig.useGravity = false;
        }

        if(boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }
    }

    public void TriggerCrash(CarController otherCar, Vector3 contactPoint)
    {
        if (IsCrashed) return;
        IsCrashed = true;

        if (boxCollider != null) boxCollider.isTrigger = false;

        if(rig != null)
        {
            rig.isKinematic = false;
            rig.useGravity = true;
            rig.constraints = RigidbodyConstraints.None;

            Vector3 otherPos = (otherCar != null) ? otherCar.transform.position : contactPoint;
            Vector3 awayDir = this.transform.position - otherPos;
            awayDir.y = 0;
            awayDir = (awayDir.sqrMagnitude < 0.01f) ? (this.transform.right + Vector3.forward * 0.3f).normalized : awayDir.normalized;

            float flingForce = UnityEngine.Random.Range(7.5f, 10.0f);
            float upForce = UnityEngine.Random.Range(4.8f, 6.5f);
            Vector3 impulse = awayDir * flingForce + Vector3.up * upForce;
            rig.linearVelocity = Vector3.zero;
            rig.angularVelocity = Vector3.zero;
            rig.AddForce(impulse, ForceMode.Impulse);

            Vector3 torque = new Vector3(
                UnityEngine.Random.Range(-18f, 18f),
                UnityEngine.Random.Range(-28f, 28f),
                UnityEngine.Random.Range(-18f, 18f)
            );
            rig.AddTorque(torque, ForceMode.Impulse);
        }
        SpawnCrashEffects(contactPoint != Vector3.zero ? contactPoint : this.transform.position);

        OnCarCrashedEvent?.Invoke(otherCar, contactPoint);
    }
    private void SpawnCrashEffects(Vector3 pos)
    {
        if (crashFxPrefab != null)
        {
            Vector3 spawnPos = pos + Vector3.up * 0.4f;
            GameObject fx = ObjectPool.Spawn(crashFxPrefab, spawnPos, Quaternion.identity);
            if (fx != null)
            {
                ObjectPool.Despawn(fx, 2.5f);
            }
            else
            {
                GameObject instantiatedFx = Instantiate(crashFxPrefab, spawnPos, Quaternion.identity);
                Destroy(instantiatedFx, 2.5f);
            }
        }
    }
}
