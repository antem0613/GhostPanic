using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using WiimoteApi;

public class TestWiimote : MonoBehaviour
{
    [SerializeField]
    GameObject[] ir_objects;
    public RectTransform[] ir_pointer;

    Wiimote wiimote1;
    Wiimote wiimote2;
    bool isPressedB1 = false;
    bool isPressedB2 = false;
    bool isPressedA1 = false;
    bool isPressedA2 = false;
    bool isRegistered1 = false;
    bool isRegistered2 = false;

    void Start()
    {
        WiimoteManager.FindWiimotes();

        if (ir_pointer != null)
        {
            foreach (var pointer in ir_pointer)
            {
                if (pointer != null)
                    pointer.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (!WiimoteManager.HasWiimote()) 
        {
            WiimoteManager.FindWiimotes();
            return; 
        }

        wiimote1 = WiimoteManager.Wiimotes[0];
        
        if (WiimoteManager.Wiimotes.Count > 1)
        {
            wiimote2 = WiimoteManager.Wiimotes[1];
        }

        int ret1,ret2 = 0;
        do
        {
            ret1 = wiimote1.ReadWiimoteData();
            if (wiimote2 != null)
            {
                ret2 = wiimote2.ReadWiimoteData();
            }
        } while (ret1 > 0 || ret2 > 0);

        if (wiimote1.Button.a && !isPressedA1)
        {
            isPressedA1 = true;
            isRegistered1 = true;
            if (ir_pointer[0] != null)
                ir_pointer[0].gameObject.SetActive(true);
        }
        else if (!wiimote1.Button.a && isPressedA1)
        {
            isPressedA1 = false;
        }

        if (isRegistered1)
        {
            float[] pointer1 = wiimote1.Ir.GetPointingPosition();
            ir_pointer[0].anchorMin = new Vector2(pointer1[0], pointer1[1]);
            ir_pointer[0].anchorMax = new Vector2(pointer1[0], pointer1[1]);

            if (wiimote1.Button.b && !isPressedB1)
            {
                isPressedB1 = true;
                Shot(new Vector2(pointer1[0] * Screen.width, pointer1[1] * Screen.height));
            }
            else if (isPressedB1)
            {
                isPressedB1 = false;
            }
        }

        if (wiimote2 != null)
        {
            if (wiimote2.Button.a && !isPressedA2)
            {
                isPressedA2 = true;
                isRegistered2 = true;
                if (ir_pointer[1] != null)
                    ir_pointer[1].gameObject.SetActive(true);
            }
            else if (!wiimote2.Button.a && isPressedA2)
            {
                isPressedA2 = false;
            }

            if (isRegistered2)
            {
                float[] pointer2 = wiimote2.Ir.GetPointingPosition();
                ir_pointer[1].anchorMin = new Vector2(pointer2[0], pointer2[1]);
                ir_pointer[1].anchorMax = new Vector2(pointer2[0], pointer2[1]);

                if (wiimote2.Button.b && !isPressedB2)
                {
                    isPressedB2 = true;
                    Shot(new Vector2(pointer2[0] * Screen.width, pointer2[1] * Screen.height));
                }
                else if (isPressedB2)
                {
                    isPressedB2 = false;
                }
            }
        }
    }

    void Shot(Vector2 pos)
    {
        Ray ray = Camera.main.ScreenPointToRay(pos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, 1 << 6))
        {
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            enemy?.TakeDamage(1);
        }
    }
}
