#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// OUTIL D'ÉDITEUR : fabrique les prefabs « TasCarottes » et « TasBle » à partir des modèles
/// Quaternius (Carrot_Crop / Wheat_Crop) empilés en tas, puis les pose sur les charrettes
/// à la place du légume seul. Menu : Solarpunk → Créer les tas de récolte.
/// </summary>
public static class HarvestPileBuilder
{
    private const string ModelsFolder = "Assets/_Project/Models/Crops/FBX/";
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

    private const string PilesFolder = "Assets/_Project/Models/Recoltes/";
    private static readonly string[] OldPileNames = { "TasRecolte", "TasBle", "TasCarotte" };

    [MenuItem("Solarpunk/Créer les tas de récolte")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene(GameScenePath);

        // 1. Mémoriser où sont posés les tas actuels (le dessus du plateau de chaque charrette).
        bool hasCarrot = TryGetBase("BacCarotte", out Vector3 carrotBase);
        bool hasWheat = TryGetBase("BacBle", out Vector3 wheatBase);

        // 2. Refaire les prefabs (même fichier = même guid : les tas déjà posés sont mis à jour).
        //    Carottes et blé : en vrac, en forme de dôme (le blé a en plus un cœur de grain).
        GameObject carrotPile = BuildPile("Carrot_Crop.fbx", "TasCarotte", parallel: false, layers: 5, seed: 7);
        GameObject wheatPile = BuildPile("Wheat_Crop.fbx", "TasBle", parallel: false, layers: 6, seed: 3, core: true);

        // 3. Les poser, à la bonne taille, sur les charrettes.
        int placed = 0;
        if (PlaceOnCart("BacCarotte", carrotPile, hasCarrot, carrotBase, roundPile: true)) placed++;
        if (PlaceOnCart("BacBle", wheatPile, hasWheat, wheatBase, roundPile: false)) placed++;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // 4. Supprimer les essais précédents devenus inutiles (OBJ et meshes combinés).
        foreach (string old in new[] { "TasBle.obj", "TasBle.mtl", "TasCarottes.obj", "TasCarottes.mtl", "TasBle_Mesh.asset", "TasCarotte_Mesh.asset" })
            AssetDatabase.DeleteAsset(PilesFolder + old);

        EditorUtility.DisplayDialog("Tas de récolte",
            $"Prefabs TasCarotte et TasBle refaits (Models/Recoltes).\n{placed}/2 charrette(s) mise(s) à jour.", "OK");
    }

    // ---------------------------------------------------------------- Fabrication du tas
    private static GameObject BuildPile(string modelFile, string pileName, bool parallel, int layers, int seed, bool core = false)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsFolder + modelFile);
        if (model == null) { Debug.LogError("[Tas] Modèle introuvable : " + modelFile); return null; }

        GameObject root = new GameObject(pileName);
        System.Random rng = new System.Random(seed);

        // Coucher le légume : son axe le plus long (calculé sur ses sommets) devient l'axe X, à plat.
        Vector3 axis = MainAxis(model, root.transform);
        Quaternion lay = Quaternion.FromToRotation(axis, Vector3.right);

        Bounds lying = MeasureAt(model, root.transform, lay);
        float length = lying.size.x;
        float width = Mathf.Max(0.0001f, lying.size.z);
        float thickness = Mathf.Max(0.0001f, lying.size.y);

        if (parallel)
        {
            // Gerbes de blé couchées côte à côte, empilées en pyramide comme des rondins.
            float step = Mathf.Min(width, thickness * 2f) * 0.7f;
            int baseCount = layers;
            for (int layer = 0; layer < layers; layer++)
            {
                int count = baseCount - layer;
                for (int i = 0; i < count; i++)
                {
                    float z = (i - (count - 1) * 0.5f) * step + Range(rng, -0.08f, 0.08f) * step;
                    float x = Range(rng, -0.08f, 0.08f) * length;
                    float y = thickness * 0.5f + layer * thickness * 0.7f;
                    float yaw = (rng.NextDouble() < 0.5 ? 0f : 180f) + Range(rng, -6f, 6f);
                    float roll = Range(rng, 0f, 360f); // tourner la gerbe sur elle-même
                    AddItem(model, root.transform, Quaternion.Euler(0f, yaw, 0f) * Quaternion.AngleAxis(roll, Vector3.right) * lay,
                        new Vector3(x, y, z), rng);
                }
            }
        }
        else
        {
            // Carottes en vrac : un dôme, chaque couche plus petite que la précédente.
            float baseRadius = length * 0.9f;
            // Le blé est fin : on monte plus vite à chaque couche pour obtenir un vrai tas bombé.
            float layerStep = core ? baseRadius * 0.08f : thickness * 0.6f;

            if (core)
            {
                // Cœur du tas : un dôme de grain (sphère aplatie à moitié enterrée) qui comble les trous.
                GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(dome.GetComponent<Collider>());
                dome.name = "Grain";
                dome.transform.SetParent(root.transform, false);
                float domeHeight = baseRadius * 0.7f; // moitié visible, reste caché sous les gerbes
                dome.transform.localScale = new Vector3(baseRadius * 1.1f, domeHeight, baseRadius * 1.1f);
                dome.transform.localPosition = Vector3.zero;
                Renderer modelRenderer = model.GetComponentInChildren<Renderer>();
                if (modelRenderer != null) dome.GetComponent<Renderer>().sharedMaterial = modelRenderer.sharedMaterial;
            }

            for (int layer = 0; layer < layers; layer++)
            {
                float radius = baseRadius * (1f - 0.85f * layer / layers);
                int count = Mathf.Max(1, Mathf.RoundToInt(Mathf.PI * radius * radius / (length * width) * (core ? 1.4f : 1.3f)));
                float y = (core ? 0f : thickness * 0.5f) + layer * layerStep;
                for (int i = 0; i < count; i++)
                {
                    float angle = Range(rng, 0f, Mathf.PI * 2f);
                    float distance = Mathf.Sqrt((float)rng.NextDouble()) * Mathf.Max(0f, radius - length * 0.4f);
                    float yaw = Range(rng, 0f, 360f);
                    float roll = Range(rng, 0f, 360f);
                    AddItem(model, root.transform, Quaternion.Euler(0f, yaw, 0f) * Quaternion.AngleAxis(roll, Vector3.right) * lay,
                        new Vector3(Mathf.Cos(angle) * distance, y, Mathf.Sin(angle) * distance), rng);
                }
            }
        }

        string path = PilesFolder + pileName + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void AddItem(GameObject model, Transform parent, Quaternion rotation, Vector3 center, System.Random rng)
    {
        Quaternion tilt = Quaternion.Euler(Range(rng, -10f, 10f), 0f, Range(rng, -10f, 10f));
        GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        item.transform.localRotation = tilt * rotation;
        item.transform.localPosition = Vector3.zero;
        Bounds b = WorldBounds(item);
        item.transform.localPosition = center - b.center; // on place le CENTRE du légume, pas son pivot
    }

    // Axe principal du modèle (analyse en composantes principales des sommets).
    private static Vector3 MainAxis(GameObject model, Transform parent)
    {
        GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        tmp.transform.localPosition = Vector3.zero;
        tmp.transform.localRotation = Quaternion.identity;
        var points = new System.Collections.Generic.List<Vector3>();
        foreach (MeshFilter mf in tmp.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            foreach (Vector3 v in mf.sharedMesh.vertices) points.Add(mf.transform.TransformPoint(v));
        }
        Object.DestroyImmediate(tmp);
        if (points.Count < 3) return Vector3.up;

        Vector3 mean = Vector3.zero;
        foreach (Vector3 p in points) mean += p;
        mean /= points.Count;
        float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
        foreach (Vector3 p in points)
        {
            Vector3 d = p - mean;
            xx += d.x * d.x; xy += d.x * d.y; xz += d.x * d.z;
            yy += d.y * d.y; yz += d.y * d.z; zz += d.z * d.z;
        }
        Vector3 axis = new Vector3(1f, 1f, 1f).normalized;
        for (int i = 0; i < 50; i++) // méthode de la puissance
        {
            axis = new Vector3(xx * axis.x + xy * axis.y + xz * axis.z,
                               xy * axis.x + yy * axis.y + yz * axis.z,
                               xz * axis.x + yz * axis.y + zz * axis.z).normalized;
        }
        return axis;
    }

    // ---------------------------------------------------------------- Pose sur la charrette
    private static Transform FindPile(Transform bin)
    {
        foreach (string n in OldPileNames)
        {
            Transform t = FindDeep(bin, n);
            if (t != null) return t;
        }
        return null;
    }

    // Point de pose : le dessus du plateau de la charrette, trouvé par un rayon lancé du dessus
    // (on ajoute un MeshCollider temporaire à la charrette le temps du test).
    private static bool TryGetBase(string binName, out Vector3 basePoint)
    {
        basePoint = Vector3.zero;
        GameObject bin = GameObject.Find(binName);
        if (bin == null) return false;
        Transform cart = FindDeep(bin.transform, "Charrette");
        if (cart == null) { basePoint = bin.transform.position; return true; }

        Bounds cb = WorldBounds(cart.gameObject);
        Vector3 c = bin.transform.position;
        // Dans la largeur, on prend le milieu réel de la charrette (entre les roues).
        if (cb.size.x < cb.size.z) c.x = cb.center.x; else c.z = cb.center.z;

        var temp = new System.Collections.Generic.List<MeshCollider>();
        foreach (MeshFilter mf in cart.GetComponentsInChildren<MeshFilter>())
        {
            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            temp.Add(mc);
        }
        Physics.SyncTransforms();
        float y = cb.center.y;
        Vector3 origin = new Vector3(c.x, cb.max.y + 1f, c.z);
        float best = float.MinValue;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, cb.size.y + 2f))
            if (temp.Contains(hit.collider as MeshCollider) && hit.point.y > best) best = hit.point.y;
        if (best > float.MinValue) y = best;
        foreach (MeshCollider mc in temp) Object.DestroyImmediate(mc);

        basePoint = new Vector3(c.x, y, c.z);
        return true;
    }

    private static bool PlaceOnCart(string binName, GameObject pilePrefab, bool hasBase, Vector3 basePoint, bool roundPile)
    {
        if (pilePrefab == null || !hasBase) return false;
        GameObject bin = GameObject.Find(binName);
        if (bin == null) { Debug.LogError("[Tas] Objet introuvable : " + binName); return false; }
        Transform cart = FindDeep(bin.transform, "Charrette");

        // Remplacer ce qui est posé (légume seul ou ancien tas) par une instance propre du prefab.
        Transform old = FindPile(bin.transform);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject pile = (GameObject)PrefabUtility.InstantiatePrefab(pilePrefab, bin.transform);
        pile.name = pilePrefab.name;
        pile.transform.rotation = Quaternion.identity;
        pile.transform.localScale = Vector3.one;

        if (cart != null)
        {
            // Taille et sens : on essaie le tas dans les deux sens (0° et 90°) et on garde celui
            // qui permet le plus gros tas tout en restant dans ~60 % de la charrette.
            Bounds cb = WorldBounds(cart.gameObject);
            float bestFit = 0f, bestYaw = 0f;
            foreach (float yaw in new[] { 0f, 90f })
            {
                pile.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                pile.transform.localScale = Vector3.one;
                Bounds pb = WorldBounds(pile);
                float fit = Mathf.Min(cb.size.x * 0.5f / pb.size.x, cb.size.z * 0.5f / pb.size.z);
                if (fit > bestFit) { bestFit = fit; bestYaw = yaw; }
            }
            pile.transform.rotation = Quaternion.Euler(0f, bestYaw, 0f);
            pile.transform.localScale = Vector3.one * bestFit / Mathf.Max(0.0001f, bin.transform.lossyScale.x);
        }

        // Poser le bas du tas sur le plateau (le cœur de grain, à moitié enterré, est ignoré : il s'enfonce dans le plateau).
        Bounds final = WorldBounds(pile, ignoreName: "Grain");
        pile.transform.position += basePoint - new Vector3(final.center.x, final.min.y, final.center.z);
        return true;
    }

    // ---------------------------------------------------------------- Outils
    // Diagnostic : écrit la structure du modèle dans Temp/tas_debug.txt
    private static void DumpModel(string file)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsFolder + file);
        if (model == null) return;
        var sb = new System.Text.StringBuilder(file + "\n");
        foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
        {
            sb.Append($"  {t.name} active={t.gameObject.activeSelf} pos={t.localPosition} rot={t.localEulerAngles} scale={t.localScale}");
            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) sb.Append($" mesh={mf.sharedMesh.name} bounds={mf.sharedMesh.bounds}");
            Renderer r = t.GetComponent<Renderer>();
            if (r != null) sb.Append($" renderer enabled={r.enabled}");
            sb.Append("\n");
        }
        System.IO.File.AppendAllText(System.IO.Path.Combine(Application.dataPath, "../Temp/tas_debug.txt"), sb.ToString());
    }

    private static Bounds MeasureAt(GameObject model, Transform parent, Quaternion rotation)
    {
        GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        tmp.transform.localRotation = rotation;
        Bounds b = WorldBounds(tmp);
        Object.DestroyImmediate(tmp);
        return b;
    }

    private static Bounds WorldBounds(GameObject go, string ignoreName = null)
    {
        Renderer[] renderers = System.Array.FindAll(go.GetComponentsInChildren<Renderer>(false),
            r => r.enabled && (ignoreName == null || r.name != ignoreName));
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    private static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
}
#endif
