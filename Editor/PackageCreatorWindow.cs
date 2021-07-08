using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using System.IO;
using System.Linq;

namespace Dreemar.PackageTool
{
    #region Package Information Classes
    public class AssemblyDefinition : ScriptableObject
    {
        new public string name;
        public string rootNamespace;
        [HideInInspector]
        public string[] includePlatforms;
        [HideInInspector]
        public string[] references;
    }

    public class PackageInfo : ScriptableObject
    {
        [System.Serializable]
        public class PackageAuthor
        {
            public string name;
            public string email;
            public string url;
        }

        new public string name;
        public string version = "1.0.0";
        public string displayName;
        public string unity;
        public string unityRelease;
        public PackageAuthor author;
        [Multiline]
        public string description;
    }
    #endregion

    public class PackageCreatorWindow : EditorWindow
    {
        #region Static Methods
        [MenuItem("Tools/Create Package")]
        public static void OpenWindow()
        {
            var window = GetWindow<PackageCreatorWindow>();
            window.titleContent = new GUIContent("Package Creator");
            window.Show();
        }
        #endregion

        #region Private Fields
        PackageInfo _packageInfo;
        AssemblyDefinition _runtimeAssembly;
        AssemblyDefinition _editorAssembly;

        List<(Editor editor, string label)> _editors;

        GUIStyle _headerStyle;
        #endregion

        #region Initialization
        void Startup()
        {
            // Make a nice lil header label style
            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 25;
            _headerStyle.fontStyle = FontStyle.Bold;

            // Setup the serializable items
            if (_packageInfo == null)
                _packageInfo = CreateInstance<PackageInfo>();

            if (_runtimeAssembly == null)
                _runtimeAssembly = CreateInstance<AssemblyDefinition>();
            if (_editorAssembly == null)
            {
                _editorAssembly = CreateInstance<AssemblyDefinition>();
                // Ensure the editor assembly definition only includes the "Editor" platform
                // If includePlatforms is left empty, all platforms will be included
                _editorAssembly.includePlatforms = new[] { "Editor" };
            }

            CreateEditors();
        }

        void CreateEditors()
        {
            _editors = new List<(Editor editor, string label)>();

            // Package info
            _editors.Add((Editor.CreateEditor(_packageInfo), "Package Info"));
            // Runtime assembly
            _editors.Add((Editor.CreateEditor(_runtimeAssembly), "Runtime Assembly Definition"));
            // Editor assembly
            _editors.Add((Editor.CreateEditor(_editorAssembly), "Editor Assembly Definition"));
        }
        #endregion

        #region Drawing
        private void OnGUI()
        {
            if (_editors == null || _editors.Count == 0 || _editors.Any(x => x.editor == null))
            {
                Startup();
            }

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width)))
            {
                foreach (var item in _editors)
                {
                    EditorGUILayout.Space();
                    // Draw a nice little label for the editor c:
                    GUILayout.Label(item.label, EditorStyles.boldLabel);
                    item.editor.OnInspectorGUI();
                }

                EditorGUILayout.Space();

                GUILayout.Label("Note: The Editor Assembly Definition will reference the Runtime Assembly via name. " +
                    "It is recommended that you select the \"Use GUIDs\" toggle if you intend to reference " +
                    "other assemblies outside the package. (This will automatically convert any existing references to use GUIDs)",
                    EditorStyles.wordWrappedLabel);

                if (GUILayout.Button("Create"))
                {
                    CreatePackage();
                }
            }
        }
        #endregion

        #region Package Generation
        void CreatePackage()
        {
            if (string.IsNullOrEmpty(_packageInfo.name) || string.IsNullOrEmpty(_packageInfo.version) ||
                string.IsNullOrEmpty(_packageInfo.author.name))
            {
                Debug.LogError("Required fields (marked in red) cannot be empty.");
                return;
            }

            var rootPath = Path.Combine(Application.dataPath, "../", "Packages", _packageInfo.name);
            if (Directory.Exists(rootPath))
            {
                Debug.LogError("Package already exists in Packages directory");
                return;
            }

            // Format the package name
            _packageInfo.name = $"com.{_packageInfo.author.name.Replace(" ", "-")}.{_packageInfo.name}".ToLower();

            // Create directories
            Directory.CreateDirectory(rootPath);
            Directory.CreateDirectory(Path.Combine(rootPath, "Documentation~"));
            Directory.CreateDirectory(Path.Combine(rootPath, "Samples~"));
            Directory.CreateDirectory(Path.Combine(rootPath, "Editor"));
            Directory.CreateDirectory(Path.Combine(rootPath, "Runtime"));

            // Create empty markdown files
            File.Create(Path.Combine(rootPath, "Documentation~", $"{_packageInfo.name}.md"));
            File.Create(Path.Combine(rootPath, "CHANGELOG.md"));
            File.WriteAllText(Path.Combine(rootPath, "README.md"), $"# {_packageInfo.name}");

            // Create the package.json file from the package information
            File.WriteAllText(Path.Combine(rootPath, "package.json"), JsonUtility.ToJson(_packageInfo, true));

            if (!string.IsNullOrEmpty(_runtimeAssembly.name))
            {
                // Create runtime assembly
                File.WriteAllText(Path.Combine(rootPath, "Runtime", $"{_runtimeAssembly.name}.asmdef"), JsonUtility.ToJson(_runtimeAssembly, true));

                // Reference the runtime assembly in the editor. This will store as name and the user can opt to change it to use GUIDs once the package is generated.
                _editorAssembly.references = new[] { _runtimeAssembly.name };
            }

            if (!string.IsNullOrEmpty(_editorAssembly.name))
            {
                // Create runtime assembly
                File.WriteAllText(Path.Combine(rootPath, "Editor", $"{_editorAssembly.name}.asmdef"), JsonUtility.ToJson(_editorAssembly, true));
            }

            Debug.Log($"Successfully created new package: {_packageInfo.name}");

            _packageInfo = null;
            _runtimeAssembly = null;
            _editorAssembly = null;
            _editors = null;

            //NOTE: I intentially don't generate a .gitignore since I can't think of anything that needs to be ignored by default.

            // Tell the package manager to resolve packages
            Client.Resolve();
            // Package creation complete, close the window
            Close();
        }
        #endregion
    }
}
