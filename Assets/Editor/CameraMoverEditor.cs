using UnityEngine;
using UnityEditor;
using UnityEditorInternal; // Required for ReorderableList
using Dreamteck.Splines; // Required for SplineComputer field

// This attribute tells Unity to use this Editor for the SplineCameraMover component
[CustomEditor(typeof(CameraMover))]
public class eCameraMoverEditor : Editor
{
    private SerializedProperty splineProp;
    private SerializedProperty encountersProp;
    private SerializedProperty destroyEnemiesProp;

    private ReorderableList encountersList;

    private void OnEnable()
    {
        // Find the properties we want to edit by their variable name in SplineCameraMover.cs
        splineProp = serializedObject.FindProperty("spline");
        encountersProp = serializedObject.FindProperty("encounters");
        destroyEnemiesProp = serializedObject.FindProperty("destroyEnemiesOnArrival");

        // --- Set up the ReorderableList ---
        encountersList = new ReorderableList(serializedObject, encountersProp,
                                            true, // draggable
                                            true, // displayHeader
                                            true, // displayAddButton
                                            true  // displayRemoveButton
                                            );

        // Header drawing callback
        encountersList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Encounters");
        };

        // Element drawing callback (how each item in the list looks)
        encountersList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty element = encountersList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2; // 少しパディング

            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;

            // --- 1行目: Node | Type | Speed | Spawner ---
            float firstLineWidth = rect.width;
            Rect nodeIndexRect = new Rect(rect.x, rect.y, firstLineWidth * 0.15f, singleLineHeight); // 幅調整
            Rect typeRect = new Rect(nodeIndexRect.xMax + 5, rect.y, firstLineWidth * 0.20f, singleLineHeight); // 幅調整
            Rect speedLabelRect = new Rect(typeRect.xMax + 10, rect.y, 40f, singleLineHeight);
            Rect speedRect = new Rect(speedLabelRect.xMax, rect.y, firstLineWidth * 0.15f, singleLineHeight); // 幅調整
            Rect spawnerRect = new Rect(speedRect.xMax + 5, rect.y, firstLineWidth - (speedRect.xMax + 5 - rect.x), singleLineHeight); // 残り

            // (各プロパティの取得と null チェックは適宜行う)
            EditorGUI.PropertyField(nodeIndexRect, element.FindPropertyRelative("nodeIndex"), GUIContent.none);
            EditorGUI.PropertyField(typeRect, element.FindPropertyRelative("encounterType"), GUIContent.none);
            EditorGUI.LabelField(speedLabelRect, "Speed:");
            EditorGUI.PropertyField(speedRect, element.FindPropertyRelative("followSpeed"), GUIContent.none);
            EditorGUI.PropertyField(spawnerRect, element.FindPropertyRelative("spawner"), GUIContent.none);

            // --- 2行目以降: UnityEvent フィールド ---
            Rect eventRect = new Rect(rect.x, rect.y + singleLineHeight + verticalSpacing, rect.width, EditorGUI.GetPropertyHeight(element.FindPropertyRelative("onNodeReachedAction")));
            EditorGUI.PropertyField(eventRect, element.FindPropertyRelative("onNodeReachedAction"), new GUIContent("On Reached Action")); // ラベル付きで描画
        };

        // Element height callback (can make elements taller if needed)
        encountersList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty element = encountersList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2; // Add a little vertical padding

            // Calculate heights and positions for fields
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            float height = singleLineHeight + verticalSpacing;
            SerializedProperty eventProp = element.FindPropertyRelative("onNodeReachedAction");
            height += EditorGUI.GetPropertyHeight(eventProp, true);

            // --- Draw Node Index ---
            Rect nodeIndexRect = new Rect(rect.x, rect.y, rect.width * 0.2f, singleLineHeight);
            SerializedProperty nodeIndexProp = element.FindPropertyRelative("index");
            if (nodeIndexProp != null) // <-- ADD NULL CHECK
            {
                EditorGUI.PropertyField(nodeIndexRect, nodeIndexProp, GUIContent.none);
            }
            else { EditorGUI.LabelField(nodeIndexRect, "Err: index"); } // Show error if null


            // --- Draw Encounter Type ---
            Rect typeRect = new Rect(nodeIndexRect.xMax + 5, rect.y, rect.width * 0.25f, singleLineHeight);
            SerializedProperty typeProp = element.FindPropertyRelative("encounterType");
            if (typeProp != null) // <-- ADD NULL CHECK
            {
                EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
            }
            else { EditorGUI.LabelField(typeRect, "Err: type"); } // Show error if null


            // --- Draw Follow Speed ---
            Rect speedLabelRect = new Rect(typeRect.xMax + 10, rect.y, 40f, singleLineHeight);
            EditorGUI.LabelField(speedLabelRect, "Speed:");
            Rect speedRect = new Rect(speedLabelRect.xMax, rect.y, rect.width * 0.15f, singleLineHeight);
            SerializedProperty speedProp = element.FindPropertyRelative("followSpeed");
            if (speedProp != null) // <-- ADD NULL CHECK
            {
                EditorGUI.PropertyField(speedRect, speedProp, GUIContent.none);
            }
            else { EditorGUI.LabelField(speedRect, "Err: speed"); } // Show error if null


            // --- Draw Spawner Object Field ---
            Rect spawnerRect = new Rect(speedRect.xMax + 5, rect.y, rect.width - (speedRect.xMax + 5 - rect.x), singleLineHeight);
            SerializedProperty spawnerProp = element.FindPropertyRelative("spawner");
            if (spawnerProp != null) // <-- ADD NULL CHECK
            {
                EditorGUI.PropertyField(spawnerRect, spawnerProp, GUIContent.none);
            }
            else { EditorGUI.LabelField(spawnerRect, "Err: spawner"); } // Show error if null

            // --- Draw UnityEvent Field ---
            Rect eventRect = new Rect(rect.x, rect.y + singleLineHeight + verticalSpacing, rect.width, EditorGUI.GetPropertyHeight(eventProp, true));
            EditorGUI.PropertyField(eventRect, eventProp, new GUIContent("On Reached Action")); // Draw with label

        };

        encountersList.elementHeightCallback = (int index) => {
            SerializedProperty element = encountersList.serializedProperty.GetArrayElementAtIndex(index);
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;

            // 1行目の高さ
            float height = singleLineHeight + verticalSpacing;

            // UnityEventフィールドの高さを取得して加算
            SerializedProperty eventProp = element.FindPropertyRelative("onNodeReachedAction");
            height += EditorGUI.GetPropertyHeight(eventProp, true); // true で子要素も考慮

            height += verticalSpacing * 2; // 上下のパディング
            return height;
        };
    }

    public override void OnInspectorGUI()
    {
        // Update the serializedObject representation (important!)
        serializedObject.Update();

        // Draw the Spline Computer field
        EditorGUILayout.PropertyField(splineProp);
        EditorGUILayout.Space(); // Add some space

        // Draw the ReorderableList for encounters
        encountersList.DoLayoutList();
        EditorGUILayout.Space(); // Add some space

        // Draw the Destroy Enemies On Arrival toggle
        EditorGUILayout.PropertyField(destroyEnemiesProp);

        // Apply changes back to the actual component (important!)
        serializedObject.ApplyModifiedProperties();

        // Optional: Draw the default inspector for any fields not handled above
        // DrawDefaultInspector();
    }
}
