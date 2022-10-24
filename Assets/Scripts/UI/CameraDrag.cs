using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDrag : MonoBehaviour
{
    public float dragSpeed = 6;
    private Vector3 dragOrigin;
    private Vector3 lastMousePos;
    private Vector3 cameraOrigin;
    public float minSize = 2f;
    public float maxSize = 20f;
    public float zoomSensitivity = 10f;
    private Camera myCamera;

    void Start()
    {
        myCamera = Camera.main;
    }

    void Update()
    {
        Drag();

        float size = myCamera.orthographicSize;
        size += Input.mouseScrollDelta.y * zoomSensitivity;
        // Debug.Log(Input.mouseScrollDelta.y);
        size = Mathf.Clamp(size, minSize, maxSize);
        //myCamera.orthographicSize = size;
    }

    void Drag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
            lastMousePos = Input.mousePosition;
            cameraOrigin = transform.position;
            return;
        }

        if (!Input.GetMouseButton(0)) return;

        //Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
        //Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.ScreenToWorldPoint(dragOrigin);
        Vector3 pos = Input.mousePosition - lastMousePos;
        //Debug.Log(pos);
        Vector3 move = new Vector3(-pos.x * dragSpeed, -pos.y * dragSpeed, 0);

        transform.position = cameraOrigin + move;

        lastMousePos = Input.mousePosition;
    }
}

