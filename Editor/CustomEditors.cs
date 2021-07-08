using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dreemar.PackageTool
{
    [CustomEditor(typeof(AssemblyDefinition))]
    public class NoScriptEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(PackageInfo))]
    public class PackageInfoEditor : Editor
    {
        SerializedProperty _name;
        SerializedProperty _version;

        SerializedProperty _author;
        SerializedProperty _authorName;
        SerializedProperty _authorEmail;
        SerializedProperty _authorUrl;

        private void Awake()
        {
            _name = serializedObject.FindProperty("name");
            _version = serializedObject.FindProperty("version");

            _author = serializedObject.FindProperty("author");
            _authorName = _author.FindPropertyRelative("name");
            _authorEmail = _author.FindPropertyRelative("email");
            _authorUrl = _author.FindPropertyRelative("url");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Cache editor label colour
            var labelColourDefault = EditorStyles.label.normal.textColor;
            // The first few elements we draw will be required, so draw them in red
            EditorStyles.label.normal.textColor = Color.red;

            // Draw the package name field
            using (new EditorGUILayout.HorizontalScope())
            {
                _name.stringValue = EditorGUILayout.TextField("Name", _name.stringValue);
                // Replace spaces with hyphens
                _name.stringValue = _name.stringValue.Replace(" ", "-");
            }

            // Draw the version code field
            // Looks like: [   ].[   ].[   ]
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Version ");
                GUILayout.Space(2);

                string[] versionSplit = _version.stringValue.Split('.');

                // Ensure there are only 3 elements in the string
                string[] nVer = new string[3];
                for (int i = 0; i < nVer.Length; i++)
                {
                    if (i < versionSplit.Length)
                    {
                        if (i < 2)
                        {
                            nVer[i] = versionSplit[i];
                            // Ensure each item has a value
                            if (string.IsNullOrEmpty(nVer[i]))
                            {
                                if (i == 0)
                                    nVer[i] = "1";
                                else
                                    nVer[i] = "0";
                            }

                        }
                        else
                        {
                            // Combine the remaining split strings
                            // This may occur if the verion code looks something like
                            // 1.0.0-preview-3.1
                            nVer[i] = string.Join(".", versionSplit, i, versionSplit.Length - i);
                        }
                    }
                    // Fill any remaining with zeroes
                    // There will always be 1 element in versionSplit so this will always be x.[missing].[missing]
                    // And the above code will ensure the first element has a value
                    else
                        nVer[i] = "0";
                }
                versionSplit = nVer;

                // Draw gui for each item
                for (int i = 0; i < versionSplit.Length; i++)
                {
                    versionSplit[i] = EditorGUILayout.TextField(versionSplit[i]);
                    if (i < versionSplit.Length - 1)
                        GUILayout.Label(".");
                }

                // Combine the items with a decimal between each
                _version.stringValue = string.Join(".", versionSplit);
                // Replace spaces with hyphens
                _version.stringValue = _version.stringValue.Replace(" ", "-");
            }

            // Cache this in case the user has a different style set in some custom stuff
            // Who even knows, you do you
            var foldoutColourDefault = EditorStyles.foldout.normal.textColor;

            /* 
             * For whatever reason I can't for the life of me find how to change the colour of the
             * foldout label when it's expanded. Not that big of a deal since we can just highlight the
             * inner elements instead. Whatever.
            */

            // Draw the foldout with a red label (the name field is required)
            EditorStyles.foldout.normal.textColor = Color.red;

            // Draw the foldout for the author details
            _author.isExpanded = EditorGUILayout.Foldout(_author.isExpanded, "Author");

            // Reset to default colour
            EditorStyles.foldout.normal.textColor = foldoutColourDefault;

            if (_author.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_authorName);
                // Other elements are not required, revert to default label colour
                EditorStyles.label.normal.textColor = labelColourDefault;
                EditorGUILayout.PropertyField(_authorEmail);
                EditorGUILayout.PropertyField(_authorUrl);

                EditorGUI.indentLevel--;
            }

            // Other elements are not required, revert to default label colour
            EditorStyles.label.normal.textColor = labelColourDefault;

            // Draw other fields with default editor fields
            DrawPropertiesExcluding(serializedObject, "m_Script", "name", "version", "author");

            // TODO: Create a property drawer for the Unity Version field
            // (to emulate the one shown in the package.json inspector)

            serializedObject.ApplyModifiedProperties();
        }
    }
}
