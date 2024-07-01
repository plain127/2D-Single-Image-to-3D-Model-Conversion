using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModelLoader : MonoBehaviour
{
    public void LoadModels(Dictionary<(string name, int count), (float x, float y)> detectedObjects)
    {
        StartCoroutine(LoadModelsInOutputScene(detectedObjects));
    }

    public IEnumerator LoadModelsInOutputScene(Dictionary<(string name, int count), (float x, float y)> detectedObjects)
    {
        SceneManager.LoadScene("Output", LoadSceneMode.Single);
        yield return new WaitForSeconds(1f);

        foreach (var key in detectedObjects.Keys)
        {
            GameObject modelPrefab = Resources.Load<GameObject>(key.name);

            if (modelPrefab != null)
            {
                GameObject modelInstance = Instantiate(modelPrefab);
                var value = detectedObjects[key];
                float y = value.y;
                float z = value.x;
                PositionAndScaleModel(modelInstance, key.name, key.count, y, z);
            }
        }
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void PositionAndScaleModel(GameObject modelInstance, string name, int count, float y, float z)
    {

        float scaleFactor = 10.0f;
        modelInstance.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

        Vector3 position = Vector3.zero;
        if (name == "frame")
        {
            position = new Vector3(0, y, z);
        }
        else if (name == "seatpost")
        {
            position = new Vector3(0, y, z);
        }
        else if (name == "handle")
        {
            position = new Vector3(0, y, z);
        }
        else if (name == "saddle")
        {
            position = new Vector3(0, y, z);
        }
        else if (name == "wheel")
        {
            position = new Vector3(0, y, z);
        }
        else if (name == "crank")
        {
            position = new Vector3(0, y, z);
        }
        else if (name == "chainring")
        {
            position = new Vector3(0, y, z);
        }
        else if (name == "chain")
        {
            position = new Vector3(0, y, z);
        }

        modelInstance.transform.position = position;

        float yRotation;
        if (name == "wheel" && count == 0)
        {
            yRotation = 52f;
        }
        else if (name == "wheel" && count > 0)
        {
            yRotation = 90f;
        }
        else
        {
            yRotation = 90f;
        }
        modelInstance.transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}

