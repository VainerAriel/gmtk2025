using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//skibidi
public class CameraFollow : MonoBehaviour
{

    public Transform followTransform;
    public BoxCollider2D mapBounds;

    private float xMin, xMax, yMin, yMax;
    private float camY,camX;
    private float camOrthsize;
    private float cameraXRatio;
    private float cameraYRatio;
    private Camera mainCam;
    private Vector3 smoothPos;
    public float smoothSpeed = 0.5f;

    private void Start()
    {
        xMin = mapBounds.bounds.min.x;
        xMax = mapBounds.bounds.max.x;
        yMin = mapBounds.bounds.min.y;
        yMax = mapBounds.bounds.max.y;
        mainCam = GetComponent<Camera>();
        camOrthsize = mainCam.orthographicSize;
        cameraXRatio = (xMax + camOrthsize) / 2.0f;
        cameraYRatio = (yMax + camOrthsize) / 2.5f;
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        // camY = Mathf.Clamp(followTransform.position.y, yMin + camOrthsize, yMax - camOrthsize);
        camY = Mathf.Clamp(followTransform.position.y, yMin + cameraYRatio, yMax - cameraYRatio);
        camX = Mathf.Clamp(followTransform.position.x, xMin + cameraXRatio, xMax - cameraXRatio);
        smoothPos = Vector3.Lerp(this.transform.position, new Vector3(camX, camY, this.transform.position.z), smoothSpeed);
        this.transform.position = smoothPos;
        
        
    }
}