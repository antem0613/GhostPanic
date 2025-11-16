using UnityEngine;

public class SpawnCity : MonoBehaviour
{
    [SerializeField]
    GameObject city;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        city.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    
    }

    public void SpawnNewCity()
    {
        city.SetActive(true);
    }
}
