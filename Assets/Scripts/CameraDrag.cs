using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDrag : MonoBehaviour
{
    public Vector2 xLimits;
    public GameObject MoveCameralGameObject;

    private Vector3 initWorldPoint;
    private Vector3 cameraScreenStart;
    
    [HideInInspector]
    public static bool dragging;

    private void Awake()
    {
        GameServices.Initialize(null);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragging = false;
            cameraScreenStart = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            if (!dragging)
            {
                var delta = Input.mousePosition - cameraScreenStart;
                if (delta.sqrMagnitude > Screen.width * 0.05f)
                {
                    dragging = true;
                    initWorldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                }
            }
            else
            {
                var currentWorldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var delta = initWorldPoint - currentWorldPoint;
                if (Mathf.Abs(delta.x) > Mathf.Epsilon)
                {
                    var cameraPos = MoveCameralGameObject.transform.position;
                    cameraPos.x = Mathf.Clamp(cameraPos.x + delta.x, xLimits.x, xLimits.y);
                    MoveCameralGameObject.transform.position = cameraPos;
                }

                initWorldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);;
            }
        }
    }
}
