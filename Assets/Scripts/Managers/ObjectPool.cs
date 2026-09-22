using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    private static ObjectPool instance;
    public static ObjectPool Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject poolRoot = new GameObject("--- OBJECT_POOL ---");
                instance = poolRoot.AddComponent<ObjectPool>();
                DontDestroyOnLoad(poolRoot);
            }
            return instance;
        }
    }

    private readonly Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefabMap = new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        Instance.InternalPrewarm(prefab, count);
    }

    private void InternalPrewarm(GameObject prefab, int count)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();
        }

        Queue<GameObject> queue = poolDictionary[prefab];
        while (queue.Count < count)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            instanceToPrefabMap[obj] = prefab;
            queue.Enqueue(obj);
        }
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;
        return Instance.InternalSpawn(prefab, position, rotation, parent);
    }

    private GameObject InternalSpawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();
        }

        Queue<GameObject> queue = poolDictionary[prefab];
        GameObject obj = null;

        while (queue.Count > 0)
        {
            GameObject candidate = queue.Dequeue();
            if (candidate != null)
            {
                obj = candidate;
                break;
            }
        }

        if (obj == null)
        {
            obj = Instantiate(prefab);
            instanceToPrefabMap[obj] = prefab;
        }

        obj.transform.SetParent(parent != null ? parent : transform);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        return obj;
    }

    public static void Despawn(GameObject instance, float delay = 0f)
    {
        if (instance == null) return;
        if (delay <= 0f)
        {
            Instance.InternalDespawn(instance);
        }
        else
        {
            Instance.StartCoroutine(Instance.DespawnRoutine(instance, delay));
        }
    }

    private IEnumerator DespawnRoutine(GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (instance != null && instance.activeSelf)
        {
            InternalDespawn(instance);
        }
    }

    private void InternalDespawn(GameObject instance)
    {
        if (instance == null) return;

        ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        instance.SetActive(false);
        instance.transform.SetParent(transform);

        if (instanceToPrefabMap.TryGetValue(instance, out GameObject prefab))
        {
            if (!poolDictionary.ContainsKey(prefab))
            {
                poolDictionary[prefab] = new Queue<GameObject>();
            }
            poolDictionary[prefab].Enqueue(instance);
        }
        else
        {
            instance.SetActive(false);
        }
    }
}
