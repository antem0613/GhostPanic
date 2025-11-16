using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffect : MonoBehaviour
{
     private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Bullet"))
        {
            //敵(スクリプトがアタッチされているオブジェクト自身)を削除
            Destroy(gameObject);
            //弾(引数オブジェクト)を削除
            Destroy(collision.gameObject);
        }
    }
}
