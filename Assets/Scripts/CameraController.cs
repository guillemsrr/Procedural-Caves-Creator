using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private GameObject cam;
    private Vector3 newPosition;
    private float speed = 5f;
    private float zoomSpeed = 25f;
    private float moveSpeed = 200f;

    void Start()
    {
        newPosition = transform.position;
    }

    void FixedUpdate()
    {
        Zoom();
        Movement();

        cam.transform.LookAt(Vector3.zero);
        Rotate();
        transform.position = Vector3.Slerp(transform.position, newPosition, speed*Time.fixedDeltaTime);
    }

    private void Zoom()
    {
        newPosition += cam.transform.forward * Input.mouseScrollDelta.y * zoomSpeed;
    }

    private void Movement()
    {
        if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            newPosition += transform.up * moveSpeed * Time.fixedDeltaTime;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            newPosition -= transform.up * moveSpeed * Time.fixedDeltaTime;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            newPosition -= Vector3.Cross(newPosition.normalized,Vector3.up) * moveSpeed * Time.fixedDeltaTime;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            newPosition += Vector3.Cross(newPosition.normalized,Vector3.up) * moveSpeed * Time.fixedDeltaTime;
        }
    }

    private void Rotate()
    {
        Vector3 dir = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        transform.forward = dir.normalized;
    }
}
