using UnityEngine;
using UnityEditor;
using UnityEditorInternal; // ReorderableList に必要
using System;
using System.Collections.Generic; // Listのために追加

[CustomEditor(typeof(WaveSpawner))]
public class WaveSpawnerEditor : Editor
{
    private SerializedProperty wavesProp;
    private SerializedProperty startOnAwakeProp;
    private SerializedProperty playerTransformProp;

    private ReorderableList wavesList;
    // 各Wave要素に対応する敵リストを保持するための辞書
    private Dictionary<string, ReorderableList> enemyLists = new Dictionary<string, ReorderableList>();

    private void OnEnable()
    {
        wavesProp = serializedObject.FindProperty("waves");
        startOnAwakeProp = serializedObject.FindProperty("startOnAwake");
        playerTransformProp = serializedObject.FindProperty("playerTransform");

        // --- Waves リストの設定 ---
        wavesList = new ReorderableList(serializedObject, wavesProp, true, true, true, true);

        wavesList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Waves");
        };

        wavesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty waveElement = wavesList.serializedProperty.GetArrayElementAtIndex(index);
            string elementPath = waveElement.propertyPath; // Wave要素の一意なパス

            rect.y += 2;
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;

            Rect delayLabelRect = new Rect(rect.x, rect.y, 45f, singleLineHeight); // ラベル幅を少し広めに
            Rect delayRect = new Rect(delayLabelRect.xMax, rect.y, rect.width - delayLabelRect.width, singleLineHeight); // 残りの幅を使用

            EditorGUI.LabelField(delayLabelRect, "Delay:"); // ラベル表示

            // delayBeforeThisWave の null チェックと描画 (位置調整済み)
            SerializedProperty delayProp = waveElement.FindPropertyRelative("delayBeforeThisWave");
            if (delayProp != null)
            {
                EditorGUI.PropertyField(delayRect, delayProp, GUIContent.none); // ラベルなしで値のみ表示
            }
            else { EditorGUI.LabelField(delayRect, "Err: delay"); }

            // --- EnemiesToSpawn リスト (2行目以降) ---
            SerializedProperty enemiesProp = waveElement.FindPropertyRelative("enemiesToSpawn");
            if (enemiesProp == null)
            {
                EditorGUI.LabelField(new Rect(rect.x, rect.y + singleLineHeight + verticalSpacing, rect.width, singleLineHeight), "Error: Could not find 'enemiesToSpawn'");
                return;
            }

            // (敵リストの取得・描画処理は変更なし)
            if (!enemyLists.ContainsKey(elementPath))
            {
                enemyLists[elementPath] = CreateEnemyList(enemiesProp, $"Wave {index} Enemies");
            }
            ReorderableList enemyList = enemyLists[elementPath];
            float listHeight = enemyList.GetHeight();
            Rect listRect = new Rect(rect.x, rect.y + singleLineHeight + verticalSpacing, rect.width, listHeight);
            enemyList.DoList(listRect);
        };

        wavesList.elementHeightCallback = (int index) => {
            SerializedProperty waveElement = wavesList.serializedProperty.GetArrayElementAtIndex(index);
            string elementPath = waveElement.propertyPath;
            SerializedProperty enemiesProp = waveElement.FindPropertyRelative("enemiesToSpawn");

            // 対応するリストがなければ仮生成して高さを計算
            if (!enemyLists.ContainsKey(elementPath))
            {
                enemyLists[elementPath] = CreateEnemyList(enemiesProp, $"Wave {index} Enemies");
            }
            ReorderableList enemyList = enemyLists[elementPath];

            // Wave名とDelayの1行 + 敵リストの高さ + 少しのパディング
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + enemyList.GetHeight() + (EditorGUIUtility.standardVerticalSpacing * 2);
        };

        // 要素が削除された時に辞書からも削除
        wavesList.onRemoveCallback = (ReorderableList list) =>
        {
            string elementPath = list.serializedProperty.GetArrayElementAtIndex(list.index).propertyPath;
            if (enemyLists.ContainsKey(elementPath))
            {
                enemyLists.Remove(elementPath);
            }
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
        };
    }

    // --- 敵リストを作成するヘルパーメソッド ---
    private ReorderableList CreateEnemyList(SerializedProperty enemiesProp, string headerText)
    {
        ReorderableList enemyList = new ReorderableList(enemiesProp.serializedObject, enemiesProp, true, true, true, true);

        enemyList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, headerText);
        };

        enemyList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty enemyElement = enemyList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            float halfWidth = rect.width / 2f - 5;
            float thirdWidth = rect.width / 3f - 5;
            float quarterWidth = rect.width / 4f - 5;

            // --- 1行目: Prefab | SpawnPoint ---
            Rect prefabRect = new Rect(rect.x, rect.y, halfWidth, singleLineHeight);
            Rect pointRect = new Rect(prefabRect.xMax + 5, rect.y, halfWidth, singleLineHeight);
            EditorGUI.PropertyField(prefabRect, enemyElement.FindPropertyRelative("enemyPrefab"), GUIContent.none);
            EditorGUI.PropertyField(pointRect, enemyElement.FindPropertyRelative("spawnPoint"), GUIContent.none);

            float secondLineY = rect.y + singleLineHeight + verticalSpacing;
            Rect triggerTypeRect = new Rect(rect.x, rect.y + singleLineHeight + verticalSpacing, thirdWidth, singleLineHeight);
            float valueLabelWidth = 55f; // "Delay:" や "Distance:" ラベルの幅
            float valueFieldWidth = thirdWidth - valueLabelWidth; // 残りを入力欄の幅に
            Rect valueLabelRect = new Rect(triggerTypeRect.xMax + 5, secondLineY, valueLabelWidth, singleLineHeight);
            Rect valueFieldRect = new Rect(valueLabelRect.xMax, secondLineY, valueFieldWidth, singleLineHeight);
            Rect valueRect = new Rect(triggerTypeRect.xMax + 5, rect.y + singleLineHeight + verticalSpacing, thirdWidth, singleLineHeight);
            float offsetLabelWidth = 45f; // "Offset:" ラベルの幅
            float offsetFieldWidth = thirdWidth - offsetLabelWidth; // 残りを入力欄の幅に
            Rect offsetLabelRect = new Rect(valueRect.xMax + 5, rect.y + singleLineHeight + verticalSpacing, offsetLabelWidth, singleLineHeight);
            Rect offsetFieldRect = new Rect(offsetLabelRect.xMax, rect.y + singleLineHeight + verticalSpacing, offsetFieldWidth, singleLineHeight);

            SerializedProperty triggerTypeProp = enemyElement.FindPropertyRelative("triggerType");
            EditorGUI.PropertyField(triggerTypeRect, triggerTypeProp, GUIContent.none);

            WaveSpawner.EnemySpawnInfo.SpawnTriggerType trigger = (WaveSpawner.EnemySpawnInfo.SpawnTriggerType)triggerTypeProp.enumValueIndex;
            if (trigger == WaveSpawner.EnemySpawnInfo.SpawnTriggerType.OnDelay)
            {
                SerializedProperty delayProp = enemyElement.FindPropertyRelative("spawnDelay");
                if (delayProp != null)
                {
                    // ラベルとフィールドを別々に描画
                    EditorGUI.LabelField(valueLabelRect, "Delay:");
                    delayProp.floatValue = EditorGUI.FloatField(valueFieldRect, GUIContent.none, delayProp.floatValue); // ラベルなし
                }
                else EditorGUI.LabelField(valueLabelRect, "Err: delay");
            }
            else // OnDistance
            {
                SerializedProperty distanceProp = enemyElement.FindPropertyRelative("spawnDistance");
                if (distanceProp != null)
                {
                    // ラベルとフィールドを別々に描画
                    EditorGUI.LabelField(valueLabelRect, "Distance:");
                    distanceProp.floatValue = EditorGUI.FloatField(valueFieldRect, GUIContent.none, distanceProp.floatValue); // ラベルなし
                }
                else EditorGUI.LabelField(valueLabelRect, "Err: distance");
            }

            SerializedProperty offsetProp = enemyElement.FindPropertyRelative("Offset");
            if (offsetProp != null)
            {
                // ラベルとフィールドを別々に描画
                EditorGUI.LabelField(offsetLabelRect, "Offset:"); // 短いラベルを表示
                EditorGUI.PropertyField(offsetFieldRect, offsetProp, GUIContent.none); // 入力欄のみ表示
            }
            else EditorGUI.LabelField(offsetLabelRect, "Err: offset");

        };

        enemyList.elementHeightCallback = (int index) => {
            // 2行分の高さ + パディング
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2 + (EditorGUIUtility.standardVerticalSpacing);
        };
        return enemyList;
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 標準フィールドを描画
        EditorGUILayout.PropertyField(startOnAwakeProp);
        EditorGUILayout.PropertyField(playerTransformProp);
        EditorGUILayout.Space();

        // Wavesリストを描画
        wavesList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();

        // DrawDefaultInspector(); // デバッグ用
    }
}