#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// OUTIL D'ÉDITEUR : captures d'écran du jeu pour la page itch.io et l'oral.
/// Les images sont enregistrées dans le dossier « Captures » à la racine du projet.
/// </summary>
public static class CaptureTools
{
    private const int Width = 1920;
    private const int Height = 1080;

    private static string Folder
    {
        get
        {
            string f = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Captures");
            Directory.CreateDirectory(f);
            return f;
        }
    }

    // ---------------------------------------------------------------- Vues de la scène (hors Play)
    [MenuItem("Solarpunk/Captures/Vues de la scène")]
    public static void CaptureSceneViews()
    {
        Camera main = Camera.main;
        if (main == null) { EditorUtility.DisplayDialog("Captures", "Pas de Main Camera dans la scène ouverte.", "OK"); return; }

        Bounds world = DecorBounds();
        Vector3 center = world.center;
        float radius = world.extents.magnitude;

        // 1. Vue d'ensemble de 3/4, en hauteur.
        Vector3 dir1 = new Vector3(-0.8f, 0.75f, -1f).normalized;
        Shoot(main, center + dir1 * radius * 1.35f, center, 40f, "VueEnsemble.png");

        // 2. Vue plus proche du potager et du drone.
        GameObject drone = GameObject.Find("Drone");
        GameObject garden = GameObject.Find("Jardin");
        Vector3 focus = garden != null ? BoundsOf(garden).center : center;
        if (drone != null) focus = Vector3.Lerp(focus, drone.transform.position, 0.4f);
        Vector3 dir2 = new Vector3(0.9f, 0.6f, -1f).normalized;
        Shoot(main, focus + dir2 * 11f, focus, 45f, "VuePotager.png");

        // 3. Vue rasante, comme si on était au sol dans le jardin.
        Vector3 dir3 = new Vector3(-1f, 0.25f, 0.6f).normalized;
        Shoot(main, focus + dir3 * 13f + Vector3.up * 1.5f, focus + Vector3.up * 1f, 50f, "VueSol.png");

        EditorUtility.RevealInFinder(Path.Combine(Folder, "VueEnsemble.png"));
        Debug.Log("[Captures] Vues enregistrées dans " + Folder);
    }

    // ---------------------------------------------------------------- Capture en jeu (Play)
    [MenuItem("Solarpunk/Captures/Capture du jeu (en Play)")]
    public static void CaptureGameView()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Captures", "Lance le jeu (Play) puis utilise ce menu : la capture prend la vue Game avec l'interface.", "OK");
            return;
        }
        string file = Path.Combine(Folder, "EnJeu_" + System.DateTime.Now.ToString("HHmmss") + ".png");
        ScreenCapture.CaptureScreenshot(file, 2);
        Debug.Log("[Captures] " + file);
    }

    // ---------------------------------------------------------------- Outils
    private static void Shoot(Camera template, Vector3 position, Vector3 lookAt, float fov, string fileName)
    {
        // Copie de la caméra du jeu : mêmes réglages URP (post-process, ciel, brouillard).
        GameObject copy = Object.Instantiate(template.gameObject);
        copy.hideFlags = HideFlags.HideAndDontSave;
        foreach (MonoBehaviour mb in copy.GetComponents<MonoBehaviour>())
            if (mb.GetType().Name == "CameraOrbit") Object.DestroyImmediate(mb);
        foreach (AudioListener al in copy.GetComponents<AudioListener>()) Object.DestroyImmediate(al);

        Camera cam = copy.GetComponent<Camera>();
        copy.transform.position = position;
        copy.transform.LookAt(lookAt);
        cam.fieldOfView = fov;
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, 500f);

        RenderTexture rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;

        File.WriteAllBytes(Path.Combine(Folder, fileName), image.EncodeToPNG());

        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(copy);
    }

    // Boîte englobante du décor : tout ce qui est visible, sauf le sol (immense) et l'interface.
    private static Bounds DecorBounds()
    {
        bool has = false;
        Bounds b = new Bounds();
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (r.gameObject.name == "Ground" || r.GetComponentInParent<Canvas>() != null) continue;
            if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
        }
        return has ? b : new Bounds(Vector3.zero, Vector3.one * 20f);
    }

    private static Bounds BoundsOf(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
#endif
