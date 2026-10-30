#if UNITY_EDITOR
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// OUTILS D'ÉDITEUR du jalon 6 (non inclus dans le jeu) :
/// nettoyage du projet et génération de la build Windows + zip prêt pour itch.io.
/// </summary>
public static class ProjectTools
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string ProductName = "Le Potager Solarpunk";
    private const string BuildFolder = "Build/LePotagerSolarpunk";
    private const string ZipPath = "Build/LePotagerSolarpunk_Windows.zip";

    // ---------------------------------------------------------------- Nettoyage
    [MenuItem("Solarpunk/Nettoyer le projet")]
    public static void CleanProject()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;

        // 1. Ancien dossier Docs (contenait une vieille version du PDF, remplacée à la racine).
        string docs = Path.Combine(projectRoot, "Docs");
        if (Directory.Exists(docs)) Directory.Delete(docs, true);

        // 2. Noms propres dans la scène de jeu.
        Scene scene = EditorSceneManager.OpenScene(GameScenePath);
        int renamed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string newName = BetterName(t);
                if (newName != null && newName != t.name)
                {
                    t.name = newName;
                    renamed++;
                }
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[Nettoyage] Dossier Docs supprimé, {renamed} objet(s) renommé(s).");
        EditorUtility.DisplayDialog("Nettoyage", $"Dossier Docs supprimé.\n{renamed} objet(s) renommé(s) dans Game.", "OK");
    }

    private static string BetterName(Transform t)
    {
        string n = t.name;
        string parent = t.parent != null ? t.parent.name : "";

        // « Parcelle(1) », « Parcelle (2) »… → Parcelle1, Parcelle2…
        var m = System.Text.RegularExpressions.Regex.Match(n, @"^Parcelle\s*\((\d+)\)$");
        if (m.Success) return "Parcelle" + m.Groups[1].Value;

        // Modèles importés (noms anglais du pack) → noms qui disent à quoi ils servent.
        if (n.StartsWith("chimney-top")) return "Silo";
        if (n == "cart") return "Charrette";
        if (n == "fountain-round") return "Fontaine";
        if (n == "Wheat_Crop" || n == "Carrot_Crop") return "TasRecolte";

        // Textes des boutons copiés depuis « Reprendre ».
        if (n == "TitreReprendre" && parent == "BoutonSon") return "TexteSon";
        if (n == "TitreReprendre" && parent == "BoutonRetour") return "TexteRetour";
        return null;
    }

    // ---------------------------------------------------------------- Build
    [MenuItem("Solarpunk/Construire le jeu (Windows)")]
    public static void BuildWindows()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        PlayerSettings.productName = ProductName;
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string folder = Path.Combine(projectRoot, BuildFolder);
        if (Directory.Exists(folder)) Directory.Delete(folder, true);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(folder, ProductName + ".exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("Build", "La build a échoué : regarde la Console.", "OK");
            return;
        }

        // Le dossier « _BurstDebugInformation_DoNotShip » ne doit pas être distribué.
        foreach (string dir in Directory.GetDirectories(folder, "*DoNotShip*")) Directory.Delete(dir, true);

        // Zip prêt à envoyer sur itch.io.
        string zip = Path.Combine(projectRoot, ZipPath);
        if (File.Exists(zip)) File.Delete(zip);
        ZipFile.CreateFromDirectory(folder, zip);

        long mb = new FileInfo(zip).Length / (1024 * 1024);
        Debug.Log($"[Build] OK : {zip} ({mb} Mo)");
        EditorUtility.DisplayDialog("Build réussie", $"Jeu : {BuildFolder}\nZip pour itch.io : {ZipPath} ({mb} Mo)", "OK");
        EditorUtility.RevealInFinder(zip);
    }
    // ---------------------------------------------------------------- Tas de récolte
    [MenuItem("Solarpunk/Mettre les tas de récolte dans les charrettes")]
    public static void ReplaceHarvestPiles()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene(GameScenePath);

        int done = ReplacePile("BacBle", "Assets/_Project/Models/Recoltes/TasBle.obj")
                 + ReplacePile("BacCarotte", "Assets/_Project/Models/Recoltes/TasCarottes.obj");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Tas de récolte", $"{done} charrette(s) garnie(s).", "OK");
    }

    private static int ReplacePile(string binName, string modelPath)
    {
        GameObject bin = GameObject.Find(binName);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (bin == null || model == null) { Debug.LogError($"[Tas] {binName} ou {modelPath} introuvable."); return 0; }

        Transform old = bin.transform.Find("TasRecolte");
        Transform cart = bin.transform.Find("Charrette");
        Bounds cartBounds = RenderBounds(cart != null ? cart : bin.transform);
        Bounds oldBounds = old != null ? RenderBounds(old) : cartBounds;

        GameObject pile = (GameObject)PrefabUtility.InstantiatePrefab(model, bin.scene);
        pile.name = "TasRecolte";
        Transform parent = old != null ? old.parent : bin.transform;
        pile.transform.SetParent(parent, false);

        // Orientation : suit la charrette (rotation autour de Y seulement).
        float yaw = cart != null ? cart.eulerAngles.y : bin.transform.eulerAngles.y;
        pile.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Taille : le tas occupe environ 60 % de la largeur de la charrette.
        pile.transform.localScale = Vector3.one;
        Bounds pileBounds = RenderBounds(pile.transform);
        float footprint = Mathf.Max(pileBounds.size.x, pileBounds.size.z);
        float target = 0.6f * Mathf.Min(cartBounds.size.x, cartBounds.size.z);
        float s = footprint > 0f ? target / footprint : 1f;
        Vector3 ps = parent.lossyScale;
        pile.transform.localScale = new Vector3(s / ps.x, s / ps.y, s / ps.z);

        // Position : posé là où était l'ancien légume (au fond de la charrette).
        pileBounds = RenderBounds(pile.transform);
        Vector3 wanted = new Vector3(oldBounds.center.x, oldBounds.min.y, oldBounds.center.z);
        Vector3 current = new Vector3(pileBounds.center.x, pileBounds.min.y, pileBounds.center.z);
        pile.transform.position += wanted - current;

        if (old != null)
        {
            RedirectReferences(old.gameObject, pile);
            Object.DestroyImmediate(old.gameObject);
        }
        return 1;
    }

    private static Bounds RenderBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    // Si un script pointait vers l'ancien modèle, il pointe maintenant vers le nouveau tas.
    private static void RedirectReferences(GameObject oldGo, GameObject newGo)
    {
        foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            SerializedObject so = new SerializedObject(mb);
            SerializedProperty it = so.GetIterator();
            bool changed = false;
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (it.objectReferenceValue == oldGo) { it.objectReferenceValue = newGo; changed = true; }
                else if (it.objectReferenceValue == oldGo.transform) { it.objectReferenceValue = newGo.transform; changed = true; }
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
