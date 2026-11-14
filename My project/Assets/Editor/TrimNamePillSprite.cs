#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class TrimNamePillSprite : Editor
{
    [MenuItem("Tools/Utsuru/1. Perbaiki Semua (English NPC, Romaji Choice, NamePill & RolePill)", priority = 0)]
    public static void FixPillsAndScoreBadge()
    {
        // 0. Update Semua Dialog: Terjemahan NPC jadi English & Jawaban Pilihan jadi Romaji
        InjectNPCResponses.UpdateAllDialogue();

        Color32 fillColor = new Color32(58, 38, 26, 255);    // Dark brown Figma (#3A261A)
        Color32 strokeColor = new Color32(115, 82, 58, 255); // Subtle border stroke (#73523A)

        // 1. Buat Kapsul 100% Setengah Lingkaran Sempurna untuk NamePill (Tinggi 36px, Radius 18px)
        string namePillPath = "Assets/UI/namepill.png";
        GenerateCapsuleTexture(namePillPath, 120, 36, 18f, fillColor, strokeColor, new Vector4(18, 10, 18, 10));
        Sprite namePillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(namePillPath);

        // 2. Buat Kapsul 100% Setengah Lingkaran Sempurna untuk RolePill (Tinggi 34px, Radius 17px)
        string rolePillPath = "Assets/UI/Pill_Brown_Bg.png";
        GenerateCapsuleTexture(rolePillPath, 120, 34, 17f, fillColor, strokeColor, new Vector4(17, 9, 17, 9));
        Sprite rolePillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(rolePillPath);

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // ==========================================
        // 3. PERBAIKI NAMEPILL
        // ==========================================
        GameObject namePillObj = FindGameObjectInCanvases(canvases, "NamePill");
        if (namePillObj != null)
        {
            Undo.RecordObject(namePillObj, "Fix NamePill");

            Image img = namePillObj.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, "Fix NamePill Image");
                if (namePillSprite != null) img.sprite = namePillSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                img.pixelsPerUnitMultiplier = 1f;
            }

            ContentSizeFitter csf = namePillObj.GetComponent<ContentSizeFitter>();
            if (csf != null) Undo.DestroyObjectImmediate(csf);

            HorizontalLayoutGroup hlg = namePillObj.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) Undo.DestroyObjectImmediate(hlg);

            RectTransform npRt = namePillObj.GetComponent<RectTransform>();
            if (npRt != null)
            {
                Undo.RecordObject(npRt, "Fix NamePill Transform");
                npRt.anchorMin = new Vector2(0.5f, 0f);
                npRt.anchorMax = new Vector2(0.5f, 0f);
                npRt.pivot = new Vector2(0.5f, 0.5f);
                npRt.anchoredPosition = new Vector2(0f, 0f);
                npRt.sizeDelta = new Vector2(165f, 36f);
            }
            namePillObj.transform.SetAsLastSibling();

            Transform ntTr = namePillObj.transform.Find("NameText");
            if (ntTr != null)
            {
                Undo.RecordObject(ntTr.gameObject, "Fix NameText");
                ntTr.gameObject.SetActive(true);

                LayoutElement le = ntTr.GetComponent<LayoutElement>();
                if (le != null) Undo.DestroyObjectImmediate(le);

                RectTransform ntRt = ntTr.GetComponent<RectTransform>();
                if (ntRt != null)
                {
                    Undo.RecordObject(ntRt, "Fix NameText Transform");
                    ntRt.anchorMin = Vector2.zero;
                    ntRt.anchorMax = Vector2.one;
                    ntRt.offsetMin = Vector2.zero;
                    ntRt.offsetMax = Vector2.zero;
                }

                TMP_Text tmp = ntTr.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    Undo.RecordObject(tmp, "Fix NameText Component");
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.fontSize = 17f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Color.white;
                    tmp.raycastTarget = false;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                }
            }
            EditorUtility.SetDirty(namePillObj);
            Debug.Log("<color=green>[Utsuru] NamePill berhasil diperbaiki (100% Rounded Sempurna).</color>");
        }

        // ==========================================
        // 4. PERBAIKI ROLEPILL (ROLE TAG)
        // ==========================================
        GameObject rolePillObj = FindGameObjectInCanvases(canvases, "RolePill");
        if (rolePillObj != null)
        {
            Undo.RecordObject(rolePillObj, "Fix RolePill");

            Image img = rolePillObj.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, "Fix RolePill Image");
                if (rolePillSprite != null) img.sprite = rolePillSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                img.pixelsPerUnitMultiplier = 1f;
            }

            ContentSizeFitter csf = rolePillObj.GetComponent<ContentSizeFitter>();
            if (csf != null) Undo.DestroyObjectImmediate(csf);

            HorizontalLayoutGroup hlg = rolePillObj.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) Undo.DestroyObjectImmediate(hlg);

            RectTransform rpRt = rolePillObj.GetComponent<RectTransform>();
            if (rpRt != null)
            {
                Undo.RecordObject(rpRt, "Fix RolePill Transform");
                rpRt.anchorMin = new Vector2(0f, 0.5f);
                rpRt.anchorMax = new Vector2(0f, 0.5f);
                rpRt.pivot = new Vector2(0f, 0.5f);
                rpRt.sizeDelta = new Vector2(150f, 34f);
            }

            Transform rtTr = rolePillObj.transform.Find("RoleText");
            if (rtTr != null)
            {
                Undo.RecordObject(rtTr.gameObject, "Fix RoleText");
                rtTr.gameObject.SetActive(true);

                RectTransform rtRt = rtTr.GetComponent<RectTransform>();
                if (rtRt != null)
                {
                    Undo.RecordObject(rtRt, "Fix RoleText Transform");
                    rtRt.anchorMin = Vector2.zero;
                    rtRt.anchorMax = Vector2.one;
                    rtRt.offsetMin = Vector2.zero;
                    rtRt.offsetMax = Vector2.zero;
                }

                TMP_Text tmp = rtTr.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    Undo.RecordObject(tmp, "Fix RoleText Component");
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.fontSize = 14f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Color.white;
                    tmp.raycastTarget = false;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                }
            }
            EditorUtility.SetDirty(rolePillObj);
            Debug.Log("<color=green>[Utsuru] RolePill (Role Tag) berhasil diperbaiki (100% Rounded Sempurna).</color>");
        }

        // ==========================================
        // 5. HAPUS BORDER & BACKGROUND SCORE BADGE (HANYA TEKS +100 Points)
        // ==========================================
        GameObject scoreBadgeObj = FindGameObjectInCanvases(canvases, "ScoreBadgePill");
        if (scoreBadgeObj != null)
        {
            Undo.RecordObject(scoreBadgeObj, "Fix ScoreBadgePill");

            Image img = scoreBadgeObj.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, "Disable ScoreBadge Image");
                img.enabled = false;
            }

            TMP_Text sbTmp = scoreBadgeObj.GetComponentInChildren<TMP_Text>(true);
            if (sbTmp != null)
            {
                Undo.RecordObject(sbTmp, "Format ScoreBadge Text");
                sbTmp.text = "+100 Points";
                sbTmp.fontSize = 13f;
                sbTmp.fontStyle = FontStyles.Bold;
                sbTmp.color = new Color32(190, 120, 20, 255);
                sbTmp.alignment = TextAlignmentOptions.Left;
                sbTmp.raycastTarget = false;

                RectTransform sbtRt = sbTmp.rectTransform;
                if (sbtRt != null)
                {
                    Undo.RecordObject(sbtRt, "Fix ScoreText Transform");
                    sbtRt.anchorMin = Vector2.zero;
                    sbtRt.anchorMax = Vector2.one;
                    sbtRt.offsetMin = Vector2.zero;
                    sbtRt.offsetMax = Vector2.zero;
                }
            }
            EditorUtility.SetDirty(scoreBadgeObj);
            Debug.Log("<color=green>[Utsuru] Border & bintang pada Score Badge berhasil dihapus (hanya teks murni +100 Points).</color>");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=cyan><b>[Utsuru] SEMUA PERBAIKAN SELESAI DENGAN SUKSES!</b></color>");
    }

    private static void GenerateCapsuleTexture(string path, int w, int h, float r, Color32 fillColor, Color32 strokeColor, Vector4 border)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32 transparent = new Color32(0, 0, 0, 0);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float cx = (x < r) ? r : ((x > w - r) ? (w - r) : x);
                float cy = h * 0.5f;
                float px = x + 0.5f;
                float py = y + 0.5f;
                float dist = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));

                if (dist <= r - 2.0f)
                {
                    tex.SetPixel(x, y, fillColor);
                }
                else if (dist <= r - 0.5f)
                {
                    float blend = Mathf.Clamp01(dist - (r - 2.0f));
                    Color c = Color.Lerp(fillColor, strokeColor, blend);
                    tex.SetPixel(x, y, c);
                }
                else if (dist <= r + 0.5f)
                {
                    float alpha = Mathf.Clamp01(1f - (dist - (r - 0.5f)));
                    Color c = strokeColor;
                    c.a *= alpha;
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();

        byte[] pngBytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, pngBytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }
    }

    private static GameObject FindGameObjectInCanvases(Canvas[] canvases, string name)
    {
        foreach (Canvas c in canvases)
        {
            RectTransform[] rts = c.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rt in rts)
            {
                if (rt.gameObject.name == name) return rt.gameObject;
            }
        }
        return null;
    }
}
#endif