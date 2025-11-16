using UnityEngine;
using System.Collections;
using Dreamteck.Splines;

public class LookUp : MonoBehaviour
{
    [Header("視点設定")]
    [Tooltip("見上げる目標の角度 (ローカルX軸の回転。通常、上を向くのはマイナス値です。例: -30)")]
    public float targetLookUpAngle = -30f;

    [Tooltip("目標の角度で待機する時間 (秒)")]
    public float waitTime = 2.0f;

    [Header("速度設定")]
    [Tooltip("見上げる際の回転速度 (度/秒)")]
    public float lookUpSpeed = 20f;

    [Tooltip("元の視点に戻る際の回転速度 (度/秒)")]
    public float lookDownSpeed = 40f;

    [SerializeField]
    SplineFollower follower;
    [SerializeField]
    CameraMover cameraMover;

    float originalSpeed;

    // --- プライベート変数 ---
    private Quaternion initialRotation; // 演出開始時の元の回転
    private bool isSequenceRunning = false; // 演出が実行中かどうかのフラグ


    private void Start()
    {
        follower = follower.GetComponent<SplineFollower>();
        cameraMover = cameraMover.GetComponent<CameraMover>();
    }

    public void StartLookUp()
    {
        // 既に演出が実行中の場合は、新しく開始しない
        if (isSequenceRunning)
        {
            Debug.LogWarning("視点演出は既に実行中です。");
            return;
        }

        originalSpeed = follower.followSpeed;
        follower.followSpeed = 0;
        follower.applyDirectionRotation = false;
        follower.follow = false;

        // コルーチンを開始
        StartCoroutine(LookUpAndWaitCoroutine());
    }

    /// <summary>
    /// 見上げる -> 待機 -> 戻る の一連の処理を行うコルーチン
    /// </summary>
    private IEnumerator LookUpAndWaitCoroutine()
    {
        isSequenceRunning = true;

        // 1. 現在のローカル回転を「元の視点」として保存
        initialRotation = transform.localRotation;

        // 元のローカル角度(Euler)を取得
        // (transform.localEulerAngles は 0-360 の範囲で値を返します)
        float initialAngleX = transform.localEulerAngles.x;
        float initialAngleY = transform.localEulerAngles.y;
        float initialAngleZ = transform.localEulerAngles.z;

        // 2. 目標の角度(Euler)を設定
        //    (YとZは元の角度を維持)
        float targetAngleX = targetLookUpAngle; // Inspectorで設定した値 (例: -30)

        Debug.Log($"演出開始: 現在X {initialAngleX} -> 目標X {targetAngleX}");

        // 3. 見上げる (目標角度になるまで)
        // Mathf.DeltaAngle は 359度 と -1度 の差を 0度 と正しく計算してくれます
        while (Mathf.Abs(Mathf.DeltaAngle(transform.localEulerAngles.x, targetAngleX)) > 0.01f)
        {
            // 現在のX角度から目標のX角度へ、指定した速度で向かう「次のフレームの角度」を計算
            float newAngleX = Mathf.MoveTowardsAngle(
                transform.localEulerAngles.x,
                targetAngleX,
                lookUpSpeed * Time.deltaTime
            );

            // 現在のX角度と新しいX角度の「差分 (delta)」を計算
            float deltaAngleX = Mathf.DeltaAngle(transform.localEulerAngles.x, newAngleX);

            // 差分だけローカルX軸周りに回転させる (Space.Self)
            // これが transform.Rotate を使った実装です
            transform.Rotate(deltaAngleX, 0, 0, Space.Self);

            yield return null; // 次のフレームまで待機
        }

        // 角度を正確に目標値に設定 (YとZは元の角度を維持)
        transform.localRotation = Quaternion.Euler(targetAngleX, initialAngleY, initialAngleZ);

        Debug.Log("目標角度に到達。待機開始。");

        // 4. 指定秒数待機
        yield return new WaitForSeconds(waitTime);

        Debug.Log("待機終了。元の角度へ復帰。");

        // 5. 元に戻る (保存した initialAngleX に戻る)
        while (Mathf.Abs(Mathf.DeltaAngle(transform.localEulerAngles.x, initialAngleX)) > 0.01f)
        {
            float newAngleX = Mathf.MoveTowardsAngle(
                transform.localEulerAngles.x,
                initialAngleX,
                lookDownSpeed * Time.deltaTime
            );

            float deltaAngleX = Mathf.DeltaAngle(transform.localEulerAngles.x, newAngleX);

            // 差分だけローカルX軸周りに回転させる
            transform.Rotate(deltaAngleX, 0, 0, Space.Self);

            yield return null; // 次のフレームまで待機
        }

        // 角度を正確に元の値に戻す
        transform.localRotation = initialRotation;

        // 実行中フラグを解除
        isSequenceRunning = false;
        follower.followSpeed = originalSpeed;
        follower.applyDirectionRotation = true;
        follower.follow = true;

        if (cameraMover != null)
        {
            Debug.Log("LookUpSequence 完了。カメラムーバーにシグナルを送信。");
            cameraMover.OnEventCompleted();
        }
    }
}