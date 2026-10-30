#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// OUTIL D'ÉDITEUR (n'est pas inclus dans le jeu) : construit automatiquement
/// les scènes « MenuPrincipal » et « Fin » avec leur interface, relie les boutons aux scripts
/// TitleMenu / EndMenu, et déclare les 3 scènes dans la build dans le bon ordre.
/// Menu : Solarpunk → Construire les menus.
/// </summary>
public static class MenuSceneBuilder
{
    private const string ScenesFolder = "Assets/_Project/Scenes/";
    private const string TitleScene = "MenuPrincipal";
    private const string GameScene = "Game";
    private const string EndScene = "Fin";

    private static readonly Color ButtonColor = new Color32(0x4E, 0x7A, 0x3A, 0xFF); // vert solarpunk
    private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.35f);

    [MenuItem("Solarpunk/Construire les menus")]
    public static void BuildMenus()
    {
        // Proposer de sauvegarder la scène ouverte avant de la quitter.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        BuildTitleScene();
        BuildEndScene();
        SetBuildScenes();

        EditorSceneManager.OpenScene(ScenesFolder + TitleScene + ".unity");
        EditorUtility.DisplayDialog("Menus construits",
            "Les scènes MenuPrincipal et Fin ont été créées et ajoutées à la build\n(ordre : MenuPrincipal → Game → Fin).",
            "OK");
    }

    // ---------------------------------------------------------------- Écran titre
    private static void BuildTitleScene()
    {
        Scene scene = NewMenuScene();
        Transform canvas = CreateCanvas("MenuTitre");

        TMP_Text title = CreateText(canvas, "TitreJeu", "Le Potager Solarpunk", 110, true);
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1500f, 160f));

        TMP_Text subtitle = CreateText(canvas, "SousTitre", "Plante, arrose, récolte… avant la fin du chrono !", 40, false);
        Anchor(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -290f), new Vector2(1500f, 80f));

        Transform content = CreateContent(canvas, new Vector2(560f, 380f), new Vector2(0f, -60f));
        TMP_Text best = CreateText(content, "MeilleurScoreText", "Meilleur score : 0", 44, false);
        Button play = CreateButton(content, "BoutonJouer", "Jouer");
        Button quit = CreateButton(content, "BoutonQuitter", "Quitter");

        TMP_Text controls = CreateText(canvas, "CommandesText",
            "ZQSD / stick gauche : voler   ·   Souris / stick droit : caméra   ·   Espace / Ctrl : altitude\n" +
            "Clic gauche / RT : planter   ·   Clic droit / LT : arroser   ·   A / E : changer de graine   ·   R : déposer / recharger   ·   Échap : pause",
            26, false);
        Anchor(controls.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1800f, 110f));

        TitleMenu menu = canvas.gameObject.AddComponent<TitleMenu>();
        SetField(menu, "gameSceneName", GameScene);
        SetField(menu, "bestScoreText", best);
        UnityEventTools.AddPersistentListener(play.onClick, new UnityAction(menu.Play));
        UnityEventTools.AddPersistentListener(quit.onClick, new UnityAction(menu.Quit));

        CreateEventSystem(play.gameObject);
        AddSoundSettings();
        EditorSceneManager.SaveScene(scene, ScenesFolder + TitleScene + ".unity");
    }

    // ---------------------------------------------------------------- Écran de fin
    private static void BuildEndScene()
    {
        Scene scene = NewMenuScene();
        Transform canvas = CreateCanvas("MenuFin");

        Transform content = CreateContent(canvas, new Vector2(800f, 640f), Vector2.zero);
        TMP_Text result = CreateText(content, "ResultatText", "Potager réussi !", 84, true);
        TMP_Text score = CreateText(content, "ScoreFinalText", "Score : 0", 52, false);
        TMP_Text record = CreateText(content, "RecordText", "Meilleur score : 0", 42, false);
        Button replay = CreateButton(content, "BoutonRejouer", "Rejouer");
        Button back = CreateButton(content, "BoutonMenu", "Menu principal");

        EndMenu menu = canvas.gameObject.AddComponent<EndMenu>();
        SetField(menu, "gameSceneName", GameScene);
        SetField(menu, "titleSceneName", TitleScene);
        SetField(menu, "titleText", result);
        SetField(menu, "scoreText", score);
        SetField(menu, "bestScoreText", record);
        UnityEventTools.AddPersistentListener(replay.onClick, new UnityAction(menu.Replay));
        UnityEventTools.AddPersistentListener(back.onClick, new UnityAction(menu.BackToTitle));

        CreateEventSystem(replay.gameObject);
        EditorSceneManager.SaveScene(scene, ScenesFolder + EndScene + ".unity");
    }

    // ---------------------------------------------------------------- Build
    private static void SetBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenesFolder + TitleScene + ".unity", true),
            new EditorBuildSettingsScene(ScenesFolder + GameScene + ".unity", true),
            new EditorBuildSettingsScene(ScenesFolder + EndScene + ".unity", true),
        };
    }

    // ---------------------------------------------------------------- Réglages du son
    [MenuItem("Solarpunk/Ajouter les réglages du son")]
    public static void AddSoundSettingsToScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (string name in new[] { TitleScene, GameScene })
        {
            Scene scene = EditorSceneManager.OpenScene(ScenesFolder + name + ".unity");
            AddSoundSettings();
            EditorSceneManager.SaveScene(scene);
        }

        EditorSceneManager.OpenScene(ScenesFolder + TitleScene + ".unity");
        EditorUtility.DisplayDialog("Réglages du son",
            "Bouton « Son » ajouté au menu titre et au menu pause.", "OK");
    }

    // Ajoute un bouton « Son » au menu de la scène ouverte (TitleMenu ou PauseMenu) et le panneau de réglages.
    // Peut être relancé sans risque : l'ancienne version est d'abord supprimée.
    private static void AddSoundSettings()
    {
        MonoBehaviour host;
        Transform panelParent;
        TitleMenu title = Object.FindFirstObjectByType<TitleMenu>(FindObjectsInactive.Include);
        PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (title != null)
        {
            host = title;
            panelParent = title.transform;
        }
        else if (pause != null)
        {
            host = pause;
            GameObject pausePanel = new SerializedObject(pause).FindProperty("pausePanel").objectReferenceValue as GameObject;
            if (pausePanel == null) { Debug.LogError("[Son] PauseMenu n'a pas de Pause Panel."); return; }
            panelParent = pausePanel.transform;
        }
        else { Debug.LogError("[Son] Ni TitleMenu ni PauseMenu dans la scène."); return; }

        Transform content = FindDeep(panelParent, "Contenu");
        if (content == null) { Debug.LogError("[Son] Objet « Contenu » introuvable."); return; }

        AudioMixer mixer = FindAsset<AudioMixer>("t:AudioMixer");
        if (mixer == null) { Debug.LogError("[Son] Aucun AudioMixer trouvé dans _Project."); return; }

        // Nettoyer une ancienne version.
        Transform oldPanel = FindDeep(panelParent, "PanneauSon");
        if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);
        Transform oldButton = content.Find("BoutonSon");
        bool hadButton = oldButton != null;
        if (hadButton) Object.DestroyImmediate(oldButton.gameObject);
        foreach (SoundSettingsUI old in host.GetComponents<SoundSettingsUI>()) Object.DestroyImmediate(old);

        // Bouton « Son » : copie du premier bouton du menu, placé juste avant le dernier.
        Button[] buttons = content.GetComponentsInChildren<Button>(true);
        Button template = buttons[0];
        Button last = buttons[buttons.Length - 1];
        Button openButton = CloneButton(template, content, "BoutonSon", "Son");
        openButton.transform.SetSiblingIndex(last.transform.GetSiblingIndex());
        if (!hadButton) ((RectTransform)content).sizeDelta += new Vector2(0f, 105f);

        // Panneau des réglages.
        GameObject panel = new GameObject("PanneauSon", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(panelParent, false);
        panel.layer = LayerMask.NameToLayer("UI");
        RectTransform prt = (RectTransform)panel.transform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(1000f, 560f);
        Image bg = panel.GetComponent<Image>();
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.08f, 0.12f, 0.08f, 0.9f);
        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(50, 50, 40, 40);
        vlg.spacing = 20f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        TMP_Text heading = CreateText(panel.transform, "TitreSon", "Réglages du son", 56, true);
        heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 75f;

        CreateSoundRow(panel.transform, "Général", out Slider generalSlider, out Toggle generalMute);
        CreateSoundRow(panel.transform, "Musique", out Slider musicSlider, out Toggle musicMute);
        CreateSoundRow(panel.transform, "Effets", out Slider sfxSlider, out Toggle sfxMute);

        Button back = CloneButton(template, panel.transform, "BoutonRetour", "Retour");

        // Script + références.
        SoundSettingsUI ui = host.gameObject.AddComponent<SoundSettingsUI>();
        SetField(ui, "mixer", mixer);
        SetField(ui, "settingsPanel", panel);
        SetField(ui, "menuContent", content.gameObject);
        SetField(ui, "openButton", openButton.gameObject);
        SetField(ui, "generalSlider", generalSlider);
        SetField(ui, "musicSlider", musicSlider);
        SetField(ui, "sfxSlider", sfxSlider);
        SetField(ui, "generalMute", generalMute);
        SetField(ui, "musicMute", musicMute);
        SetField(ui, "sfxMute", sfxMute);
        UnityEventTools.AddPersistentListener(openButton.onClick, new UnityAction(ui.Open));
        UnityEventTools.AddPersistentListener(back.onClick, new UnityAction(ui.Close));

        panel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
    }

    private static void CreateSoundRow(Transform parent, string label, out Slider slider, out Toggle mute)
    {
        GameObject row = new GameObject("Ligne" + label, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 70f;
        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;

        TMP_Text text = CreateText(row.transform, "Nom", label, 40, false);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
        textLayout.preferredWidth = 220f;
        textLayout.preferredHeight = 70f;

        DefaultControls.Resources res = UiResources();

        // Curseur 0..1
        GameObject sliderGo = DefaultControls.CreateSlider(res);
        sliderGo.name = "Curseur";
        sliderGo.transform.SetParent(row.transform, false);
        LayoutElement sliderLayout = sliderGo.AddComponent<LayoutElement>();
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.preferredHeight = 34f;
        slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        Transform fill = sliderGo.transform.Find("Fill Area/Fill");
        if (fill != null) fill.GetComponent<Image>().color = ButtonColor;
        Transform handle = sliderGo.transform.Find("Handle Slide Area/Handle");
        if (handle != null) ((RectTransform)handle).sizeDelta = new Vector2(34f, 0f);

        // Case « Muet »
        GameObject toggleGo = DefaultControls.CreateToggle(res);
        toggleGo.name = "Muet";
        toggleGo.transform.SetParent(row.transform, false);
        LayoutElement toggleLayout = toggleGo.AddComponent<LayoutElement>();
        toggleLayout.preferredWidth = 170f;
        toggleLayout.preferredHeight = 50f;
        mute = toggleGo.GetComponent<Toggle>();
        mute.isOn = false;

        RectTransform box = (RectTransform)toggleGo.transform.Find("Background");
        box.anchorMin = box.anchorMax = new Vector2(0f, 0.5f);
        box.pivot = new Vector2(0f, 0.5f);
        box.anchoredPosition = Vector2.zero;
        box.sizeDelta = new Vector2(44f, 44f);
        RectTransform check = (RectTransform)box.Find("Checkmark");
        check.anchorMin = Vector2.zero; check.anchorMax = Vector2.one;
        check.offsetMin = new Vector2(4f, 4f); check.offsetMax = new Vector2(-4f, -4f);

        Transform oldLabel = toggleGo.transform.Find("Label");
        if (oldLabel != null) Object.DestroyImmediate(oldLabel.gameObject); // texte « ancien » remplacé par TextMeshPro
        TMP_Text muteText = CreateText(toggleGo.transform, "Label", "Muet", 34, false);
        muteText.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform mrt = muteText.rectTransform;
        mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
        mrt.offsetMin = new Vector2(58f, 0f); mrt.offsetMax = Vector2.zero;

        foreach (Transform t in row.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LayerMask.NameToLayer("UI");
    }

    private static DefaultControls.Resources UiResources()
    {
        return new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        };
    }

    private static Button CloneButton(Button template, Transform parent, string name, string label)
    {
        GameObject go = Object.Instantiate(template.gameObject, parent, false);
        go.name = name;
        TMP_Text text = go.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
        Button button = go.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent(); // ne pas garder l'action du bouton copié
        return button;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    // ---------------------------------------------------------------- Outils
    private static Scene NewMenuScene()
    {
        // Caméra + lumière par défaut, avec le ciel du jeu en fond.
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (camera != null) camera.clearFlags = CameraClearFlags.Skybox;

        Material sky = FindAsset<Material>("Ciel t:Material");
        if (sky != null) RenderSettings.skybox = sky;
        return scene;
    }

    private static Transform CreateCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Voile sombre plein écran : le texte reste lisible sur le ciel.
        GameObject veil = new GameObject("Voile", typeof(RectTransform), typeof(Image));
        veil.transform.SetParent(go.transform, false);
        RectTransform rt = (RectTransform)veil.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Image image = veil.GetComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;
        return go.transform;
    }

    private static Transform CreateContent(Transform parent, Vector2 size, Vector2 position)
    {
        GameObject go = new GameObject("Contenu", typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;

        VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 25f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return go.transform;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float size, bool bold)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = ButtonColor;

        go.GetComponent<LayoutElement>().preferredHeight = 80f;

        TMP_Text text = CreateText(go.transform, "Texte", label, 40, true);
        RectTransform rt = text.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        return go.GetComponent<Button>();
    }

    private static void CreateEventSystem(GameObject firstSelected)
    {
        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.GetComponent<EventSystem>().firstSelectedGameObject = firstSelected; // navigation à la manette
    }

    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, anchor.y);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    // Remplit un champ [SerializeField] privé, comme si on le faisait dans l'Inspector.
    private static void SetField(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetField(Object target, string field, string value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T FindAsset<T>(string filter) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets(filter, new[] { "Assets/_Project" }))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) return asset;
        }
        return null;
    }
}
#endif
