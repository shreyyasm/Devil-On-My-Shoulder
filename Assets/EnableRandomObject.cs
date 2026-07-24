using System.Collections.Generic;
using UnityEngine;

public class EnableRandomObject : MonoBehaviour
{
    [SerializeField] private List<GameObject> objects = new List<GameObject>();

    private void OnEnable()
    {
        if (objects == null || objects.Count == 0)
            return;

        // Disable all objects first
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // Pick one random object and enable it
        int randomIndex = Random.Range(0, objects.Count);

        if (objects[randomIndex] != null)
            objects[randomIndex].SetActive(true);
    }
}