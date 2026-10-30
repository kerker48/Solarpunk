#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>OUTIL D'ÉDITEUR : lance la dernière build Windows pour la tester hors d'Unity.</summary>
public static class LaunchBuild
{
    [MenuItem("Solarpunk/Lancer la build")]
    public static void Launch()
    {
        string exe = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build/LePotagerSolarpunk/Le Potager Solarpunk.exe");
        if (!File.Exists(exe)) { EditorUtility.DisplayDialog("Build", "Aucune build trouvée : utilise d'abord « Construire le jeu (Windows) ».", "OK"); return; }
        System.Diagnostics.Process.Start(exe);
    }
}
#endif
