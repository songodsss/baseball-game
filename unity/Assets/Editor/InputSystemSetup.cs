using System;
using UnityEditor;
using UnityEngine;

namespace BaseballGame.EditorTools
{
    /// <summary>
    /// Switches the project to the new Input System (Player Settings > Active Input Handling), so the
    /// Input Manager deprecation warning goes away without changing settings by hand. Unity only
    /// picks the change up after the editor restarts, so it offers to restart.
    /// </summary>
    static class InputSystemSetup
    {
        const string AskedKey = "BaseballGame.InputSystemAsked";
        /// <summary>Active Input Handling: 0 = Input Manager (old), 1 = Input System (new), 2 = both.</summary>
        const int NewInputSystem = 1;

        [InitializeOnLoadMethod]
        static void OnLoad() => EditorApplication.delayCall += Apply;

        static void Apply()
        {
            // Only once per editor session, and only once the package itself is installed.
            if (SessionState.GetBool(AskedKey, false)) return;
            if (Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem") == null) return;
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var settings = new SerializedObject(assets[0]);
            var handler = settings.FindProperty("activeInputHandler");
            if (handler == null || handler.intValue == NewInputSystem) return;

            SessionState.SetBool(AskedKey, true);
            handler.intValue = NewInputSystem;
            settings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[입력] 새 Input System으로 전환했어요. 에디터를 다시 시작하면 적용돼요.");
            if (EditorUtility.DisplayDialog("입력 방식 전환",
                    "예전 Input Manager 대신 새 Input System을 쓰도록 바꿨어요.\n에디터를 다시 시작해야 적용돼요.",
                    "지금 다시 시작", "나중에"))
                EditorApplication.OpenProject(Environment.CurrentDirectory);
        }
    }
}
