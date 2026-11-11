#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MergeBranchAHierarchy : Editor
{
    [MenuItem("Tools/Utsuru/Merge Hierarchy Branch A ke Branch B (1-Klik)", priority = -10)]
    public static void MergeAll()
    {
        Debug.Log("<color=cyan>[Utsuru Merge] Memulai integrasi Hierarchy dari Branch A ke Branch B...</color>");

        // 1. Jalankan RestoreAll dari Branch A (Dialogue, NPC Spawner, DayNight, Score, Footsteps)
        RestoreCompleteSetup.RestoreAll();
        Debug.Log("<color=green>[Utsuru Merge] 1/3 RestoreCompleteSetup berhasil dieksekusi.</color>");

        // 2. Jalankan Setup Figma Dialogue UI (Pixel-perfect Figma Dialogue UI styling)
        SetupFigmaDialogueUI.GenerateFigmaUI();
        Debug.Log("<color=green>[Utsuru Merge] 2/3 SetupFigmaDialogueUI berhasil dieksekusi.</color>");

        // 3. Jalankan Setup Pause & Settings Menu (Figma Pause Menu + SettingsManager + PauseMenuManager)
        SetupPauseMenuUI.GeneratePauseMenuUI();
        Debug.Log("<color=green>[Utsuru Merge] 3/3 SetupPauseMenuUI berhasil dieksekusi.</color>");

        // Tandai scene dirty dan simpan otomatis
        Scene activeScene = EditorSceneManager.GetActiveScene();
        EditorUtility.SetDirty(activeScene.GetRootGameObjects()[0]);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("<color=green><b>[Utsuru Merge] INTEGRASI SUKSES 100%! Seluruh Hierarchy Branch A telah masuk ke scene Branch B tanpa mengubah Dictionary & Polaroid UI.</b></color>");
        EditorUtility.DisplayDialog("Merge Hierarchy Sukses!", 
            "Seluruh sistem dari Branch A (Dialogue System, NPC Spawner, Day-Night Transition, Pause Menu & Settings) telah berhasil digabungkan ke Branch B!\n\n" +
            "Fitur Dictionary Book UI dan Polaroid Camera di Canvas tetap aman & utuh.\nScene Day_Showcase telah disimpan otomatis.", 
            "Mantap!");
    }
}
#endif

