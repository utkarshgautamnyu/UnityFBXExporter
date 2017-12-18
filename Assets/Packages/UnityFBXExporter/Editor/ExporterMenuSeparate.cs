using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

namespace UnityFBXExporter
{
    public class ExporterMenuSeparate : Editor
    {
        // Global Variables
        public static Dictionary<GameObject, GameObject> childParentMapping = new Dictionary<GameObject, GameObject>();

        // Dropdown
        [MenuItem("GameObject/HiFi FBX Exporter1/Only GameObject", false, 40)]
        public static void ExportDropdownGameObjectToFBX()
        {
            ExportCurrentGameObject(false, false);
        }

        [MenuItem("GameObject/HiFi FBX Exporter1/With new Materials", false, 41)]
        public static void ExportDropdownGameObjectAndMaterialsToFBX()
        {
            ExportCurrentGameObject(true, false);
        }

        [MenuItem("GameObject/HiFi FBX Exporter1/With new Materials and Textures", false, 42)]
        public static void ExportDropdownGameObjectAndMaterialsTexturesToFBX()
        {
            ExportCurrentGameObject(true, true);
        }

        // Assets
        [MenuItem("Assets/HiFi FBX Exporter1/Only GameObject", false, 30)]
        public static void ExportGameObjectToFBX()
        {
            ExportCurrentGameObject(false, false);
        }

        [MenuItem("Assets/HiFi FBX Exporter1/With new Materials", false, 31)]
        public static void ExportGameObjectAndMaterialsToFBX()
        {
            ExportCurrentGameObject(true, false);
        }

        [MenuItem("Assets/HiFi FBX Exporter1/With new Materials and Textures", false, 32)]
        public static void ExportGameObjectAndMaterialsTexturesToFBX()
        {
            ExportCurrentGameObject(true, true);
        }
        
        private static string convertIDToString (int ID) {
            return ID.ToString("{00000000-0000-0000-0000-000000000000}");
        }

        private static void UnparentChildRecursive(GameObject obj) {
            if (null == obj) {
                return;
            } else {
                var transform = obj.GetComponentsInChildren<Transform>();
                var parent = obj.transform;
                foreach (Transform child in transform) {
                    if (child == parent) {
                        continue;
                    }

                    if (null == child) {
                        continue;
                    }

                    if (!childParentMapping.ContainsKey(child.gameObject)) {
                        var parentObj = child.parent ? child.parent.gameObject : null;
                        childParentMapping.Add(child.gameObject, parentObj);
                    }
                    child.parent = null;
                    UnparentChildRecursive(child.gameObject);
                }
            }
        }

        private static void ExportCurrentGameObject(bool copyMaterials, bool copyTextures)
        {
            List<GameObject> currentGameObjects = new List<GameObject>();
            List<GameObject> separatedGameObjects = new List<GameObject>();
            GameObject currentGameObject;

            // Export the entire scene if no object selected
            if (Selection.activeGameObject == null)
            {
                if (EditorUtility.DisplayDialog("Export Scene", "No Game Object Selected. Do you want to export the entire scene?", "OK", "Cancel"))
                {
                    var gameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    foreach (var obj in gameObjects)
                    {
                        if (obj.activeInHierarchy)
                        {
                            currentGameObjects.Add(obj);
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                foreach (var obj in Selection.objects)
                {
                    currentGameObject = obj as GameObject;
                    if (currentGameObject == null)
                    {
                        EditorUtility.DisplayDialog("Warning", "Item selected is not a GameObject", "Okay");
                        return;
                    }
                    else
                    {
                        currentGameObjects.Add(currentGameObject);
                    }
                }
            }

            foreach (var obj in currentGameObjects) {
                UnparentChildRecursive(obj);
                if (!childParentMapping.ContainsKey(obj)) {
                    var parentObj = obj.transform.parent ? obj.transform.parent.gameObject : null;
                    childParentMapping.Add(obj, parentObj);
                }
            }

            foreach (var key in childParentMapping.Keys) {
                separatedGameObjects.Add(key);
            }

            string path = ExportGameObject(separatedGameObjects, copyMaterials, copyTextures);
            if (path == null) {
                return;
            }

            ExportGameObjectAsJson(separatedGameObjects, path);

            EditorUtility.DisplayDialog("Success", "Success " + separatedGameObjects.Count + " game objects exported", "Okay");

            // Re-Parent all object
            foreach(var key in childParentMapping.Keys) {
                var parent = childParentMapping[key] ? childParentMapping[key].transform : null;
                key.transform.parent = parent;
            }

            childParentMapping.Clear();
        }

        /// <summary>
        /// Exports ANY Game Object given to it. Will provide a dialog and return the path of the newly exported file
        /// </summary>
        /// <returns>The path of the newly exported FBX file</returns>
        /// <param name="gameObj">Game object to be exported</param>
        /// <param name="copyMaterials">If set to <c>true</c> copy materials.</param>
        /// <param name="copyTextures">If set to <c>true</c> copy textures.</param>
        /// <param name="oldPath">Old path.</param>
        public static string ExportGameObject(List<GameObject> gameObjects, bool copyMaterials, bool copyTextures, string oldPath = null)
        {
            foreach (var gameObj in gameObjects)
            {
                if (gameObj == null)
                {
                    EditorUtility.DisplayDialog("Object is null", "Please select any GameObject to Export to FBX", "Okay");
                    return null;
                }
            }
            // Get folder path
            string newPath = GetNewPath(oldPath);
            if (newPath == null)
            {
                return null;
            }
            foreach (var gameObject in gameObjects)
            {
                var fileName = newPath + "/" + gameObject.name + ".fbx";
                if (fileName != null && fileName.Length != 0)
                {
                    bool isSuccess = FBXExporter.ExportGameObjToFBX(gameObject, fileName, copyMaterials, copyTextures);
                    if (!isSuccess)
                    {
                        EditorUtility.DisplayDialog("Warning", "The extension probably wasn't an FBX file, could not export.", "Okay");
                    }
                }
            }
            return newPath;
        }

        /// <summary>
        /// Creates save dialog window depending on old path or right to the /Assets folder no old path is given
        /// </summary>
        /// <returns>The new path.</returns>
        /// <param name="gameObject">Item to be exported</param>
        /// <param name="oldPath">The old path that this object was original at.</param>
        private static string GetNewPath(string oldPath = null)
        {
            // NOTE: This must return a path with the starting "Assets/" or else textures won't copy right
            string newPath = null;

            if (oldPath == null)
            {
                newPath = EditorUtility.SaveFolderPanel("Select Folder to Export FBX", "/Assets", "");
                if (!newPath.Contains("/Assets"))
                {
                    EditorUtility.DisplayDialog("Warning", "Must save file in the project's assets folder", "Okay");
                    return null;
                }
            }
            else
            {
                if (oldPath.StartsWith("/Assets"))
                {
                    oldPath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/Assets"), 7) + oldPath;
                    oldPath = oldPath.Remove(oldPath.LastIndexOf('/'), oldPath.Length - oldPath.LastIndexOf('/'));
                }
                newPath = EditorUtility.SaveFolderPanel("Select Folder to Export FBX", oldPath, "");
            }

            int assetsIndex = newPath.IndexOf("Assets");

            if (assetsIndex < 0)
                return null;

            if (assetsIndex > 0)
                newPath = newPath.Remove(0, assetsIndex);

            return newPath;
        }

        public static void ExportGameObjectAsJson(List<GameObject> gameObjects, string path)
        {
            Vector3 nullVector = new Vector3(0, 0, 0);
            Vector3 unitVector = new Vector3(1, 1, 1);
            Dictionary<GameObject, int> objectToIDMapping = new Dictionary<GameObject, int>();
            int objectID = 2;
            foreach(var key in childParentMapping.Keys) {
                if (!objectToIDMapping.ContainsKey(key)) {
                    objectToIDMapping.Add(key, objectID++);
                }
            }
            string filePath = EditorUtility.SaveFilePanelInProject("Select JSON Filename", "gameObjects.json", "json", "Export GameObjects to a JSON file");

            StringBuilder jsonOutput = new StringBuilder("{\"Entities\":[");

            for (int i = 0; i < gameObjects.Count; i++)
            {
                SerializeJSON jsonObject = new SerializeJSON();

                // Setting position
                jsonObject.position = gameObjects[i].transform.position;
                jsonObject.position.x *= -1;

                // Setting registration Point
                if (gameObjects[i].GetComponent<MeshFilter>())
                {
                    Mesh mesh = gameObjects[i].GetComponent<MeshFilter>().mesh;
                    Vector3 minBound = mesh.bounds.min;
                    Vector3 boundSize = mesh.bounds.size;
                    jsonObject.registrationPoint.x = (boundSize.x == 0) ? 0 : (minBound.x * -1) / boundSize.x;
                    jsonObject.registrationPoint.y = (boundSize.y == 0) ? 0 : (minBound.y * -1) / boundSize.y;
                    jsonObject.registrationPoint.z = (boundSize.z == 0) ? 0 : (minBound.z * -1) / boundSize.z;
                }

                // Setting Dimensions

                Bounds bounds = new Bounds();
                
                if (gameObjects[i].GetComponent<MeshFilter>()) {
                    bounds = gameObjects[i].GetComponent<MeshFilter>().mesh.bounds;
                }
                
                jsonObject.dimensions = bounds.size;
                jsonObject.dimensions = Vector3.Scale(jsonObject.dimensions, gameObjects[i].transform.localScale);

                if (jsonObject.dimensions == nullVector)
                {
                    jsonObject.dimensions = unitVector;
                }

                // Setting type of model
                
                if (gameObjects[i].GetComponent<Light>()) {
                    jsonObject.type = "Light";
                } else if (gameObjects[i].GetComponent<Camera>()) {
                    jsonObject.type = "";
                } else {
                    jsonObject.type = "Model";
                    jsonObject.shapeType = "compound";
                }

                // Setting Object ID and Parent ID
                jsonObject.id = convertIDToString(objectToIDMapping[gameObjects[i]]);

                var parent = childParentMapping[gameObjects[i]];
                if (parent && objectToIDMapping.ContainsKey(parent)) {
                    jsonObject.parentID = convertIDToString(objectToIDMapping[parent]);
                } else {
                    jsonObject.parentID = convertIDToString(0);
                }



                string directory = Application.dataPath.Replace("Assets", "");
                jsonObject.modelURL = "file:///" + directory + path + "/" + gameObjects[i].name + ".fbx";

                string jsonString = JsonUtility.ToJson(jsonObject);
                jsonOutput.Append(jsonString);
                if (i != gameObjects.Count - 1)
                {
                    jsonOutput.Append(",");
                }
            }
            jsonOutput.Append("]}");
            System.IO.File.WriteAllText(filePath, jsonOutput.ToString());
        }
    }

}
