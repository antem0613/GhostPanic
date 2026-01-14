using UnityEngine;
using System.Collections;
using Dreamteck.Splines;

public class LookUp : MonoBehaviour
{
    public float targetLookUpAngle = -30f;

    public float waitTime = 2.0f;

    public float lookUpSpeed = 20f;

    public float lookDownSpeed = 40f;

    [SerializeField]
    SplineFollower follower;
    [SerializeField]
    CameraMover cameraMover;

    float originalSpeed;

    private Quaternion initialRotation;
    private bool isSequenceRunning = false;


    private void Start()
    {
        follower = follower.GetComponent<SplineFollower>();
        cameraMover = cameraMover.GetComponent<CameraMover>();
    }

    public void StartLookUp()
    {
        if (isSequenceRunning)
        {
            return;
        }

        originalSpeed = follower.followSpeed;
        follower.followSpeed = 0;
        follower.applyDirectionRotation = false;
        follower.follow = false;

        StartCoroutine(LookUpAndWaitCoroutine());
    }
    private IEnumerator LookUpAndWaitCoroutine()
    {
        isSequenceRunning = true;

        initialRotation = transform.localRotation;

        float initialAngleX = transform.localEulerAngles.x;
        float initialAngleY = transform.localEulerAngles.y;
        float initialAngleZ = transform.localEulerAngles.z;

        float targetAngleX = targetLookUpAngle;

        while (Mathf.Abs(Mathf.DeltaAngle(transform.localEulerAngles.x, targetAngleX)) > 0.01f)
        {
            float newAngleX = Mathf.MoveTowardsAngle(
                transform.localEulerAngles.x,
                targetAngleX,
                lookUpSpeed * Time.deltaTime
            );

            float deltaAngleX = Mathf.DeltaAngle(transform.localEulerAngles.x, newAngleX);

            transform.Rotate(deltaAngleX, 0, 0, Space.Self);

            yield return null;
        }

        transform.localRotation = Quaternion.Euler(targetAngleX, initialAngleY, initialAngleZ);

        yield return new WaitForSeconds(waitTime);

        while (Mathf.Abs(Mathf.DeltaAngle(transform.localEulerAngles.x, initialAngleX)) > 0.01f)
        {
            float newAngleX = Mathf.MoveTowardsAngle(
                transform.localEulerAngles.x,
                initialAngleX,
                lookDownSpeed * Time.deltaTime
            );

            float deltaAngleX = Mathf.DeltaAngle(transform.localEulerAngles.x, newAngleX);

            transform.Rotate(deltaAngleX, 0, 0, Space.Self);

            yield return null;
        }

        transform.localRotation = initialRotation;

        isSequenceRunning = false;
        follower.followSpeed = originalSpeed;
        follower.applyDirectionRotation = true;
        follower.follow = true;

        if (cameraMover != null)
        {
            cameraMover.OnEventCompleted();
        }
    }
}