using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TunnelerAnimScript : MonoBehaviour
{
    [HideInInspector]
    public static TunnelerAnimScript instance;

    [SerializeField] private Animator bridgeAnimator;
    [SerializeField] private Animator lDoorAnimator;
    [SerializeField] private Animator rDoorAnimator;


    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        OpenTunneler();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenTunneler()
    {
        bridgeAnimator.SetBool("Open", true);
        lDoorAnimator.SetBool("Open", true);
        rDoorAnimator.SetBool("Open", true);

        Invoke("DoorSound", 0.85f);
    }

    public void CloseTunneler()
    {
        bridgeAnimator.SetBool("Open", false);
        lDoorAnimator.SetBool("Open", false);
        rDoorAnimator.SetBool("Open", false);
    }
}
