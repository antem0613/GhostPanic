using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerBullets : MonoBehaviour{

    [SerializeField] GameObject bulletPrefab, battery; //弾のプレハブ、銃口のオブジェクト

    Camera cam;             //メインカメラ

    void Start()
    {
        cam = Camera.main;      //メインカメラを取得し代入
    }

    void Update()
    {
        //マウスが左クリックされたとき
        if(Input.GetMouseButtonDown(0))
        {
            //マウスの座標を取得
            var mousePos=Input.mousePosition;
            //カメラからの奥行きを10に設定
            mousePos.z = 10.0f;
            // スクリーン座標をワールド座標に変換
            var targetPos = cam.ScreenToWorldPoint(mousePos);
            // 弾を生成
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            // クリックした位置へのベクトルを取得し標準化
            var dir = (targetPos - battery.transform.position).normalized;
            // Rigidbodyに力を加える（クリックした座標に2000の力で飛ばす）
            bullet.GetComponent<Rigidbody>().AddForce(dir * 3000);
            //弾を3秒後に削除
            Destroy(bullet, 3.0f);
        }
    }
}
