using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class bulletsController : MonoBehaviour {
 
    public GameObject bulletPrefab;
    public float shotSpeed;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            GameObject bullet = (GameObject)Instantiate(bulletPrefab, transform.position, Quaternion.Euler(transform.parent.eulerAngles.x, transform.parent.eulerAngles.y, 0));
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            bulletRb.AddForce(transform.forward * shotSpeed);
 
            //射撃されてから3秒後に銃弾のオブジェクトを破壊する.
            Destroy(bullet, 3.0f);
        }
    }
}