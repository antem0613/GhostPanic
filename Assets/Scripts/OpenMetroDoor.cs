using UnityEngine;

public class OpenMetroDoor : MonoBehaviour
{
    Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenDoor()
    {
        animator.SetBool("OpenDoor",true);
    }
}
