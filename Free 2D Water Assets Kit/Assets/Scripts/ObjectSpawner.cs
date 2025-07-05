using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class ObjectSpawner : MonoBehaviour
{

    public static ObjectSpawner instance;
    public GameObject Block;
    public float repeatRate;
    public bool IsPlaying;

    public void Update()
    {
        while (IsPlaying)
        {
            SpawnObject();
            break;
        }
        

    }

    private void SpawnObject()
    {
        InvokeRepeating("SpawnBlock", 1f, repeatRate);
    }

    private void SpawnBlock()
    {
        Vector3 spawnPos = transform.position;

        Instantiate(Block, spawnPos , Quaternion.identity);
    }
}
