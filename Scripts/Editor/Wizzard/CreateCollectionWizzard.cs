using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CreateCollectionWizzard : EditorWindow
    {
        private const string WAITING_SCRIPTS_TO_RECOMPILE_TO_CONTINUE_KEY = "WaitingScriptsToRecompileToContinueKey";
        private const string LAST_COLLECTION_SCRIPTABLE_OBJECT_PATH_KEY = "CollectionScriptableObjectPathKey";
        private const string LAST_COLLECTION_FULL_NAME_KEY = "CollectionFullNameKey";
        private const string LAST_GENERATED_COLLECTION_SCRIPT_PATH_KEY = "CollectionScriptPathKey";
        private const string LAST_TARGET_SCRIPTS_FOLDER_KEY = "LastTargetScriptsFolder";
        
        private static CreateCollectionWizzard windowInstance;
        private static string targetFolder;


        private DefaultAsset cachedScriptableObjectFolder;
        private DefaultAsset ScriptableObjectFolder
        {
            get
            {
                if (cachedScriptableObjectFolder != null) 
                    return cachedScriptableObjectFolder;

                if (!string.IsNullOrEmpty(targetFolder))
                {
                    cachedScriptableObjectFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(targetFolder);
                    return cachedScriptableObjectFolder;
                }

                if (!string.IsNullOrEmpty(LastCollectionScriptableObjectPath))
                {
                    cachedScriptableObjectFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(LastCollectionScriptableObjectPath);
                    return cachedScriptableObjectFolder;
                }
                
                return cachedScriptableObjectFolder;
            }
            set => cachedScriptableObjectFolder = value;
        }

        private DefaultAsset cachedScriptsFolder;
        private DefaultAsset ScriptsFolder
        {
            get
            {
                if (cachedScriptsFolder != null) 
                    return cachedScriptsFolder;
                
                if (!string.IsNullOrEmpty(LastScriptsTargetFolder))
                {
                    cachedScriptsFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Path.GetDirectoryName(LastScriptsTargetFolder));
                    return cachedScriptsFolder;
                }
                
                if (!string.IsNullOrEmpty(targetFolder))
                {
                    cachedScriptsFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(targetFolder);
                    return cachedScriptsFolder;
                }
                
                return cachedScriptsFolder;
            }
            set => cachedScriptsFolder = value;
        }

        private string cachedNameSpace;
        private string TargetNameSpace
        {
            get
            {
                if (string.IsNullOrEmpty(cachedNameSpace))
                    cachedNameSpace = ScriptableObjectCollectionSettings.GetInstance().DefaultNamespace;
                return cachedNameSpace;
            }
            set
            {
                cachedNameSpace = value;
                if (string.IsNullOrEmpty(ScriptableObjectCollectionSettings.GetInstance().DefaultNamespace))
                    ScriptableObjectCollectionSettings.GetInstance().SetDefaultNamespace(cachedNameSpace);
            }
        }


        private static bool WaitingRecompileForContinue
        {
            get => EditorPrefs.GetBool(WAITING_SCRIPTS_TO_RECOMPILE_TO_CONTINUE_KEY, false);
            set => EditorPrefs.SetBool(WAITING_SCRIPTS_TO_RECOMPILE_TO_CONTINUE_KEY, value);
        }
        
        private static string LastCollectionScriptableObjectPath
        {
            get => EditorPrefs.GetString(LAST_COLLECTION_SCRIPTABLE_OBJECT_PATH_KEY, String.Empty);
            set => EditorPrefs.SetString(LAST_COLLECTION_SCRIPTABLE_OBJECT_PATH_KEY, value);
        }

        private static string LastCollectionFullName
        {
            get => EditorPrefs.GetString(LAST_COLLECTION_FULL_NAME_KEY, String.Empty);
            set => EditorPrefs.SetString(LAST_COLLECTION_FULL_NAME_KEY, value);
        }

        private static string LastScriptsTargetFolder
        {
            get => EditorPrefs.GetString(LAST_TARGET_SCRIPTS_FOLDER_KEY, String.Empty);
            set => EditorPrefs.SetString(LAST_TARGET_SCRIPTS_FOLDER_KEY, value);
        }
        
        private static string LastGeneratedCollectionScriptPath
        {
            get => EditorPrefs.GetString(LAST_GENERATED_COLLECTION_SCRIPT_PATH_KEY, String.Empty);
            set => EditorPrefs.SetString(LAST_GENERATED_COLLECTION_SCRIPT_PATH_KEY, value);
        }

        private bool createFoldForThisCollection = true;
        private bool createFoldForThisCollectionScripts = true;

        private string collectionName = "Collection";
        private string collectionItemName = "Item";
        private bool generateIndirectAccess = true;
        

        private static CreateCollectionWizzard GetWindowInstance()
        {
            if (windowInstance == null)
            {
                windowInstance =  CreateInstance<CreateCollectionWizzard>();
                windowInstance.titleContent = new GUIContent("Create New Collection");
            }

            return windowInstance;
        }

        private void OnEnable()
        {
            windowInstance = this;
        }

        public static void Show(string targetPath)
        {
            targetFolder = targetPath;
            GetWindowInstance().ShowUtility();
        }

        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        EditorGUILayout.LabelField("Settings", EditorStyles.foldoutHeader);
                        EditorGUILayout.Space();

                        collectionItemName = EditorGUILayout.TextField("Item Name", collectionItemName);
                        collectionName = EditorGUILayout.TextField("Collection Name", collectionName);

                        generateIndirectAccess = EditorGUILayout.Toggle("Generate Indirect Access", generateIndirectAccess);
                    }
                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        EditorGUILayout.LabelField("Scriptable Object", EditorStyles.foldoutHeader);
                        EditorGUILayout.Space();

                        ScriptableObjectFolder = (DefaultAsset) EditorGUILayout.ObjectField("Scriptable Object Folder",
                            ScriptableObjectFolder, typeof(DefaultAsset),
                            false);
                        if (ScriptableObjectFolder != null)
                            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(ScriptableObjectFolder));
                        createFoldForThisCollection =
                            EditorGUILayout.ToggleLeft($"Create parent {collectionName} folder",
                                createFoldForThisCollection);
                    }

                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        EditorGUILayout.LabelField("Script", EditorStyles.foldoutHeader);
                        EditorGUILayout.Space();

                        ScriptsFolder = (DefaultAsset) EditorGUILayout.ObjectField("Script Folder", ScriptsFolder,
                            typeof(DefaultAsset),
                            false);
                        if (ScriptsFolder != null)
                        {
                            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(ScriptsFolder));
                        }

                        createFoldForThisCollectionScripts =
                            EditorGUILayout.ToggleLeft($"Create parent {collectionName} folder",
                                createFoldForThisCollectionScripts);

                        TargetNameSpace = EditorGUILayout.TextField("Namespace", TargetNameSpace);
                    }

                    using (new EditorGUI.DisabledScope(!AreSettingsValid()))
                    {
                        Color color = GUI.color;
                        GUI.color = Color.green;
                        if (GUILayout.Button("Create"))
                            CreateNewCollection();

                        GUI.color = color;
                    }
                }
            }
        }

        private void CreateNewCollection()
        {
            bool scriptsGenerated = false;
            scriptsGenerated |= CreateCollectionItemScript();
            scriptsGenerated |= CreateCollectionScript();

            if (generateIndirectAccess)
                CreateIndirectAccess();
                
            WaitingRecompileForContinue = true;
            
            AssetDatabase.Refresh();

            if (!scriptsGenerated)
                OnAfterScriptsReloading();
        }

        private void CreateIndirectAccess()
        {
            string folderPath = AssetDatabase.GetAssetPath(ScriptsFolder);
            if (createFoldForThisCollectionScripts)
                folderPath = Path.Combine(folderPath, $"{collectionName}");

            string fileName = $"{collectionItemName}IndirectReference";

            AssetDatabaseUtils.CreatePathIfDontExist(folderPath);
            using (StreamWriter writer = new StreamWriter(Path.Combine(folderPath, $"{fileName}.cs")))
            {
                int indentation = 0;
                List<string> directives = new List<string>();
                directives.Add(typeof(ScriptableObjectCollectionItem).Namespace);
                directives.Add(TargetNameSpace);
                directives.Add("System");
                directives.Add("UnityEngine");

                CodeGenerationUtility.AppendHeader(writer, ref indentation, TargetNameSpace, "[Serializable]",
                    $"public sealed class {collectionItemName}IndirectReference : CollectionItemIndirectReference<{collectionItemName}>",
                    directives.Distinct().ToArray());

                CodeGenerationUtility.AppendLine(writer, indentation,
                    $"public {collectionItemName}IndirectReference() {{}}");
                
                CodeGenerationUtility.AppendLine(writer, indentation,
                    $"public {collectionItemName}IndirectReference({collectionItemName} collectionItemScriptableObject) : base(collectionItemScriptableObject) {{}}");

                indentation--;
                CodeGenerationUtility.AppendFooter(writer, ref indentation, TargetNameSpace);
            }
        }

        private bool CreateCollectionItemScript()
        {
            string folder = AssetDatabase.GetAssetPath(ScriptsFolder);
            LastScriptsTargetFolder = folder;
            if (createFoldForThisCollectionScripts)
                folder = Path.Combine(folder, $"{collectionName}");
            
            return CodeGenerationUtility.CreateNewEmptyScript(collectionItemName, 
                folder,
                TargetNameSpace, 
                string.Empty,
                $"public partial class {collectionItemName} : ScriptableObjectCollectionItem", 
                    typeof(ScriptableObjectCollectionItem).Namespace);
        }
        
        private bool CreateCollectionScript()
        {
            string folder = AssetDatabase.GetAssetPath(ScriptsFolder);
            if (createFoldForThisCollectionScripts)
                folder = Path.Combine(folder, $"{collectionName}");

            bool result = CodeGenerationUtility.CreateNewEmptyScript(collectionName,
                folder,
                TargetNameSpace,
                $"[CreateAssetMenu(menuName = \"ScriptableObject Collection/Collections/Create {collectionName}\", fileName = \"{collectionName}\", order = 0)]",
                $"public class {collectionName} : ScriptableObjectCollection<{collectionItemName}>", typeof(ScriptableObjectCollection).Namespace, "UnityEngine");

            if (string.IsNullOrEmpty(TargetNameSpace))
                LastCollectionFullName = $"{collectionName}";
            else
                LastCollectionFullName = $"{TargetNameSpace}.{collectionName}";

            LastGeneratedCollectionScriptPath = Path.Combine(folder, $"{collectionName}.cs");
            return result;
        }

        private bool AreSettingsValid()
        {
            if (string.IsNullOrEmpty(collectionItemName))
                return false;

            if (string.IsNullOrEmpty(collectionName))
                return false;

            if (ScriptsFolder == null)
                return false;

            if (ScriptableObjectFolder == null)
                return false;

            return true;
        }

        [DidReloadScripts]
        static void OnAfterScriptsReloading()
        {
            if (!WaitingRecompileForContinue)
                return;

            WaitingRecompileForContinue = false;

            string assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(LastGeneratedCollectionScriptPath);

            Type targetType = Type.GetType($"{LastCollectionFullName}, {assemblyName}");
            
            ScriptableObjectCollection collectionAsset =
                ScriptableObjectCollectionUtils.CreateScriptableObjectOfType(targetType, 
                    windowInstance.ScriptableObjectFolder, windowInstance.createFoldForThisCollection,
                    windowInstance.collectionName) as ScriptableObjectCollection;
            
            Selection.objects = new Object[] {collectionAsset};
            EditorGUIUtility.PingObject(collectionAsset);

            CreateCollectionWizzard openWindowInstance = GetWindow<CreateCollectionWizzard>();
            openWindowInstance.Close();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
