using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPS{

  public class cameraController : MonoBehaviour {


    [Range(0.1f, 10f)]
    //カメラ感度、数値が大きいほどより直感的な操作が可能.
    public float lookSensitivity = 5f;
    [Range(0.1f, 1f)]
    //数値が大きいほどカメラが向きたい方向に向くまでの時間が長くなる.
    public float lookSmooth = 0.1f;

    public Vector2 MinMaxAngle = new Vector2(-65, 65);

    private float yRot;
    private float xRot;

    private float currentYRot;
    private float currentXRot;

    private float yRotVelocity;
    private float xRotVelocity;

    void Update()
    {

        yRot += Input.GetAxis("Mouse X") * lookSensitivity; //マウスの移動.
        xRot -= Input.GetAxis("Mouse Y") * lookSensitivity; //マウスの移動.


      
        xRot = Mathf.Clamp(xRot, MinMaxAngle.x, MinMaxAngle.y);//上下の角度移動の最大、最小.

       
        currentXRot = Mathf.SmoothDamp(currentXRot, xRot, ref xRotVelocity, lookSmooth);
        currentYRot = Mathf.SmoothDamp(currentYRot, yRot, ref yRotVelocity, lookSmooth);

        transform.rotation = Quaternion.Euler(currentXRot, currentYRot, 0);


    }
  }
}

