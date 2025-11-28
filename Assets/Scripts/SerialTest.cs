using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;
using System.Runtime.InteropServices;

public class SerialTest : MonoBehaviour
{
    [SerializeField] string portName = "COM3";
    [SerializeField] int baudRate = 115200;
    [SerializeField] bool dtrEnable = false;
    [SerializeField] bool rtsEnable = false;
    [SerializeField] int timeout = 1000;
    [SerializeField] int SamplingRate = 50;
    SerialPort sp;
    Thread readThread;
    int previous = 2;
    int count = 0;
    volatile bool running = false;
    int coinCount = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        coinCount = 0;
        count = 0;
        previous = 2;
        try
        {
            sp = new SerialPort(portName, baudRate)
            {
                NewLine = "\n",
                ReadTimeout = timeout,
                DtrEnable = dtrEnable,
                RtsEnable = rtsEnable,
            };

            sp.NewLine = "\n";
            sp.Open();

            StartCoroutine(DelayStartRead(1000));
            Debug.Log($"[Serial Opened {portName} @ {baudRate}");
        }
        catch(Exception e)
        {
            Debug.LogError($"[Serial] Open failed: {e.Message}");
        }
    }

    private void Open()
    {
        sp = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        sp.Open();
        running = true;

        StartCoroutine(DelayStartRead(1000));
    }

    System.Collections.IEnumerator DelayStartRead(int ms)
    {
        yield return new WaitForSeconds(ms/1000f);
        StartReadThread();
    }

    void StartReadThread()
    {
        if(sp == null || !sp.IsOpen)
        {
            return;
        }

        running = true;
        readThread = new Thread(ReadLoop);
        readThread.IsBackground = true;
        readThread.Start();
    }

    void ReadLoop()
    {
        while(running && sp != null && sp.IsOpen)
        {
            try
            {
                if (sp.BytesToRead > 0)
                {
                    int current = sp.ReadChar();
                    count++;
                    Debug.Log(count);

                    if(count == 9)
                    {
                        if(!Player.Instance.iscoinBlocking)   Player.Instance.AddCoin(1);
                        count = 0;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Serial] Read error: {e.Message}");
                Thread.Sleep(50);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendChar('0');
        }
    }

    void SendChar(char ch)
    {
        try
        {
            if(sp != null && sp.IsOpen)
            {
                sp.Write(new[] { ch }, 0, 1);
                Debug.Log($"[Serial TX] {ch}");
            }
        }
        catch(Exception e)
        {
            Debug.LogWarning($"[Serial] Write error: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        running = false;

        try
        {
            if(readThread != null && readThread.IsAlive)
            {
                readThread.Join(200);
                readThread = null;
            }
        }
        catch { }

        try
        {
            if(sp != null)
            {
                if (sp.IsOpen)
                {
                    sp.Close();
                }
                sp.Dispose();
            }
        }
        catch { }
    }
}
