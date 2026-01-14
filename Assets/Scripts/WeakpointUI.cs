using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WeakpointUI : MonoBehaviour
{
    public static WeakpointUI Instance { get; private set; }
    public GameObject weakPointMarkerPrefab;
    public Camera mainCamera;
    public Vector2 screenOffset = Vector2.zero;

    private Dictionary<Transform, GameObject> markerInstances = new Dictionary<Transform, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    public void AddTarget(Transform newTarget)
    {
        if (weakPointMarkerPrefab == null) return;

        if (markerInstances.ContainsKey(newTarget)) return;

        GameObject newMarker = Instantiate(weakPointMarkerPrefab, this.transform);

        markerInstances.Add(newTarget, newMarker);

        Debug.Log($"WeakPointUI がターゲットを追加: {newTarget.name}");
    }

    public void RemoveTarget(Transform targetToRemove)
    {
        if (markerInstances.ContainsKey(targetToRemove))
        {
            Destroy(markerInstances[targetToRemove]);
            markerInstances.Remove(targetToRemove);

            Debug.Log($"WeakPointUI がターゲットを削除: {targetToRemove.name}");
        }
    }

    void LateUpdate()
    {
        if (markerInstances.Count == 0) return;

        foreach (var pair in markerInstances)
        {
            Transform target = pair.Key;
            GameObject marker = pair.Value;
            RectTransform markerRect = marker.GetComponent<RectTransform>();

            if (target == null || !target.gameObject.activeInHierarchy)
            {
                if (marker.activeSelf) marker.SetActive(false);
                continue;
            }

            Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);

            if (screenPos.z < 0)
            {
                if (marker.activeSelf) marker.SetActive(false);
            }
            else
            {
                if (!marker.activeSelf) marker.SetActive(true);

                markerRect.position = new Vector2(screenPos.x, screenPos.y) + screenOffset;
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}